using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("移动速度")]
    public float moveSpeed = 5f;

    [Header("背景 SpriteRenderer：把背景图拖进来，自动按背景算边界")]
    public SpriteRenderer background;

    [Header("玩家离背景边缘留多少距离")]
    public float padding = 0.1f;

    private Rigidbody2D rb;
    private Collider2D col;
    private Vector2 moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    void Update()
    {
        float moveX = 0;
        float moveY = 0;

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            moveX -= 1;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            moveX += 1;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            moveY += 1;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            moveY -= 1;

        moveInput = new Vector2(moveX, moveY).normalized;
    }

    void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;
        ClampToBackground();
    }

    private void ClampToBackground()
    {
        if (rb == null || col == null || background == null) return;

        Bounds b = background.bounds;
        Bounds cb = col.bounds;
        Vector2 pos = rb.position;

        // 玩家碰撞体左边不能超出背景左边 + padding
        if (cb.min.x < b.min.x + padding)
            pos.x += (b.min.x + padding) - cb.min.x;
        // 玩家碰撞体右边不能超出背景右边 - padding
        if (cb.max.x > b.max.x - padding)
            pos.x -= cb.max.x - (b.max.x - padding);

        // 上下同理
        if (cb.min.y < b.min.y + padding)
            pos.y += (b.min.y + padding) - cb.min.y;
        if (cb.max.y > b.max.y - padding)
            pos.y -= cb.max.y - (b.max.y - padding);

        rb.position = pos;
    }
}
