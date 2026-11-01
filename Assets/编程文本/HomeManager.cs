using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

public class HomeManager : MonoBehaviour
{
    [Header("点Start后进入的场景名，必须和文件名一致")]
    public string startSceneName = "Level1";

    [SerializeField] private Transform menuRoot;

    private bool _clicked;
    private bool _initialized;
    private PauseManager _runMenuOwner;
    private VideoPlayer _videoPlayer;
    private RenderTexture _videoTexture;

    private bool IsRunMenu => menuRoot != null;

    void Start()
    {
        InitializeMenu();
    }

    private void InitializeMenu()
    {
        if (_initialized) return;
        _initialized = true;

        AudioManager.Ensure();
        SetupVideo();
        SetupStartButton();
        SetupHomeSettingsUI();
        if (IsRunMenu)
        {
            SetupRunResumeButton();
            FindInactive("SettingsButton")?.SetActive(false);
            _homeVolumePanel?.SetActive(false);
        }
    }

    public void ShowRunMenu(PauseManager owner)
    {
        _runMenuOwner = owner;
        gameObject.SetActive(true);
        InitializeMenu();
        _clicked = false;
        CloseSettings();
        RefreshHomeVolume();
        _videoPlayer?.Play();
    }

    public void HideRunMenu()
    {
        CloseSettings();
        _videoPlayer?.Pause();
        gameObject.SetActive(false);
    }

    public bool CloseSettings()
    {
        if (_homeVolumePanel == null || !_homeVolumePanel.activeSelf) return false;
        _homeVolumePanel.SetActive(false);
        return true;
    }

    public void OnResumeButton()
    {
        if (_runMenuOwner == null) return;
        AudioManager.Instance?.PlayClick();
        _runMenuOwner.Resume();
    }

    private void SetupVideo()
    {
        // 不用 Camera Far Plane，改成 UI RawImage，最稳
        Canvas canvas = FindMenuComponent<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("HomeCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (menuRoot != null) canvasGo.transform.SetParent(menuRoot, false);
            else SceneManager.MoveGameObjectToScene(canvasGo, gameObject.scene);
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        // 复用已有 RawImage，避免重复建
        GameObject rawGo = FindInactive("HomeVideoRawImage");
        if (rawGo == null)
        {
            rawGo = new GameObject("HomeVideoRawImage", typeof(RectTransform), typeof(RawImage));
            rawGo.transform.SetParent(canvas.transform, false);
            rawGo.transform.SetAsFirstSibling();
        }

        RectTransform rt = (RectTransform)rawGo.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        _videoTexture = new RenderTexture(1920, 1080, 0);
        _videoTexture.Create();
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = _videoTexture;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = previous;

        RawImage raw = rawGo.GetComponent<RawImage>();
        raw.texture = _videoTexture;
        raw.color = Color.white;
        raw.raycastTarget = true;

        GameObject videoGo = FindInactive("VideoBackground");
        if (videoGo == null)
        {
            videoGo = new GameObject("VideoBackground");
            if (menuRoot != null) videoGo.transform.SetParent(menuRoot, false);
            else SceneManager.MoveGameObjectToScene(videoGo, gameObject.scene);
        }

        VideoPlayer vp = videoGo.GetComponent<VideoPlayer>();
        if (vp == null) vp = videoGo.AddComponent<VideoPlayer>();

        // 如果你已经在 VideoPlayer 上挂了视频，就尊重你挂的，不乱换
        if (vp.clip == null)
        {
            string videoPath = System.IO.Path.Combine(
                Application.dataPath,
                "美术资源/背景/404D258C-5705-4801-9A6D-4311511430EC.mp4");

            if (System.IO.File.Exists(videoPath))
            {
                vp.source = VideoSource.Url;
                vp.url = videoPath;
            }
            else
            {
                vp.source = VideoSource.VideoClip;
                vp.clip = Resources.Load<VideoClip>("Videos/home");
            }
        }

        vp.playOnAwake = true;
        vp.isLooping = true;
        vp.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        vp.renderMode = VideoRenderMode.RenderTexture;
        vp.targetTexture = _videoTexture;
        vp.audioOutputMode = VideoAudioOutputMode.None;
        _videoPlayer = vp;
        vp.Play();
    }

    private void SetupStartButton()
    {
        Button btn = FindInactive("StartButton")?.GetComponent<Button>();
        if (btn == null) return;

        btn.onClick.RemoveListener(OnStartButton);
        btn.onClick.AddListener(OnStartButton);

        Image img = btn.GetComponent<Image>();
        Sprite sprite = LoadSprite(IsRunMenu ? "start_new_game" : "start");
        if (img != null && sprite != null)
        {
            img.sprite = sprite;
        }

        // 强制居中锚点 + 固定大小，防止它跟着原图或旧 RectTransform 变大
        RectTransform btnRt = (RectTransform)btn.transform;
        btnRt.anchorMin = new Vector2(0.5f, 0.5f);
        btnRt.anchorMax = new Vector2(0.5f, 0.5f);
        btnRt.pivot = new Vector2(0.5f, 0.5f);
        btnRt.sizeDelta = new Vector2(320f, 150f);
        btnRt.anchoredPosition = IsRunMenu ? new Vector2(0f, -175f) : Vector2.zero;
        btnRt.localScale = Vector3.one;

        // 直接删掉按钮上自带的 "Button" 文字子物体，不是只清空
        for (int i = btn.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = btn.transform.GetChild(i);
            if (child.GetComponent<Text>() != null || child.GetComponent<TMP_Text>() != null)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void SetupRunResumeButton()
    {
        Canvas canvas = FindMenuComponent<Canvas>();
        if (canvas == null) return;

        GameObject resumeGo = FindInactive("RunResumeButton");
        if (resumeGo == null)
        {
            resumeGo = new GameObject("RunResumeButton", typeof(RectTransform), typeof(Image), typeof(Button));
            resumeGo.transform.SetParent(canvas.transform, false);
        }

        RectTransform rt = (RectTransform)resumeGo.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(320f, 150f);
        rt.localScale = Vector3.one;

        ApplyHomeSprite(resumeGo.GetComponent<Image>(), "resume");
        Button resume = resumeGo.GetComponent<Button>();
        resume.onClick.RemoveListener(OnResumeButton);
        resume.onClick.AddListener(OnResumeButton);
    }

    private Sprite LoadSprite(string name)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>("UI/" + name);
        return sprites != null && sprites.Length > 0 ? sprites[0] : null;
    }

    private void ClearButtonText(Button btn)
    {
        foreach (Text t in btn.GetComponentsInChildren<Text>(true)) t.text = "";
        foreach (TMP_Text t in btn.GetComponentsInChildren<TMP_Text>(true)) t.text = "";
    }

    // Home 右下角设置按钮 + 音量调节
    private Text _homeBgmValue;
    private Text _homeSfxValue;
    private GameObject _homeVolumePanel;

    private GameObject FindInactive(string name)
    {
        if (menuRoot != null)
        {
            foreach (Transform t in menuRoot.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t.gameObject;
            return null;
        }

        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t.gameObject;
        return null;
    }

    private T FindMenuComponent<T>() where T : Component
    {
        if (menuRoot != null) return menuRoot.GetComponentInChildren<T>(true);

        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null) return component;
        }
        return null;
    }

    private void SetupHomeSettingsUI()
    {
        Canvas canvas = FindMenuComponent<Canvas>();
        if (canvas == null) return;

        // 设置图标按钮：已存在就复用，不再新建
        GameObject settingsGo = FindInactive("SettingsButton");
        if (settingsGo == null)
        {
            settingsGo = new GameObject("SettingsButton", typeof(RectTransform), typeof(Image), typeof(Button));
            settingsGo.transform.SetParent(canvas.transform, false);
            RectTransform srt = (RectTransform)settingsGo.transform;
            srt.anchorMin = new Vector2(1f, 0f);
            srt.anchorMax = new Vector2(1f, 0f);
            srt.pivot = new Vector2(1f, 0f);
            srt.anchoredPosition = new Vector2(-50f, 50f);
            srt.sizeDelta = new Vector2(100f, 100f);
        }
        ApplyHomeSprite(settingsGo.GetComponent<Image>(), "settings");

        Button settingsBtn = settingsGo.GetComponent<Button>();
        settingsBtn.onClick.RemoveListener(ToggleSettings);
        settingsBtn.onClick.AddListener(ToggleSettings);

        // 音量面板：已存在就复用
        GameObject panel = FindInactive("HomeVolumePanel");
        bool newPanel = panel == null;
        if (newPanel)
        {
            panel = new GameObject("HomeVolumePanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            RectTransform prt = (RectTransform)panel.transform;
            prt.anchorMin = new Vector2(1f, 0f);
            prt.anchorMax = new Vector2(1f, 0f);
            prt.pivot = new Vector2(1f, 0f);
            prt.anchoredPosition = new Vector2(-180f, 180f);
            prt.sizeDelta = new Vector2(340f, 200f);
            panel.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);
            panel.SetActive(false);

            CreateHomeVolumeRow(panel.transform, "music", 50, out _homeBgmValue, true);
            CreateHomeVolumeRow(panel.transform, "sound", 120, out _homeSfxValue, false);
        }
        _homeVolumePanel = panel;

        if (!newPanel)
        {
            _homeBgmValue = panel.transform.Find("BGMRow/Value")?.GetComponent<Text>();
            _homeSfxValue = panel.transform.Find("SFXRow/Value")?.GetComponent<Text>();
            WireExistingHomeRow(panel.transform.Find("BGMRow"), true);
            WireExistingHomeRow(panel.transform.Find("SFXRow"), false);
        }

        RefreshHomeVolume();
    }

    private void ToggleSettings()
    {
        if (_homeVolumePanel == null) return;
        AudioManager.Instance?.PlayClick();
        _homeVolumePanel.SetActive(!_homeVolumePanel.activeSelf);
        if (_homeVolumePanel.activeSelf) RefreshHomeVolume();
    }

    private void CreateHomeVolumeRow(Transform parent, string iconName, float y, out Text valueText, bool isBgm)
    {
        GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(parent, false);
        RectTransform irt = (RectTransform)iconGo.transform;
        irt.anchoredPosition = new Vector2(-120f, y);
        irt.sizeDelta = new Vector2(50f, 50f);
        ApplyHomeSprite(iconGo.GetComponent<Image>(), iconName);

        Button minus = MakeHomeSmallButton(parent, "-", new Vector2(-50f, y));
        minus.onClick.AddListener(() =>
        {
            if (isBgm) AudioManager.Instance?.BgmDown();
            else AudioManager.Instance?.SfxDown();
            RefreshHomeVolume();
        });

        valueText = MakeHomeLabel(parent, "8", new Vector2(20f, y));

        Button plus = MakeHomeSmallButton(parent, "+", new Vector2(90f, y));
        plus.onClick.AddListener(() =>
        {
            if (isBgm) AudioManager.Instance?.BgmUp();
            else AudioManager.Instance?.SfxUp();
            RefreshHomeVolume();
        });
    }

    private Button MakeHomeSmallButton(Transform parent, string text, Vector2 pos)
    {
        GameObject go = new GameObject("SmallBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(50f, 45f);
        go.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.8f, 1f);

        Text t = MakeHomeLabel(go.transform, text, Vector2.zero);
        return go.GetComponent<Button>();
    }

    private Text MakeHomeLabel(Transform parent, string text, Vector2 pos)
    {
        GameObject go = new GameObject("Label", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(60f, 45f);
        Text t = go.GetComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 26;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        return t;
    }

    private void WireExistingHomeRow(Transform row, bool isBgm)
    {
        if (row == null) return;

        Button minus = row.Find("Minus")?.GetComponent<Button>();
        Button plus = row.Find("Plus")?.GetComponent<Button>();

        if (minus != null)
        {
            EnsureHomeText(minus, "-");
            minus.onClick.RemoveAllListeners();
            minus.onClick.AddListener(() =>
            {
                if (isBgm) AudioManager.Instance?.BgmDown();
                else AudioManager.Instance?.SfxDown();
                RefreshHomeVolume();
            });
        }

        if (plus != null)
        {
            EnsureHomeText(plus, "+");
            plus.onClick.RemoveAllListeners();
            plus.onClick.AddListener(() =>
            {
                if (isBgm) AudioManager.Instance?.BgmUp();
                else AudioManager.Instance?.SfxUp();
                RefreshHomeVolume();
            });
        }
    }

    private void EnsureHomeText(Button btn, string content)
    {
        Text t = btn.GetComponentInChildren<Text>();
        if (t == null)
        {
            GameObject go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(btn.transform, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            t = go.GetComponent<Text>();
        }
        t.text = content;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 28;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;

        Image img = btn.GetComponent<Image>();
        if (img != null && img.sprite == null)
            img.color = new Color(0.2f, 0.5f, 0.8f, 1f);
    }

    private void RefreshHomeVolume()
    {
        if (AudioManager.Instance == null) return;
        if (_homeBgmValue != null) _homeBgmValue.text = AudioManager.Instance.GetBgmText();
        if (_homeSfxValue != null) _homeSfxValue.text = AudioManager.Instance.GetSfxText();
    }

    private void ApplyHomeSprite(Image img, string name)
    {
        Sprite s = LoadSprite(name);
        if (s != null) img.sprite = s;
    }

    public void OnStartButton()
    {
        if (_clicked) return;
        _clicked = true;

        AudioManager.Instance?.PlayClick();
        GameManager.StartNewRun(startSceneName);
    }

    void OnDestroy()
    {
        if (_videoTexture == null) return;
        if (_videoPlayer != null && _videoPlayer.targetTexture == _videoTexture)
        {
            _videoPlayer.Stop();
            _videoPlayer.targetTexture = null;
        }
        _videoTexture.Release();
        Destroy(_videoTexture);
        _videoTexture = null;
    }
}
