using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CutsceneManager : MonoBehaviour
{
    [Header("旧场景时长配置（保留兼容，不再自动跳转）")]
    public float showTime = 12f;

    [Header("看完进入哪个场景")]
    public string nextSceneName = "Level2";

    private const float ContinueInputDelay = 0.25f;
    private float _readyAt;
    private bool _loaded;

    void Awake()
    {
        _readyAt = Time.unscaledTime + ContinueInputDelay;
    }

    void Start()
    {
        AudioManager.Instance?.PlayLevelUp();
        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (canvas.gameObject.scene != gameObject.scene || !canvas.isActiveAndEnabled) continue;
            CreateNextButton(canvas, LoadNext);
            break;
        }
    }

    public static Button CreateNextButton(Canvas canvas, UnityEngine.Events.UnityAction action)
    {
        if (canvas == null) return null;

        Button button = null;
        foreach (Button candidate in canvas.GetComponentsInChildren<Button>(true))
        {
            if (candidate.name != "NextButton") continue;
            button = candidate;
            break;
        }
        if (button == null)
        {
            GameObject buttonGo = new GameObject("NextButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(canvas.transform, false);
            button = buttonGo.GetComponent<Button>();
        }

        // 两类过渡页共用右下角按钮，不再生成底部提示文字。
        RectTransform brt = (RectTransform)button.transform;
        brt.anchorMin = new Vector2(1f, 0f);
        brt.anchorMax = new Vector2(1f, 0f);
        brt.pivot = new Vector2(1f, 0f);
        brt.anchoredPosition = new Vector2(-50f, 50f);
        brt.sizeDelta = new Vector2(220f, 110f);

        Image img = button.GetComponent<Image>();
        if (img == null) img = button.gameObject.AddComponent<Image>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("UI/next");
        if (sprites != null && sprites.Length > 0) img.sprite = sprites[0];
        img.color = Color.white;
        img.preserveAspect = true;
        button.targetGraphic = img;
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;
        button.gameObject.SetActive(true);

        if (action != null)
        {
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }
        return button;
    }

    public void LoadNext()
    {
        if (_loaded || Time.unscaledTime < _readyAt) return;
        _loaded = true;
        AudioManager.Instance?.PlayClick();
        SceneManager.LoadScene(nextSceneName);
    }
}
