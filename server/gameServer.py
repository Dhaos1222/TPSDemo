import socket
import sys
import time
from threading import Thread, Timer
from utilities import safe_string
from sqliteDB import sqliteDB


class GameServer:
    socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

    db = None

    # Store list of connections
    connections = []

    # Store disconnecting players
    disconnections = []

    # Chat messages
    chat = []

    players = dict()

    # is_working = True

    def __init__(self, host, port):
        self.host = host
        self.port = port
        

    def run(self):
        self.db = sqliteDB("TPSDemo.db")
        self.db.start_up()
        self.db.initDB()
        # Bind socket to local host and port
        try:
            self.socket.bind((self.host, self.port))
            self.socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        except socket.error as err:
            print("Error binding to server: {}".format(err))
            sys.exit()

        # Start listening on socket
        self.socket.listen(10)

        if not self.host:
            self.host = "localhost"

        print("Game server is running at {}:{}".format(self.host, self.port))

        # Run server
        try:
            thread = Thread(target=self.broadcasting_loop())
            thread.daemon = True
            thread.start()

            while True:
                conn, address = self.socket.accept()
                thread = Thread(target=self.handler, args=(conn, address))
                thread.daemon = True
                thread.start()
                self.connections.append(conn)
            
            thread.close()


        except KeyboardInterrupt:
            self.shutdown()
            # self.socket.close()

        finally:
            self.shutdown()
            # self.socket.close()


    # Broadcast player movements to all clients
    def broadcasting_loop(self):
        Timer(1, self.broadcasting_loop).start()

        if self.players:
            data = ''
            
            # update player attributes
            data += "attributes-request;"

            for connection in self.connections:
                connection.sendall(str.encode(data))

            data = ''
            
            # Send player updates
            for player in self.players:
                data += "player-update,{},{};".format(player, self.players[player]['location'])

            for connection in self.connections:
                connection.sendall(str.encode(data))

        # Send disconnections
        if self.disconnections:
            data = ''

            for player_id in self.disconnections:
                if player_id:
                    print("* Player " + player_id + " disconnected from server")
                    data += "player-disconnect,{};".format(player_id)
                    self.disconnections.remove(player_id)

            for connection in self.connections:
                connection.sendall(str.encode(data))


    def handler(self, conn, a):
        print("* {}:{} connected...".format(a[0], a[1]))

        # Send a message asking client to identify client UUID
        conn.send(str.encode('auth-request'))

        # conn.send("auth-request")
        player_id = None
        while True:
            try:
                data = conn.recv(1024)
                message = data.decode('UTF-8')
                message = message.replace('\n', '')

                if not data:
                    print("* {}:{} disconnected...".format(a[0], a[1]))

                    save = self.quitAndSave(player_id)
                    if save:
                        print("user:%s data save successfully" % player_id) 
                    # Remove from connections
                    self.connections.remove(conn)

                    # Remove from players
                    self.players.pop(player_id, None)

                    self.disconnections.append(player_id)

                    # Prepare to close connection
                    conn.shutdown(socket.SHUT_RDWR)
                    break

                if len(message) <= 1:
                    continue

                ret = self.cmdHandler(player_id, conn, message)
                if ret:
                    player_id = ret

            except socket.error as e:
                print("Error! {}".format(e))
                break
        conn.close()
    
    def cmdHandler(self, player_id, conn, msg):

        messages = msg.split(';')

        for msg in messages:
            # print(msg)
            arr = msg.split(',')
            
            # shutdown
            if arr[0] == 'shutdown':
                self.is_working = False
            # Handle user register
            if arr[0] == 'register':
                para = {}
                para['account'] = arr[1]
                para['password'] = arr[2]
                para['hp'] = arr[3]
                para['ammo'] = arr[4]
                para['exp'] = arr[5]
                ret = self.db.createUser(para)
                if ret:
                    conn.sendall(str.encode("register-success"))
                else:
                    conn.sendall(str.encode("register-failed"))

            # Handle user login
            if arr[0] == 'login':
                para = {}
                para['account'] = arr[1]
                para['password'] = arr[2]
                auth = self.db.authUser(para)
                print("auth state: %s" % auth)
                if not auth:
                    conn.sendall(str.encode("auth-failed"))
                    continue
            
                player_id = arr[1]
                # self.player_id = arr[1]
                if len(player_id) < 1:
                    continue

                if player_id not in self.players:
                    # Set coordinate
                    coordinate = "0.0,0.0,0.0"
                    hp = self.db.queryByUser(player_id, 'hp')
                    ammo = self.db.queryByUser(player_id, 'ammo')
                    exp = self.db.queryByUser(player_id, 'exp')
                    self.players[player_id] = {}
                    self.players[player_id]['location'] = coordinate
                    conn.sendall(str.encode("auth-success,{},{},{},{},{}".format(player_id, coordinate, hp, ammo, exp)))

                # Send all locations of current players
                else:
                    coordinate = self.players.get(player_id, {}).get('location') or "0.0,0.0,0.0"
                    hp = self.db.queryByUser(player_id, 'hp')
                    ammo = self.db.queryByUser(player_id, 'ammo')
                    exp = self.db.queryByUser(player_id, 'exp')
                    conn.sendall(str.encode("auth-success,{},{},{},{},{}".format(player_id, coordinate, hp, ammo, exp)))

            # exist player_id for handling other cmd
            if not player_id:
                return

            # Handle position update
            if arr[0] == 'position':
                # Position
                rx = float(arr[1])
                ry = float(arr[2])
                rz = float(arr[3])

                # Rotation
                px = float(arr[5])
                py = float(arr[6])
                pz = float(arr[7])

                self.players[player_id]['location'] = '{},{},{},{},{},{};'.format(rx, ry, rz, px, py, pz)
                conn.sendall(str.encode("update-success"))

            # Handle attrib update
            if arr[0] == 'attributes-update':
                hp = arr[1]
                ammo = arr[2]
                exp = arr[3]
                self.players[player_id]['hp'] = hp
                self.players[player_id]['ammo'] = ammo
                self.players[player_id]['exp'] = exp


            return player_id
    
    def shutdown(self):
        self.db.shutdown()
        for connection in self.connections:
                connection.close()
        self.socket.close()
        print("server shutdown successfully")

    def quitAndSave(self, player_id):
        cur_player = self.players[player_id]
        para = {}
        para['account'] = player_id
        para['hp'] = cur_player['hp']
        para['ammo'] = cur_player['ammo']
        para['exp'] = cur_player['exp']
        return self.db.saveUser(para)
        


def main():
    print("Python version: " + sys.version)
    server = GameServer('', 50000)
    server.run()




main()
