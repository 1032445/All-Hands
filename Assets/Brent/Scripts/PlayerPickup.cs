using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerPickup : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Camera viewCamera;
    [SerializeField] private LayerMask pickupLayers = Physics.DefaultRaycastLayers;
    [SerializeField, Min(0.1f)] private float pickupRange = 3.5f;
    [SerializeField, Min(0.1f)] private float holdDistance = 1.6f;
    [SerializeField, Min(0f)] private float holdBelowCamera = 0.35f;
    [SerializeField, Min(0.1f)] private float dropSpeed = 1.5f;
    [SerializeField, Min(0.1f)] private float placementRange = 3f;

    private const float CarryResponse = 12f;
    private const float MaxCarrySpeed = 10f;
    private const float MaxCarryAcceleration = 65f;
    private const float DropRampTime = 0.1f;

    private Rigidbody playerBody;
    private Rigidbody heldBody;
    private InputAction interactAction;
    private float dropTimeRemaining;
    private bool previousGravity;
    private RigidbodyInterpolation previousInterpolation;
    private CollisionDetectionMode previousCollisionDetection;

    private void Awake()
    {
        InputAction source = inputActions != null ? inputActions.FindAction("Player/Interact") : null;
        if (source == null || viewCamera == null)
        {
            enabled = false;
            return;
        }

        playerBody = GetComponent<Rigidbody>();
        interactAction = source.Clone();
    }

    private void OnEnable() => interactAction?.Enable();

    private void OnDisable()
    {
        interactAction?.Disable();
        Release();
    }

    private void OnDestroy() => interactAction?.Dispose();

    private void LateUpdate()
    {
        if (Cursor.lockState != CursorLockMode.Locked || !interactAction.WasPressedThisFrame())
            return;

        if (heldBody == null)
            TryPickUp();
        else if (dropTimeRemaining <= 0f)
            BeginDrop();
    }

    private void FixedUpdate()
    {
        if (heldBody == null)
            return;

        if (dropTimeRemaining > 0f)
        {
            UpdateDrop();
            return;
        }

        Vector3 forward = Vector3.ProjectOnPlane(viewCamera.transform.forward, Vector3.up).normalized;
        Vector3 cameraOffset = viewCamera.transform.position - transform.position;
        Vector3 holdPoint = playerBody.position + cameraOffset + forward * holdDistance -
                            Vector3.up * holdBelowCamera;

        Vector3 correction = (holdPoint - heldBody.worldCenterOfMass) * CarryResponse;
        Vector3 targetVelocity = Vector3.ClampMagnitude(playerBody.linearVelocity + correction, MaxCarrySpeed);
        Vector3 velocityChange = Vector3.ClampMagnitude(targetVelocity - heldBody.linearVelocity,
            MaxCarryAcceleration * Time.fixedDeltaTime);
        heldBody.AddForce(velocityChange, ForceMode.VelocityChange);
    }

    private void TryPickUp()
    {
        Ray ray = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, pickupRange, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
            return;

        Rigidbody candidate = hit.rigidbody;
        if (candidate == null || candidate == playerBody || candidate.isKinematic ||
            (pickupLayers.value & (1 << hit.collider.gameObject.layer)) == 0)
            return;

        heldBody = candidate;
        dropTimeRemaining = 0f;
        previousGravity = heldBody.useGravity;
        previousInterpolation = heldBody.interpolation;
        previousCollisionDetection = heldBody.collisionDetectionMode;
        heldBody.useGravity = false;
        heldBody.interpolation = RigidbodyInterpolation.Interpolate;
        heldBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        heldBody.angularVelocity = Vector3.zero;
    }

    private void BeginDrop()
    {
        heldBody.linearVelocity = Vector3.zero;
        heldBody.angularVelocity = Vector3.zero;

        if (heldBody.SweepTest(Vector3.down, out RaycastHit hit, placementRange, QueryTriggerInteraction.Ignore))
            dropTimeRemaining = hit.distance / dropSpeed + DropRampTime;
        else
            Release();
    }

    private void UpdateDrop()
    {
        float step = Time.fixedDeltaTime;
        dropTimeRemaining -= step;
        if (dropTimeRemaining <= 0f)
        {
            Release();
            return;
        }

        Vector3 velocity = heldBody.linearVelocity;
        velocity.y = Mathf.MoveTowards(velocity.y, -dropSpeed, dropSpeed * step / DropRampTime);

        float landingDistance = Mathf.Max(0.02f, -velocity.y * step);
        if (heldBody.SweepTest(Vector3.down, out RaycastHit hit, landingDistance, QueryTriggerInteraction.Ignore))
        {
            // Keep continuous collision detection active through the landing step.
            velocity.y = Mathf.Max(velocity.y, -hit.distance / step);
            dropTimeRemaining = step;
        }

        heldBody.linearVelocity = velocity;
    }

    private void Release()
    {
        dropTimeRemaining = 0f;
        if (heldBody == null)
            return;

        heldBody.useGravity = previousGravity;
        heldBody.interpolation = previousInterpolation;
        heldBody.collisionDetectionMode = previousCollisionDetection;
        heldBody.WakeUp();
        heldBody = null;
    }
}
