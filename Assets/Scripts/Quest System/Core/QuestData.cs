using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuestSystem
{
    /// <summary>
    /// The state of a quest through its lifecycle.
    /// </summary>
    public enum QuestState
    {
        Created,
        Posted,
        Assigned,
        Completed,
        Failed
    }

    /// <summary>
    /// Runtime data for a single quest instance.
    /// This is a plain C# class (not a MonoBehaviour/ScriptableObject) so it can be freely
    /// created, serialized, and passed around without scene coupling.
    /// </summary>
    [Serializable]
    public class QuestData
    {
        // ── Identity ──────────────────────────────────────────────────────────────
        public string QuestId { get; private set; }
        public string QuestName { get; private set; }
        public string Description { get; private set; }
        public QuestCategory Category { get; private set; }
        
        // ── Parameters set by quest creator ──────────────────────────────────────
        public int Rank { get; private set; }
        public int GoldReward { get; private set; }
        public int AdventurerLimit { get; private set; }
        public int TimeLimitMinutes { get; private set; }
        
        // ── Calculated ───────────────────────────────────────────────────────────
        /// <summary>
        /// Points = Rank * CategoryMultiplier.
        /// Multipliers are defined in QuestPointsCalculator.
        /// </summary>
        public int Points { get; private set; }
        
        // ── State ────────────────────────────────────────────────────────────────
        public QuestState State { get; private set; } = QuestState.Created; 
        
        // ── Time tracking (starts when posted) ───────────────────────────────────
        public DateTime? PostedAt { get; private set; }
        
        // ── Constructor (only the Factory should call this) ───────────────────────
        internal QuestData(
            string questName,
            string description,
            QuestCategory category,
            int rank,
            int goldReward,
            int adventurerLimit,
            int timeLimitMinutes,
            int points
        )
        {
            QuestId = Guid.NewGuid().ToString();
            QuestName = questName;
            Description = description;
            Category = category;
            Rank = rank;
            GoldReward = goldReward;
            AdventurerLimit = adventurerLimit;
            TimeLimitMinutes = timeLimitMinutes;
            Points = points;
        }
        
        // ── State transitions ─────────────────────────────────────────────────────
        public void Post()
        {
            if (State !=  QuestState.Created)
                throw new InvalidOperationException($"Quest '{QuestName}' is already {State}; cannot post.");
            
            State = QuestState.Posted;
            PostedAt = DateTime.UtcNow;
        }
        
        // TODO: Assign(), Complete(), Fail() — wire up in later stages.
    }
    
    #region Points Calculator

    /// <summary>
    /// Centralizes the points formula so both the factory and the UI preview can
    /// show the same number without code duplication.
    /// </summary>
    public static class QuestPointsCalculator
    {
        // Tweak these multipliers to balance - @Avihoo
        private static readonly Dictionary<QuestCategory, float> Multipliers
            = new Dictionary<QuestCategory, float>
            {
                { QuestCategory.Combat, 1.5f },
                { QuestCategory.Gathering, 0.8f },
                { QuestCategory.Exploration, 1f },
                { QuestCategory.Escort, 1.3f },
                { QuestCategory.Delivery, 0.9f },
                { QuestCategory.Investigation, 1.2f },
                { QuestCategory.Crafting, 0.7f },
                { QuestCategory.Dungeon, 2f },
            };

        public static int Calculate(int rank, QuestCategory category)
        {
            var multipler = Multipliers.GetValueOrDefault(category, 1f);
            return Mathf.RoundToInt(rank * multipler * 100);
        }
    }
    
    #endregion
    
    #region Factory

    /// <summary>
    /// Factory Method pattern: the only place QuestData objects are constructed.
    /// Having a single creation path means:
    ///   - Validation lives here, not scattered in UI code.
    ///   - Points calculation is automatic.
    ///   - Future logging, analytics, or pooling slots in here cleanly.
    /// </summary>
    public static class QuestFactory
    {
        /// <summary>
        /// Creates a new <see cref="QuestData"/> from a request and the values chosen by the player.
        /// Throws <see cref="ArgumentException"/> if any value is out of range.
        /// </summary>
        /// <param name="request">The source request ScriptableObject.</param>
        /// <param name="chosenRank">Rank picked by the player (validated against request bounds).</param>
        /// <param name="chosenReward">Reward in gold (0 → request.maxRewardGold).</param>
        /// <param name="globalMaxRank">The project-wide rank ceiling from QuestSettings.</param>
        public static QuestData CreateFromRequest(
            QuestRequest request,
            int chosenRank,
            int chosenReward,
            int globalMaxRank
        )
        {
            if (!request)
                throw new ArgumentNullException(nameof(request));
            
            // Rank window: [requestMin-1...requestMax+1], further clamped to [0...globalMax]
            var rankMin = Mathf.Max(0, request.minRank - 1);
            var rankMax = Mathf.Min(globalMaxRank, request.maxRank + 1);

            if (chosenRank < rankMin || chosenRank > rankMax)
                throw new ArgumentException(
                    $"Chosen rank {chosenRank} is outside allowed range [{rankMin}, {rankMax}].");

            if (chosenReward < 0 || chosenReward > request.maxGoldReward)
                throw new ArgumentException(
                    $"Chosen reward {chosenReward} is outside allowed range [0, {request.maxGoldReward}].");
            
            var points = QuestPointsCalculator.Calculate(chosenRank, request.category);

            return new QuestData(
                questName: request.requestName,
                description: request.description,
                category: request.category,
                rank: chosenRank,
                goldReward: chosenReward,
                adventurerLimit: request.adventurerLimit,
                timeLimitMinutes: request.timeLimitMinutes,
                points: points
            );
        }
    }
    
    #endregion
}