using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public SpriteRenderer background;
    public float smoothSpeed = 6f;

    private Vector3 _offset;
    private Camera _cam;

    void Start()
    {
        _cam = GetComponent<Camera>();
        if (target != null)
            _offset = transform.position - target.position;
    }

    void LateUpdate()
    {
        if (target == null) return;

        float targetX = target.position.x + _offset.x;
        float targetY = target.position.y + _offset.y;

        if (background != null && _cam != null)
        {
            float halfH = _cam.orthographicSize;
            float halfW = halfH * _cam.aspect;
            Bounds b = background.bounds;
            float minXc = b.min.x + halfW;
            float maxXc = b.max.x - halfW;
            float minYc = b.min.y + halfH;
            float maxYc = b.max.y - halfH;

            targetX = minXc < maxXc ? Mathf.Clamp(targetX, minXc, maxXc) : b.center.x;
            targetY = minYc < maxYc ? Mathf.Clamp(targetY, minYc, maxYc) : b.center.y;
        }

        Vector3 desiredPos = new Vector3(targetX, targetY, _offset.z);
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
    }
}
