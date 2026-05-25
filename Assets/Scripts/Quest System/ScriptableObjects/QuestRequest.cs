using UnityEngine;

namespace QuestSystem
{
    /// <summary>
    /// A Quest Request is a data-only ScriptableObject authored in the editor (or generated at runtime).
    /// It describes what kind of quest is being asked for before a quest creator turns it into a real quest.
    /// Create via: Assets → Create → QuestSystem → Quest Request
    /// </summary>
    [CreateAssetMenu(menuName = "QuestSystem/Quest Request", fileName = "NewQuestRequest")]
    public class QuestRequest : ScriptableObject
    {
        // ── Basic Info ────────────────────────────────────────────────────────────
        [Header("Basic Info")]
        [Tooltip("Display name of the request / resulting quest.")]
        public string requestName = "Unnamed Request";
 
        [TextArea(3, 6)]
        [Tooltip("Short description of what needs to be done.")]
        public string description = "";
 
        [Tooltip("Category this quest belongs to.")]
        public QuestCategory category = QuestCategory.Combat;
 
        // ── Rank ─────────────────────────────────────────────────────────────────
        [Header("Rank")]
        [Tooltip("Minimum rank for the resulting quest (clamped to 0 and globalMaxRank).")]
        [Min(0)]
        public int minRank = 1;
 
        [Tooltip("Maximum rank for the resulting quest (clamped to minRank and globalMaxRank).")]
        [Min(0)]
        public int maxRank = 5;
 
        // ── Reward ────────────────────────────────────────────────────────────────
        [Header("Reward")]
        [Tooltip("Maximum gold reward the quest creator can offer (slider goes 0 → this value).")]
        [Min(0)]
        public int maxGoldReward = 500;
 
        // ── Party & Time ─────────────────────────────────────────────────────────
        [Header("Party & Time")]
        [Tooltip("Maximum number of adventurers that can accept this quest.")]
        [Min(1)]
        public int adventurerLimit = 4;
 
        [Tooltip("Time limit in minutes to complete the quest once it is accepted from the board.")]
        [Min(1)]
        public int timeLimitMinutes = 60;
 
        // ── Runtime Validation ────────────────────────────────────────────────────
        /// <summary>
        /// Clamps rank values against the global max rank cap.
        /// Call this after loading if you need guaranteed safe values at runtime.
        /// </summary>
        public void ClampToGlobalMax(int globalMaxRank)
        {
            minRank = Mathf.Clamp(minRank, 0, globalMaxRank);
            maxRank = Mathf.Clamp(maxRank, minRank, globalMaxRank);
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Keep min < max in the editor inspector without needing globalMaxRank
            if (maxRank < minRank)
                maxRank = minRank;
        }
#endif
    }
}