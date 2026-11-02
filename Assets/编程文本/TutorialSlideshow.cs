using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TutorialSlideshow : MonoBehaviour
{
    public Sprite[] pages;
    public Sprite nextButtonSprite;
    public string nextSceneName = "Level1";

    public int CurrentPage { get; private set; }

    private const float InitialInputDelay = 0.25f;
    private Image _pageImage;
    private Image _advanceImage;
    private Button _advanceButton;
    private float _inputReadyAt;
    private int _lastAdvanceFrame = -1;
    private bool _hasValidPage;
    private bool _loading;

    void Start()
    {
        Time.timeScale = 1f;
        CurrentPage = 0;
        _inputReadyAt = Time.unscaledTime + InitialInputDelay;
        EnsureEventSystem();
        CreateUI();
        ShowCurrentPage();
    }

    void Update()
    {
        if (!_hasValidPage || _loading) return;

        bool inputReady = Time.unscaledTime >= _inputReadyAt;
        _advanceButton.interactable = inputReady;
    }

    public void Advance()
    {
        if (!_hasValidPage || _loading || Time.unscaledTime < _inputReadyAt ||
            _lastAdvanceFrame == Time.frameCount) return;

        _lastAdvanceFrame = Time.frameCount;
        AudioManager.Instance?.PlayClick();

        if (CurrentPage < pages.Length - 1)
        {
            CurrentPage++;
            ShowCurrentPage();
            return;
        }

        _loading = true;
        _advanceButton.interactable = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(nextSceneName);
    }

    private void CreateUI()
    {
        GameObject canvasGo = new GameObject("TutorialCanvas", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Image background = CreateFullScreenImage("OceanBackground", canvasGo.transform);
        background.color = new Color(0.04f, 0.25f, 0.36f, 1f);
        background.raycastTarget = true;

        _pageImage = CreateFullScreenImage("TutorialPage", canvasGo.transform);
        _pageImage.preserveAspect = true;
        _pageImage.raycastTarget = false;

        GameObject buttonGo = new GameObject("AdvanceButton", typeof(RectTransform),
            typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(canvasGo.transform, false);
        RectTransform buttonRect = (RectTransform)buttonGo.transform;
        buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(1f, 0f);
        buttonRect.pivot = new Vector2(1f, 0f);
        buttonRect.anchoredPosition = new Vector2(-64f, 48f);
        buttonRect.sizeDelta = new Vector2(320f, 150f);

        _advanceImage = buttonGo.GetComponent<Image>();
        _advanceImage.preserveAspect = true;
        _advanceButton = buttonGo.GetComponent<Button>();
        Navigation navigation = _advanceButton.navigation;
        navigation.mode = Navigation.Mode.None;
        _advanceButton.navigation = navigation;
        _advanceButton.interactable = false;
        _advanceButton.onClick.AddListener(Advance);
    }

    private static Image CreateFullScreenImage(string objectName, Transform parent)
    {
        GameObject imageGo = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageGo.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)imageGo.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return imageGo.GetComponent<Image>();
    }

    private void ShowCurrentPage()
    {
        _hasValidPage = pages != null && CurrentPage < pages.Length && pages[CurrentPage] != null;
        if (!_hasValidPage)
        {
            _advanceButton.interactable = false;
            Debug.LogError("教学页未配置，无法显示当前教学内容。", this);
            return;
        }

        _pageImage.sprite = pages[CurrentPage];
        _advanceImage.sprite = nextButtonSprite;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null || Object.FindFirstObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }
}
