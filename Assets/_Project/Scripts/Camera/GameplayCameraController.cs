using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameplayCameraController : MonoBehaviour
{
    [Header("Camera Movement")]
    [SerializeField] private float panSpeed = 10f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float deceleration = 15f;

    [Header("Camera Bounds")]
    [SerializeField] private Vector2 xBounds = new Vector2(-20f, 20f);
    [SerializeField] private Vector2 zBounds = new Vector2(-20f, 20f);

    [Header("Input")]
    [SerializeField] private InputActionReference panAction;
    [SerializeField] private InputActionReference touch0PositionAction;
    [SerializeField] private InputActionReference touch1PositionAction;

    [SerializeField] private InputActionReference touch0PressAction;
    [SerializeField] private InputActionReference touch1PressAction;
    [SerializeField] private float touchPanSpeed = 0.01f;

    private Vector2 lastTouchPosition;
    private Vector2 touchPressPosition;
    private bool trackingTouch;
    private bool draggingTouch;
    private bool mouseWasPressed;
    private bool mouseBlockedByUI;
    private bool draggingMouse;
    private Vector2 mousePressPosition;
    private Vector2 lastMousePosition;
    private readonly System.Collections.Generic.List<RaycastResult> uiHits =
        new System.Collections.Generic.List<RaycastResult>();
    public bool SuppressZoneClick { get; private set; }

    [Header("Zoom")]
    [SerializeField] private TMPro.TMP_Text debugText;
    [SerializeField] private Transform gameplayCamera;
    [SerializeField] private InputActionReference zoomAction;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float zoomSmoothness = 10f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 20f;

    [Header("Pinch Zoom")]
    [SerializeField] private float pinchSensitivity = 0.01f;

    [Header("Zone Camera Movement")]
    [SerializeField] private float zoneMoveDuration = 0.8f;

    private float lastPinchDistance;
    private bool isPinching;

    private float targetZoom;
    private Vector3 initialCameraDirection;

    private Vector3 currentVelocity;

    private bool isMovingToZone;
    private Vector3 zoneMoveStartPosition;
    private Vector3 zoneMoveTargetPosition;
    private float zoneMoveTimer;

    private bool touch0WasPressed;
    private bool touch1WasPressed;

    private bool touch0BlockedByUI;
    private bool touch1BlockedByUI;

    public event Action UserStartedCameraMovement;

    private void Start()
    {
        targetZoom =
            gameplayCamera.localPosition.magnitude;

        initialCameraDirection =
            gameplayCamera.localPosition.normalized;
    }

    private void OnEnable()
    {
        panAction.action.Enable();
        zoomAction.action.Enable();

        touch0PositionAction.action.Enable();
        touch1PositionAction.action.Enable();

        touch0PressAction.action.Enable();
        touch1PressAction.action.Enable();
    }

    private void OnDisable()
    {
        trackingTouch = draggingTouch = draggingMouse = mouseWasPressed = false;
        touch0WasPressed = touch1WasPressed = false;
        touch0BlockedByUI = touch1BlockedByUI = false;
        isPinching = false;
        panAction.action.Disable();
        zoomAction.action.Disable();

        touch0PositionAction.action.Disable();
        touch1PositionAction.action.Disable();

        touch0PressAction.action.Disable();
        touch1PressAction.action.Disable();
    }

    private void Update()
    {
        UpdateTouchUIState();
        HandleKeyboardPan();
        HandleTouchPan();
        HandleMousePan();
        HandleZoom();
        HandlePinchZoom();
        if (isMovingToZone) HandleZoneCameraMovement();
        ApplyBounds();
    }

    private void UpdateTouchUIState()
    {
        bool touch0Pressed =
            touch0PressAction.action.IsPressed();

        bool touch1Pressed =
            touch1PressAction.action.IsPressed();

        Vector2 touch0Position =
            touch0PositionAction.action.ReadValue<Vector2>();

        Vector2 touch1Position =
            touch1PositionAction.action.ReadValue<Vector2>();

        if (touch0Pressed && !touch0WasPressed)
        {
            SuppressZoneClick = false;
            touch0BlockedByUI =
                IsScreenPositionOverUI(
                    touch0Position);
        }

        if (touch1Pressed && !touch1WasPressed)
        {
            touch1BlockedByUI =
                IsScreenPositionOverUI(
                    touch1Position);
        }

        if (!touch0Pressed)
        {
            touch0BlockedByUI = false;
        }

        if (!touch1Pressed)
        {
            touch1BlockedByUI = false;
        }

        touch0WasPressed =
            touch0Pressed;

        touch1WasPressed =
            touch1Pressed;
    }

    private bool IsScreenPositionOverUI(
        Vector2 screenPosition)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData pointerData =
            new PointerEventData(
                EventSystem.current);

        pointerData.position =
            screenPosition;

        uiHits.Clear();
        EventSystem.current.RaycastAll(
            pointerData,
            uiHits);
        foreach (RaycastResult hit in uiHits)
            if (IsUIHit(hit)) return true;
        return false;
    }

    private static bool IsUIHit(RaycastResult hit)
    {
        // PhysicsRaycaster hits on factory zones are world input, not UI.
        return hit.module is GraphicRaycaster;
    }

    public static bool ExceedsDragThreshold(Vector2 start, Vector2 current)
    {
        float threshold = EventSystem.current != null ? Mathf.Max(1, EventSystem.current.pixelDragThreshold) : 10f;
        return (current - start).sqrMagnitude >= threshold * threshold;
    }

    private void HandleKeyboardPan()
    {
        Vector2 input =
            panAction.action.ReadValue<Vector2>();

        if (input.sqrMagnitude > 0.01f)
        {
            NotifyUserStartedCameraMovement();
        }

        Vector3 targetVelocity =
            new Vector3(
                input.x,
                0f,
                input.y
            ) * panSpeed;

        float smoothSpeed =
            input.sqrMagnitude > 0.01f
                ? acceleration
                : deceleration;

        currentVelocity =
            Vector3.MoveTowards(
                currentVelocity,
                targetVelocity,
                smoothSpeed * Time.deltaTime
            );

        transform.position +=
            currentVelocity * Time.deltaTime;
    }

    private void HandleZoom()
    {
        float input =
            zoomAction.action.ReadValue<float>();

        if (Mathf.Abs(input) > 0.01f)
        {
            NotifyUserStartedCameraMovement();

            targetZoom -=
                input * zoomSpeed;

            targetZoom =
                Mathf.Clamp(
                    targetZoom,
                    minZoom,
                    maxZoom
                );
        }

        float currentZoom =
            gameplayCamera.localPosition.magnitude;

        currentZoom =
            Mathf.Lerp(
                currentZoom,
                targetZoom,
                zoomSmoothness * Time.deltaTime
            );

        gameplayCamera.localPosition =
            initialCameraDirection *
            currentZoom;
    }

    private void HandlePinchZoom()
    {
        bool touch0Pressed =
            touch0PressAction.action.IsPressed();

        bool touch1Pressed =
            touch1PressAction.action.IsPressed();

        Vector2 touch0 =
            touch0PositionAction.action.ReadValue<Vector2>();

        Vector2 touch1 =
            touch1PositionAction.action.ReadValue<Vector2>();

        float currentDistance =
            Vector2.Distance(
                touch0,
                touch1
            );

        float pinchDelta = 0f;

        if (debugText != null)
        {
            debugText.text =
                "Touch0: " + touch0Pressed +
                "\nTouch1: " + touch1Pressed +
                "\nTouch0 Pos: " + touch0 +
                "\nTouch1 Pos: " + touch1 +
                "\nDistance: " + currentDistance +
                "\nPinch Delta: " + pinchDelta +
                "\nTarget Zoom: " + targetZoom;
        }

        if (!touch0Pressed ||
            !touch1Pressed)
        {
            isPinching = false;
            return;
        }

        if (touch0BlockedByUI ||
            touch1BlockedByUI)
        {
            isPinching = false;
            return;
        }

        if (!isPinching)
        {
            SuppressZoneClick = true;
            isPinching = true;

            lastPinchDistance =
                currentDistance;

            return;
        }

        pinchDelta =
            currentDistance -
            lastPinchDistance;

        lastPinchDistance =
            currentDistance;

        if (debugText != null)
        {
            debugText.text =
                "Touch0: " + touch0Pressed +
                "\nTouch1: " + touch1Pressed +
                "\nTouch0 Pos: " + touch0 +
                "\nTouch1 Pos: " + touch1 +
                "\nDistance: " + currentDistance +
                "\nPinch Delta: " + pinchDelta +
                "\nTarget Zoom: " + targetZoom;
        }

        if (Mathf.Abs(pinchDelta) > 0.001f)
        {
            NotifyUserStartedCameraMovement();
        }

        targetZoom -=
            pinchDelta *
            pinchSensitivity;

        targetZoom =
            Mathf.Clamp(
                targetZoom,
                minZoom,
                maxZoom
            );
    }

    private void HandleTouchPan()
    {
        if (touch0PressAction.action.IsPressed() &&
            touch1PressAction.action.IsPressed())
        {
            trackingTouch = false;
            draggingTouch = false;

            return;
        }

        if (!touch0PressAction.action.IsPressed())
        {
            trackingTouch = false;
            draggingTouch = false;

            return;
        }

        if (touch0BlockedByUI)
        {
            trackingTouch = false;
            draggingTouch = false;

            return;
        }

        Vector2 currentTouchPosition =
            touch0PositionAction.action.ReadValue<Vector2>();

        if (!trackingTouch)
        {
            trackingTouch = true;
            touchPressPosition = currentTouchPosition;
            lastTouchPosition =
                currentTouchPosition;

            return;
        }

        if (!draggingTouch)
        {
            if (!ExceedsDragThreshold(touchPressPosition, currentTouchPosition)) return;
            draggingTouch = true;
            SuppressZoneClick = true;
        }

        Vector2 delta =
            currentTouchPosition -
            lastTouchPosition;

        lastTouchPosition =
            currentTouchPosition;

        if (delta.sqrMagnitude > 0.01f)
        {
            NotifyUserStartedCameraMovement();
        }

        Vector3 cameraRight =
            gameplayCamera.transform.right;

        Vector3 cameraForward =
            gameplayCamera.transform.forward;

        cameraRight.y = 0f;
        cameraForward.y = 0f;

        cameraRight.Normalize();
        cameraForward.Normalize();

        Vector3 movement =
            (-cameraRight * delta.x) +
            (-cameraForward * delta.y);

        transform.position +=
            movement *
            touchPanSpeed;
    }

    private void HandleZoneCameraMovement()
    {
        zoneMoveTimer +=
            Time.deltaTime;

        float normalizedTime =
            zoneMoveTimer /
            zoneMoveDuration;

        normalizedTime =
            Mathf.Clamp01(
                normalizedTime
            );

        float smoothTime =
            Mathf.SmoothStep(
                0f,
                1f,
                normalizedTime
            );

        transform.position =
            Vector3.Lerp(
                zoneMoveStartPosition,
                zoneMoveTargetPosition,
                smoothTime
            );

        if (normalizedTime >= 1f)
        {
            transform.position =
                zoneMoveTargetPosition;

            isMovingToZone = false;
            currentVelocity = Vector3.zero;

            ApplyBounds();
        }
    }

    public void MoveToZone(
        Transform zoneArea)
    {
        if (zoneArea == null)
        {
            return;
        }

        zoneMoveStartPosition =
            transform.position;

        zoneMoveTargetPosition =
            zoneArea.position;

        zoneMoveTimer = 0f;
        isMovingToZone = true;

        currentVelocity =
            Vector3.zero;
    }

    private void NotifyUserStartedCameraMovement()
    {
        isMovingToZone = false;
        UserStartedCameraMovement?.Invoke();
    }

    private void HandleMousePan()
    {
        // Do not process a simulated mouse alongside a real touch gesture.
        if (Mouse.current == null || touch0PressAction.action.IsPressed() || touch1PressAction.action.IsPressed())
        {
            mouseWasPressed = false;
            draggingMouse = false;
            return;
        }
        bool pressed = Mouse.current.leftButton.isPressed;
        Vector2 position = Mouse.current.position.ReadValue();
        if (pressed && !mouseWasPressed)
        {
            mousePressPosition = lastMousePosition = position;
            mouseBlockedByUI = IsScreenPositionOverUI(position);
            draggingMouse = false;
            SuppressZoneClick = false;
        }
        mouseWasPressed = pressed;
        if (!pressed || mouseBlockedByUI) return;
        if (!draggingMouse)
        {
            if (!ExceedsDragThreshold(mousePressPosition, position)) return;
            draggingMouse = true;
            SuppressZoneClick = true;
        }
        Vector2 delta = position - lastMousePosition;
        lastMousePosition = position;
        if (delta.sqrMagnitude <= 0f) return;
        NotifyUserStartedCameraMovement();
        Vector3 right = gameplayCamera.right;
        Vector3 forward = gameplayCamera.forward;
        right.y = forward.y = 0f;
        transform.position += (-right.normalized * delta.x - forward.normalized * delta.y) * touchPanSpeed;
    }

    public bool IsMovingToZone
    {
        get
        {
            return isMovingToZone;
        }
    }

    private void ApplyBounds()
    {
        Vector3 position =
            transform.position;

        position.x =
            Mathf.Clamp(
                position.x,
                xBounds.x,
                xBounds.y
            );

        position.z =
            Mathf.Clamp(
                position.z,
                zBounds.x,
                zBounds.y
            );

        transform.position =
            position;
    }
}
