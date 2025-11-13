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
    public int linesCleared = 0;
    private int[] grid;
    private bool isConnected = false;

    void Awake()
    {
        grid = new int[rows * cols];
        linesCleared = 0;
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
        grid = board.GetGridState(); // Assume this method returns a flattened int array of the grid state
        linesCleared = board.GetLinesCleared(); // Assume this method returns the number of lines cleared
        string json = JsonUtility.ToJson(new GridData(grid, linesCleared));
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
public class GridData
{
    public int[] grid;
    public int linesCleared;

    public GridData(int[] grid, int linesCleared)
    {
        this.grid = grid;
        this.linesCleared = linesCleared;
    }
}