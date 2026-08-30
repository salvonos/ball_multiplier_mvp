using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BallSpawner : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private Ball ballPrefab;
    [SerializeField] private int initialBallCount = 5;
    [SerializeField] private float ballGap = 0f;

    [Header("Camera Anchoring")]
    [SerializeField] private Camera worldCamera;
    [Range(0.01f, 0.25f)]
    [Tooltip("Screen-space distance from the top edge. 0.06 = 6% below the top.")]
    [SerializeField] private float topViewportPadding = 0.06f;
    [Range(0.01f, 0.25f)]
    [Tooltip("Screen-space distance from the left/right edges.")]
    [SerializeField] private float sideViewportPadding = 0.03f;

    [Header("Drag")]
    [SerializeField] private float clickVerticalToleranceAtSize9 = 0.45f;
    [SerializeField] private float clickHorizontalPaddingAtSize9 = 0.25f;

    [Header("Launcher Guide")]
    [SerializeField] private Color guideColor = Color.white;
    [SerializeField] private float guideWidthAtSize9 = 0.045f;
    [SerializeField] private float endCapHeightAtSize9 = 0.20f;
    [SerializeField] private float referenceOrthographicSize = 9f;

    private bool dragging;
    private bool readyToDrop = true;
    private readonly List<Ball> spawnedBalls = new List<Ball>();

    private LineRenderer guideLine;
    private Material guideMaterial;

    public int InitialBallCount => initialBallCount;
    public IReadOnlyList<Ball> SpawnedBalls => spawnedBalls;

    private void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;

        CreateGuideLine();
        SnapToCameraTop();
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

        SnapToCameraTop();
        SetGuideVisible(true);
        UpdateGuideLine();
        HandlePointer();
    }

    private float ZoomScale()
    {
        if (worldCamera == null || !worldCamera.orthographic)
            return 1f;

        return worldCamera.orthographicSize / Mathf.Max(0.01f, referenceOrthographicSize);
    }

    private Vector3 ViewportToWorld(float x, float y)
    {
        float distance = Mathf.Abs(worldCamera.transform.position.z);
        Vector3 world = worldCamera.ViewportToWorldPoint(new Vector3(x, y, distance));
        world.z = 0f;
        return world;
    }

    private void SnapToCameraTop()
    {
        if (worldCamera == null || !worldCamera.orthographic)
            return;

        float viewportY = 1f - Mathf.Clamp(topViewportPadding, 0.01f, 0.25f);
        float targetY = ViewportToWorld(0.5f, viewportY).y;

        float occupiedHalfWidth = GetOccupiedHalfWidth();
        float leftWorld = ViewportToWorld(Mathf.Clamp(sideViewportPadding, 0.01f, 0.25f), 0.5f).x;
        float rightWorld = ViewportToWorld(1f - Mathf.Clamp(sideViewportPadding, 0.01f, 0.25f), 0.5f).x;

        float minX = leftWorld + occupiedHalfWidth;
        float maxX = rightWorld - occupiedHalfWidth;
        float clampedX = minX <= maxX
            ? Mathf.Clamp(transform.position.x, minX, maxX)
            : worldCamera.transform.position.x;

        transform.position = new Vector3(clampedX, targetY, 0f);
    }

    private void HandlePointer()
    {
        if (Pointer.current == null || worldCamera == null)
            return;

        Vector2 screenPosition = Pointer.current.position.ReadValue();
        Vector3 worldPosition = ScreenToWorld(screenPosition);

        if (Pointer.current.press.wasPressedThisFrame && PointerHitsGuide(worldPosition))
            dragging = true;

        if (dragging && Pointer.current.press.isPressed)
        {
            float occupiedHalfWidth = GetOccupiedHalfWidth();
            float leftWorld = ViewportToWorld(Mathf.Clamp(sideViewportPadding, 0.01f, 0.25f), 0.5f).x;
            float rightWorld = ViewportToWorld(1f - Mathf.Clamp(sideViewportPadding, 0.01f, 0.25f), 0.5f).x;
            float minX = leftWorld + occupiedHalfWidth;
            float maxX = rightWorld - occupiedHalfWidth;

            float x = minX <= maxX
                ? Mathf.Clamp(worldPosition.x, minX, maxX)
                : worldCamera.transform.position.x;

            transform.position = new Vector3(x, transform.position.y, 0f);
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
        float distance = Mathf.Abs(worldCamera.transform.position.z);
        Vector3 worldPosition = worldCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, distance)
        );

        worldPosition.z = 0f;
        return worldPosition;
    }

    private bool PointerHitsGuide(Vector3 worldPosition)
    {
        float zoom = ZoomScale();
        float halfWidth = GetOccupiedHalfWidth();
        float verticalTolerance = clickVerticalToleranceAtSize9 * zoom;
        float horizontalPadding = clickHorizontalPaddingAtSize9 * zoom;

        bool insideY = Mathf.Abs(worldPosition.y - transform.position.y) <= verticalTolerance;
        bool insideX = worldPosition.x >= transform.position.x - halfWidth - horizontalPadding &&
                       worldPosition.x <= transform.position.x + halfWidth + horizontalPadding;

        return insideX && insideY;
    }

    private float GetBallVisualRadius()
    {
        if (ballPrefab == null)
            return 0.5f;

        SpriteRenderer spriteRenderer = ballPrefab.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            float width = spriteRenderer.sprite.bounds.size.x;
            float scale = Mathf.Abs(ballPrefab.transform.localScale.x);
            return width * scale * 0.5f;
        }

        CircleCollider2D circle = ballPrefab.GetComponent<CircleCollider2D>();
        if (circle != null)
        {
            float scale = Mathf.Abs(ballPrefab.transform.localScale.x);
            return circle.radius * scale;
        }

        return 0.5f;
    }

    private float GetCenterSpacing()
    {
        return GetBallVisualRadius() * 2f + Mathf.Max(0f, ballGap);
    }

    private float GetOccupiedHalfWidth()
    {
        float radius = GetBallVisualRadius();
        float centersWidth = Mathf.Max(0, initialBallCount - 1) * GetCenterSpacing();
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

        float zoom = ZoomScale();
        float halfWidth = GetOccupiedHalfWidth();
        float guideWidth = guideWidthAtSize9 * zoom;
        float halfCap = endCapHeightAtSize9 * zoom * 0.5f;

        guideLine.startWidth = guideWidth;
        guideLine.endWidth = guideWidth;
        guideLine.startColor = guideColor;
        guideLine.endColor = guideColor;

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

        float radius = GetBallVisualRadius();
        float spacing = GetCenterSpacing();
        float spawnY = transform.position.y - radius;

        for (int i = 0; i < initialBallCount; i++)
        {
            float offset = (i - (initialBallCount - 1) * 0.5f) * spacing;

            Ball newBall = Instantiate(
                ballPrefab,
                new Vector3(transform.position.x + offset, spawnY, 0f),
                Quaternion.identity
            );

            spawnedBalls.Add(newBall);
        }
    }

    public void ResetForEdit()
    {
        ClearBalls();
        readyToDrop = true;
        dragging = false;
        SnapToCameraTop();
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
