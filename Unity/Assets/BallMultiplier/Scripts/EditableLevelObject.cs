using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class EditableLevelObject : MonoBehaviour
{
    [SerializeField] private bool allowRotation = true;

    private Camera worldCamera;
    private Vector3 dragOffset;
    private bool dragging;

    private void Awake()
    {
        worldCamera = Camera.main;
    }

    private void Update()
    {
        if (GameModeController.Instance == null ||
            GameModeController.Instance.Mode != GameMode.Edit)
        {
            dragging = false;
            return;
        }

        HandlePointer();
    }

    private void HandlePointer()
    {
        if (Pointer.current == null || worldCamera == null)
            return;

        Vector2 screenPosition = Pointer.current.position.ReadValue();

        Vector3 worldPosition = worldCamera.ScreenToWorldPoint(
            new Vector3(
                screenPosition.x,
                screenPosition.y,
                -worldCamera.transform.position.z
            )
        );

        worldPosition.z = 0f;

        if (Pointer.current.press.wasPressedThisFrame)
        {
            Collider2D hit = Physics2D.OverlapPoint(worldPosition);

            if (hit != null && hit.transform == transform)
            {
                dragging = true;
                dragOffset = transform.position - worldPosition;
            }
        }

        if (dragging && Pointer.current.press.isPressed)
        {
            transform.position = worldPosition + dragOffset;
        }

        if (Pointer.current.press.wasReleasedThisFrame)
        {
            dragging = false;
        }
    }

    public void RotateBy(float degrees)
    {
        if (!allowRotation)
            return;

        transform.Rotate(0f, 0f, degrees);
    }
}