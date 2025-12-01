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
            Debug.LogError("TetrisAgent: Board reference not set.");
        if (piece == null)
            Debug.LogError("TetrisAgent: Piece reference not set.");
    }

    public override void OnEpisodeBegin()
    {
        Debug.Log("[AGENT] OnEpisodeBegin called");

        if (board != null)
        {
            board.ResetForEpisode();
        }
        else
        {
            Debug.LogError("[AGENT] Board reference NULL in OnEpisodeBegin!");
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Debug.Log("[OBS] CollectObservations CALLED with grid size " + board.GetGridState().Length);
        if (board == null) return;

        // 1) Flattened board grid
        //I CANNOT GET THIS TO WORK PROPERLY
        //int[] grid = board.GetContour();

        //full board
        int[] grid = board.GetGridState();

        for (int i = 0; i < grid.Length; i++)
        {
            sensor.AddObservation(grid[i] / 3.0f);
        }

        // 2) Current piece ID
        int pieceId = board.GetCurrentPieceId();
        sensor.AddObservation((pieceId + 1) / 8.0f);

        // 3) Lines cleared
        sensor.AddObservation(board.GetNormalLinesCleared() / 100.0f);
        sensor.AddObservation(board.GetGarbageLinesCleared() / 100.0f);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        AddReward(-0.0005f);
       // Debug.Log(">>> OnActionReceived CALLED with action " + actionBuffers.DiscreteActions[0]);

        if (board == null || piece == null)
            return;

        if (board.gameOver)
        {
            Debug.Log("[DONE] Game Over triggered -> Ending episode");
            AddReward(-10f);
            EndEpisode();
            return;
        }

        int act = actionBuffers.DiscreteActions[0];
       // Debug.Log("[ACT] Unity received action: " + act);

        bool didSomething = piece.ApplyAction(act);



        if(didSomething)
        {
            AddReward(-0.001f); // small penalty for making a move
        }

        float boardReward = board.ConsumeReward();
        if (Mathf.Abs(boardReward) > 0.0001f)
        {
            Debug.Log("[REWARD] " + boardReward);
            AddReward(boardReward);
        }
    }
}
