using UnityEngine;
using UnityEngine.InputSystem;

public enum RitualRole
{
    Crew,
    Cultist
}

public enum HudMessage
{
    None,
    Pickup,
    Drop,
    Curse,
    AlreadyCursed,
    BringItem,
    BringCursedItem,
    Offer,
    MoveCloser,
    PickedUp = 10,
    Dropped,
    NothingToPickUp,
    NothingToInteract,
    Cursed,
    Offered,
    CursedOffering,
    CrewRole,
    CultistRole,
    CrewWin,
    CultistWin
}

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

    public RitualRole Role => role;
    public Rigidbody HeldBody => pickup.HeldBody;
    public RitualSite MatchSite => matchSite;
    public HudMessage InteractionPrompt { get; private set; }
    public HudMessage PickupPrompt { get; private set; }
    public HudMessage Feedback => Time.unscaledTime < feedbackUntil ? feedback : HudMessage.None;
    public bool IsCursing => curseTarget != null;
    public float CurseProgress => IsCursing ? Mathf.Clamp01(curseElapsed / curseTarget.CurseDuration) : 0f;
    public float CurseTimeRemaining => IsCursing ? Mathf.Max(0f, curseTarget.CurseDuration - curseElapsed) : 0f;

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
        CancelCurse();
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
            CancelCurse();
            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            CancelCurse();
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
            if (target == null || !target.TryInteract(this))
                ShowFeedback(HudMessage.NothingToInteract);
            return;
        }

        if (!pickupPressed)
            return;

        if (HeldBody != null)
        {
            ShowFeedback(HudMessage.Dropped);
            pickup.BeginDrop();
        }
        else if (canPickUp)
        {
            pickup.PickUp(hit);
            ShowFeedback(HudMessage.PickedUp);
        }
        else
        {
            ShowFeedback(HudMessage.NothingToPickUp);
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
        curseTarget = item;
        curseElapsed = 0f;
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

    private void CancelCurse()
    {
        curseTarget = null;
        curseElapsed = 0f;
    }

    internal void ReleaseHeldObject() => pickup.Release();
}
