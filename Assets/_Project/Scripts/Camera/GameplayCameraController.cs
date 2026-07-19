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

    [Header("Input")]
    [SerializeField] private InputActionReference panAction;
    [SerializeField] private InputActionReference touch0PositionAction;
    [SerializeField] private InputActionReference touch1PositionAction;

    [SerializeField] private InputActionReference touch0PressAction;
    [SerializeField] private InputActionReference touch1PressAction;
    [SerializeField] private float touchPanSpeed = 0.01f;
    private Vector2 lastTouchPosition;

    [Header("Zoom")]
    [SerializeField] private Transform gameplayCamera;
    [SerializeField] private InputActionReference zoomAction;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float zoomSmoothness = 10f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 20f;

    [Header("Pinch Zoom")]
    [SerializeField] private float pinchSensitivity = 0.01f;

    private float lastPinchDistance;
    private bool isPinching;

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
        zoomAction.action.Enable();

        touch0PositionAction.action.Enable();
        touch1PositionAction.action.Enable();

        touch0PressAction.action.Enable();
        touch1PressAction.action.Enable();
    }

    private void OnDisable()
    {
        panAction.action.Disable();
        zoomAction.action.Disable();

        touch0PositionAction.action.Disable();
        touch1PositionAction.action.Disable();

        touch0PressAction.action.Disable();
        touch1PressAction.action.Disable();
    }

    private void Update()
    {
        HandleKeyboardPan();
        HandleTouchPan();
        HandleZoom();
        HandlePinchZoom();

        ApplyBounds();
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
    private void HandlePinchZoom()
    {
        bool touch0Pressed =
            touch0PressAction.action.IsPressed();

        bool touch1Pressed =
            touch1PressAction.action.IsPressed();

        // Для Pinch потрібні два пальці
        if (!touch0Pressed || !touch1Pressed)
        {
            isPinching = false;
            return;
        }

        Vector2 touch0 =
            touch0PositionAction.action.ReadValue<Vector2>();

        Vector2 touch1 =
            touch1PositionAction.action.ReadValue<Vector2>();

        float currentDistance =
            Vector2.Distance(touch0, touch1);

        // Перший кадр Pinch
        if (!isPinching)
        {
            isPinching = true;
            lastPinchDistance = currentDistance;
            return;
        }

        float pinchDelta =
            currentDistance - lastPinchDistance;

        lastPinchDistance = currentDistance;

        // Розводимо пальці → наближаємо
        // Зводимо пальці → віддаляємо
        targetZoom -=
            pinchDelta * pinchSensitivity;

        targetZoom = Mathf.Clamp(
            targetZoom,
            minZoom,
            maxZoom
        );
    }
    private void HandleTouchPan()
    {
        // Якщо два пальці на екрані — Pan не працює,
        // бо зараз використовується Pinch Zoom
        if (touch0PressAction.action.IsPressed() &&
            touch1PressAction.action.IsPressed())
        {
            lastTouchPosition = Vector2.zero;
            return;
        }

        // Якщо перший палець не натиснутий — скидаємо позицію
        if (!touch0PressAction.action.IsPressed())
        {
            lastTouchPosition = Vector2.zero;
            return;
        }

        // Отримуємо позицію першого пальця
        Vector2 currentTouchPosition =
            touch0PositionAction.action.ReadValue<Vector2>();

        // Перший кадр дотику
        if (lastTouchPosition == Vector2.zero)
        {
            lastTouchPosition = currentTouchPosition;
            return;
        }

        // Різниця позиції пальця між кадрами
        Vector2 delta =
            currentTouchPosition - lastTouchPosition;

        lastTouchPosition = currentTouchPosition;

        // Напрямки камери
        Vector3 cameraRight =
            gameplayCamera.transform.right;

        Vector3 cameraForward =
            gameplayCamera.transform.forward;

        // Ігноруємо вертикаль
        cameraRight.y = 0f;
        cameraForward.y = 0f;

        cameraRight.Normalize();
        cameraForward.Normalize();

        // Рух камери
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