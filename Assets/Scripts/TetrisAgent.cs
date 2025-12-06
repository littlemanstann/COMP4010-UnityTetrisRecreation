using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class TetrisAgent : Agent
{
    [Header("References")]
    public Board board;
    public Piece piece;

    public override void Initialize()
    {
        if (board == null)
            Debug.LogError("TetrisAgent: Board reference not set.");
        if (piece == null)
            Debug.LogError("TetrisAgent: Piece reference not set.");
    }

    public override void OnEpisodeBegin()
    {
        if (board != null)
        {
            board.ResetForEpisode();
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (board == null)
            return;

        // Heights (0 to 20)
        int[] heights = board.GetColumnHeights();
        foreach (int h in heights)
            sensor.AddObservation(h / 20f);

        // Bumpiness (optional but safe)
        int[] diffs = board.GetContourDiffs();
        foreach (int d in diffs)
            sensor.AddObservation(d / 20f);

        // Holes (0 to ~40)
        sensor.AddObservation(board.CountHoles() / 40f);

        // Max height
        sensor.AddObservation(board.GetMaxHeight() / 20f);

        // Piece type one-hot
        int pieceId = board.GetCurrentPieceId();
        float[] onehot = new float[7];
        if (pieceId >= 0 && pieceId < 7)
            onehot[pieceId] = 1f;
        foreach (var v in onehot)
            sensor.AddObservation(v);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        if (board == null || piece == null)
            return;

        if (board.gameOver)
        {
            AddReward(-10f);  // Gentle terminal penalty
            EndEpisode();
            return;
        }

        int act = actionBuffers.DiscreteActions[0];

        // ACTIONS
        switch (act)
        {
            case 1: piece.ApplyAction(1); break; // move left
            case 2: piece.ApplyAction(2); break; // move right
            case 3: piece.ApplyAction(3); break; // rotate
            case 4: piece.ApplyAction(4); break; // soft drop
            // Hard drop intentionally disabled
        }

        // Always gravity
        piece.StepGravity();

        // Add survival reward (+0.01 each step)
        AddReward(+0.01f);

        // Add placement rewards from Board.cs
        float placementReward = board.ConsumeReward();
        if (Mathf.Abs(placementReward) > 1e-6f)
            AddReward(placementReward);
    }
}
