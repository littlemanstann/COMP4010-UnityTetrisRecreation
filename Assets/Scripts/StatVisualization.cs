using TMPro;
using Unity.AppUI.UI;
using UnityEngine;

public class StatVisualization : MonoBehaviour
{
    public Board board;
    public TetrisAgent agent;
    public Piece piece;

    // Text elements to display stats
    public TextMeshProUGUI Episodes;
    public TextMeshProUGUI CurrentSteps;
    public TextMeshProUGUI TotalSteps;
    public TextMeshProUGUI Contour;
    public TextMeshProUGUI NormaLinesCleared;
    public TextMeshProUGUI GarbageLinesCleared;
    public TextMeshProUGUI ActionHistory;

    private void Update()
    {
        if (board == null)
            return;

        // Update the text elements with the latest stats from the board
        Episodes.text = $"Episodes: {agent.currentEpisode}";
        TotalSteps.text = $"Total Steps: {agent.totalSteps}";
        CurrentSteps.text = $"Current Steps: {agent.currentSteps}";
        Contour.text = $"Contour:\n[" + string.Join(", ", board.GetContour()) + "]";
        NormaLinesCleared.text = $"Normal Lines Cleared: {board.totalNormalLinesCleared}";
        GarbageLinesCleared.text = $"Garbage Lines Cleared: {board.totalGarbageLinesCleared}";
        ActionHistory.text = "Action History: " + string.Join(", ", piece.actionHistory);
    }
}
