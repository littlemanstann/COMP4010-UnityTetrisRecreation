using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;

[RequireComponent(typeof(BehaviorParameters))]
public class TetrisAgent : Agent
{
    [Header("References")]
    public Board board; // assign in inspector
    public Piece piece; // current active piece

    // Action mapping:
    // 0 = No-op
    // 1 = Left
    // 2 = Right
    // 3 = Rotate
    // 4 = SoftDrop (one step)
    // 5 = HardDrop

    public override void Initialize()
    {
        if (board == null)
        {
            Debug.LogError("TetrisAgent: Board reference not set.");
        }

        if (piece == null)
        {
            Debug.LogError("TetrisAgent: Piece reference not set.");
        }
    }

    // If you have a proper reset function on your Board, you can use this:
    /*
    public override void OnEpisodeBegin()
    {
        if (board != null)
        {
            // TODO: call your own reset / restart logic here, e.g.:
            // board.ResetBoard();
            // board.SpawnNewPiece();
        }
    }
    */

    public override void CollectObservations(VectorSensor sensor)
    {
        if (board == null) return;

        // 1) Flattened board grid
        int[] grid = board.GetContour(); // length = width * height
        for (int i = 0; i < grid.Length; i++)
        {
            // Values assumed in 0..3 -> normalize
            sensor.AddObservation(grid[i] / 3.0f);
        }

        // 2) Current piece id (one integer normalized)
        int pieceId = board.GetCurrentPieceId(); // e.g., -1..6
        sensor.AddObservation((pieceId + 1) / 8.0f);

        // 3) Counts of lines cleared (normalized)
        sensor.AddObservation(board.GetNormalLinesCleared() / 100.0f);
        sensor.AddObservation(board.GetGarbageLinesCleared() / 100.0f);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        Debug.Log("TetrisAgent received an action!");
        if (board == null || piece == null)
            return;

        // If game is over, end the episode and bail out
        if (board.gameOver)
        {
            Debug.Log("[DONE] Game Over triggered -> Ending episode");
            SetReward(-1f);
            EndEpisode();
            return;
        }

        int act = actionBuffers.DiscreteActions[0];
        Debug.Log("[ACT] Unity received action: " + act);

        bool didSomething = false;

        switch (act)
        {
            case 0:
                // no-op
                break;
            case 1:
                didSomething = piece.Move(Vector2Int.left);
                break;
            case 2:
                didSomething = piece.Move(Vector2Int.right);
                break;
            case 3:
                didSomething = piece.Rotate(1);
                break;
            case 4:
                didSomething = piece.Move(Vector2Int.down);
                break;
            case 5:
                piece.HardDrop();
                didSomething = true;
                break;
        }

        // Small time penalty to encourage faster clearing / avoiding stalling
        AddReward(-0.001f);

        // Get reward from board (lines cleared, etc.)
        float rewardFromBoard = board.ConsumeReward();
        if (Mathf.Abs(rewardFromBoard) > 0.0001f)
        {
            Debug.Log("[REWARD] " + rewardFromBoard);
            AddReward(rewardFromBoard);
        }

        // Optional: if a move failed, you could penalize it slightly
        // if (!didSomething && act != 0)
        // {
        //     AddReward(-0.0005f);
        // }
    }




    // We no longer need FixedUpdate to manage gameOver/end episode,
    // since that is handled cleanly in OnActionReceived.
    // ML-Agents will call OnActionReceived as long as a DecisionRequester
    // is attached or you manually RequestDecision().

    // Optional: heuristic for debugging: map keyboard to actions
    /*
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discrete = actionsOut.DiscreteActions;
        int a = 0;
        if (Input.GetKey(KeyCode.LeftArrow)) a = 1;
        else if (Input.GetKey(KeyCode.RightArrow)) a = 2;
        else if (Input.GetKey(KeyCode.UpArrow)) a = 3; // rotate
        else if (Input.GetKey(KeyCode.DownArrow)) a = 4; // soft drop
        else if (Input.GetKeyDown(KeyCode.Space)) a = 5; // hard drop

        discrete[0] = a;
    }
    */
}
