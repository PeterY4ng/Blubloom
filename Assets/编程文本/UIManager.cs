using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    // 全局单例，其他脚本自动调用
    public static UIManager Instance;

    [Header("HUD 数值文本：拖图片旁边显示数字的 TMP 文本")]
    public TextMeshProUGUI healthValueText;
    public TextMeshProUGUI scoreValueText;

    public const float DamageFlashDuration = 0.3f;
    private const float DamageFlashAlpha = 0.32f;
    private RectTransform _healthFill;
    private RectTransform _scoreFill;
    private Image _damageOverlay;
    private int _entryScore;
    private int _scoreCapacity;
    private int _maxHealth;
    private float _flashRemaining;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        GameManager game = GameManager.Instance;
        if (game == null) return;

        // 保持原来的 1920 宽布局比例，避免小窗口中左侧数值条盖住居中的关卡标题。
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        _entryScore = game.EntryScore;
        _maxHealth = Mathf.Max(1, game.startHealth);
        PlayerStats player = Object.FindFirstObjectByType<PlayerStats>();
        int foodCount = Mathf.Max(1, GameObject.FindGameObjectsWithTag("Food").Length);
        int pointsPerFood = player != null ? player.scorePerFood : 10;
        _scoreCapacity = Mathf.Max(1, foodCount * pointsPerFood);

        if (healthValueText != null)
            _healthFill = CreateBar("HealthBar", healthValueText, new Vector2(420f, -84f),
                new Color(0.23f, 0.06f, 0.08f), new Color(0.92f, 0.16f, 0.22f), _maxHealth, Color.white);
        if (scoreValueText != null)
            _scoreFill = CreateBar("ScoreBar", scoreValueText, new Vector2(420f, -219f),
                new Color(0.15f, 0.65f, 0.25f), new Color(1f, 0.82f, 0.12f), foodCount,
                new Color(0.03f, 0.12f, 0.17f));

        RefreshHealth(game.CurrentHealth);
        RefreshScore(game.CurrentScore);
    }

    private RectTransform CreateBar(string barName, TextMeshProUGUI valueText, Vector2 position,
        Color trackColor, Color fillColor, int segments, Color textColor)
    {
        Transform canvas = valueText.canvas.transform;
        Image frame = CreateImage(barName, canvas, new Color(0.02f, 0.16f, 0.23f));
        RectTransform rect = frame.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(300f, 60f);

        Image track = CreateImage("Track", rect, trackColor);
        track.rectTransform.offsetMin = new Vector2(3f, 3f);
        track.rectTransform.offsetMax = new Vector2(-3f, -3f);
        Image fill = CreateImage("Fill", track.transform, fillColor);
        for (int i = 1; i < segments; i++)
        {
            Image separator = CreateImage("Divider", track.transform, new Color(0.02f, 0.16f, 0.23f));
            RectTransform divider = separator.rectTransform;
            divider.anchorMin = new Vector2((float)i / segments, 0f);
            divider.anchorMax = new Vector2((float)i / segments, 1f);
            divider.offsetMin = new Vector2(-1.5f, 0f);
            divider.offsetMax = new Vector2(1.5f, 0f);
        }

        RectTransform textRect = valueText.rectTransform;
        textRect.SetParent(rect, false);
        textRect.localScale = Vector3.one;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        valueText.fontSize = 44f;
        valueText.enableAutoSizing = false;
        valueText.fontStyle = FontStyles.Bold;
        valueText.alignment = TextAlignmentOptions.Center;
        valueText.color = textColor;
        valueText.margin = Vector4.zero;
        valueText.raycastTarget = false;
        return fill.rectTransform;
    }

    private static Image CreateImage(string imageName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(imageName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return image;
    }

    void Update()
    {
        if (_flashRemaining <= 0f) return;
        _flashRemaining = Mathf.Max(0f, _flashRemaining - Time.unscaledDeltaTime);
        _damageOverlay.color = new Color(1f, 0f, 0f, DamageFlashAlpha * _flashRemaining / DamageFlashDuration);
    }

    public void ShowDamageFlash()
    {
        if (_damageOverlay == null)
        {
            Canvas canvas = GetComponent<Canvas>();
            _damageOverlay = CreateImage("DamageFlash", canvas.transform, Color.clear);
        }
        _damageOverlay.transform.SetAsLastSibling();
        _flashRemaining = DamageFlashDuration;
        _damageOverlay.color = new Color(1f, 0f, 0f, DamageFlashAlpha);
    }

    // 数字保留累计积分，填充只计算进入本关后获得的分数。
    public void RefreshScore(int score)
    {
        if (scoreValueText != null) scoreValueText.text = score.ToString();
        if (_scoreFill != null)
            _scoreFill.anchorMax = new Vector2(Mathf.Clamp01((float)(score - _entryScore) / _scoreCapacity), 1f);
    }

    // 在原有标牌旁边显示当前血量及本关血条。
    public void RefreshHealth(int health)
    {
        int maxHealth = _maxHealth > 0 ? _maxHealth :
            Mathf.Max(1, GameManager.Instance != null ? GameManager.Instance.startHealth : 3);
        if (healthValueText != null) healthValueText.text = $"{health}/{maxHealth}";
        if (_healthFill != null)
            _healthFill.anchorMax = new Vector2(Mathf.Clamp01((float)health / maxHealth), 1f);
    }
}
