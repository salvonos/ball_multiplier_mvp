using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class JumpPad : MonoBehaviour
{
    [Header("Jump")]
    [SerializeField] private float jumpVelocity = 12f;

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

        // The pad launches along its own local +Y axis.
        // Rotating the JumpPad therefore rotates the launch direction too.
        Vector2 launchDirection = ((Vector2)transform.up).normalized;

        // Replace the current velocity with a clean launch along the pad normal.
        // This makes the behaviour consistent and physically aligned with the pad orientation.
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
        box.size = bounds.size;
        box.offset = bounds.center;
    }
}
