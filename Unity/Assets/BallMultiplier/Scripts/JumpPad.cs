using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class JumpPad : MonoBehaviour
{
    [Header("Jump")]
    [SerializeField] private float jumpVelocity = 12f;

    [Header("Behaviour")]
    [Tooltip("A ball can use this specific Jump Pad only once, but can still use other Jump Pads.")]
    [SerializeField] private bool oneUsePerBall = true;

    [Header("Collider")]
    [Tooltip("Automatically matches the trigger width to the sprite and gives it enough invisible thickness to catch fast balls.")]
    [SerializeField] private bool fitColliderToSprite = true;

    [Tooltip("Minimum trigger thickness in world units. Prevents fast balls from tunnelling through thin/inclined pads.")]
    [SerializeField] private float minimumWorldTriggerThickness = 1.0f;

    private BoxCollider2D triggerCollider;
    private readonly HashSet<Ball> usedBalls = new HashSet<Ball>();

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
        TryLaunch(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Extra safety for fast-moving balls that enter the trigger between
        // physics steps. The one-use check prevents double activation.
        TryLaunch(other);
    }

    private void TryLaunch(Collider2D other)
    {
        Ball ball = other.GetComponent<Ball>();
        if (ball == null || ball.Body == null)
            return;

        if (oneUsePerBall && usedBalls.Contains(ball))
            return;

        if (oneUsePerBall)
            usedBalls.Add(ball);

        // Launch along this pad's local +Y axis.
        // Therefore every Jump Pad has its own launch direction.
        Vector2 launchDirection = ((Vector2)transform.up).normalized;
        ball.Body.linearVelocity = launchDirection * Mathf.Abs(jumpVelocity);
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

        float worldScaleY = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.y));
        float requiredLocalThickness = Mathf.Max(0f, minimumWorldTriggerThickness) / worldScaleY;

        Vector2 size = bounds.size;
        size.y = Mathf.Max(size.y, requiredLocalThickness);

        box.size = size;
        box.offset = bounds.center;
        box.isTrigger = true;
    }
}
