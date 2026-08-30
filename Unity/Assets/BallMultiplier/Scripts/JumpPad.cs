using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class JumpPad : MonoBehaviour
{
    [Header("Jump")]
    [SerializeField] private float jumpVelocity = 12f;
    [Tooltip("Minimum time before the same ball can trigger this pad again.")]
    [SerializeField] private float retriggerDelay = 0.15f;

    [Header("Collider")]
    [Tooltip("Automatically matches the BoxCollider2D to the visible sprite.")]
    [SerializeField] private bool fitColliderToSprite = true;

    private BoxCollider2D triggerCollider;

    private void Awake()
    {
        triggerCollider = GetComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;

        if (fitColliderToSprite)
            FitColliderToSprite();
    }

    private void Reset()
    {
        triggerCollider = GetComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;
        FitColliderToSprite();
    }

    private void OnValidate()
    {
        if (!fitColliderToSprite)
            return;

        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
            FitColliderToSprite(box);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Ball ball = other.GetComponent<Ball>();
        if (ball == null || ball.Body == null)
            return;

        JumpPadBallState state = ball.GetComponent<JumpPadBallState>();
        if (state == null)
            state = ball.gameObject.AddComponent<JumpPadBallState>();

        if (!state.CanTrigger(this, retriggerDelay))
            return;

        Vector2 velocity = ball.Body.linearVelocity;
        velocity.y = Mathf.Abs(jumpVelocity);
        ball.Body.linearVelocity = velocity;
    }

    private void FitColliderToSprite()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<BoxCollider2D>();

        FitColliderToSprite(triggerCollider);
    }

    private void FitColliderToSprite(BoxCollider2D box)
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null || spriteRenderer.sprite == null)
            return;

        Bounds bounds = spriteRenderer.sprite.bounds;
        box.size = bounds.size;
        box.offset = bounds.center;
    }
}

public class JumpPadBallState : MonoBehaviour
{
    private JumpPad lastPad;
    private float lastTriggerTime = -999f;

    public bool CanTrigger(JumpPad pad, float delay)
    {
        if (lastPad == pad && Time.time < lastTriggerTime + Mathf.Max(0f, delay))
            return false;

        lastPad = pad;
        lastTriggerTime = Time.time;
        return true;
    }
}
