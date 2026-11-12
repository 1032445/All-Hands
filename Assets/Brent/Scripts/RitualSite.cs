using UnityEngine;

public class RitualSite : MonoBehaviour, IInteractable
{
    [SerializeField] private float offerDistance = 1f;
    [Header("Ritual Progress")]
    [SerializeField] private int crewTarget = 3;
    [SerializeField] private int crewProgress;
    [SerializeField] private int cultistTarget = 3;
    [SerializeField] private int cultistProgress;

    public int CrewProgress => crewProgress;
    public int CultistProgress => cultistProgress;
    public int CrewTarget => crewTarget;
    public int CultistTarget => cultistTarget;
    public RitualRole? Winner { get; private set; }

    private void OnValidate()
    {
        crewTarget = Mathf.Max(1, crewTarget);
        cultistTarget = Mathf.Max(1, cultistTarget);
        crewProgress = Mathf.Clamp(crewProgress, 0, crewTarget);
        cultistProgress = Mathf.Clamp(cultistProgress, 0, cultistTarget);
    }

    private void Update() => UpdateWinner();

    internal bool TryGetOfferBounds(PlayerInteractor player, out Bounds bounds)
    {
        Collider siteCollider = GetComponentInChildren<Collider>();
        bounds = siteCollider != null ? siteCollider.bounds : default;
        return siteCollider != null && siteCollider.enabled &&
               bounds.SqrDistance(player.transform.position) <= offerDistance * offerDistance;
    }

    internal RitualItem GetOffering(PlayerInteractor player)
    {
        Rigidbody heldBody = player.HeldBody;
        if (Winner.HasValue || heldBody == null ||
            !heldBody.TryGetComponent(out RitualItem item) || !item.isActiveAndEnabled)
            return null;

        return player.Role == RitualRole.Crew || item.IsCursed ? item : null;
    }

    public HudMessage GetInteractionPrompt(PlayerInteractor player)
    {
        if (!isActiveAndEnabled)
            return HudMessage.None;
        RitualItem item = GetOffering(player);
        if (item == null)
            return player.Role == RitualRole.Cultist ? HudMessage.BringCursedItem : HudMessage.BringItem;

        return TryGetOfferBounds(player, out _) ? HudMessage.Offer : HudMessage.MoveCloser;
    }

    public bool TryInteract(PlayerInteractor player)
    {
        HudMessage prompt = GetInteractionPrompt(player);
        if (prompt == HudMessage.None)
            return false;

        if (prompt != HudMessage.Offer)
        {
            player.ShowFeedback(prompt);
            return true;
        }

        RitualItem item = player.HeldBody.GetComponent<RitualItem>();
        int value = item.RitualValue;
        if (item.IsCursed)
        {
            crewProgress = Mathf.Max(0, crewProgress - value);
            cultistProgress += Mathf.Min(value, cultistTarget - cultistProgress);
        }
        else
        {
            crewProgress += Mathf.Min(value, crewTarget - crewProgress);
        }

        UpdateWinner();
        player.ReleaseHeldObject();
        player.ShowFeedback(item.IsCursed ? HudMessage.CursedOffering : HudMessage.Offered);
        item.gameObject.SetActive(false);
        return true;
    }

    private void UpdateWinner()
    {
        if (Winner.HasValue)
            return;

        if (crewProgress >= crewTarget)
            Winner = RitualRole.Crew;
        else if (cultistProgress >= cultistTarget)
            Winner = RitualRole.Cultist;
    }
}
