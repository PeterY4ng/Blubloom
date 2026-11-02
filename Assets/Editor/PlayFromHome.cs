#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// Normal Play always begins at the real menu, even after inspecting a level.
[InitializeOnLoad]
public static class PlayFromHome
{
    private const string HomePath = "Assets/Scenes/Home.unity";
    private const string SessionKey = "Blubloom.OpenedHomeThisEditorSession";

    static PlayFromHome()
    {
        EditorApplication.delayCall += Configure;
    }

    private static void Configure()
    {
        SceneAsset home = AssetDatabase.LoadAssetAtPath<SceneAsset>(HomePath);
        if (home == null) return;
        EditorSceneManager.playModeStartScene = home;

        if (SessionState.GetBool(SessionKey, false)) return;
        SessionState.SetBool(SessionKey, true);
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) return;
        EditorSceneManager.OpenScene(HomePath, OpenSceneMode.Single);
    }
}
#endif
