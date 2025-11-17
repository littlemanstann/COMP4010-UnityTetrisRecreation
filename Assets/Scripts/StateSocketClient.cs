using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

public class StateSocketClient : MonoBehaviour
{
    // Get board state from Board.cs
    [SerializeField] private Board board;

    private TcpClient client;
    private StreamWriter writer;

    public int rows = 20;
    public int cols = 10;
    private int[] contour;
    private char currentPiece;
    private int normalLinesCleared;
    private int garbageLinesCleared;
    private bool isConnected = false;

    void Awake()
    {
        contour = new int[rows * cols];
        normalLinesCleared = 0;
        garbageLinesCleared = 0;
        // InvokeRepeating(nameof(SendData), 0f, 0f); // send every frame
    }

    public void ConnectToServer()
    {
        try
        {
            client = new TcpClient("127.0.0.1", 5000);
            writer = new StreamWriter(client.GetStream());
            writer.AutoFlush = true;
            Debug.Log("Connected to Python server");
            isConnected = true;
        }
        catch (Exception e)
        {
            Debug.LogError("Connection error: " + e.Message);
        }
    }

    public void SendData()
    {
        if (!isConnected)
        {
            Debug.LogWarning("Not connected to server.");
            return;
        }

        // Send array as JSON
        contour = board.GetContour(); // Assume this method returns a flattened int array of the contour state
        currentPiece = board.GetCurrentPieceChar(); // Assume this method returns the current piece as a char
        normalLinesCleared = board.GetNormalLinesCleared(); // Assume this method returns the number of lines cleared
        garbageLinesCleared = board.GetGarbageLinesCleared(); // Assume this method returns the number of garbage lines cleared

        string json = JsonUtility.ToJson(new StateData(contour, currentPiece, normalLinesCleared, garbageLinesCleared));
        writer.Write(json + "\n");
        Debug.Log(json);
        writer.Flush();
    }

    void OnApplicationQuit()
    {
        writer?.Close();
        client?.Close();
        isConnected = false;
    }
}


[System.Serializable]
public class StateData
{
    public int[] contour;
    public char currentPiece;
    public int normalLinesCleared;
    public int garbageLinesCleared;

    public StateData(int[] contour, char currentPiece, int normalLinesCleared, int garbageLinesCleared)
    {
        this.contour = contour;
        this.currentPiece = currentPiece;
        this.normalLinesCleared = normalLinesCleared;
        this.garbageLinesCleared = garbageLinesCleared; 
    }
}