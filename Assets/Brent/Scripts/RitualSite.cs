using UnityEngine;

public class RitualSite : MonoBehaviour, IInteractable
{
    [SerializeField] private float offerDistance = 1f;
    [SerializeField] private RitualItemCategory requiredCategory = RitualItemCategory.Cube;
    [SerializeField, Min(0.1f)] private float categoryChangeInterval = 30f;
    [Header("Ritual Progress")]
    [SerializeField] private int crewTarget = 3;
    [SerializeField] private int crewProgress;
    [SerializeField] private int cultistTarget = 3;
    [SerializeField] private int cultistProgress;

    public int CrewProgress => crewProgress;
    public int CultistProgress => cultistProgress;
    public int CrewTarget => crewTarget;
    public int CultistTarget => cultistTarget;
    public RitualItemCategory RequiredCategory => requiredCategory;
    public float CategoryTimeRemaining => Mathf.Max(0f, categoryTimeRemaining);
    public RitualRole? Winner { get; private set; }

    private float categoryTimeRemaining;

    private void OnEnable() => categoryTimeRemaining = Mathf.Max(0.1f, categoryChangeInterval);

    private void OnValidate()
    {
        categoryChangeInterval = Mathf.Max(0.1f, categoryChangeInterval);
        crewTarget = Mathf.Max(1, crewTarget);
        cultistTarget = Mathf.Max(1, cultistTarget);
        crewProgress = Mathf.Clamp(crewProgress, 0, crewTarget);
        cultistProgress = Mathf.Clamp(cultistProgress, 0, cultistTarget);
    }

    private void Update()
    {
        UpdateWinner();
        if (Winner.HasValue)
            return;

        categoryTimeRemaining -= Time.deltaTime;
        if (categoryTimeRemaining > 0f)
            return;

        requiredCategory = requiredCategory == RitualItemCategory.Cube
            ? RitualItemCategory.Sphere
            : requiredCategory == RitualItemCategory.Sphere
                ? RitualItemCategory.Cylinder
                : RitualItemCategory.Cube;
        categoryTimeRemaining = Mathf.Max(0.1f, categoryChangeInterval);
    }

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

        if (player.Role == RitualRole.Cultist && !item.IsCursed)
            return null;

        return item;
    }

    public HudMessage GetInteractionPrompt(PlayerInteractor player)
    {
        if (!isActiveAndEnabled)
            return HudMessage.None;
        RitualItem item = GetOffering(player);
        if (item == null)
            return player.Role == RitualRole.Cultist ? HudMessage.BringCursedItem : HudMessage.BringItem;

        return TryGetOfferBounds(player, out _) ? HudMessage.Offer : HudMessage.None;
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
        bool wrongShape = !item.IsCursed && item.Category != requiredCategory;
        if (item.IsCursed)
        {
            cultistProgress += Mathf.Min(value, cultistTarget - cultistProgress);
        }
        else if (wrongShape)
        {
            crewProgress = Mathf.Max(0, crewProgress - 1);
        }
        else
        {
            crewProgress += Mathf.Min(value, crewTarget - crewProgress);
        }

        UpdateWinner();
        player.ReleaseHeldObject();
        HudMessage feedback = item.IsCursed
            ? HudMessage.CursedOffering
            : wrongShape ? HudMessage.WrongOffering : HudMessage.Offered;
        player.ShowFeedback(feedback);
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
