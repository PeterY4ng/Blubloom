using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("跟随目标：把 Player 拖进来")]
    public Transform target;

    [Header("背景 SpriteRenderer：把背景图拖进来，自动按背景算边界")]
    public SpriteRenderer background;

    [Header("跟随平滑度（越大越紧跟，建议 10+）")]
    public float smoothSpeed = 15f;

    private Camera _cam;

    void Start()
    {
        _cam = GetComponent<Camera>();
        if (background != null)
        {
            Bounds b = background.bounds;
            float halfH = _cam.orthographicSize;
            float halfW = halfH * _cam.aspect;
            Debug.Log($"[CameraFollow] background bounds: min=({b.min.x:F2},{b.min.y:F2}) max=({b.max.x:F2},{b.max.y:F2}) size=({b.size.x:F2},{b.size.y:F2})");
            Debug.Log($"[CameraFollow] halfW={halfW:F2} halfH={halfH:F2}  clampX=[{b.min.x+halfW:F2}, {b.max.x-halfW:F2}]");
        }
    }

    void LateUpdate()
    {
        if (target == null || background == null || _cam == null) return;

        Bounds b = background.bounds;
        float halfH = _cam.orthographicSize;
        float halfW = halfH * _cam.aspect;

        // 相机跟随玩家位置
        float targetX = target.position.x;
        float targetY = target.position.y;

        // X 方向：相机中心不能让背景左右边缘露出
        float minXc = b.min.x + halfW;
        float maxXc = b.max.x - halfW;
        if (minXc < maxXc)
            targetX = Mathf.Clamp(targetX, minXc, maxXc);
        else
            targetX = b.center.x;

        // Y 方向：相机中心不能让背景上下边缘露出
        float minYc = b.min.y + halfH;
        float maxYc = b.max.y - halfH;
        if (minYc < maxYc)
            targetY = Mathf.Clamp(targetY, minYc, maxYc);
        else
            targetY = b.center.y;

        Vector3 desiredPos = new Vector3(targetX, targetY, transform.position.z);
        transform.position = desiredPos;
    }
}
