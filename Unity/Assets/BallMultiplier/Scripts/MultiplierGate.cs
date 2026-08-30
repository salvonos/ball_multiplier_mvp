using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class MultiplierGate : MonoBehaviour
{
    [Min(2)]
    [SerializeField] private int multiplier = 2;

    [SerializeField] private Ball ballPrefab;
    [SerializeField] private float spawnSpread = 0.14f;

    private void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Ball ball = other.GetComponent<Ball>();
        if (ball == null)
            return;

        Vector2 position = ball.transform.position;
        Vector2 velocity = ball.Body.linearVelocity;

        Destroy(ball.gameObject);

        for (int i = 0; i < multiplier; i++)
        {
            float offset = (i - (multiplier - 1) * 0.5f) * spawnSpread;
            Ball clone = Instantiate(ballPrefab, position + Vector2.right * offset, Quaternion.identity);
            clone.Body.linearVelocity = velocity + Vector2.right * offset * 0.5f;
        }
    }

    public void SetMultiplier(int value)
    {
        multiplier = Mathf.Max(2, value);
    }
}
