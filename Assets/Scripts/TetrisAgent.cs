using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;

[RequireComponent(typeof(BehaviorParameters))]
public class TetrisAgent : Agent
{
    public Board board; // assign in inspector
    public int decisionRepeat = 1; // how many FixedUpdate frames per decision (or use DecisionRequester)

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
    }

    public override void OnEpisodeBegin()
    {
        if (board != null)
        {
            //board.ResetBoard();
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (board == null) return;

        // 1) Flattened board grid
        int[] grid = board.GetGridState(); // length = width*height
        for (int i = 0; i < grid.Length; i++)
        {
            // Send as normalized float. Values 0..3 -> divide by 3.
            sensor.AddObservation(grid[i] / 3.0f);
        }

        // 2) Current piece id (one integer normalized)
        //int pieceId = board.GetCurrentPieceId(); // -1..6
        //sensor.AddObservation((pieceId + 1) / 8.0f); // normalize small range

        // 3) Optional: counts of lines cleared (normalized)
        sensor.AddObservation(board.GetNormalLinesCleared() / 100.0f);
        sensor.AddObservation(board.GetGarbageLinesCleared() / 100.0f);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        if (board == null) return;
        int act = actionBuffers.DiscreteActions[0];

        float rewardFromBoard = 0f;
        bool didSomething = false;

        switch (act)
        {
            case 0:
                // no-op
                break;
            case 1:
                //didSomething = board.TryMove(Vector3Int.left);
                break;
            case 2:
                //didSomething = board.TryMove(Vector3Int.right);
                break;
            case 3:
                //didSomething = board.TryRotate();
                break;
            case 4:
                //didSomething = board.SoftDrop();
                break;
            case 5:
                //board.HardDrop();
                didSomething = true;
                break;
        }

        // Small time penalty to encourage progress
        AddReward(-0.001f);

        // get reward from board (lines, garbage)
        //rewardFromBoard = board.ConsumeReward();
        if (rewardFromBoard != 0f)
        {
            AddReward(rewardFromBoard);
        }

        // End episode if game over
        //if (board.IsGameOver())
        {
            EndEpisode();
        }
    }

    // Optional: heuristic for debugging: map keyboard to actions
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
}