﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Client : MonoBehaviour
{
    public string clientName;

    public bool isHost;
    private bool isSocketStarted;

    private List<GameClient> players = new List<GameClient>(); 
    private TcpClient socket;
    private NetworkStream stream;
    private StreamWriter writer;
    private StreamReader reader;

    private HexMapEditor mapEditor;
    public Player player;

    void Start()
    {
        DontDestroyOnLoad(gameObject);
        player = new Player();
    }

    public bool ConnectToServer(string host, int port)
    {
        if(isSocketStarted) 
            return false;

        try
        {
            socket = new TcpClient(host, port);
            stream = socket.GetStream();
            writer = new StreamWriter(stream);
            reader = new StreamReader(stream);

            isSocketStarted = true;
        }
        catch(Exception e)
        {
            Debug.Log(e.Message);
        }

        return isSocketStarted;
    }

    void Update()
    {
        if(isSocketStarted && stream.DataAvailable)
        {
            string data = reader.ReadLine();
            if(data != null)
                Read(data);
        }
    }

    public void Send(string data)
    {
        if(!isSocketStarted)
            return;

        writer.WriteLine(data);
        writer.Flush();
    }

    void Read(string data)
    {
        Debug.Log("Client " + clientName + " received: " + data);

        string[] receivedData = data.Split('|');
        switch(receivedData[0])
        {
            /////// GAMEPLAY
            case "SMOV":
                FindObjectOfType<HexGameUI>().NetworkDoMove(receivedData[1]);
                break;

            case "SUNC":
                mapEditor = FindObjectOfType<HexMapEditor>();
                mapEditor.NetworkCreateUnit(receivedData[1]);
                break;

            case "SUND":
                mapEditor = FindObjectOfType<HexMapEditor>();
                mapEditor.NetworkDestroyUnit(receivedData[1]);
                break;

            case "SEDI":
                mapEditor = FindObjectOfType<HexMapEditor>();
                mapEditor.NetworkEditCell(receivedData[1]);
                break;

            case "SYGO":
                FindObjectOfType<ButtonManager>().TakeTurn();
                break;

            /////// CHAT
            case "SMSG":
                FindObjectOfType<ChatManager>().ChatMessage(receivedData[2], (ChatManager.MessageType)int.Parse(receivedData[1]));
                break;

            case "SMSE":
                FindObjectOfType<ChatManager>().ChatMessage(receivedData[1], ChatManager.MessageType.ALERT);
                break;

            /////// REGISTER ON SERVER
            case "SWHO":
                for(int i = 1; i < receivedData.Length - 1; ++i)
                    UserConnected(receivedData[i], false);

                Send("CIAM|" + clientName + "#" + ((isHost)? 1:0).ToString());
                break;

            case "SCNN":
                string[] clientStatus = receivedData[1].Split('#');
                UserConnected(clientStatus[0], (clientStatus[1]) == "1");
                break;

            case "SDEC":
                FindObjectOfType<ChatManager>().ChatMessage(receivedData[1] + " left the game.", ChatManager.MessageType.ALERT);
                break;

            case "SKIK":
                FindObjectOfType<ButtonManager>().DeconnectionButton();
                break;

            case "SLOD":
                SceneManager.LoadScene("Map");
                break;

            case "SMAP":
                MapSenderReceiver mapLoader = FindObjectOfType<MapSenderReceiver>();
                mapLoader.StartGame(receivedData[1]);
                break;
        }
    }

    void UserConnected(string name, bool host)
    {
        GameClient client = new GameClient { name = name };
        players.Add(client);
    }

    void OnApplicationQuit()
    {
        CloseSocket();
    }

    void OnDisable()
    {
        CloseSocket();
    }

    public void CloseSocket()
    {
        if(!isSocketStarted)
            return;

        writer.Close();
        reader.Close();
        socket.Close();
        isSocketStarted = false;
    }
}

public class GameClient
{
    public string name;
    public bool isHost;
}
