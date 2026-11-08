using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Camera viewCamera;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float groundAcceleration = 25f;
    [SerializeField] private float airAcceleration = 6f;
    [SerializeField] private float jumpSpeed = 5f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayers = Physics.DefaultRaycastLayers;
    [SerializeField] private float groundCheckDistance = 0.08f;
    [SerializeField] private float maxGroundAngle = 50f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float stickLookSpeed = 180f;

    private Rigidbody body;
    private CapsuleCollider capsule;
    private InputActionMap playerActions;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private Vector2 moveInput;
    private bool jumpQueued;
    private bool jumpingUp;
    private float yaw;
    private float pitch;

    private void Awake()
    {
        InputActionMap sourceMap = inputActions != null ? inputActions.FindActionMap("Player") : null;
        if (sourceMap == null || sourceMap.FindAction("Move") == null ||
            sourceMap.FindAction("Look") == null || sourceMap.FindAction("Jump") == null ||
            viewCamera == null)
        {
            enabled = false;
            return;
        }

        playerActions = sourceMap.Clone();
        moveAction = playerActions.FindAction("Move", true);
        lookAction = playerActions.FindAction("Look", true);
        jumpAction = playerActions.FindAction("Jump", true);

        body = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        body.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        yaw = body.rotation.eulerAngles.y;
        pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, viewCamera.transform.eulerAngles.x), -85f, 85f);
    }

    private void OnEnable()
    {
        playerActions.Enable();
        SetCursorLocked(true);
    }

    private void OnDisable()
    {
        if (playerActions == null)
            return;

        playerActions.Disable();
        moveInput = Vector2.zero;
        jumpQueued = false;
        SetCursorLocked(false);
    }

    private void OnDestroy() => playerActions?.Dispose();

    private void Update()
    {
        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            SetCursorLocked(false);
        else if (Cursor.lockState != CursorLockMode.Locked &&
                 Mouse.current?.leftButton.wasPressedThisFrame == true)
            SetCursorLocked(true);

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            moveInput = Vector2.zero;
            jumpQueued = false;
            return;
        }

        moveInput = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
        jumpQueued |= jumpAction.WasPressedThisFrame();

        Vector2 lookInput = lookAction.ReadValue<Vector2>();
        float lookScale = lookAction.activeControl?.device is Pointer
            ? mouseSensitivity
            : stickLookSpeed * Time.deltaTime;
        yaw = Mathf.Repeat(yaw + lookInput.x * lookScale, 360f);
        pitch = Mathf.Clamp(pitch - lookInput.y * lookScale, -85f, 85f);
    }

    private void LateUpdate()
    {
        viewCamera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void FixedUpdate()
    {
        Quaternion facing = Quaternion.Euler(0f, yaw, 0f);
        body.MoveRotation(facing);

        Vector3 velocity = body.linearVelocity;
        if (velocity.y <= 0f)
            jumpingUp = false;

        bool grounded = !jumpingUp && IsGrounded();
        if (grounded || moveInput.sqrMagnitude > 0f)
        {
            Vector3 targetVelocity = facing * new Vector3(moveInput.x, 0f, moveInput.y) * moveSpeed;
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
            float acceleration = grounded ? groundAcceleration : airAcceleration;
            Vector3 change = Vector3.ClampMagnitude(targetVelocity - horizontalVelocity,
                acceleration * Time.fixedDeltaTime);
            body.AddForce(change, ForceMode.VelocityChange);
        }

        if (jumpQueued && grounded && jumpSpeed > 0f)
        {
            body.AddForce(Vector3.up * jumpSpeed, ForceMode.VelocityChange);
            jumpingUp = true;
        }

        jumpQueued = false;
    }

    private bool IsGrounded()
    {
        Vector3 scale = transform.lossyScale;
        float radius = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float halfHeight = Mathf.Max(capsule.height * Mathf.Abs(scale.y) * 0.5f, radius);
        Vector3 center = body.position + body.rotation * Vector3.Scale(capsule.center, scale);
        Vector3 lowerSphereCenter = center - Vector3.up * (halfHeight - radius);

        // Start the probe inside the capsule so it can sweep down to the floor.
        float probeRadius = radius * 0.9f;
        float probeDistance = groundCheckDistance + radius - probeRadius;
        return Physics.SphereCast(lowerSphereCenter, probeRadius, Vector3.down, out RaycastHit hit,
                   probeDistance, groundLayers, QueryTriggerInteraction.Ignore) &&
               Vector3.Dot(hit.normal, Vector3.up) >= Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
