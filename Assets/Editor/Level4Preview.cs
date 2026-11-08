#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
internal static class Level4Preview
{
    private const string MenuPath = "Tools/Blubloom/Preview Level 3 to 4";
    private const string PreviewPath = "Assets/Scenes/Evolution3To4.unity";
    private const string PreviewActive = "Blubloom.Level4Preview.Active";
    private const string PreviousStartPath = "Blubloom.Level4Preview.PreviousStartPath";

    static Level4Preview()
    {
        EditorApplication.playModeStateChanged += RestorePlayStart;
    }

    [MenuItem(MenuPath, true)]
    private static bool CanPreview()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling;
    }

    [MenuItem(MenuPath)]
    private static void Preview()
    {
        SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(PreviewPath);
        if (scene == null) return;

        SceneAsset previous = EditorSceneManager.playModeStartScene;
        SessionState.SetString(PreviousStartPath, previous == null ? "" : AssetDatabase.GetAssetPath(previous));
        SessionState.SetBool(PreviewActive, true);
        EditorSceneManager.playModeStartScene = scene;
        EditorApplication.EnterPlaymode();
    }

    private static void RestorePlayStart(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(PreviewActive, false)) return;

        string previousPath = SessionState.GetString(PreviousStartPath, "");
        EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previousPath)
            ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previousPath);
        SessionState.EraseBool(PreviewActive);
        SessionState.EraseString(PreviousStartPath);
    }
}
#endif
