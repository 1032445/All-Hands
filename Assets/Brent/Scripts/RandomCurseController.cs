using UnityEngine;

public class RandomCurseController : MonoBehaviour
{
    [SerializeField] private RitualSite ritualSite;
    [SerializeField] private float curseInterval = 10f;

    private float timeRemaining;

    private void OnEnable() => timeRemaining = Mathf.Max(0.1f, curseInterval);

    private void Update()
    {
        if (ritualSite == null || ritualSite.Winner.HasValue)
            return;

        timeRemaining -= Time.deltaTime;
        if (timeRemaining > 0f)
            return;

        CurseRandomItem();
        timeRemaining = Mathf.Max(0.1f, curseInterval);
    }

    private void CurseRandomItem()
    {
        RitualItem[] items = FindObjectsByType<RitualItem>();
        PlayerPickup[] pickups = FindObjectsByType<PlayerPickup>();
        PlayerInteractor[] players = FindObjectsByType<PlayerInteractor>();
        RitualItem selected = null;
        int eligibleCount = 0;

        foreach (RitualItem item in items)
        {
            if (!item.isActiveAndEnabled || item.IsCursed || item.gameObject.scene != ritualSite.gameObject.scene ||
                IsInUse(item, pickups, players))
                continue;

            eligibleCount++;
            if (Random.Range(0, eligibleCount) == 0)
                selected = item;
        }

        if (selected != null)
            selected.TryCurse();
    }

    private static bool IsInUse(RitualItem item, PlayerPickup[] pickups, PlayerInteractor[] players)
    {
        Rigidbody body = item.GetComponent<Rigidbody>();
        foreach (PlayerPickup pickup in pickups)
        {
            if (body != null && pickup.HeldBody == body)
                return true;
        }

        foreach (PlayerInteractor player in players)
        {
            if (player.CurseTarget == item)
                return true;
        }

        return false;
    }
}
