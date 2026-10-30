using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

// 全局暂停菜单管理器：跨场景存在。
// 暂停设置使用关卡原有 UI；局内退出菜单在当前关卡中按需创建，不卸载关卡。
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;

    [Header("退回到哪个主场景")]
    public string homeSceneName = "Home";

    private bool _paused;
    private float _timeScaleBeforePause = 1f;
    private bool _runMenuOpen;
    private HomeManager _runMenu;
    private GameObject _runMenuObject;
    private readonly List<PlayerMovement> _suspendedPlayers = new List<PlayerMovement>();
    private bool _pauseButtonWasActive;
    private bool _hasSavedPauseButtonVisibility;
    private GameObject _pausePanel;
    private Button _resumeButton;
    private Button _quitButton;
    private Button _pauseButton;
    private Slider _bgmSlider;
    private Slider _sfxSlider;

    public bool IsRunMenuOpen => _runMenuOpen;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void Start() => BindCurrentScene();

    void Update()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.escapeKey.wasPressedThisFrame ||
            Keyboard.current.pKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 菜单及玩家属于旧关卡。切场景时不再恢复旧关卡的组件或 UI。
        _runMenuOpen = false;
        _runMenu = null;
        _runMenuObject = null;
        _suspendedPlayers.Clear();
        _hasSavedPauseButtonVisibility = false;
        _paused = false;
        _timeScaleBeforePause = 1f;
        Time.timeScale = 1f;
        BindCurrentScene();
    }

    // 每次切场景后，重新找一遍当前场景里的暂停 UI 并绑事件
    private void BindCurrentScene()
    {
        AudioManager.Ensure();

        _pausePanel = FindInactive("PausePanel");
        if (_pausePanel != null) _pausePanel.SetActive(false);

        _resumeButton = FindButton("ResumeButton");
        if (_resumeButton != null)
        {
            _resumeButton.onClick.RemoveListener(Resume);
            _resumeButton.onClick.AddListener(Resume);
        }

        _quitButton = FindButton("QuitButton");
        if (_quitButton != null)
        {
            _quitButton.onClick.RemoveListener(QuitToHome);
            _quitButton.onClick.AddListener(QuitToHome);
        }

        // 音量滑条：在 BGMRow / SFXRow 下找名为 Slider 的子物体
        _bgmSlider = FindSliderUnder("BGMRow");
        _sfxSlider = FindSliderUnder("SFXRow");

        if (_bgmSlider != null)
        {
            _bgmSlider.onValueChanged.RemoveAllListeners();
            _bgmSlider.onValueChanged.AddListener(v =>
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.SetBgmVolume(Mathf.RoundToInt(v));
            });
            if (AudioManager.Instance != null)
                _bgmSlider.value = AudioManager.Instance.GetBgmVolume01() * 10f;
        }

        if (_sfxSlider != null)
        {
            _sfxSlider.onValueChanged.RemoveAllListeners();
            _sfxSlider.onValueChanged.AddListener(v =>
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.SetSfxVolume(Mathf.RoundToInt(v));
            });
            if (AudioManager.Instance != null)
                _sfxSlider.value = AudioManager.Instance.GetSfxVolume01() * 10f;
        }

        // 右上角暂停按钮（只在 Level 场景显示）
        _pauseButton = FindButton("PauseButton");
        bool inLevel = SceneManager.GetActiveScene().name.StartsWith("Level");
        if (_pauseButton != null)
        {
            _pauseButton.gameObject.SetActive(inLevel);
            _pauseButton.onClick.RemoveListener(TogglePause);
            _pauseButton.onClick.AddListener(TogglePause);
        }
    }

    private Slider FindSliderUnder(string rowName)
    {
        GameObject row = FindInactive(rowName);
        if (row == null) return null;
        Transform sliderT = row.transform.Find("Slider");
        return sliderT != null ? sliderT.GetComponent<Slider>() : null;
    }

    private GameObject FindInactive(string objectName)
    {
        Scene currentScene = SceneManager.GetActiveScene();

        // 同名音量控件优先取关卡原暂停面板，避免与局内菜单互相绑定。
        if (_pausePanel != null && _pausePanel.scene == currentScene)
        {
            foreach (Transform t in _pausePanel.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == objectName) return t.gameObject;
            }
        }

        foreach (GameObject root in currentScene.GetRootGameObjects())
        {
            if (root == _runMenuObject) continue;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (_runMenuObject != null && t.IsChildOf(_runMenuObject.transform)) continue;
                if (t.name == objectName) return t.gameObject;
            }
        }
        return null;
    }

    private Button FindButton(string objectName)
    {
        GameObject go = FindInactive(objectName);
        return go != null ? go.GetComponent<Button>() : null;
    }

    public void TogglePause()
    {
        AudioManager.Instance?.PlayClick();

        if (_runMenuOpen)
        {
            if (_runMenu != null && _runMenu.CloseSettings()) return;
            Resume();
            return;
        }

        if (_paused)
        {
            Resume();
            return;
        }

        BeginPause();
        if (_pausePanel != null)
        {
            _pausePanel.SetActive(true);
            RefreshSliderValues();
        }
    }

    private void BeginPause()
    {
        if (!_paused)
            _timeScaleBeforePause = Time.timeScale > 0f ? Time.timeScale : 1f;
        _paused = true;
        Time.timeScale = 0f;
    }

    private void RefreshSliderValues()
    {
        if (AudioManager.Instance == null) return;
        if (_bgmSlider != null) _bgmSlider.value = AudioManager.Instance.GetBgmVolume01() * 10f;
        if (_sfxSlider != null) _sfxSlider.value = AudioManager.Instance.GetSfxVolume01() * 10f;
    }

    public void Resume()
    {
        bool wasPaused = _paused;
        _runMenuOpen = false;
        if (_runMenu != null) _runMenu.HideRunMenu();
        RestorePlayerInput();
        RestorePauseButtonVisibility();
        _paused = false;
        if (_pausePanel != null) _pausePanel.SetActive(false);
        if (wasPaused) Time.timeScale = _timeScaleBeforePause;
    }

    public void QuitToHome()
    {
        AudioManager.Instance?.PlayClick();
        if (!SceneManager.GetActiveScene().name.StartsWith("Level"))
        {
            Resume();
            Time.timeScale = 1f;
            SceneManager.LoadScene(homeSceneName);
            return;
        }

        // 同一关内反复打开菜单始终复用同一个对象和暂停前的时间比例。
        if (_runMenuOpen) return;
        BeginPause();
        if (!TryCreateRunMenu())
        {
            RestorePauseAfterMenuFailure();
            return;
        }

        SuspendPlayerInput();
        if (_pauseButton != null)
        {
            _pauseButtonWasActive = _pauseButton.gameObject.activeSelf;
            _hasSavedPauseButtonVisibility = true;
            _pauseButton.gameObject.SetActive(false);
        }
        if (_pausePanel != null) _pausePanel.SetActive(false);
        _runMenuOpen = true;

        try
        {
            _runMenu.ShowRunMenu(this);
            Time.timeScale = 0f;
        }
        catch (System.Exception exception)
        {
            Debug.LogError("无法打开局内菜单，已恢复暂停设置。", this);
            Debug.LogException(exception, this);
            RestorePauseAfterMenuFailure();
        }
    }

    private bool TryCreateRunMenu()
    {
        if (_runMenu != null) return true;

        GameObject prefab = Resources.Load<GameObject>("UI/RunMenu");
        if (prefab == null || prefab.GetComponent<HomeManager>() == null)
        {
            Debug.LogError("局内菜单缺少 Resources/UI/RunMenu.prefab 或根节点 HomeManager，保留当前关卡和暂停设置。", this);
            return false;
        }

        _runMenuObject = Instantiate(prefab);
        SceneManager.MoveGameObjectToScene(_runMenuObject, SceneManager.GetActiveScene());
        _runMenu = _runMenuObject.GetComponent<HomeManager>();
        return true;
    }

    private void SuspendPlayerInput()
    {
        _suspendedPlayers.Clear();
        Scene currentScene = SceneManager.GetActiveScene();
        foreach (PlayerMovement movement in Object.FindObjectsByType<PlayerMovement>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (movement.gameObject.scene != currentScene || !movement.enabled) continue;
            _suspendedPlayers.Add(movement);
            movement.enabled = false;
        }
    }

    private void RestorePlayerInput()
    {
        foreach (PlayerMovement movement in _suspendedPlayers)
            if (movement != null) movement.enabled = true;
        _suspendedPlayers.Clear();
    }

    private void RestorePauseButtonVisibility()
    {
        if (_hasSavedPauseButtonVisibility && _pauseButton != null)
            _pauseButton.gameObject.SetActive(_pauseButtonWasActive);
        _hasSavedPauseButtonVisibility = false;
    }

    private void RestorePauseAfterMenuFailure()
    {
        _runMenuOpen = false;
        if (_runMenuObject != null) _runMenuObject.SetActive(false);
        RestorePlayerInput();
        RestorePauseButtonVisibility();
        if (_pausePanel != null)
        {
            _pausePanel.SetActive(true);
            RefreshSliderValues();
            Time.timeScale = 0f;
        }
        else
        {
            // 缺少暂停 UI 时至少允许玩家继续，而不是停在不可操作的画面。
            Resume();
        }
    }
}
