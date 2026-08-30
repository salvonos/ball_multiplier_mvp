using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class JumpPad : MonoBehaviour
{
    [SerializeField] private float jumpVelocity = 8f;

    private void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Ball ball = other.GetComponent<Ball>();
        if (ball == null)
            return;

        Vector2 velocity = ball.Body.linearVelocity;
        velocity.y = Mathf.Abs(jumpVelocity);
        ball.Body.linearVelocity = velocity;
    }
}
