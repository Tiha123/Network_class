using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using LitJson;
using System.IO;
using kcp2k;
using System;
using I18N.Common;

public enum Type
{
    Empty = 0,
    Client,
    Server
}

public class Item
{
    public string License;
    public string ServerIP;
    public string Port;

    public Item(string l_index, string IPValue, string port)
    {
        License = l_index;
        ServerIP = IPValue;
        Port = port;
    }
}

public class ServerChecker : MonoBehaviour
{
    public Type type;
    private NetworkManager networkManager;
    private KcpTransport kcp;
    [SerializeField] private string filePath;
    public string ServerIP {get;private set;}
    public string Port {get;private set;}

    void Awake()
    {
        if (filePath == string.Empty)
        {
            filePath = Application.dataPath + "/License";
        }
        if (File.Exists(filePath) == false)
        {
            Directory.CreateDirectory(filePath);
        }
        if (File.Exists(filePath + "/License.json")==false)
        {
            DefaultData(filePath);
        }
        networkManager = GetComponent<NetworkManager>();
        kcp = (KcpTransport)networkManager.transport;
    }

    private void DefaultData(string path)
    {
        List<Item> item = new List<Item>();
        item.Add(new Item("0", "127.0.0.1", "7777"));

        JsonData data = JsonMapper.ToJson(item);
        File.WriteAllText(path + "/License.json", data.ToString());
    }

    private Type License_type()
    {
        try
        {
            string jsonString = File.ReadAllText(filePath+"/License.json");
            JsonData itemdata=JsonMapper.ToObject(jsonString);
            string type_s=itemdata[0]["License"].ToString();
            string ip_s=itemdata[0]["ServerIP"].ToString();
            string port_s=itemdata[0]["Port"].ToString();

            ServerIP = ip_s;
            Port = port_s;
            type = (Type)Enum.Parse(typeof(Type), type_s);

            networkManager.networkAddress=ServerIP;
            kcp.port = ushort.Parse(Port);
            return type;
        }
        catch(Exception e)
        {
            Debug.Log(e.Message);
            return Type.Empty;
        }
    }

    void Start()
    {
        type=License_type();

        if (type.Equals(Type.Server))
        {
            Start_Server();
        }
        else
        {
            Start_Client();
        }
    }

    public void Start_Server()
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            Debug.Log("WebGL cannot be server");
        }
        else
        {
            networkManager.StartServer();
            Debug.Log($"{networkManager.networkAddress} start server...");
            NetworkServer.OnConnectedEvent += (NetworkConnectionToClient) =>
            {
                Debug.Log($"New client connected: {NetworkConnectionToClient.address}");
            };
            NetworkServer.OnDisconnectedEvent += (NetworkConnectionToClient) =>
            {
                Debug.Log($"Client disconnected: {NetworkConnectionToClient.address}");
            };
        }
    }

    public void Start_Client()
    {
        networkManager.StartClient();
        Debug.Log($"{networkManager.networkAddress}: Start client");
    }

    void OnApplicationQuit()
    {
        if (NetworkClient.isConnected)
        {
            networkManager.StopClient();
        }
        if (NetworkServer.active)
        {
            networkManager.StopServer();
        }
    }
}
