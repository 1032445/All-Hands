using UnityEngine;

public class CleansingStation : MonoBehaviour, IInteractable
{
    public HudMessage GetInteractionPrompt(PlayerInteractor player)
    {
        RitualItem item = GetHeldRitualItem(player);
        return isActiveAndEnabled && item != null ? HudMessage.Cleanse : HudMessage.None;
    }

    public bool TryInteract(PlayerInteractor player)
    {
        RitualItem item = GetHeldRitualItem(player);
        if (!isActiveAndEnabled || item == null)
            return false;

        player.BeginCleanse(this, item);
        return true;
    }

    internal void CompleteCleanse(PlayerInteractor player, RitualItem item)
    {
        if (!isActiveAndEnabled || GetHeldRitualItem(player) != item)
            return;

        item.TryCleanse();
        player.ShowFeedback(HudMessage.Cleansed);
    }

    private static RitualItem GetHeldRitualItem(PlayerInteractor player)
    {
        if (player.Role != RitualRole.Crew || player.HeldBody == null ||
            !player.HeldBody.TryGetComponent(out RitualItem item) || !item.isActiveAndEnabled)
            return null;

        return item;
    }
}
