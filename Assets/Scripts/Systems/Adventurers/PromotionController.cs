// Observer that submits a rank-up application when an adventurer's rank progress crosses the threshold.

using UnityEngine;

[DefaultExecutionOrder(-60)]
public class PromotionController : MonoBehaviour
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
        if (!AdventurerRoster.Exists || !ApplicationBoard.Exists)
            return;

        var threshold = GameConfig.Instance.Adventurer.rankProgressForPromotion;
        var cap = GameConfig.Instance.Adventurer.defaultRankCap;
        
        foreach (var a in AdventurerRoster.Instance.GetAll())
        {
            if (a.State != AdventurerState.Idle)
                continue;
            if (a.Rank >= cap)
                continue;
            if (a.RankProgress >= threshold)
                ApplicationBoard.Instance.SubmitRankUpApplication(a.Id);
        }
    }
}