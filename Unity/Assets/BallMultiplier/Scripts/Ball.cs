using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class Ball : MonoBehaviour
{
    public Rigidbody2D Body { get; private set; }

    private readonly HashSet<int> usedGateIds = new();

    private void Awake()
    {
        Body = GetComponent<Rigidbody2D>();
    }

    public bool CanUseGate(int gateId)
    {
        return !usedGateIds.Contains(gateId);
    }

    public void MarkGateUsed(int gateId)
    {
        usedGateIds.Add(gateId);
    }

    public void CopyUsedGatesFrom(Ball source)
    {
        usedGateIds.Clear();

        if (source == null)
            return;

        foreach (int gateId in source.usedGateIds)
            usedGateIds.Add(gateId);
    }
}
