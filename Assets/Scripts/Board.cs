using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[DefaultExecutionOrder(-1)]
public class Board : MonoBehaviour
{
    [Header("Configuration")]
    public TetrominoData[] tetrominoes;
    public Tile garbageTile;
    public Tile ghostTile;
    public Vector3Int spawnPosition = new Vector3Int(-1, 8, 0);
    public Vector2Int boardSize = new Vector2Int(10, 20);

    public float pendingReward = 0f;
    private int previousHoleCount = 0;

    public Tilemap tilemap { get; private set; }
    public Piece activePiece { get; private set; }
    public StateSocketClient socketClient;

    public HashSet<Vector3Int> activePositions = new HashSet<Vector3Int>();
    public bool gameOver = false;
    public float lastReward = 0f;

    public int normalLinesCleared = 0;
    public int garbageLinesCleared = 0;

    private List<Tetromino> bag = new List<Tetromino>();
    public bool sevenBag = true;

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
        socketClient = GetComponent<StateSocketClient>();

        for (int i = 0; i < tetrominoes.Length; i++)
        {
            tetrominoes[i].Initialize();
        }
        InitializeBag();
    }

    private void InitializeBag()
    {
        bag.Clear();
        for (int i = 0; i < tetrominoes.Length; i++)
        {
            bag.Add(tetrominoes[i].tetromino);
        }
    }

    public void ResetForEpisode()
    {
        lastReward = 0f;
        gameOver = false;
        normalLinesCleared = 0;
        garbageLinesCleared = 0;

        tilemap.ClearAllTiles();
        activePositions.Clear();

        CreateGarbageLines(3);
        SpawnPiece();

        previousHoleCount = CountHoles();
    }

    public void SpawnPiece()
    {
        TetrominoData data = GetNextTetromino();
        activePiece.Initialize(this, spawnPosition, data);

        if (IsValidPosition(activePiece, spawnPosition))
        {
            Set(activePiece, false);
        }
        else
        {
            gameOver = true;
        }
    }

    public void UpdateBag()
    {
        bag.Remove(activePiece.data.tetromino);
    }

    private TetrominoData GetNextTetromino()
    {
        TetrominoData data = tetrominoes[0];

        if (sevenBag)
        {
            if (bag.Count == 0)
                InitializeBag();

            int index = Random.Range(0, bag.Count);
            Tetromino type = bag[index];

            foreach (var t in tetrominoes)
            {
                if (t.tetromino == type)
                {
                    data = t;
                    break;
                }
            }
        }
        else
        {
            data = tetrominoes[Random.Range(0, tetrominoes.Length)];
        }

        return data;
    }

    /// <summary>
    /// Draws the piece tiles on the tilemap.
    /// No reward logic here.
    /// </summary>
    public void Set(Piece piece, bool locked)
    {
        for (int i = 0; i < piece.cells.Length; i++)
        {
            Vector3Int tilePos = piece.cells[i] + piece.position;

            if (!locked)
                activePositions.Add(tilePos);

            tilemap.SetTile(tilePos, piece.data.tile);
        }

        if (socketClient != null)
            socketClient.SendData();
    }

    public void Clear(Piece piece)
    {
        for (int i = 0; i < piece.cells.Length; i++)
        {
            Vector3Int tilePos = piece.cells[i] + piece.position;
            activePositions.Remove(tilePos);
            tilemap.SetTile(tilePos, null);
        }
    }

    public bool IsValidPosition(Piece piece, Vector3Int position)
    {
        RectInt bounds = Bounds;

        for (int i = 0; i < piece.cells.Length; i++)
        {
            Vector3Int tilePos = piece.cells[i] + position;

            if (!bounds.Contains((Vector2Int)tilePos))
                return false;

            if (tilemap.HasTile(tilePos))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Clears all full lines and returns how many lines were cleared.
    /// No reward added here; only board state + counters.
    /// </summary>
    public int ClearLines()
    {
        RectInt bounds = Bounds;
        int row = bounds.yMin;
        int cleared = 0;
        int garbage = 0;

        while (row < bounds.yMax)
        {
            if (IsLineFull(row))
            {
                bool isGarbage = LineClear(row);
                cleared++;

                if (isGarbage)
                    garbage++;

                // do not increment row; we want to re-check same index after collapse
            }
            else
            {
                row++;
            }
        }

        int nonGarbage = cleared - garbage;

        normalLinesCleared += nonGarbage;
        garbageLinesCleared += garbage;

        if (socketClient != null && cleared > 0)
            socketClient.SendData();

        return cleared;
    }

    private bool IsLineFull(int row)
    {
        RectInt bounds = Bounds;
        for (int col = bounds.xMin; col < bounds.xMax; col++)
        {
            if (!tilemap.HasTile(new Vector3Int(col, row, 0)))
                return false;
        }
        return true;
    }

    public bool LineClear(int row)
    {
        RectInt bounds = Bounds;
        bool isGarbageLine = false;

        for (int col = bounds.xMin; col < bounds.xMax; col++)
        {
            Vector3Int pos = new Vector3Int(col, row, 0);
            if (tilemap.GetTile(pos) == garbageTile)
                isGarbageLine = true;

            tilemap.SetTile(pos, null);
        }

        while (row < bounds.yMax)
        {
            for (int col = bounds.xMin; col < bounds.xMax; col++)
            {
                Vector3Int pos = new Vector3Int(col, row + 1, 0);
                TileBase above = tilemap.GetTile(pos);
                tilemap.SetTile(new Vector3Int(col, row, 0), above);
            }
            row++;
        }

        return isGarbageLine;
    }

    public void CreateGarbageLines(int amount)
    {
        RectInt bounds = Bounds;
        for (int i = 0; i < amount; i++)
        {
            for (int row = bounds.yMax - 1; row >= bounds.yMin; row--)
            {
                for (int col = bounds.xMin; col < bounds.xMax; col++)
                {
                    Vector3Int pos = new Vector3Int(col, row - 1, 0);
                    TileBase below = tilemap.GetTile(pos);
                    tilemap.SetTile(new Vector3Int(col, row, 0), below);
                }
            }

            int hole = Random.Range(bounds.xMin, bounds.xMax);
            for (int col = bounds.xMin; col < bounds.xMax; col++)
            {
                if (col == hole)
                    continue;

                tilemap.SetTile(new Vector3Int(col, bounds.yMin, 0), garbageTile);
            }
        }
    }

    public int[] GetGridState()
    {
        RectInt bounds = Bounds;
        int[] grid = new int[bounds.height * bounds.width];

        for (int row = bounds.yMin; row < bounds.yMax; row++)
        {
            for (int col = bounds.xMin; col < bounds.xMax; col++)
            {
                Vector3Int pos = new Vector3Int(col, row, 0);
                TileBase tile = tilemap.GetTile(pos);
                int val = 0;

                if (tile == null)
                    val = 0;
                else if (activePositions.Contains(pos))
                    val = 3;
                else if (tile == garbageTile)
                    val = 2;
                else
                    val = 1;

                grid[(row - bounds.yMin) * bounds.width + (col - bounds.xMin)] = val;
            }
        }

        return grid;
    }

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
                    if (col != bounds.xMin)
                    {
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

    public char GetCurrentPieceChar()
    {
        if (activePiece == null || activePiece.data.tetromino == 0)
            return ' ';
        return activePiece.data.tetromino.ToString()[0];
    }

    // ===== Heuristic helpers used for reward & observations =====

    public int[] GetColumnHeights()
    {
        RectInt bounds = Bounds;
        int width = bounds.width;
        int height = bounds.height;

        int[] heights = new int[width];

        for (int x = 0; x < width; x++)
        {
            int worldX = bounds.xMin + x;

            for (int y = height - 1; y >= 0; y--)
            {
                int worldY = bounds.yMin + y;
                Vector3Int pos = new Vector3Int(worldX, worldY, 0);

                if (tilemap.GetTile(pos) != null)
                {
                    heights[x] = y + 1;
                    break;
                }
            }
        }

        return heights;
    }

    public int[] GetContourDiffs()
    {
        int[] h = GetColumnHeights();
        int[] diffs = new int[h.Length - 1];

        for (int i = 0; i < diffs.Length; i++)
        {
            diffs[i] = h[i + 1] - h[i];
        }

        return diffs;
    }

    public int CountHoles()
    {
        RectInt bounds = Bounds;
        int width = bounds.width;
        int height = bounds.height;

        int holes = 0;

        for (int x = 0; x < width; x++)
        {
            bool blockSeen = false;
            int worldX = bounds.xMin + x;

            for (int y = height - 1; y >= 0; y--)
            {
                int worldY = bounds.yMin + y;
                Vector3Int pos = new Vector3Int(worldX, worldY, 0);
                bool filled = tilemap.GetTile(pos) != null;

                if (filled)
                {
                    blockSeen = true;
                }
                else if (blockSeen)
                {
                    holes++;
                }
            }
        }

        return holes;
    }

    public int GetMaxHeight()
    {
        int[] heights = GetColumnHeights();
        int max = 0;

        foreach (int h in heights)
        {
            if (h > max)
                max = h;
        }

        return max;
    }

    // ===== RL Reward Accumulation =====

    public void AddReward(float v)
    {
        lastReward += v;
    }

    public float ConsumeReward()
    {
        float r = lastReward;
        lastReward = 0f;
        return r;
    }

    public int GetCurrentPieceId()
    {
        return (activePiece == null) ? 0 : (int)activePiece.data.tetromino;
    }

    public int GetGarbageLinesCleared()
    {
        return garbageLinesCleared;
    }

    public int GetNormalLinesCleared()
    {
        return normalLinesCleared;
    }
public void SaveLastLockedPieceLocation(Piece piece)
{
    // Optional debug hook
}

// =====================
// RL REWARD EVALUATION
// =====================

public float EvaluatePlacement(Piece piece, int clearedLines)
{
    float reward = 0f;

    // 1. Line clear rewards
    reward += LineClearReward(clearedLines);

    // 2. Hole delta penalty (gentle)
    reward += HoleReward();

    // 3. Height penalty (gentle)
    reward += HeightPenalty();

    // 4. Survival reward — every placement gives small positive reward
    reward += 0.5f;

    AddReward(reward);
    return reward;
}

// -----------------
// Line Clear Reward
// -----------------
private float LineClearReward(int lines)
{
    switch (lines)
    {
        case 1: return 100f;
        case 2: return 300f;
        case 3: return 500f;
        case 4: return 1000f;
        default: return 0f;
    }
}

// -----------------
// Hole Reward
// -----------------
private float HoleReward()
{
    int newHoles = CountHoles();
    int delta = newHoles - previousHoleCount;

    float reward = 0f;

    if (delta > 0)
        reward -= 0.4f * delta;   // Slight penalty for creating holes
    else if (delta < 0)
        reward += 0.3f * (-delta); // Reward for reducing holes

    previousHoleCount = newHoles;
    return reward;
}

// --------------------
// Bumpiness Penalty
// --------------------
private float BumpinessPenalty()
{
    int[] diffs = GetContourDiffs();
    float bumpiness = 0f;

    for (int i = 0; i < diffs.Length; i++)
        bumpiness += Mathf.Abs(diffs[i]);

    return -0.1f * bumpiness;
}

// --------------------
// Height Penalty
// --------------------
private float HeightPenalty()
{
    int maxH = GetMaxHeight();
    return -0.02f * maxH;
}

// -----------------------------
// Landing Height Reward
// -----------------------------
private float LandingHeightReward(Piece piece)
{
    RectInt bounds = Bounds;
    int landingY = piece.position.y - bounds.yMin;
    int boardHeight = bounds.height;

    // reward lower placements
    return (boardHeight - landingY) * 0.2f;
}
}