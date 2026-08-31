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
    [Tooltip("Automatically matches the solid collider to the visible sprite.")]
    [SerializeField] private bool fitColliderToSprite = true;

    private BoxCollider2D solidCollider;
    private readonly HashSet<Ball> usedBalls = new HashSet<Ball>();

    private void Awake()
    {
        solidCollider = GetComponent<BoxCollider2D>();
        solidCollider.isTrigger = false;

        if (fitColliderToSprite)
            FitColliderToSprite();
    }

    private void Reset()
    {
        solidCollider = GetComponent<BoxCollider2D>();
        solidCollider.isTrigger = false;
        FitColliderToSprite();
    }

    private void OnValidate()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null)
            return;

        box.isTrigger = false;

        if (fitColliderToSprite)
            FitColliderToSprite(box);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryLaunch(collision.collider);
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

        // Real physical surface: the ball first collides with the pad,
        // then receives a launch impulse along the pad's local normal.
        Vector2 launchDirection = ((Vector2)transform.up).normalized;
        ball.Body.linearVelocity = launchDirection * Mathf.Abs(jumpVelocity);
    }

    private void FitColliderToSprite()
    {
        if (solidCollider == null)
            solidCollider = GetComponent<BoxCollider2D>();

        FitColliderToSprite(solidCollider);
    }

    private void FitColliderToSprite(BoxCollider2D box)
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null || spriteRenderer.sprite == null)
            return;

        Bounds bounds = spriteRenderer.sprite.bounds;
        box.size = bounds.size;
        box.offset = bounds.center;
        box.isTrigger = false;
    }
}
