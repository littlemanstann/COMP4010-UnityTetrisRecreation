using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class Piece : MonoBehaviour
{
    public Board board { get; private set; }
    public TetrominoData data { get; private set; }
    public Vector3Int[] cells { get; private set; }
    public Vector3Int position { get; private set; }
    public int rotationIndex { get; private set; }


    // Array to keep track of last 10 inputs
    private string[] inputHistory = new string[10];
    // Visual to keep track of inputs
    public TextMeshProUGUI inputText;


    public float stepDelay = 1f;
    public float moveDelay = 0.1f;
    public float lockDelay = 0.5f;


    private float stepTime;
    private float moveTime;
    private float lockTime;


    public void Initialize(Board board, Vector3Int position, TetrominoData data)
    {
        this.data = data;
        this.board = board;
        this.position = position;


        rotationIndex = 0;
        stepTime = Time.time + stepDelay;
        moveTime = Time.time + moveDelay;
        lockTime = 0f;


        if (cells == null) {
            cells = new Vector3Int[data.cells.Length];
        }


        for (int i = 0; i < cells.Length; i++) {
            cells[i] = (Vector3Int)data.cells[i];
            //cells[i]
        }
    }

    public void StepGravity()
    {
        if (board.gameOver)
            return;

        bool moved = Move(Vector2Int.down);

        if (!moved)
        {
            Lock();
        }
}    private void Update()
    {
        /*
        board.Clear(this);


        if (board.gameOver) {
            return;
        }


        // We use a timer to allow the player to make adjustments to the piece
        // before it locks in place
        lockTime += Time.deltaTime;


        // Handle rotation
        if (Input.GetKeyDown(KeyCode.Q)) {
            AddInputToHistory("Q");
            Rotate(-1);
        } else if (Input.GetKeyDown(KeyCode.E)) {
            AddInputToHistory("E");
            Rotate(1);
        }


        // Handle hard drop
        if (Input.GetKeyDown(KeyCode.Space)) {
            AddInputToHistory("Space");
            HardDrop();
        }


        // Allow the player to hold movement keys but only after a move delay
        // so it does not move too fast
        if (Time.time > moveTime) {
            HandleMoveInputs();
        }


        // Advance the piece to the next row every x seconds
        if (Time.time > stepTime) {
            Step();
        }


        // Update visual text
        inputText.text = "Input History: " + string.Join(", ", inputHistory);


        board.Set(this, true);
        */
    }


    private void HandleMoveInputs()
    {
        // Soft drop movement
        if (Input.GetKey(KeyCode.S))
        {
            AddInputToHistory("S");
            if (Move(Vector2Int.down)) {
                // Update the step time to prevent double movement
                stepTime = Time.time + stepDelay;
            }
        }


        // Left/right movement
        if (Input.GetKey(KeyCode.A)) {
            AddInputToHistory("A");
            Move(Vector2Int.left);
        } else if (Input.GetKey(KeyCode.D)) {
            AddInputToHistory("D");
            Move(Vector2Int.right);
        }
    }


    private void Step()
    {
        stepTime = Time.time + stepDelay;


        // Step down to the next row
        Move(Vector2Int.down);


        // Once the piece has been inactive for too long it becomes locked
        if (lockTime >= lockDelay) {
            Lock();
        }
    }


    public void HardDrop()
    {
        int dropDistance = 0;

        while (Move(Vector2Int.down))
        {
            dropDistance++;
        }


        board.AddReward(0.015f * dropDistance);

        Lock();
    }




    private void Lock()
    {
        board.AddReward(+0.1f);
        // Clear active piece once and re-set as locked tiles
        board.Clear(this);
        board.Set(this, true);       // now part of the board


        board.UpdateBag();
        board.ClearLines();
        board.SpawnPiece();
    }


public bool Move(Vector2Int translation)
{
    Vector3Int oldPosition = position;
    Vector3Int newPosition = position + (Vector3Int)translation;

    board.Clear(this);

    bool valid = board.IsValidPosition(this, newPosition);

    if (valid)
    {
        position = newPosition;
        moveTime = Time.time + moveDelay;
        lockTime = 0f;

        //  REWARD LOGIC 


        if (translation.x != 0)
            board.AddReward(-0.001f);

    }
    else
    {
        position = oldPosition;
    }

    board.Set(this, false);
    return valid;
}


    public bool Rotate(int direction)
    {
        int originalRotation = rotationIndex;
        Vector3Int originalPosition = position;


        // Clear from board at current placement
        board.Clear(this);


        // Apply rotation
        rotationIndex = Wrap(rotationIndex + direction, 0, 4);
        ApplyRotationMatrix(direction);


        // Test wall kicks using the new rotation
        if (!TestWallKicks(originalRotation, direction))
        {
            // Rotation failed, revert everything
            rotationIndex = originalRotation;
            ApplyRotationMatrix(-direction);
            position = originalPosition;


            board.Set(this, false);
            return false;
        }


        // Successful rotation, re-draw in new orientation
        board.Set(this, false);
        lockTime = 0f;
        board.AddReward(-0.002f);
        return true;
    }


    private void ApplyRotationMatrix(int direction)
    {
        float[] matrix = Data.RotationMatrix;


        // Rotate all of the cells using the rotation matrix
        for (int i = 0; i < cells.Length; i++)
        {
            Vector3 cell = cells[i];


            int x, y;


            switch (data.tetromino)
            {
                case Tetromino.I:
                case Tetromino.O:
                    // "I" and "O" are rotated from an offset center point
                    cell.x -= 0.5f;
                    cell.y -= 0.5f;
                    x = Mathf.CeilToInt((cell.x * matrix[0] * direction) + (cell.y * matrix[1] * direction));
                    y = Mathf.CeilToInt((cell.x * matrix[2] * direction) + (cell.y * matrix[3] * direction));
                    break;


                default:
                    x = Mathf.RoundToInt((cell.x * matrix[0] * direction) + (cell.y * matrix[1] * direction));
                    y = Mathf.RoundToInt((cell.x * matrix[2] * direction) + (cell.y * matrix[3] * direction));
                    break;
            }


            cells[i] = new Vector3Int(x, y, 0);
        }
    }


    private bool TestWallKicks(int rotationIndex, int rotationDirection)
    {
        int wallKickIndex = GetWallKickIndex(rotationIndex, rotationDirection);


        for (int i = 0; i < data.wallKicks.GetLength(1); i++)
        {
            Vector2Int translation = data.wallKicks[wallKickIndex, i];


            if (Move(translation)) {
                return true;
            }
        }


        return false;
    }


    private int GetWallKickIndex(int rotationIndex, int rotationDirection)
    {
        int wallKickIndex = rotationIndex * 2;


        if (rotationDirection < 0) {
            wallKickIndex--;
        }


        return Wrap(wallKickIndex, 0, data.wallKicks.GetLength(0));
    }


    private int Wrap(int input, int min, int max)
    {
        if (input < min) {
            return max - (min - input) % (max - min);
        } else {
            return min + (input - min) % (max - min);
        }
    }




    // Add input to history and update visual
    private void AddInputToHistory(string input)
    {
        // Shift all elements to the left
        for (int i = 0; i < inputHistory.Length - 1; i++)
        {
            inputHistory[i] = inputHistory[i + 1];
        }


        // Add new input at the end
        inputHistory[inputHistory.Length - 1] = input;
    }




public bool ApplyAction(int act)
{
    switch (act)
    {
        case 0:
            return false; // nothing happened

        case 1:
            return Move(Vector2Int.left);  // returns true if moved

        case 2:
            return Move(Vector2Int.right); // returns true if moved

        case 3:
            return Rotate(1);              // returns true if success

        case 4:
            return Move(Vector2Int.down);  // soft drop, returns true if moved

        case 5:
            HardDrop();                    
            return true;                   // hard drop ALWAYS does something
    }

    return false;
}
}
