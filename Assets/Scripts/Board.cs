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
    public Vector3Int spawnPosition = new Vector3Int(-1, 9, 0);
    public Tile garbageTile;
    public Tile ghostTile;


    public bool sevenBag = true;
    private List<Tetromino> bag = new List<Tetromino>();
   
    public int normalLinesCleared = 0;
    public int garbageLinesCleared = 0;


    public bool gameOver = false;


    // State reward to be consumed by agent
    public float lastReward = 0f;

    private float gravityTimer = 0f;
    public float gravityInterval = 0.5f;

    // Store information about the last locked piece for reward calculation
    public Vector3Int lastLockedPiecePosition { get; private set; }
    public Vector3Int[] lastLockedPieceCells { get; private set; }
    public Tetromino lastLockedPieceType { get; private set; }
    public bool hasLastLockedPiece { get; private set; } = false;


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


        // Set bag to full set of tetrominoes
        for (int i = 0; i < tetrominoes.Length; i++)
        {
            bag.Add(tetrominoes[i].tetromino);
        }


        CreateGarbageLines(5);
    }


    private void Start()
    {
        SpawnPiece();
    }


    public void SpawnPiece()
    {
        // Print locked piece information when a new piece is spawned
        if (hasLastLockedPiece)
        {
            Debug.Log($"[BOARD] Last Locked Piece Info (before spawning new piece):");
            Debug.Log($"  - Type: {lastLockedPieceType}");
            Debug.Log($"  - Position: {lastLockedPiecePosition}");
            Debug.Log($"  - Number of cells: {lastLockedPieceCells?.Length ?? 0}");
            
            if (lastLockedPieceCells != null)
            {
                Vector3Int[] worldPositions = GetLastLockedPieceWorldPositions();
                string positionsStr = "";
                for (int i = 0; i < worldPositions.Length; i++)
                {
                    positionsStr += worldPositions[i].ToString();
                    if (i < worldPositions.Length - 1) positionsStr += ", ";
                }
                Debug.Log($"  - World Positions: [{positionsStr}]");
            }
        }

        TetrominoData data = tetrominoes[0];


        // If using 7-bag system, ensure all pieces are used before repeating
        if (sevenBag)
        {
            // If bag is empty
            if (bag.Count == 0)
            {
                // Refill the bag
                for (int i = 0; i < tetrominoes.Length; i++)
                {
                    bag.Add(tetrominoes[i].tetromino);
                }
            }


            int bagIndex = Random.Range(0, bag.Count);
            Tetromino tetrominoType = bag[bagIndex];
            // Find the index of the tetromino type in the tetrominoes array
            for (int i = 0; i < tetrominoes.Length; i++)
            {
                if (tetrominoes[i].tetromino == tetrominoType)
                {
                    data = tetrominoes[i];
                    break;
                }
            }
        }
        else
        {
            int random = Random.Range(0, tetrominoes.Length);
            data = tetrominoes[random];
        }
        activePiece.Initialize(this, spawnPosition, data);


        if (IsValidPosition(activePiece, spawnPosition))
        {
            Set(activePiece, false); // draw new active piece
        }
        else
        {
            Debug.LogError("[SPAWN] INVALID spawn position -> setting gameOver = true");
            gameOver = true;
        }
    }

    private void Update()
    {
        if (gameOver) return;

        gravityTimer += Time.deltaTime;

        if (gravityTimer >= gravityInterval)
        {
            gravityTimer = 0f;
            activePiece.StepGravity();
        }
    }

    public void UpdateBag()
    {
        bag.Remove(activePiece.data.tetromino);
    }


    public void GameOver()
    {
        tilemap.ClearAllTiles();


        // Reset garbage
        CreateGarbageLines(5);


        // Reset game over flag
        gameOver = false;


        // Reset line cleared counts
        normalLinesCleared = 0;
        garbageLinesCleared = 0;
        lastReward = 0f;
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

        // UPDATE COUNTERS + REWARD (ONLY ONCE)
        if (isGarbageLine)
        {
            garbageLinesCleared++;
            CreateGarbageLines(1);
            AddReward(+1.0f);
        }
        else
        {
            normalLinesCleared++;
            AddReward(+0.5f);
        }

        // Notify Python AFTER reward
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


    /// <summary>
    /// AI Agent Helper Functions
    /// </summary>
    /// <returns></returns>I th




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
    public void ResetForEpisode()
{
    Debug.Log("[BOARD] ResetForEpisode called");

    // Reuse existing reset logic
    lastReward = 0f;
    hasLastLockedPiece = false; // Reset locked piece tracking
    GameOver();


    // Spawn a fresh piece on the reset board
    SpawnPiece();


    Debug.Log($"[BOARD] After ResetForEpisode, gameOver = {gameOver}");
}

    /// <summary>
    /// Saves the location information of a piece when it is locked
    /// Should only be called from Piece.Lock() to control when pieces are locked
    /// </summary>
    public void SaveLastLockedPieceLocation(Piece piece)
    {
        lastLockedPiecePosition = piece.position;
        lastLockedPieceType = piece.data.tetromino;
        
        // Save all cell positions (relative to piece origin)
        lastLockedPieceCells = new Vector3Int[piece.cells.Length];
        for (int i = 0; i < piece.cells.Length; i++)
        {
            lastLockedPieceCells[i] = piece.cells[i];
        }
        
        hasLastLockedPiece = true;
        
        Debug.Log($"[BOARD] Saved locked piece location: Position={lastLockedPiecePosition}, Type={lastLockedPieceType}, Cells={lastLockedPieceCells.Length}");
    }

    /// <summary>
    /// Gets all world positions where the last locked piece's blocks were placed
    /// </summary>
    public Vector3Int[] GetLastLockedPieceWorldPositions()
    {
        if (!hasLastLockedPiece || lastLockedPieceCells == null)
        {
            return new Vector3Int[0];
        }
        
        Vector3Int[] worldPositions = new Vector3Int[lastLockedPieceCells.Length];
        for (int i = 0; i < lastLockedPieceCells.Length; i++)
        {
            worldPositions[i] = lastLockedPieceCells[i] + lastLockedPiecePosition;
        }
        
        return worldPositions;
    }






    /// <summary>
    /// Functions for getting state data to send to Python server
    /// </summary>
    /// <returns></returns>


    // STATE FUNCTION: Get contour of the board
    public int[] GetContour()
    {
        RectInt bounds = Bounds;
        int[] contour = new int[bounds.width - 1];
        int index = 0;
        int currHeight = 0;
        int prevHeight = 0;


        for (int col = bounds.xMin; col < bounds.xMax; col++)
        {
            for (int row = bounds.yMax - 1; row >= bounds.yMin; row--)
            {
                Vector3Int position = new Vector3Int(col, row, 0);
                TileBase tile = tilemap.GetTile(position);


                if (tile != null && tile != ghostTile)
                {
                    currHeight = row - bounds.yMin + 1;
                    // Skip the first column
                    if (col != bounds.xMin) {
                        contour[index] = currHeight - prevHeight;
                        index++;
                    }
                    prevHeight = currHeight;
                    break;
                }
            }
        }


        return contour;
    }


    //TESTERS
    public bool IsWithinBounds(Vector3Int pos)
    {
        RectInt bounds = Bounds;
        return bounds.Contains((Vector2Int)pos);
    }
    public TileBase GetTile(Vector3Int pos)
    {
        return tilemap.GetTile(pos);
    }






    // Convenience helper: map piece enum char to an int ID (optional)
    public int GetCurrentPieceId()
    {
        if (activePiece == null) return -1;
        char c = GetCurrentPieceChar();
        // Map based on 'I','O','T','S','Z','J','L' — modify to match your Tetromino enum
        switch (c)
        {
            case 'I': return 0;
            case 'O': return 1;
            case 'T': return 2;
            case 'S': return 3;
            case 'Z': return 4;
            case 'J': return 5;
            case 'L': return 6;
            default: return -1;
        }
    }

    public void AddReward(float value)
    {
        lastReward += value;
        Debug.Log($"[BOARD] AddReward called: +{value:F4}, total accumulated: {lastReward:F4}");
    }
    //what does this do??
    //I think it is so that you can acumulate a reward in lastReward then set it to the current reward?
    //should lastReward be re-named to future reward? or Acumulated Reward?
    //Clears last reward
    public float ConsumeReward()
    {
        float r = lastReward;
        if (r != 0f)
        {
            Debug.Log($"[BOARD] ConsumeReward called: consuming {r:F4}, resetting to 0");
        }
        lastReward = 0f;
        return r;
    }

    /// <summary>
    /// Calculates reward based on where a piece was placed
    /// </summary>
    public void CalculatePlacementReward(Piece piece)
    {
        // Base reward for placing a piece
        float reward = 0.1f;
        
        // Get the piece's position and cells
        Vector3Int piecePosition = piece.position;
        Vector3Int[] worldPositions = new Vector3Int[piece.cells.Length];
        for (int i = 0; i < piece.cells.Length; i++)
        {
            worldPositions[i] = piece.cells[i] + piecePosition;
        }
        
        // Location-based reward calculations

        // Parameters for heuristics (tune as needed)
        float lowRowReward = 0.001f;      // Reward per row closer to bottom
        float holePenalty = -0.02f;       // Penalty per new hole created
        float fillReward = 0.01f;         // Reward per hole filled by this placement
        float stackPenalty = -0.001f;     // Penalty for high stacks
        float flatnessReward = 0.005f;    // Reward for flatter surfaces

        RectInt bounds = Bounds;
        int boardWidth = bounds.width;
        int boardHeight = bounds.height;

        // -- 1. Reward for lower placements --
        int lowestY = boardHeight;
        foreach (var pos in worldPositions)
        {
            if (pos.y < lowestY) lowestY = pos.y;
        }
        // Y increases upwards—lower y is closer to bottom
        float rewardLower = (boardHeight - 1 - lowestY) * lowRowReward;
        reward += rewardLower;

        // -- 2. Penalty for holes (empty below filled) & reward for filling holes --
        // For each column, check for new holes created or filled
        Tilemap tilemap = this.tilemap;
        int colMin = bounds.xMin;
        int colMax = bounds.xMax;

        int newHoles = 0;
        int filledHoles = 0;

        foreach (var pos in worldPositions)
        {
            // For each block placed, check if it fills an empty-under-filled "hole"
            int x = pos.x;
            int y = pos.y;

            // Is there a filled cell above and empty cell(s) below prior to this placement?
            // We'll reward if we are filling an empty cell that previously had filled above it (a hole got filled)
            bool wasHole = false;
            for (int yCheck = y + 1; yCheck < bounds.yMax; yCheck++)
            {
                Vector3Int checkAbove = new Vector3Int(x, yCheck, 0);
                if (tilemap.HasTile(checkAbove))
                {
                    wasHole = true;
                    break;
                }
            }
            if (wasHole)
                filledHoles++;

            // Conversely, is there now a tile above an empty cell (thus a new hole)?
            for (int yBelow = y - 1; yBelow >= bounds.yMin; yBelow--)
            {
                Vector3Int below = new Vector3Int(x, yBelow, 0);
                if (!tilemap.HasTile(below))
                {
                    // New hole created if empty below
                    newHoles++;
                    break; // only count the first empty directly below
                }
                else
                {
                    // Found a tile directly below, not a hole here
                    break;
                }
            }
        }
        reward += filledHoles * fillReward;
        reward += newHoles * holePenalty;

        // -- 3. Penalty for high stacks (encourage pieces to keep the board low) --
        int maxStackHeight = 0;
        for (int x = colMin; x < colMax; x++)
        {
            // topmost filled cell in that column
            for (int y = bounds.yMax - 1; y >= bounds.yMin; y--)
            {
                if (tilemap.HasTile(new Vector3Int(x, y, 0)))
                {
                    int height = y - bounds.yMin + 1;
                    if (height > maxStackHeight)
                        maxStackHeight = height;
                    break;
                }
            }
        }
        reward += maxStackHeight * stackPenalty;

        // -- 4. Reward for keeping the board flat (flatness = low diff in column heights) --
        int[] colHeights = new int[boardWidth];
        for (int x = colMin, i = 0; x < colMax; x++, i++)
        {
            int yTop = bounds.yMin - 1;
            for (int y = bounds.yMax - 1; y >= bounds.yMin; y--)
            {
                if (tilemap.HasTile(new Vector3Int(x, y, 0)))
                {
                    yTop = y;
                    break;
                }
            }
            colHeights[i] = yTop;
        }
        // Flatness metric: reward is higher the smaller the difference
        int maxH = int.MinValue, minH = int.MaxValue;
        foreach (int h in colHeights)
        {
            if (h > maxH) maxH = h;
            if (h < minH) minH = h;
        }
        int flatnessDelta = maxH - minH;
        reward += flatnessDelta == 0 ? flatnessReward : flatnessReward / (flatnessDelta + 1);

        
        // For now, just use the base reward
        AddReward(reward);
        
        Debug.Log($"[REWARD] Placement reward calculated: {reward} for piece at position {piecePosition}");
    }


    // STATE FUNCTION: Get current piece as char
    public char GetCurrentPieceChar()
    {
        // print(activePiece.data.tetromino.ToString()[0]);
        return activePiece.data.tetromino.ToString()[0];
    }


    // STATE FUNCTION: Get number of normal lines cleared
    public int GetNormalLinesCleared()
    {
        return normalLinesCleared;
    }


    // STATE FUNCTION: Get number of garbage lines cleared
    public int GetGarbageLinesCleared()
    {
        // For simplicity, assume each garbage line cleared is counted as 1
        return garbageLinesCleared;
    }
}



