using UnityEngine;
using UnityEngine.SceneManagement;

public class EvolutionAutoSkip : MonoBehaviour
{
    [Header("旧场景时长配置（保留兼容，不再自动跳转）")]
    public float delay = 5f;

    [Header("跳到哪个场景")]
    public string nextScene = "Story1";

    private const float ContinueInputDelay = 0.25f;
    private float _readyAt;
    private bool _loaded;

    void Awake()
    {
        _readyAt = Time.unscaledTime + ContinueInputDelay;
    }

    void Start()
    {
        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (canvas.gameObject.scene != gameObject.scene || !canvas.isActiveAndEnabled) continue;
            CutsceneManager.CreateNextButton(canvas, GoNext);
            break;
        }
    }

    public void GoNext()
    {
        if (_loaded || Time.unscaledTime < _readyAt) return;
        _loaded = true;
        SceneManager.LoadScene(nextScene);
    }
}
