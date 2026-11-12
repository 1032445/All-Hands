using UnityEngine;
using UnityEngine.InputSystem;

public interface IInteractable
{
    HudMessage GetInteractionPrompt(PlayerInteractor player);

    bool TryInteract(PlayerInteractor player);
}

public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Camera viewCamera;
    [SerializeField] private float interactionRange = 3.5f;
    [SerializeField] private RitualRole role;
    [SerializeField] private RitualSite matchSite;

    private PlayerPickup pickup;
    private InputAction pickupAction;
    private InputAction interactAction;
    private readonly RaycastHit[] hitBuffer = new RaycastHit[16];
    private HudMessage feedback;
    private float feedbackUntil;
    private RitualItem curseTarget;
    private float curseElapsed;
    private CleansingStation cleanseTarget;
    private RitualItem cleanseItem;
    private float cleanseElapsed;

    public RitualRole Role => role;
    public Rigidbody HeldBody => pickup.HeldBody;
    public RitualSite MatchSite => matchSite;
    public HudMessage InteractionPrompt { get; private set; }
    public HudMessage PickupPrompt { get; private set; }
    public HudMessage Feedback => Time.unscaledTime < feedbackUntil ? feedback : HudMessage.None;
    public bool IsProcessing => IsCursing || IsCleansing;
    public bool IsCursing => curseTarget != null;
    public bool IsCleansing => cleanseTarget != null && cleanseItem != null;
    internal RitualItem CurseTarget => curseTarget;
    public float ProcessProgress => IsCursing
        ? Mathf.Clamp01(curseElapsed / curseTarget.CurseDuration)
        : IsCleansing ? Mathf.Clamp01(cleanseElapsed / cleanseItem.CurseDuration) : 0f;
    public float ProcessTimeRemaining => IsCursing
        ? Mathf.Max(0f, curseTarget.CurseDuration - curseElapsed)
        : IsCleansing ? Mathf.Max(0f, cleanseItem.CurseDuration - cleanseElapsed) : 0f;

    private void Awake()
    {
        InputAction pickupSource = inputActions != null ? inputActions.FindAction("Player/Pickup") : null;
        InputAction interactSource = inputActions != null ? inputActions.FindAction("Player/Interact") : null;
        if (pickupSource == null || interactSource == null || viewCamera == null)
        {
            enabled = false;
            return;
        }

        pickup = GetComponent<PlayerPickup>();
        pickupAction = pickupSource.Clone();
        interactAction = interactSource.Clone();
    }

    private void OnEnable()
    {
        pickupAction?.Enable();
        interactAction?.Enable();
    }

    private void OnDisable()
    {
        CancelTimedActions();
        pickupAction?.Disable();
        interactAction?.Disable();
    }

    private void OnDestroy()
    {
        pickupAction?.Dispose();
        interactAction?.Dispose();
    }

    private void LateUpdate()
    {
        InteractionPrompt = HudMessage.None;
        PickupPrompt = HudMessage.None;
        if (pickup.IsDropping || (matchSite != null && matchSite.Winner.HasValue))
        {
            CancelTimedActions();
            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            CancelTimedActions();
            return;
        }

        UpdateInteraction(pickupAction.WasPressedThisFrame(), interactAction.WasPressedThisFrame(),
            interactAction.IsPressed());
    }

    private void UpdateInteraction(bool pickupPressed, bool interactPressed, bool interactHeld)
    {
        Ray viewRay = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit = GetTarget(viewRay, interactionRange, QueryTriggerInteraction.Collide);
        IInteractable target = hit.collider != null ? hit.collider.GetComponentInParent<IInteractable>() : null;
        InteractionPrompt = target?.GetInteractionPrompt(this) ?? HudMessage.None;
        if (InteractionPrompt == HudMessage.None && CanOfferAtNearbySite())
        {
            target = matchSite;
            InteractionPrompt = target.GetInteractionPrompt(this);
        }

        UpdateCurse(target, interactHeld && !pickupPressed);
        UpdateCleanse(target, interactHeld && !pickupPressed);

        if (HeldBody == null && hit.collider != null && hit.collider.isTrigger)
            hit = GetTarget(viewRay, interactionRange, QueryTriggerInteraction.Ignore);

        bool canPickUp = pickup.CanPickUp(hit);
        PickupPrompt = HudMessage.None;
        if (HeldBody != null)
            PickupPrompt = HudMessage.Drop;
        else if (canPickUp)
            PickupPrompt = HudMessage.Pickup;

        if (interactPressed)
        {
            target?.TryInteract(this);
            return;
        }

        if (!pickupPressed)
            return;

        if (HeldBody != null)
        {
            pickup.BeginDrop();
        }
        else if (canPickUp)
        {
            pickup.PickUp(hit);
            ShowFeedback(HudMessage.PickedUp);
        }
    }

    private bool CanOfferAtNearbySite()
    {
        if (matchSite == null || !matchSite.isActiveAndEnabled || matchSite.GetOffering(this) == null ||
            !matchSite.TryGetOfferBounds(this, out Bounds siteBounds))
            return false;

        if (siteBounds.Contains(transform.position))
            return true;

        Vector3 eyePosition = viewCamera.transform.position;
        Vector3 toSite = siteBounds.center - eyePosition;
        if (Vector3.Dot(viewCamera.transform.forward, toSite.normalized) < 0.5f)
            return false;

        RaycastHit obstruction = GetTarget(new Ray(eyePosition, toSite), toSite.magnitude,
            QueryTriggerInteraction.Ignore);
        return obstruction.collider == null || obstruction.collider.transform.IsChildOf(matchSite.transform);
    }

    private RaycastHit GetTarget(Ray ray, float range, QueryTriggerInteraction triggers)
    {
        RaycastHit[] hits = hitBuffer;
        int count = Physics.RaycastNonAlloc(ray, hits, range, Physics.DefaultRaycastLayers, triggers);
        if (count == hits.Length)
        {
            hits = Physics.RaycastAll(ray, range, Physics.DefaultRaycastLayers, triggers);
            count = hits.Length;
        }

        RaycastHit nearest = default;
        float distance = float.PositiveInfinity;
        Rigidbody held = HeldBody;

        for (int index = 0; index < count; index++)
        {
            RaycastHit hit = hits[index];
            if (hit.distance >= distance || hit.collider.transform.IsChildOf(transform) ||
                (held != null && hit.rigidbody == held))
                continue;

            if (hit.collider.isTrigger && hit.collider.GetComponentInParent<IInteractable>() == null)
                continue;

            nearest = hit;
            distance = hit.distance;
        }

        return nearest;
    }

    public void ShowFeedback(HudMessage message)
    {
        feedback = message;
        feedbackUntil = Time.unscaledTime + 3f;
    }

    internal void BeginCurse(RitualItem item)
    {
        CancelCleanse();
        curseTarget = item;
        curseElapsed = 0f;
    }

    internal void BeginCleanse(CleansingStation station, RitualItem item)
    {
        CancelCurse();
        cleanseTarget = station;
        cleanseItem = item;
        cleanseElapsed = 0f;
    }

    private void UpdateCurse(IInteractable target, bool held)
    {
        if (!IsCursing)
            return;

        if (!held || !ReferenceEquals(target, curseTarget) ||
            curseTarget.GetInteractionPrompt(this) != HudMessage.Curse)
        {
            CancelCurse();
            return;
        }

        curseElapsed += Time.deltaTime;
        if (curseElapsed >= curseTarget.CurseDuration)
        {
            curseTarget.CompleteCurse(this);
            CancelCurse();
        }
    }

    private void UpdateCleanse(IInteractable target, bool held)
    {
        if (!IsCleansing)
            return;

        bool stillHoldingItem = HeldBody != null && HeldBody.TryGetComponent(out RitualItem heldItem) &&
                                heldItem == cleanseItem;
        if (!held || !ReferenceEquals(target, cleanseTarget) || !stillHoldingItem ||
            cleanseTarget.GetInteractionPrompt(this) != HudMessage.Cleanse)
        {
            CancelCleanse();
            return;
        }

        cleanseElapsed += Time.deltaTime;
        if (cleanseElapsed >= cleanseItem.CurseDuration)
        {
            cleanseTarget.CompleteCleanse(this, cleanseItem);
            CancelCleanse();
        }
    }

    private void CancelCurse()
    {
        curseTarget = null;
        curseElapsed = 0f;
    }

    private void CancelCleanse()
    {
        cleanseTarget = null;
        cleanseItem = null;
        cleanseElapsed = 0f;
    }

    private void CancelTimedActions()
    {
        CancelCurse();
        CancelCleanse();
    }

    internal void ReleaseHeldObject() => pickup.Release();
}
