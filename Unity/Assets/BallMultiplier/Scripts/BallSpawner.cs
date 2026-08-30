using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BallSpawner : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private Ball ballPrefab;
    [SerializeField] private int initialBallCount = 5;
    [SerializeField] private float spacing = 0.35f;

    [Header("Horizontal Drag")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private float minX = -7f;
    [SerializeField] private float maxX = 7f;

    private bool dragging;
    private readonly List<Ball> spawnedBalls = new();

    public int InitialBallCount => initialBallCount;
    public IReadOnlyList<Ball> SpawnedBalls => spawnedBalls;

    private void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;
    }

    private void Update()
    {
        if (GameModeController.Instance != null &&
            GameModeController.Instance.Mode != GameMode.Edit)
            return;

        HandlePointer();
    }

    private void HandlePointer()
    {
        if (Pointer.current == null)
            return;

        if (Pointer.current.press.wasPressedThisFrame)
            dragging = true;

        if (Pointer.current.press.wasReleasedThisFrame)
            dragging = false;

        if (!dragging)
            return;

        Vector2 screenPosition = Pointer.current.position.ReadValue();

        Vector3 world = worldCamera.ScreenToWorldPoint(
            new Vector3(
                screenPosition.x,
                screenPosition.y,
                -worldCamera.transform.position.z
            )
        );

        transform.position = new Vector3(
            Mathf.Clamp(world.x, minX, maxX),
            transform.position.y,
            0f
        );
    }

    public void DropBalls()
    {
        ClearBalls();

        for (int i = 0; i < initialBallCount; i++)
        {
            float offset =
                (i - (initialBallCount - 1) * 0.5f) * spacing;

            Ball ball = Instantiate(
                ballPrefab,
                transform.position + Vector3.right * offset,
                Quaternion.identity
            );

            spawnedBalls.Add(ball);
        }
    }

    public void ClearBalls()
    {
        for (int i = spawnedBalls.Count - 1; i >= 0; i--)
        {
            if (spawnedBalls[i] != null)
                Destroy(spawnedBalls[i].gameObject);
        }

        spawnedBalls.Clear();
    }
}