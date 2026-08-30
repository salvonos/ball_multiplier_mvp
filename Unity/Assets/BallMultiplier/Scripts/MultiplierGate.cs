using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class MultiplierGate : MonoBehaviour
{
    [Header("Multiplier")]
    [Min(2)]
    [SerializeField] private int multiplier = 2;

    [Header("Ball")]
    [SerializeField] private Ball ballPrefab;
    [SerializeField] private float cloneGap = 0.02f;

    private BoxCollider2D triggerCollider;
    private int gateId;
    private static int nextGateId = 1;

    private void Awake()
    {
        triggerCollider = GetComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;
        gateId = nextGateId++;
    }

    private void Reset()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(3f, 0.45f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Ball sourceBall = other.GetComponent<Ball>();
        if (sourceBall == null || ballPrefab == null)
            return;

        if (!sourceBall.CanUseGate(gateId))
            return;

        Multiply(sourceBall);
    }

    private void Multiply(Ball sourceBall)
    {
        sourceBall.MarkGateUsed(gateId);

        Vector2 velocity = sourceBall.Body.linearVelocity;
        Vector2 sourcePosition = sourceBall.transform.position;

        float diameter = GetBallDiameter();
        float spacing = diameter + Mathf.Max(0f, cloneGap);

        Vector2 travelDirection = velocity.sqrMagnitude > 0.001f
            ? velocity.normalized
            : Vector2.down;

        float exitDistance = GetGateHalfThicknessAlong(travelDirection) + diameter * 0.75f;
        Vector2 spawnCenter = sourcePosition + travelDirection * exitDistance;
        Vector2 spreadAxis = new Vector2(-travelDirection.y, travelDirection.x).normalized;

        for (int i = 0; i < multiplier; i++)
        {
            float offset = (i - (multiplier - 1) * 0.5f) * spacing;
            Vector2 spawnPosition = spawnCenter + spreadAxis * offset;

            Ball clone = Instantiate(ballPrefab, spawnPosition, Quaternion.identity);
            clone.CopyUsedGatesFrom(sourceBall);
            clone.Body.linearVelocity = velocity;
        }

        Destroy(sourceBall.gameObject);
    }

    private float GetBallDiameter()
    {
        if (ballPrefab == null)
            return 0.5f;

        SpriteRenderer sprite = ballPrefab.GetComponent<SpriteRenderer>();
        if (sprite != null && sprite.sprite != null)
            return sprite.sprite.bounds.size.x * Mathf.Abs(ballPrefab.transform.localScale.x);

        CircleCollider2D circle = ballPrefab.GetComponent<CircleCollider2D>();
        if (circle != null)
            return circle.radius * 2f * Mathf.Abs(ballPrefab.transform.localScale.x);

        return 0.5f;
    }

    private float GetGateHalfThicknessAlong(Vector2 direction)
    {
        if (triggerCollider == null)
            return 0.25f;

        Vector2 size = Vector2.Scale(triggerCollider.size, transform.lossyScale);
        Vector2 localDir = transform.InverseTransformDirection(direction);

        return Mathf.Abs(localDir.x) * size.x * 0.5f +
               Mathf.Abs(localDir.y) * size.y * 0.5f;
    }

    public void SetMultiplier(int value)
    {
        multiplier = Mathf.Max(2, value);
    }
}
