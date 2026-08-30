using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class Ball : MonoBehaviour
{
    public Rigidbody2D Body { get; private set; }

    private int lastGateId = -1;
    private float gateImmunityUntil = -1f;

    private void Awake()
    {
        Body = GetComponent<Rigidbody2D>();
    }

    public bool CanUseGate(int gateId)
    {
        return gateId != lastGateId || Time.time >= gateImmunityUntil;
    }

    public void MarkGateUsed(int gateId, float immunitySeconds)
    {
        lastGateId = gateId;
        gateImmunityUntil = Time.time + Mathf.Max(0f, immunitySeconds);
    }
}
