using UnityEngine;
using UnityEngine.UI;

public sealed class Level4EndingSequence : MonoBehaviour
{
    [SerializeField] private RawImage page;
    [SerializeField] private Texture2D epilogue;
    [SerializeField, Min(0f)] private float topHoldSeconds = 3f;
    [SerializeField, Min(0.1f)] private float secondsPerScreen = 12f;

    private RectTransform _viewport;
    private Button _next;
    private bool _showingEpilogue;
    private float _scrollStartsAt;
    private float _scrollDistance;
    private float _progress;

    private void Start()
    {
        _viewport = (RectTransform)transform;
        LayoutPage();
        _next = CutsceneManager.CreateNextButton(GetComponent<Canvas>(), ShowEpilogue);
    }

    private void ShowEpilogue()
    {
        if (_showingEpilogue) return;
        _showingEpilogue = true;
        _next.gameObject.SetActive(false);
        AudioManager.Instance?.PlayClick();
        page.texture = epilogue;
        _scrollStartsAt = Time.unscaledTime + topHoldSeconds;
        LayoutPage();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (_viewport != null) LayoutPage();
    }

    private void LayoutPage()
    {
        Vector2 view = _viewport.rect.size;
        if (view.x <= 0f || view.y <= 0f) return;
        Texture texture = page.texture;
        float scale = view.x / texture.width;
        if (!_showingEpilogue) scale = Mathf.Min(scale, view.y / texture.height);

        float height = texture.height * scale;
        page.rectTransform.sizeDelta = new Vector2(texture.width * scale, height);
        _scrollDistance = Mathf.Max(0f, height - view.y);
        // The page pivots at its top edge. Moving it up reveals the next lines.
        float y = _showingEpilogue ? _scrollDistance * _progress : (height - view.y) * 0.5f;
        page.rectTransform.anchoredPosition = new Vector2(0f, y);
    }

    private void Update()
    {
        if (!_showingEpilogue || Time.unscaledTime < _scrollStartsAt ||
            _scrollDistance <= 0f || _progress >= 1f) return;

        float speed = _viewport.rect.height / secondsPerScreen;
        _progress = Mathf.Clamp01(_progress + speed * Time.unscaledDeltaTime / _scrollDistance);
        page.rectTransform.anchoredPosition = new Vector2(0f, _progress * _scrollDistance);
    }
}
