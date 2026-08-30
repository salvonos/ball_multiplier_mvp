using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BallSpawner : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private Ball ballPrefab;
    [SerializeField] private int initialBallCount = 5;
    [Tooltip("Extra gap between balls. 0 = balls touch exactly.")]
    [SerializeField] private float ballGap = 0.02f;

    [Header("Horizontal Drag")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private float minX = -7f;
    [SerializeField] private float maxX = 7f;
    [SerializeField] private float clickVerticalTolerance = 0.45f;
    [SerializeField] private float clickHorizontalPadding = 0.25f;

    [Header("Launcher Guide")]
    [SerializeField] private Color guideColor = Color.white;
    [SerializeField] private float guideWidth = 0.045f;
    [SerializeField] private float endCapHeight = 0.20f;

    private bool dragging;
    private bool readyToDrop = true;
    private readonly List<Ball> spawnedBalls = new();

    private LineRenderer guideLine;
    private Material guideMaterial;

    public int InitialBallCount => initialBallCount;
    public IReadOnlyList<Ball> SpawnedBalls => spawnedBalls;

    private void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;

        CreateGuideLine();
        UpdateGuideLine();
    }

    private void OnDestroy()
    {
        if (guideMaterial != null)
            Destroy(guideMaterial);
    }

    private void Update()
    {
        bool editMode = GameModeController.Instance == null ||
                        GameModeController.Instance.Mode == GameMode.Edit;

        if (!editMode || !readyToDrop)
        {
            SetGuideVisible(false);
            dragging = false;
            return;
        }

        SetGuideVisible(true);
        UpdateGuideLine();
        HandlePointer();
    }

    private void HandlePointer()
    {
        if (Pointer.current == null || worldCamera == null)
            return;

        Vector2 screenPosition = Pointer.current.position.ReadValue();
        Vector3 world = ScreenToWorld(screenPosition);

        if (Pointer.current.press.wasPressedThisFrame && PointerHitsGuide(world))
            dragging = true;

        if (dragging && Pointer.current.press.isPressed)
        {
            transform.position = new Vector3(
                Mathf.Clamp(world.x, minX, maxX),
                transform.position.y,
                0f
            );

            UpdateGuideLine();
        }

        if (dragging && Pointer.current.press.wasReleasedThisFrame)
        {
            dragging = false;
            ReleaseAndDrop();
        }
    }

    private Vector3 ScreenToWorld(Vector2 screenPosition)
    {
        Vector3 world = worldCamera.ScreenToWorldPoint(
            new Vector3(
                screenPosition.x,
                screenPosition.y,
                -worldCamera.transform.position.z
            )
        );

        world.z = 0f;
        return world;
    }

    private bool PointerHitsGuide(Vector3 worldPosition)
    {
        float halfWidth = GetOccupiedHalfWidth();

        return Mathf.Abs(worldPosition.y - transform.position.y) <= clickVerticalTolerance &&
               worldPosition.x >= transform.position.x - halfWidth - clickHorizontalPadding &&
               worldPosition.x <= transform.position.x + halfWidth + clickHorizontalPadding;
    }

    private float GetBallRadius()
    {
        if (ballPrefab == null)
            return 0.125f;

        CircleCollider2D circle = ballPrefab.GetComponent<CircleCollider2D>();
        if (circle != null)
            return circle.radius * Mathf.Abs(ballPrefab.transform.localScale.x);

        SpriteRenderer sprite = ballPrefab.GetComponent<SpriteRenderer>();
        if (sprite != null && sprite.sprite != null)
            return sprite.sprite.bounds.extents.x * Mathf.Abs(ballPrefab.transform.localScale.x);

        return 0.125f;
    }

    private float GetCenterSpacing()
    {
        return GetBallRadius() * 2f + Mathf.Max(0f, ballGap);
    }

    private float GetOccupiedHalfWidth()
    {
        float radius = GetBallRadius();
        float centerSpacing = GetCenterSpacing();
        float centersWidth = Mathf.Max(0, initialBallCount - 1) * centerSpacing;
        return centersWidth * 0.5f + radius;
    }

    private void CreateGuideLine()
    {
        GameObject guideObject = new GameObject("Ball Width Guide");
        guideObject.transform.SetParent(transform, false);

        guideLine = guideObject.AddComponent<LineRenderer>();
        guideLine.useWorldSpace = false;
        guideLine.loop = false;
        guideLine.positionCount = 4;
        guideLine.startWidth = guideWidth;
        guideLine.endWidth = guideWidth;
        guideLine.startColor = guideColor;
        guideLine.endColor = guideColor;
        guideLine.numCapVertices = 4;
        guideLine.numCornerVertices = 2;
        guideLine.sortingOrder = 100;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            guideMaterial = new Material(shader);
            guideLine.material = guideMaterial;
        }
    }

    private void UpdateGuideLine()
    {
        if (guideLine == null)
            return;

        float halfWidth = GetOccupiedHalfWidth();
        float halfCap = endCapHeight * 0.5f;

        guideLine.startWidth = guideWidth;
        guideLine.endWidth = guideWidth;
        guideLine.startColor = guideColor;
        guideLine.endColor = guideColor;

        // The horizontal line is the exact top edge of the ball row.
        guideLine.SetPosition(0, new Vector3(-halfWidth, halfCap, 0f));
        guideLine.SetPosition(1, new Vector3(-halfWidth, 0f, 0f));
        guideLine.SetPosition(2, new Vector3(halfWidth, 0f, 0f));
        guideLine.SetPosition(3, new Vector3(halfWidth, halfCap, 0f));
    }

    private void SetGuideVisible(bool visible)
    {
        if (guideLine != null)
            guideLine.enabled = visible;
    }

    private void ReleaseAndDrop()
    {
        readyToDrop = false;
        SetGuideVisible(false);

        if (GameModeController.Instance != null)
            GameModeController.Instance.EnterPlayMode();

        DropBalls();
    }

    public void DropBalls()
    {
        ClearBalls();

        float radius = GetBallRadius();
        float centerSpacing = GetCenterSpacing();

        // Balls start immediately BELOW the guide, so visually they fall from the line.
        float spawnY = transform.position.y - radius - guideWidth * 0.5f;

        for (int i = 0; i < initialBallCount; i++)
        {
            float offset =
                (i - (initialBallCount - 1) * 0.5f) * centerSpacing;

            Ball ball = Instantiate(
                ballPrefab,
                new Vector3(transform.position.x + offset, spawnY, 0f),
                Quaternion.identity
            );

            spawnedBalls.Add(ball);
        }
    }

    public void ResetForEdit()
    {
        ClearBalls();
        readyToDrop = true;
        dragging = false;
        SetGuideVisible(true);
        UpdateGuideLine();
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
