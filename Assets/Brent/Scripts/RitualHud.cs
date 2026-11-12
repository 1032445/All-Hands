using System;
using UnityEngine;
using UnityEngine.UI;

public class RitualHud : MonoBehaviour
{
    [Serializable]
    private struct MessageElement
    {
        public HudMessage message;
        public GameObject content;
    }

    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private Slider crewBar;
    [SerializeField] private Text crewValue;
    [SerializeField] private Slider cultistBar;
    [SerializeField] private Text cultistValue;
    [SerializeField] private GameObject crosshair;
    [SerializeField] private GameObject curseDisplay;
    [SerializeField] private Slider curseBar;
    [SerializeField] private Text curseCountdown;
    [SerializeField] private MessageElement[] messages = Array.Empty<MessageElement>();

    private void LateUpdate()
    {
        if (interactor == null)
            return;

        RitualSite site = interactor.MatchSite;
        if (site != null)
        {
            UpdateMeter(crewBar, crewValue, site.CrewProgress, site.CrewTarget);
            UpdateMeter(cultistBar, cultistValue, site.CultistProgress, site.CultistTarget);
        }

        RitualRole? winner = site != null ? site.Winner : null;
        crosshair.SetActive(!winner.HasValue);
        if (curseDisplay != null)
        {
            curseDisplay.SetActive(interactor.IsCursing && !winner.HasValue);
            curseBar.SetValueWithoutNotify(interactor.CurseProgress);
            curseCountdown.text = $"{interactor.CurseTimeRemaining:0.0}s";
        }
        HudMessage role = interactor.Role == RitualRole.Crew ? HudMessage.CrewRole : HudMessage.CultistRole;
        HudMessage winMessage = HudMessage.None;
        if (winner.HasValue)
            winMessage = winner == RitualRole.Crew ? HudMessage.CrewWin : HudMessage.CultistWin;

        foreach (MessageElement element in messages)
        {
            bool show = element.message != HudMessage.None &&
                        (element.message == role || element.message == winMessage ||
                         element.message == interactor.Feedback ||
                         (!winner.HasValue && (element.message == interactor.InteractionPrompt ||
                                               element.message == interactor.PickupPrompt)));
            if (element.content != null && element.content.activeSelf != show)
                element.content.SetActive(show);
        }
    }

    private static void UpdateMeter(Slider bar, Text label, int progress, int target)
    {
        bar.maxValue = target;
        bar.SetValueWithoutNotify(progress);
        label.text = $"{progress}/{target}";
    }
}
