using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerPickup : MonoBehaviour
{
    [SerializeField] private Camera viewCamera;
    [SerializeField] private LayerMask pickupLayers = Physics.DefaultRaycastLayers;
    [SerializeField] private float holdDistance = 1.6f;
    [SerializeField] private float holdBelowCamera = 0.35f;
    [SerializeField] private float dropSpeed = 1.5f;
    [SerializeField] private float placementRange = 3f;

    private const float CarryResponse = 12f;
    private const float MaxCarrySpeed = 10f;
    private const float MaxCarryAcceleration = 65f;
    private const float DropRampTime = 0.1f;

    private Rigidbody playerBody;
    private Rigidbody heldBody;
    private Collider[] heldColliders;
    private readonly List<(Collider Player, Collider Item)> ignoredCollisions = new();
    private float dropTimeRemaining;
    private bool previousGravity;
    private RigidbodyInterpolation previousInterpolation;
    private CollisionDetectionMode previousCollisionDetection;

    internal Rigidbody HeldBody => heldBody;
    internal bool IsDropping => heldBody != null && dropTimeRemaining > 0f;

    private void Awake()
    {
        if (viewCamera == null)
        {
            enabled = false;
            return;
        }

        playerBody = GetComponent<Rigidbody>();
    }

    private void OnDisable() => Release();

    private void FixedUpdate()
    {
        if (heldBody == null)
        {
            Release();
            return;
        }

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

    internal bool CanPickUp(RaycastHit hit)
    {
        Rigidbody candidate = hit.rigidbody;
        return isActiveAndEnabled && heldBody == null && candidate != null && candidate != playerBody &&
               !candidate.isKinematic && (pickupLayers.value & (1 << hit.collider.gameObject.layer)) != 0;
    }

    internal void PickUp(RaycastHit hit)
    {
        RestorePlayerCollisions();
        heldBody = hit.rigidbody;
        heldColliders = heldBody.GetComponentsInChildren<Collider>();
        IgnorePlayerCollisions();
        dropTimeRemaining = 0f;
        previousGravity = heldBody.useGravity;
        previousInterpolation = heldBody.interpolation;
        previousCollisionDetection = heldBody.collisionDetectionMode;
        heldBody.useGravity = false;
        heldBody.interpolation = RigidbodyInterpolation.Interpolate;
        heldBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        heldBody.angularVelocity = Vector3.zero;
    }

    private void IgnorePlayerCollisions()
    {
        foreach (Collider playerCollider in playerBody.GetComponentsInChildren<Collider>())
        {
            if (playerCollider.attachedRigidbody != playerBody || playerCollider.isTrigger)
                continue;

            foreach (Collider itemCollider in heldColliders)
            {
                if (itemCollider.attachedRigidbody != heldBody || itemCollider.isTrigger ||
                    Physics.GetIgnoreCollision(playerCollider, itemCollider))
                    continue;

                Physics.IgnoreCollision(playerCollider, itemCollider);
                ignoredCollisions.Add((playerCollider, itemCollider));
            }
        }
    }

    private void RestorePlayerCollisions()
    {
        foreach ((Collider playerCollider, Collider itemCollider) in ignoredCollisions)
        {
            if (playerCollider != null && itemCollider != null)
                Physics.IgnoreCollision(playerCollider, itemCollider, false);
        }

        ignoredCollisions.Clear();
    }

    internal void BeginDrop()
    {
        if (heldBody == null || IsDropping)
            return;

        heldBody.linearVelocity = Vector3.zero;
        heldBody.angularVelocity = Vector3.zero;

        if (ResolveDropOverlap())
        {
            Release();
            return;
        }

        if (heldBody.SweepTest(Vector3.down, out RaycastHit hit, placementRange, QueryTriggerInteraction.Ignore))
            dropTimeRemaining = hit.distance / dropSpeed + DropRampTime;
        else
            Release();
    }

    private void UpdateDrop()
    {
        if (ResolveDropOverlap())
        {
            heldBody.linearVelocity = Vector3.zero;
            heldBody.angularVelocity = Vector3.zero;
            Release();
            return;
        }

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
            velocity.y = Mathf.Max(velocity.y, -hit.distance / step);
            dropTimeRemaining = step;
        }

        heldBody.linearVelocity = velocity;
    }

    private bool ResolveDropOverlap()
    {
        Vector3 correction = Vector3.zero;
        Vector3 bodyOffset = heldBody.position - heldBody.transform.position;

        // Resolve corners in a few passes before handing the item back to physics.
        for (int pass = 0; pass < 4; pass++)
        {
            bool overlapping = false;
            foreach (Collider item in heldColliders)
            {
                if (item == null || !item.enabled || item.isTrigger || item.attachedRigidbody != heldBody)
                    continue;

                Bounds bounds = item.bounds;
                Collider[] nearby = Physics.OverlapBox(bounds.center + bodyOffset + correction,
                    bounds.extents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                foreach (Collider other in nearby)
                {
                    bool playerCollision = ignoredCollisions.Contains((other, item));
                    if (other.attachedRigidbody == heldBody ||
                        (Physics.GetIgnoreCollision(item, other) && !playerCollision) ||
                        Physics.GetIgnoreLayerCollision(item.gameObject.layer, other.gameObject.layer))
                        continue;

                    Vector3 otherOffset = other.attachedRigidbody != null
                        ? other.attachedRigidbody.position - other.attachedRigidbody.transform.position
                        : Vector3.zero;
                    if (Physics.ComputePenetration(item, item.transform.position + bodyOffset + correction,
                            item.transform.rotation, other, other.transform.position + otherOffset,
                            other.transform.rotation, out Vector3 direction, out float distance))
                    {
                        correction += direction * (distance + 0.01f);
                        overlapping = true;
                    }
                }
            }

            if (!overlapping)
                break;
        }

        if (correction == Vector3.zero)
            return false;

        heldBody.position += correction;
        return true;
    }

    internal void Release()
    {
        dropTimeRemaining = 0f;
        RestorePlayerCollisions();
        heldColliders = null;
        if (heldBody == null)
            return;

        heldBody.useGravity = previousGravity;
        heldBody.interpolation = previousInterpolation;
        heldBody.collisionDetectionMode = previousCollisionDetection;
        heldBody.WakeUp();
        heldBody = null;
    }
}
