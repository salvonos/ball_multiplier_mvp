using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class JumpPad : MonoBehaviour
{
    [Header("Jump")]
    [SerializeField] private float jumpVelocity = 12f;
    [Tooltip("If horizontal speed is almost zero, add a small sideways push so balls do not bounce forever in a vertical loop.")]
    [SerializeField] private float minimumHorizontalExitSpeed = 1.25f;

    [Header("Behaviour")]
    [Tooltip("A ball can use this specific Jump Pad only once.")]
    [SerializeField] private bool oneUsePerBall = true;

    [Header("Collider")]
    [Tooltip("Automatically matches the BoxCollider2D to the visible sprite.")]
    [SerializeField] private bool fitColliderToSprite = true;

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
        Ball ball = other.GetComponent<Ball>();
        if (ball == null || ball.Body == null)
            return;

        if (oneUsePerBall && usedBalls.Contains(ball))
            return;

        if (oneUsePerBall)
            usedBalls.Add(ball);

        Vector2 velocity = ball.Body.linearVelocity;

        // Always launch upward.
        velocity.y = Mathf.Abs(jumpVelocity);

        // Avoid a perfectly vertical endless loop. If the ball has almost no
        // X velocity, push it away from the pad centre. If it is exactly centred,
        // choose a deterministic side from its current relative position.
        if (Mathf.Abs(velocity.x) < minimumHorizontalExitSpeed)
        {
            float side = Mathf.Sign(ball.transform.position.x - transform.position.x);
            if (Mathf.Approximately(side, 0f))
                side = 1f;

            velocity.x = side * Mathf.Abs(minimumHorizontalExitSpeed);
        }

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
