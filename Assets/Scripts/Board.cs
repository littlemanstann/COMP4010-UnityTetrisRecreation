using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[DefaultExecutionOrder(-1)]
public class Board : MonoBehaviour
{
    public Tilemap tilemap { get; private set; }
    public Piece activePiece { get; private set; }
    // Store active piece cell locations
    public HashSet<Vector3Int> activePositions = new HashSet<Vector3Int>();

    public TetrominoData[] tetrominoes;
    public Vector2Int boardSize = new Vector2Int(10, 20);
    public Vector3Int spawnPosition = new Vector3Int(-1, 8, 0);
    public Tile garbageTile;
    public Tile ghostTile;

    public int linesCleared = 0;

    // Reference to Socket Client to send data
    public StateSocketClient socketClient;

    public RectInt Bounds
    {
        get
        {
            Vector2Int position = new Vector2Int(-boardSize.x / 2, -boardSize.y / 2);
            return new RectInt(position, boardSize);
        }
    }

    private void Awake()
    {
        tilemap = GetComponentInChildren<Tilemap>();
        activePiece = GetComponentInChildren<Piece>();

        for (int i = 0; i < tetrominoes.Length; i++)
        {
            tetrominoes[i].Initialize();
        }

        CreateGarbageLines(5);
    }

    private void Start()
    {
        SpawnPiece();
    }

    public void SpawnPiece()
    {
        int random = Random.Range(0, tetrominoes.Length);
        TetrominoData data = tetrominoes[random];

        activePiece.Initialize(this, spawnPosition, data);

        if (IsValidPosition(activePiece, spawnPosition))
        {
            Set(activePiece, false);
        }
        else
        {
            GameOver();
        }
    }

    public void GameOver()
    {
        tilemap.ClearAllTiles();

        // Do anything else you want on game over here..
    }

    public void Set(Piece piece, bool locked)
    {
        for (int i = 0; i < piece.cells.Length; i++)
        {
            if (!locked)
            {
                activePositions.Add(piece.cells[i] + piece.position);
            }
            Vector3Int tilePosition = piece.cells[i] + piece.position;
            tilemap.SetTile(tilePosition, piece.data.tile);
        }



        // Send updated board state to Python
        // socketClient.SendData();
    }

    public void Clear(Piece piece)
    {
        for (int i = 0; i < piece.cells.Length; i++)
        {
            activePositions.Remove(piece.cells[i] + piece.position);
            Vector3Int tilePosition = piece.cells[i] + piece.position;
            tilemap.SetTile(tilePosition, null);
        }
    }

    public bool IsValidPosition(Piece piece, Vector3Int position)
    {
        RectInt bounds = Bounds;

        // The position is only valid if every cell is valid
        for (int i = 0; i < piece.cells.Length; i++)
        {
            Vector3Int tilePosition = piece.cells[i] + position;

            // An out of bounds tile is invalid
            if (!bounds.Contains((Vector2Int)tilePosition))
            {
                return false;
            }

            // A tile already occupies the position, thus invalid
            if (tilemap.HasTile(tilePosition))
            {
                return false;
            }
        }

        return true;
    }

    public void ClearLines()
    {
        RectInt bounds = Bounds;
        int row = bounds.yMin;

        // Clear from bottom to top
        while (row < bounds.yMax)
        {
            // Only advance to the next row if the current is not cleared
            // because the tiles above will fall down when a row is cleared
            if (IsLineFull(row))
            {
                LineClear(row);
            }
            else
            {
                row++;
            }
        }
    }

    public bool IsLineFull(int row)
    {
        RectInt bounds = Bounds;

        for (int col = bounds.xMin; col < bounds.xMax; col++)
        {
            Vector3Int position = new Vector3Int(col, row, 0);

            // The line is not full if a tile is missing
            if (!tilemap.HasTile(position))
            {
                return false;
            }
        }

        return true;
    }

    public void LineClear(int row)
    {
        RectInt bounds = Bounds;
        bool isGarbageLine = false;

        // Clear all tiles in the row
        for (int col = bounds.xMin; col < bounds.xMax; col++)
        {
            Vector3Int position = new Vector3Int(col, row, 0);
            if (tilemap.GetTile(position) == garbageTile)
            {
                isGarbageLine = true;
            }
            tilemap.SetTile(position, null);
        }

        // Shift every row above down one
        while (row < bounds.yMax)
        {
            for (int col = bounds.xMin; col < bounds.xMax; col++)
            {
                Vector3Int position = new Vector3Int(col, row + 1, 0);
                TileBase above = tilemap.GetTile(position);

                position = new Vector3Int(col, row, 0);
                tilemap.SetTile(position, above);
            }

            row++;
        }

        // If row was made of garbage tiles, create 1 garbage line
        if (isGarbageLine)
        {
            CreateGarbageLines(1);
        }

        // Increment cleared lines count
        linesCleared++;

        // Notify Python after clearing a line
        socketClient.SendData();
    }


    // Create lines of garbage at the bottom of the board
    public void CreateGarbageLines(int amount)
    {
        RectInt bounds = Bounds;

        for (int i = 0; i < amount; i++)
        {
            // Shift every row up one
            for (int row = bounds.yMax - 1; row >= bounds.yMin; row--)
            {
                for (int col = bounds.xMin; col < bounds.xMax; col++)
                {
                    Vector3Int position = new Vector3Int(col, row - 1, 0);
                    TileBase below = tilemap.GetTile(position);

                    position = new Vector3Int(col, row, 0);
                    tilemap.SetTile(position, below);
                }
            }

            // Create garbage line at the bottom
            int hole = Random.Range(bounds.xMin, bounds.xMax);

            for (int col = bounds.xMin; col < bounds.xMax; col++)
            {
                if (col == hole)
                    continue;

                Vector3Int position = new Vector3Int(col, bounds.yMin, 0);
                tilemap.SetTile(position, garbageTile); // Use garbage tile
            }
        }
    }


    // HELPER FUNCTION: Get board grid as 1D array
    public int[] GetGridState()
    {
        RectInt bounds = Bounds;
        int[] grid = new int[bounds.height * bounds.width];

        for (int row = bounds.yMin; row < bounds.yMax; row++)
        {
            for (int col = bounds.xMin; col < bounds.xMax; col++)
            {
                Vector3Int position = new Vector3Int(col, row, 0);
                TileBase tile = tilemap.GetTile(position);

                int value = 0; // default empty
                if (tile == null || tile == ghostTile)
                    value = 0;
                // Check if tile belongs to active piece
                else if (activePositions.Contains(position))
                    value = 3;
                else if (tile == garbageTile)
                    value = 2;
                else
                    value = 1;

                grid[(row - bounds.yMin) * bounds.width + (col - bounds.xMin)] = value;
            }
        }

        return grid;
    }

    public int GetLinesCleared()
    {
        return linesCleared;
    }
}
