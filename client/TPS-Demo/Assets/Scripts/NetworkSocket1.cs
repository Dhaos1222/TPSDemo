using System.Net;
using System.Net.Sockets;
using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

[System.Serializable]
public class Message {
    public string text;
    public Text textObject;
}

public class NetworkSocket1 : MonoBehaviour {

    // Static var to hold player location
    static public Transform PLAYER_TRANSFORM;

    // TCP client connection and data buffer
    readonly internal TcpClient client = new TcpClient();
    readonly internal byte[] buffer = new byte[5000];

    // GameFlowManager
    public GameFlowManager gameFlowManager;

    // Pre-fab for player
    public GameObject prefabPlayer;

    // Other player's cars
    public GameObject prefabOther;

    // msg prefab
    public GameObject prefabMsg;

    public GameObject LoginObj;

    public GameObject RegisterObj;

    // player alive state
    public bool isAlive = true;

    //
    public bool isRespawning = false;

    // Connection options
    readonly internal IPAddress address = IPAddress.Parse("127.0.0.1");
    readonly internal int port = 50000;

    // All players
    readonly internal Dictionary<String, GameObject> players = new Dictionary<String, GameObject>();

    // Current player
    internal GameObject player;

    // Create unique player ID on each initialization
    internal String playerId = Guid.NewGuid().ToString("N");

    // Whether client and server are ready
    internal bool isAuthenticated = false;

    // Minimize redudant server requests
    internal String lastCoordinate = "";

    // Service Cmd
    internal String ServiceCmd = "";

    // Convenience getter
    internal NetworkStream Stream 
    {
        get 
        {
            return client.GetStream();
        }
    }

    // Queue for player spawn updates
    internal Queue<PlayerInfo> spawnQueue = new Queue<PlayerInfo>();

    // Queue for player movement updates
    internal Queue<PlayerInfo> updateQueue = new Queue<PlayerInfo>();

    // Queue for player disconnects
    internal Queue<String> removalQueue = new Queue<String>();

    // Message queue
    internal Queue<String> messageQueue = new Queue<String>();

    // Info on a player
    internal struct PlayerInfo 
    {
        public String playerId;
        public Vector3 position;
        public Vector3 rotation;

        public PlayerInfo(String playerId, Vector3 position, Vector3 rotation) 
        {
            this.playerId = playerId;
            this.position = position;
            this.rotation = rotation;
        }
    }

    internal struct PlayerAttributes
    {
        public float hp;
        public float ammo;
        public float exp;
        public PlayerAttributes(float hp, float ammo, float exp)
        {
            this.hp = hp;
            this.ammo = ammo;
            this.exp = exp;
        }
    }

    internal PlayerAttributes playerAttrib;

    // Unity initialization.
    void Awake() 
    {
        client.NoDelay = true;
        client.BeginConnect(address, port, HandleConnect, null);

        // Init
        PLAYER_TRANSFORM = transform;

        Debug.Log("I should have connected to the server...");
    }

    // Unity update is called once per frame.
    void Update() {
        HandleServerCmd();
        if (isAuthenticated) 
        {
            // Send current update
            HandlePlayerUpdate();

            // Spawn queue
            while (spawnQueue.Count > 0) 
            {
                PlayerInfo p = spawnQueue.Dequeue();
                SpawnPlayer(p);
            }

            // Update queue
            while (updateQueue.Count > 0) 
            {
                PlayerInfo p = updateQueue.Dequeue();
                UpdatePlayer(p);
            }

            // Removal queue
            while (removalQueue.Count > 0)
            {
                String p = removalQueue.Dequeue();
                RemovePlayer(p);
            }

        }
    }

    // Prevent all players from spawning on same point
    internal Vector3 RandomizePosition() 
    {
        int x = UnityEngine.Random.Range(20, 50);
        int z = UnityEngine.Random.Range(20, 50);

        Vector3 position = new Vector3();

        position.x = x;
        position.z = z;

        return position;
    }

    // Spawn player in game
    internal void SpawnPlayer(PlayerInfo p) 
    {
        GameObject obj = Instantiate(prefabOther, p.position, transform.rotation);
        players.Add(p.playerId, obj);
    }

    // Update player in game
    internal void UpdatePlayer(PlayerInfo p) 
    {
        players[p.playerId].transform.position = p.position;
        players[p.playerId].transform.eulerAngles = p.rotation;
    }

    // Remove player from map
    internal void RemovePlayer(string playerId)
    {
        if(players.ContainsKey(playerId)) 
        {
            Destroy(players[playerId]);
            players.Remove(playerId);
        }
    }

    // For Auth failed Msg
    internal void ShowAuthFailedMsg()
    {
        GameObject msg_obj = Instantiate(prefabMsg, transform.position, Quaternion.identity);
    }

    // Client is reading data from server
    internal void OnRead(IAsyncResult a) 
    {
        int length = Stream.EndRead(a);
        if(length == 0) 
        {
            Debug.Log("No length!");
            return;
        }

        string msg = System.Text.Encoding.UTF8.GetString(buffer, 0, length);

        // Split messages
        string[] messages = msg.Split(';');

        // Handle each server message
        foreach (string message in messages) 
        {
            string[] arr = message.Split(',');
            Debug.Log("cmd:" + arr[0]);

            // Server is requesting authentication
            if(arr[0].Equals("auth-request")) 
            {
                // SendAuthIdentity();
                continue;
            }
            
            // Register
            if(arr[0].Equals("register-success"))
            {
                ServiceCmd = arr[0];
                continue;
            }

            // Client auth failed
            if(arr[0].Equals("auth-failed"))
            {
                ServiceCmd = arr[0];
                continue;
            }

            // Client is successfully authenticated
            if(arr[0].Equals("auth-success")) 
            {
                ServiceCmd = arr[0];
                if (!arr[1].Equals(playerId)) 
                {
                    Debug.LogWarning("Player ID created by client was changed by server.");
                }

                double x = Double.Parse(arr[2]);
                double y = Double.Parse(arr[3]);
                double z = Double.Parse(arr[4]);

                float hp = float.Parse(arr[5]);
                float ammo = float.Parse(arr[6]);
                float exp = float.Parse(arr[7]);

                playerAttrib = new PlayerAttributes(hp, ammo, exp);

                Debug.Log("Coordinates are " + x + ", " + y + ", " + z);
                Debug.Log("hp is " + hp + "," + ammo + "," + exp);

                playerId = arr[1];
                isAuthenticated = true;

                continue;
            }

            // Only run further commands if authenticated
            if(isAuthenticated) 
            {
                // Do nothing
                if(arr[0].Equals("update-success")) continue;

                if(arr[0].Equals("player-disconnect")) 
                {
                    string pid = arr[1];
                    removalQueue.Enqueue(pid);
                }

                // update user attrib
                if(arr[0].Equals("attributes-request"))
                {
                    
                    string hp = playerAttrib.hp.ToString();
                    string ammo = playerAttrib.ammo.ToString();
                    string exp = playerAttrib.exp.ToString();
                    SendMessage("attributes-update," + hp + "," + ammo + "," + exp);
                }

                // Handle player update
                if(arr[0].Equals("player-update")) 
                {
                    // Update player
                    string pid = arr[1];

                    // Skip self update (may introduce lag)
                    if (pid.Equals(playerId)) continue;

                    // Position coords
                    float px = float.Parse(arr[2]);
                    float py = float.Parse(arr[3]);
                    float pz = float.Parse(arr[4]);

                    // Rotation coords
                    float rx = float.Parse(arr[5]);
                    float ry = float.Parse(arr[6]);
                    float rz = float.Parse(arr[7]);

                    Vector3 location = new Vector3(px, py, pz);
                    Vector3 rotation = new Vector3(rx, ry, rz);

                    // Player information
                    PlayerInfo player = new PlayerInfo(pid, location, rotation);

                    if(!players.ContainsKey(pid)) {
                        Debug.Log("New player!");
                        spawnQueue.Enqueue(player);
                    } else {
                        updateQueue.Enqueue(player);
                    }
                }
            }
        }

        Stream.BeginRead(buffer, 0, buffer.Length, OnRead, null);
    }

    // Client did finish connecting asynchronously.
    void HandleConnect(IAsyncResult a) 
    {
        Stream.BeginRead(buffer, 0, buffer.Length, OnRead, null);
    }

    // Send player ID to server
    public void SendAuthIdentity(string account, string password) 
    {
        // SendMessage(playerId);
        Debug.Log("Login account:" + account + " password:" + password);
        SendMessage("login" + "," + account + "," + password);
    }

    // public void ShutdownServer()
    // {
    //     SendMessage("shutdown");
    // }

    public void CreatAccount(string account, string password)
    {
        Debug.Log("Register account:" + account + " password:" + password);
        SendMessage("register" + "," + account + "," + password + "," + "10" + "," + "30" + "," + "0");
    }

    // Send current coordinates to server
    internal void HandlePlayerUpdate() 
    {
        if(player == null && isAlive) 
        {
            player = Instantiate(prefabPlayer, RandomizePosition(), transform.rotation);
            Attributes attributes = player.GetComponent<Attributes>();
            attributes.setAttributes(playerAttrib.hp, playerAttrib.ammo, playerAttrib.exp);
            gameFlowManager.enabled = true;
        }
        else if(!isAlive)
        {
            return;
        }
        else if(isAlive && isRespawning)
        {
            player.SetActive(true);
            player.transform.position = RandomizePosition();
            Attributes attributes = player.GetComponent<Attributes>();
            attributes.fullstate();
            PlayerCharacterController playerCharacterController = player.GetComponent<PlayerCharacterController>();
            playerCharacterController.isDead = false;
            PlayerWeaponsManager playerWeaponsManager = player.GetComponent<PlayerWeaponsManager>();
            playerWeaponsManager.SwitchToWeaponIndex(0,true);
            Health health = player.GetComponent<Health>();
            health.IsDead = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            isRespawning = false;
        }
        else 
        {
            PLAYER_TRANSFORM = player.transform;
            Attributes attributes = player.GetComponent<Attributes>();
            playerAttrib.hp = attributes.getHp();
            playerAttrib.ammo = attributes.getAmmo();
            playerAttrib.exp = attributes.getExp();
        }

        string position = PLAYER_TRANSFORM.position.x + "," + PLAYER_TRANSFORM.position.y + "," + PLAYER_TRANSFORM.position.z;

        string rotation = PLAYER_TRANSFORM.eulerAngles.x + "," + PLAYER_TRANSFORM.eulerAngles.y + "," + PLAYER_TRANSFORM.eulerAngles.z;

        if(!position.Equals(lastCoordinate)) 
        {
            SendMessage("position," + position + ",rotation," + rotation + ";");
        }


        lastCoordinate = position;
    }

    // Handle Server Cmd
    internal void HandleServerCmd()
    {
        // Register
        if(ServiceCmd.Equals("register-success"))
        {
            GameObject msg_obj = Instantiate(prefabMsg, transform.position, Quaternion.identity);
            Text msg_text = msg_obj.GetComponentInChildren<Text>();
            msg_text.text = "register success";
            LoginObj.SetActive(true);
            RegisterObj.SetActive(false);
        }

                // Client auth failed
        if(ServiceCmd.Equals("auth-failed"))
        {
            ShowAuthFailedMsg();
        }

        if(ServiceCmd.Equals("auth-success")) 
        {
            LoginObj.SetActive(false);
        }

        ServiceCmd = "";
    }

    // Send a message to the server.
    internal void SendMessage(string message) 
    {
        byte[] b = System.Text.Encoding.UTF8.GetBytes(message);
        Stream.Write(b, 0, b.Length);
    }

    // Called when Unity application is closed, make sure to close connections.
    private void OnApplicationQuit() 
    {
        Debug.Log("I am quitting...");
        client.Close();
    }

}