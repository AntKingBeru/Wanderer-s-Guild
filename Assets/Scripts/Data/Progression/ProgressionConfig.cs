// ScriptableObject holding the guild's rank-up XP thresholds, pulled by ProgressionSystem
// from GameManager instead of being hardcoded in the manager itself.

using UnityEngine;

[CreateAssetMenu(fileName = "ProgressionConfig", menuName = "Guild Manager/Progression/Progression Config")]
public class ProgressionConfig : ScriptableObject
{
    [Tooltip("XP required to advance to the next guild rank, indexed by QuestRank " +
             "(0=F→E … 6=S→Special). Index 7 (Special) is unused — it's the max rank.")]
    [SerializeField] private int[] rankXpThresholds = { 500, 1000, 1750, 2750, 4000, 6000, 10000, 0 };

    public int GetThreshold(QuestRank rank)
    {
        var index = (int)rank;
        if (rankXpThresholds == null || index >= rankXpThresholds.Length)
            return 0;
        return rankXpThresholds[index];
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (rankXpThresholds != null && rankXpThresholds.Length != 8)
            Debug.LogWarning("[ProgressionConfig] RankXpThresholds should have exactly 8 entries.");
    }
#endif
}