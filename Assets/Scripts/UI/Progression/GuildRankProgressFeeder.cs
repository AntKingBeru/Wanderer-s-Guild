// Observer that grants guild-rank progress on each successful quest resolution.

using UnityEngine;

[DefaultExecutionOrder(-60)]
public class GuildRankProgressFeeder : MonoBehaviour
{
    private void OnEnable()
    {
        if (GameEventsRelay.Exists)
            GameEventsRelay.Instance.onQuestResolved.AddListener(HandleQuestResolved);
    }

    private void OnDisable()
    {
        if (GameEventsRelay.Exists)
            GameEventsRelay.Instance.onQuestResolved.RemoveListener(HandleQuestResolved);
    }
    
    private void HandleQuestResolved(int questId, bool success)
    {
        if (!success || !GuildController.Exists)
            return;
        GuildController.Instance.AddRankProgress(GameConfig.Instance.Guild.rankExpPerQuestSuccess);
    }
}