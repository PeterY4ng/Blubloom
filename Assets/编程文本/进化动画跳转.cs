using UnityEngine;
using UnityEngine.SceneManagement;

public class EvolutionAutoSkip : MonoBehaviour
{
    [Header("几秒后自动跳转")]
    public float delay = 5f;

    [Header("跳到哪个场景")]
    public string nextScene = "Story1";

    void Start()
    {
        Invoke(nameof(GoNext), delay);
    }

    void GoNext()
    {
        SceneManager.LoadScene(nextScene);
    }
}
