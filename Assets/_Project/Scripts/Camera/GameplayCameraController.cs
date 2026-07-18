using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameplayCameraController : MonoBehaviour
{
    [Header("Camera Movement")]
    [SerializeField] private float panSpeed = 10f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float deceleration = 15f;

    [Header("Camera Bounds")]
    [SerializeField] private Vector2 xBounds = new Vector2(-20f, 20f);
    [SerializeField] private Vector2 zBounds = new Vector2(-20f, 20f);

    [Header("Touch Input")]
    [SerializeField] private float touchSensitivity = 0.01f;

    [Header("Input")]
    [SerializeField] private InputActionReference panAction;
    [SerializeField] private InputActionReference touchPositionAction;
    [SerializeField] private InputActionReference touchPressAction;
    [SerializeField] private float touchPanSpeed = 0.01f;
    private Vector2 lastTouchPosition;

    [Header("Zoom")]
    [SerializeField] private Transform gameplayCamera;
    [SerializeField] private InputActionReference zoomAction;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float zoomSmoothness = 10f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 20f;

    private float targetZoom;
    private Vector3 initialCameraDirection;

    private Vector3 currentVelocity;
    private bool isDragging;
    private void Start()
    {
        targetZoom = gameplayCamera.localPosition.magnitude;
        initialCameraDirection = gameplayCamera.localPosition.normalized;
    }
    private void OnEnable()
    {
        panAction.action.Enable();
        touchPressAction.action.Enable();
        zoomAction.action.Enable();
        touchPositionAction.action.Enable();
    }

    private void OnDisable()
    {
        panAction.action.Disable();
        touchPositionAction.action.Disable();
        touchPressAction.action.Disable();
        zoomAction.action.Disable();
    }

    private void Update()
    {
        HandleKeyboardPan();
        HandleTouchPan();
        ApplyBounds();
        HandleZoom();
    }

    private void HandleKeyboardPan()
    {
        
        Vector2 input = panAction.action.ReadValue<Vector2>();

        Vector3 targetVelocity = new Vector3(
            input.x,
            0f,
            input.y
        ) * panSpeed;

        float smoothSpeed = input.sqrMagnitude > 0.01f
            ? acceleration
            : deceleration;

        currentVelocity = Vector3.MoveTowards(
            currentVelocity,
            targetVelocity,
            smoothSpeed * Time.deltaTime
        );

        transform.position += currentVelocity * Time.deltaTime;
    }
    private void HandleZoom()
    {
        float input = zoomAction.action.ReadValue<float>();

        if (Mathf.Abs(input) > 0.01f)
        {
            targetZoom -= input * zoomSpeed;
            targetZoom = Mathf.Clamp(
                targetZoom,
                minZoom,
                maxZoom
            );
        }

        float currentZoom = gameplayCamera.localPosition.magnitude;

        currentZoom = Mathf.Lerp(
            currentZoom,
            targetZoom,
            zoomSmoothness * Time.deltaTime
        );

        gameplayCamera.localPosition =
            initialCameraDirection * currentZoom;
    }
    private void HandleTouchPan()
    {
        if (!touchPressAction.action.IsPressed())
        {
            lastTouchPosition = Vector2.zero;
            return;
        }

        Vector2 currentTouchPosition =
            touchPositionAction.action.ReadValue<Vector2>();

        if (lastTouchPosition == Vector2.zero)
        {
            lastTouchPosition = currentTouchPosition;
            return;
        }

        Vector2 delta =
            currentTouchPosition - lastTouchPosition;

        lastTouchPosition = currentTouchPosition;

        // Напрямок вправо відносно камери
        Vector3 cameraRight = gameplayCamera.transform.right;

        // Напрямок вперед відносно камери
        Vector3 cameraForward = gameplayCamera.transform.forward;

        // Прибираємо вертикальну складову.
        // Нам потрібен рух тільки по площині XZ.
        cameraRight.y = 0f;
        cameraForward.y = 0f;

        cameraRight.Normalize();
        cameraForward.Normalize();

        // Перетворюємо рух пальця в рух по світу.
        Vector3 movement =
            (-cameraRight * delta.x) +
            (-cameraForward * delta.y);

        transform.position +=
            movement * touchPanSpeed;
    }

    private void ApplyBounds()
    {
        Vector3 position = transform.position;

        position.x = Mathf.Clamp(
            position.x,
            xBounds.x,
            xBounds.y
        );

        position.z = Mathf.Clamp(
            position.z,
            zBounds.x,
            zBounds.y
        );

        transform.position = position;
    }
}