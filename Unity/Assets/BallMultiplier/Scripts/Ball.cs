using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class Ball : MonoBehaviour
{
    public Rigidbody2D Body { get; private set; }

    // A gate is blocked only temporarily. This prevents the freshly spawned
    // clones from immediately multiplying again while they are still leaving
    // the same trigger, but allows them to use that gate again later.
    private readonly Dictionary<int, float> gateBlockedUntil = new();

    private void Awake()
    {
        Body = GetComponent<Rigidbody2D>();
    }

    public bool CanUseGate(int gateId)
    {
        if (!gateBlockedUntil.TryGetValue(gateId, out float blockedUntil))
            return true;

        if (Time.time >= blockedUntil)
        {
            gateBlockedUntil.Remove(gateId);
            return true;
        }

        return false;
    }

    public void BlockGateTemporarily(int gateId, float seconds)
    {
        gateBlockedUntil[gateId] = Time.time + Mathf.Max(0f, seconds);
    }

    public void CopyGateCooldownsFrom(Ball source)
    {
        gateBlockedUntil.Clear();

        if (source == null)
            return;

        foreach (KeyValuePair<int, float> pair in source.gateBlockedUntil)
        {
            if (pair.Value > Time.time)
                gateBlockedUntil[pair.Key] = pair.Value;
        }
    }
}
