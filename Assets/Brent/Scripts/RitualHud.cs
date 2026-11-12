using UnityEngine;
using UnityEngine.UI;

public class RitualHud : MonoBehaviour
{
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private Slider crewBar;
    [SerializeField] private Text crewValue;
    [SerializeField] private Slider cultistBar;
    [SerializeField] private Text cultistValue;
    [SerializeField] private GameObject crosshair;
    [SerializeField] private GameObject curseDisplay;
    [SerializeField] private Slider curseBar;
    [SerializeField] private Text curseCountdown;
    [SerializeField] private GameObject developerCurseCounter;
    [SerializeField] private Text developerCurseCount;
    [SerializeField] private GameObject cubeRequirementLabel;
    [SerializeField] private GameObject sphereRequirementLabel;
    [SerializeField] private GameObject cylinderRequirementLabel;
    [SerializeField] private GameObject categoryTimerLabel;
    [SerializeField] private Text categoryTimerValue;

    [Header("Interaction prompts")]
    [SerializeField] private GameObject pickupPrompt;
    [SerializeField] private GameObject dropPrompt;
    [SerializeField] private GameObject cursePrompt;
    [SerializeField] private GameObject alreadyCursedPrompt;
    [SerializeField] private GameObject bringItemPrompt;
    [SerializeField] private GameObject bringCursedItemPrompt;
    [SerializeField] private GameObject offerPrompt;
    [SerializeField] private GameObject cleansePrompt;
    [SerializeField] private GameObject pushPrompt;

    [Header("Feedback and game state")]
    [SerializeField] private GameObject pickedUpMessage;
    [SerializeField] private GameObject cursedMessage;
    [SerializeField] private GameObject offeredMessage;
    [SerializeField] private GameObject cursedOfferingMessage;
    [SerializeField] private GameObject cleansedMessage;
    [SerializeField] private GameObject wrongOfferingMessage;
    [SerializeField] private GameObject crewRoleLabel;
    [SerializeField] private GameObject cultistRoleLabel;
    [SerializeField] private GameObject crewWinMessage;
    [SerializeField] private GameObject cultistWinMessage;

    private void LateUpdate()
    {
#if UNITY_EDITOR
        UpdateDeveloperCurseCounter();
#else
        if (developerCurseCounter != null)
            developerCurseCounter.SetActive(false);
        if (developerCurseCount != null)
        {
            developerCurseCount.gameObject.SetActive(false);
            developerCurseCount.text = string.Empty;
        }
#endif

        if (interactor == null)
            return;

        RitualSite site = interactor.MatchSite;
        if (site != null)
        {
            UpdateMeter(crewBar, crewValue, site.CrewProgress, site.CrewTarget);
            UpdateMeter(cultistBar, cultistValue, site.CultistProgress, site.CultistTarget);
            if (categoryTimerValue != null)
                categoryTimerValue.text = Mathf.CeilToInt(site.CategoryTimeRemaining).ToString();
        }

        bool roundActive = site == null || !site.Winner.HasValue;
        crosshair.SetActive(roundActive);
        if (curseDisplay != null)
        {
            curseDisplay.SetActive(interactor.IsProcessing && roundActive);
            curseBar.SetValueWithoutNotify(interactor.ProcessProgress);
            curseCountdown.text = $"{interactor.ProcessTimeRemaining:0.0}s";
        }

        ShowMessage(crewRoleLabel, interactor.Role == RitualRole.Crew);
        ShowMessage(cultistRoleLabel, interactor.Role == RitualRole.Cultist);
        ShowMessage(crewWinMessage, site != null && site.Winner == RitualRole.Crew);
        ShowMessage(cultistWinMessage, site != null && site.Winner == RitualRole.Cultist);
        ShowMessage(cubeRequirementLabel, roundActive && site != null &&
            site.RequiredCategory == RitualItemCategory.Cube);
        ShowMessage(sphereRequirementLabel, roundActive && site != null &&
            site.RequiredCategory == RitualItemCategory.Sphere);
        ShowMessage(cylinderRequirementLabel, roundActive && site != null &&
            site.RequiredCategory == RitualItemCategory.Cylinder);
        ShowMessage(categoryTimerLabel, roundActive && site != null);
        ShowMessage(categoryTimerValue != null ? categoryTimerValue.gameObject : null,
            roundActive && site != null);

        HudMessage prompt = interactor.InteractionPrompt;
        ShowMessage(cursePrompt, roundActive && prompt == HudMessage.Curse);
        ShowMessage(alreadyCursedPrompt, roundActive && prompt == HudMessage.AlreadyCursed);
        ShowMessage(bringItemPrompt, roundActive && prompt == HudMessage.BringItem);
        ShowMessage(bringCursedItemPrompt, roundActive && prompt == HudMessage.BringCursedItem);
        ShowMessage(offerPrompt, roundActive && prompt == HudMessage.Offer);
        ShowMessage(cleansePrompt, roundActive && prompt == HudMessage.Cleanse);
        ShowMessage(pushPrompt, roundActive && prompt == HudMessage.Push);

        HudMessage pickup = interactor.PickupPrompt;
        ShowMessage(pickupPrompt, roundActive && pickup == HudMessage.Pickup);
        ShowMessage(dropPrompt, roundActive && pickup == HudMessage.Drop);

        HudMessage feedback = interactor.Feedback;
        ShowMessage(pickedUpMessage, feedback == HudMessage.PickedUp);
        ShowMessage(cursedMessage, feedback == HudMessage.Cursed);
        ShowMessage(offeredMessage, feedback == HudMessage.Offered);
        ShowMessage(cursedOfferingMessage, feedback == HudMessage.CursedOffering);
        ShowMessage(cleansedMessage, feedback == HudMessage.Cleansed);
        ShowMessage(wrongOfferingMessage, feedback == HudMessage.WrongOffering);
    }

    private static void UpdateMeter(Slider bar, Text label, int progress, int target)
    {
        bar.maxValue = target;
        bar.SetValueWithoutNotify(progress);
        label.text = $"{progress}/{target}";
    }

    private static void ShowMessage(GameObject message, bool visible)
    {
        if (message != null && message.activeSelf != visible)
            message.SetActive(visible);
    }

#if UNITY_EDITOR
    private void UpdateDeveloperCurseCounter()
    {
        if (developerCurseCounter == null || developerCurseCount == null)
            return;

        RitualSite site = interactor != null ? interactor.MatchSite : null;
        var currentScene = site != null ? site.gameObject.scene : gameObject.scene;
        int cursedCount = 0;
        foreach (RitualItem item in FindObjectsByType<RitualItem>())
        {
            if (item.IsCursed && item.isActiveAndEnabled && item.gameObject.scene == currentScene)
                cursedCount++;
        }

        developerCurseCount.text = cursedCount.ToString();
        developerCurseCounter.SetActive(true);
        developerCurseCount.gameObject.SetActive(true);
    }
#endif
}
