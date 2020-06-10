import sqlite3
import hashlib

class sqliteDB:

    cursor = None
    conn = None

    def __init__(self, db):
        self.db = db


    def start_up(self):
        self.conn = sqlite3.connect(self.db, isolation_level = None, check_same_thread = False)
        print("%s opend database successfully" % self.db)
        self.cursor = self.conn.cursor()

    def initDB(self):
        if self.cursor:
            self.cursor.execute("create table if not exists user(id integer primary key,account varchar(20),password varchar(20),hp double,ammo double,exp double)")
            print("create user table successfully")

    def queryByUser(self, user, key):
        try:
            cursor = self.cursor.execute("select %s from user where account = '%s'" % (key, user))
            for row in cursor:
                ret = row[0]
            return ret
        except:
            print("query failed")

    def authUser(self, para):
        # table_name = "user"
        try:
            cursor = self.cursor.execute("select password from user where account = '%s'" % para['account'])
            for row in cursor:
                password = row[0]
            pwd = hashlib.md5(para['password']).hexdigest()
            if password == pwd:
                return True
            else:
                return False
        except:
            print("query user failed")


    def createUser(self, para):
        # table_name = 'user'
        act = para['account']
        pwd = hashlib.md5(para['password']).hexdigest()
        hp = para['hp']
        ammo = para['ammo']
        exp = para['exp']
        try:
            self.cursor.execute("insert into user (account, password, hp, ammo, exp) values ('%s', '%s', %s, %s, %s)" % (act, pwd, hp, ammo, exp))

            return True
        except:
            print("create user failed")
            return False
    
    def saveUser(self, para):
        hp = para['hp']
        if hp == "0":
            hp = "30"
        ammo = para['ammo']
        exp = para['exp']
        try:
            self.cursor.execute("update user set hp = %s, ammo = %s, exp = %s where account = '%s'" % (hp, ammo, exp, para['account']))
            return True
        except:
            print("save user failed")
            return False


    def shutdown(self):
        self.cursor.close()
        self.conn.close()


def GetTables(db_file = 'TPSDemo.db'):
    try:
        conn = sqlite3.connect(db_file)
        cur = conn.cursor()
        cur.execute("select * from user")
        print(cur.fetchall())
    except sqlite3.Error as e:
            print(e)

GetTables()