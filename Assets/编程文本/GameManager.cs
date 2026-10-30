using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("每关进化所需分数（按关卡顺序：第1关、第2关、第3关...）")]
    public int[] evolveScores = new int[] { 100, 200, 300 };

    [Header("开局生命")]
    public int startHealth = 3;

    public int CurrentScore { get; private set; }
    public int CurrentHealth { get; private set; }
    public int StageIndex { get; private set; }

    void Awake()
    {
        // 单例：保证全程只有一个，切场景不销毁
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        CurrentHealth = startHealth;

        // 自动生成全局音频和暂停菜单
        AudioManager.Ensure();
        if (PauseManager.Instance == null)
        {
            GameObject pauseGo = new GameObject("PauseManager");
            pauseGo.AddComponent<PauseManager>();
        }
    }

    public void AddScore(int amount)
    {
        CurrentScore += amount;
    }

    public void Damage()
    {
        CurrentHealth--;
    }

    // 当前关卡该攒多少分进化
    public int GetEvolveScore()
    {
        if (StageIndex < evolveScores.Length)
            return evolveScores[StageIndex];
        return evolveScores[evolveScores.Length - 1];
    }

    public void AdvanceStage()
    {
        StageIndex++;
    }

    // 真主菜单与局内菜单共用：由第一关自己的配置创建全新一局。
    public static void StartNewRun(string firstSceneName)
    {
        if (Instance != null)
        {
            GameObject previousRun = Instance.gameObject;
            Instance = null;
            Destroy(previousRun);
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(firstSceneName);
    }

    // 死亡重开：清掉所有进度
    public void RestartRun()
    {
        CurrentScore = 0;
        CurrentHealth = startHealth;
        StageIndex = 0;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
