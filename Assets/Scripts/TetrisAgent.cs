using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class TetrisAgent : Agent
{
    private int lastAction = -1;
    [Header("References")]
    public Board board; 
    public Piece piece; 

    public override void Initialize()
    {
        if (board == null) Debug.LogError("TetrisAgent: Board reference not set.");
        if (piece == null) Debug.LogError("TetrisAgent: Piece reference not set.");
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
        if (board == null) return;

        //  Heights (normalized 0–20) ---
        int[] heights = board.GetColumnHeights();
        foreach (int h in heights)
            sensor.AddObservation(h / 20f);

        //Bumpiness 
        int[] diffs = board.GetContourDiffs();
        foreach (int d in diffs)
            sensor.AddObservation(d / 20f);

        // Hole count 
        int holes = board.CountHoles();
        sensor.AddObservation(holes / 40f);

        //Max height
        sensor.AddObservation(board.GetMaxHeight() / 20f);

        // Piece type 
        int pieceId = board.GetCurrentPieceId();
        float[] onehot = new float[7];
        if (pieceId >= 0 && pieceId < 7) onehot[pieceId] = 1f;
        foreach (float v in onehot) sensor.AddObservation(v);
    }


    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        if (board == null || piece == null)
            return;

        if (board.gameOver)
        {
            AddReward(-1.0f);
            EndEpisode();
            return;
        }

        int act = actionBuffers.DiscreteActions[0];

        AddReward(-0.0005f);


        if (act == lastAction)
        {
            AddReward(-0.001f);
        }
        lastAction = act;

        switch (act)
        {
            case 1: 
                piece.ApplyAction(1);
                break;
            case 2: 
                piece.ApplyAction(2);
                break;
            case 3: 
                piece.ApplyAction(3);
                break;
            case 4: 
                piece.ApplyAction(4);
                break;
            case 5: 
                piece.ApplyAction(5);
                break;

        }


        piece.StepGravity();


        float accumulatedReward = board.ConsumeReward();
        if (Mathf.Abs(accumulatedReward) > 1e-6f)
        {
            AddReward(accumulatedReward);
        }
    }
}