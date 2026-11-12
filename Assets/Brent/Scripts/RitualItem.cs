using UnityEngine;

public class RitualItem : MonoBehaviour, IInteractable
{
    [SerializeField] private int ritualValue = 1;
    [SerializeField, Min(0.1f)] private float curseDuration = 3f;

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
        if (!CanCurse(player) || IsCursed)
            return;

        IsCursed = true;
        player.ShowFeedback(HudMessage.Cursed);
    }
}
