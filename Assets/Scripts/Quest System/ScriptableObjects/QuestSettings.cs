using UnityEngine;

namespace QuestSystem
{
    /// <summary>
    /// Project-wide quest configuration.
    /// Create one instance via Assets → Create → QuestSystem → QuestSettings.
    /// </summary>
    [CreateAssetMenu(menuName = "QuestSystem/Quest Settings", fileName = "QuestSettings")]
    public class QuestSettings : ScriptableObject
    {
        [Tooltip("The absolute maximum rank any quest or request can have.")]
        [Min(1)]
        public int globalMaxRank = 10;
 
        [Tooltip("Maximum number of board slots (posted-quest slots).")]
        [Min(1)]
        public int boardSlotCount = 10;
    }
}