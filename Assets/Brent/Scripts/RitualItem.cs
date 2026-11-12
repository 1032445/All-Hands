using UnityEngine;

public enum RitualItemCategory
{
    Cube,
    Sphere,
    Cylinder
}

public class RitualItem : MonoBehaviour, IInteractable
{
    [SerializeField] private RitualItemCategory category;
    [SerializeField] private int ritualValue = 1;
    [SerializeField] private float curseDuration = 3f;

    public RitualItemCategory Category => category;
    public int RitualValue => ritualValue;
    public float CurseDuration => Mathf.Max(0.1f, curseDuration);
    public bool IsCursed { get; private set; }

    private bool CanCurse(PlayerInteractor player) =>
        isActiveAndEnabled && player.Role == RitualRole.Cultist && player.HeldBody == null;

    public HudMessage GetInteractionPrompt(PlayerInteractor player)
    {
        if (!CanCurse(player))
            return HudMessage.None;

        return IsCursed ? HudMessage.AlreadyCursed : HudMessage.Curse;
    }

    public bool TryInteract(PlayerInteractor player)
    {
        if (!CanCurse(player))
            return false;

        if (!IsCursed)
            player.BeginCurse(this);

        return true;
    }

    internal void CompleteCurse(PlayerInteractor player)
    {
        if (CanCurse(player) && TryCurse())
            player.ShowFeedback(HudMessage.Cursed);
    }

    internal bool TryCurse()
    {
        if (!isActiveAndEnabled || IsCursed)
            return false;

        IsCursed = true;
        return true;
    }

    internal bool TryCleanse()
    {
        if (!isActiveAndEnabled || !IsCursed)
            return false;

        IsCursed = false;
        return true;
    }
}
