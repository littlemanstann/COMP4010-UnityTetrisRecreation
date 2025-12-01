using UnityEditor;
using Unity.MLAgents;

[InitializeOnLoad]
public class MLAEditorQuitFix
{
    static MLAEditorQuitFix()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.ExitingPlayMode)
        {
            try
            {
                Academy.Instance.Dispose();
            }
            catch { }
        }
    }
}