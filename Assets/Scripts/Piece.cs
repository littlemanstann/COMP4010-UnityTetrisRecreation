using UnityEngine;

public class Piece : MonoBehaviour
{
    public Board board { get; private set; }
    public TetrominoData data { get; private set; }
    public Vector3Int[] cells { get; private set; }
    public Vector3Int position { get; private set; }
    public int rotationIndex { get; private set; }

    public void Initialize(Board board, Vector3Int position, TetrominoData data)
    {
        this.board = board;
        this.position = position;
        this.data = data;
        this.rotationIndex = 0;

        if (cells == null)
            cells = new Vector3Int[data.cells.Length];

        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = (Vector3Int)data.cells[i];
        }
    }

    public bool ApplyAction(int act)
    {
        switch (act)
        {
            case 0:
                return StepGravity();             
            case 1:
                return Move(Vector2Int.left);
            case 2:
                return Move(Vector2Int.right);
            case 3:
                return Rotate(1);
            case 4:
                return Move(Vector2Int.down);   
            //case 5:
             //   HardDrop();
              //  return true;
        }
        return false;
    }

    public bool StepGravity()
    {
        if (board.gameOver)
            return false;

        bool moved = Move(Vector2Int.down);

        if (!moved)
            Lock();

        return moved;
    }

    public void HardDrop()
    {
        while (Move(Vector2Int.down)) { }
        Lock();
    }

    private void Lock()
    {
        // Fix the current piece in place
        board.Set(this, true);

        // Clear any completed lines
        int cleared = board.ClearLines();

        // Compute the placement reward (holes, height, line clears, etc.)
        float reward = board.EvaluatePlacement(this, cleared);

        // Update the bag and spawn the next piece
        board.UpdateBag();
        board.SpawnPiece();

        // Give reward + request a new decision from the ML-Agents agent
        var agent = Object.FindFirstObjectByType<TetrisAgent>();
        if (agent != null)
        {
            agent.AddReward(reward);
            agent.RequestDecision();
        }
        else
        {
            Debug.LogWarning("TetrisAgent not found when trying to give reward.");
        }
    }

    public bool Move(Vector2Int translation)
    {
        Vector3Int newPosition = position + (Vector3Int)translation;

        board.Clear(this);
        bool valid = board.IsValidPosition(this, newPosition);

        if (valid)
        {
            position = newPosition;
        }

        board.Set(this, false);
        return valid;
    }

    public bool Rotate(int direction)
    {
        int originalRotation = rotationIndex;
        Vector3Int originalPosition = position;

        board.Clear(this);

        rotationIndex = Wrap(rotationIndex + direction, 0, 4);
        ApplyRotationMatrix(direction);

        if (!TestWallKicks(originalRotation, direction))
        {
            rotationIndex = originalRotation;
            ApplyRotationMatrix(-direction);
            position = originalPosition;
            board.Set(this, false);
            return false;
        }

        board.Set(this, false);
        return true;
    }

private void ApplyRotationMatrix(int direction)
{

    for (int i = 0; i < cells.Length; i++)
    {
        Vector3 cell = cells[i];
        float x = cell.x;
        float y = cell.y;
        if (data.tetromino == Tetromino.I || data.tetromino == Tetromino.O)
        {
            x -= 0.5f;
            y -= 0.5f;
        }

        float rotatedX;
        float rotatedY;

        if (direction > 0)
        {
            rotatedX = -y;
            rotatedY = x;
        }
        else
        {
            rotatedX = y;
            rotatedY = -x;
        }


        if (data.tetromino == Tetromino.I || data.tetromino == Tetromino.O)
        {
            rotatedX += 0.5f;
            rotatedY += 0.5f;
        }


        int finalX = Mathf.RoundToInt(rotatedX);
        int finalY = Mathf.RoundToInt(rotatedY);

        cells[i] = new Vector3Int(finalX, finalY, 0);
    }
}

    private bool TestWallKicks(int rotationIndex, int rotationDirection)
    {
        int wallKickIndex = GetWallKickIndex(rotationIndex, rotationDirection);
        for (int i = 0; i < data.wallKicks.GetLength(1); i++)
        {
            Vector2Int translation = data.wallKicks[wallKickIndex, i];
            if (IsValidAfterRotation(position + (Vector3Int)translation))
            {
                position += (Vector3Int)translation;
                return true;
            }
        }
        return false;
    }

    private bool IsValidAfterRotation(Vector3Int testPos)
    {
        RectInt bounds = board.Bounds;
        foreach (var cell in cells)
        {
            Vector3Int pos = cell + testPos;
            if (!bounds.Contains((Vector2Int)pos))
                return false;

            if (board.tilemap.HasTile(pos))
                return false;
        }
        return true;
    }

    private int GetWallKickIndex(int rIndex, int rDir)
    {
        int wIndex = rIndex * 2;
        if (rDir < 0)
            wIndex--;
        return Wrap(wIndex, 0, data.wallKicks.GetLength(0));
    }

    private int Wrap(int input, int min, int max)
    {
        if (input < min)
            return max - (min - input) % (max - min);
        return min + (input - min) % (max - min);
    }
}
