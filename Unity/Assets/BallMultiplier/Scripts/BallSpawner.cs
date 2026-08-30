using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BallSpawner : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private Ball ballPrefab;
    [SerializeField] private int initialBallCount = 5;
    [SerializeField] private float ballGap = 0f;

    [Header("Placement")]
    [SerializeField] private Camera worldCamera;
    [Tooltip("Keep the spawned row inside the visible horizontal camera bounds.")]
    [SerializeField] private bool clampToCamera = true;
    [Range(0f, 0.2f)]
    [SerializeField] private float sideViewportPadding = 0.02f;

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
        ResolveCamera();
        CreateGuideLine();
        UpdateGuideLine();
    }

    private void OnEnable()
    {
        ResolveCamera();
    }

    private void OnDestroy()
    {
        if (guideMaterial != null)
            Destroy(guideMaterial);
    }

    private void Update()
    {
        ResolveCamera();

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

    private void ResolveCamera()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;
    }

    private float ZoomScale()
    {
        if (worldCamera == null || !worldCamera.orthographic)
            return 1f;

        return worldCamera.orthographicSize / Mathf.Max(0.01f, referenceOrthographicSize);
    }

    private void HandlePointer()
    {
        if (Pointer.current == null || worldCamera == null)
            return;

        Vector2 screenPosition = Pointer.current.position.ReadValue();
        Vector3 worldPosition = ScreenToWorld(screenPosition);

        // Entire Game window is the drag area. No need to click the guide itself.
        if (Pointer.current.press.wasPressedThisFrame)
            dragging = true;

        if (dragging && Pointer.current.press.isPressed)
        {
            float targetX = worldPosition.x;

            if (clampToCamera && worldCamera.orthographic)
                targetX = ClampXToCamera(targetX);

            // Only X changes during gameplay drag. Y is defined by the prefab/scene placement.
            transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
            UpdateGuideLine();
        }

        if (dragging && Pointer.current.press.wasReleasedThisFrame)
        {
            dragging = false;
            ReleaseAndDrop();
        }
    }

    private float ClampXToCamera(float x)
    {
        float halfCameraWidth = worldCamera.orthographicSize * worldCamera.aspect;
        float cameraCenterX = worldCamera.transform.position.x;
        float occupiedHalfWidth = GetOccupiedHalfWidth();
        float sidePaddingWorld = halfCameraWidth * 2f * Mathf.Clamp(sideViewportPadding, 0f, 0.2f);
        float allowedHalfWidth = Mathf.Max(0f, halfCameraWidth - sidePaddingWorld - occupiedHalfWidth);

        return Mathf.Clamp(
            x,
            cameraCenterX - allowedHalfWidth,
            cameraCenterX + allowedHalfWidth
        );
    }

    private Vector3 ScreenToWorld(Vector2 screenPosition)
    {
        float distance = Mathf.Abs(worldCamera.transform.position.z - transform.position.z);
        Vector3 worldPosition = worldCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, distance)
        );
        worldPosition.z = transform.position.z;
        return worldPosition;
    }

    private float GetBallVisualRadius()
    {
        if (ballPrefab == null)
            return 0.5f;

        SpriteRenderer spriteRenderer = ballPrefab.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
            return spriteRenderer.sprite.bounds.size.x * Mathf.Abs(ballPrefab.transform.localScale.x) * 0.5f;

        CircleCollider2D circle = ballPrefab.GetComponent<CircleCollider2D>();
        if (circle != null)
            return circle.radius * Mathf.Abs(ballPrefab.transform.localScale.x);

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
                new Vector3(transform.position.x + offset, spawnY, transform.position.z),
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
