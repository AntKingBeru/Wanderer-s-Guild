// The full game state written to a slot's game.json; each system fills its part through ISaveContributor.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    [Serializable]
    public class GameSaveData
    {
        // NOTE: v2 added the guild state; v3 the request board; v4 the adventurer roster; v5 posted quests. Older files load
        // with the missing parts at defaults.
        public const int CurrentVersion = 5;

        // NOTE: Generous upper bound (~114 years) — anything above means a corrupted file.
        private const double MaxPlayTimeSeconds = 3.6e9;

        [SerializeField] private int version = CurrentVersion;
        [SerializeField] private long totalMinutes;
        [SerializeField] private double playTimeSeconds;

        [SerializeField] private long gold;
        [SerializeField] private int reputation;
        [SerializeField] private long rankPoints;
        [SerializeField] private GuildRank rank;

        [SerializeField] private List<SavedRequest> requests = new();
        [SerializeField] private int nextRequestId = 1;
        [SerializeField] private bool requestsInitialized;

        [SerializeField] private List<SavedAdventurer> adventurers = new();
        [SerializeField] private int nextAdventurerId = 1;
        [SerializeField] private bool adventurersInitialized;

        [SerializeField] private List<SavedQuest> quests = new();
        [SerializeField] private int nextQuestId = 1;

        public int Version => version;

        // True when this data carries the guild state (v2+); older saves start the guild from defaults.
        public bool HasGuildState => version >= 2;

        // In-game time (minutes since Year 1, Month 1, Day 1, 00:00). Written by TimeManager.
        public long TotalMinutes
        {
            get => totalMinutes;
            set => totalMinutes = Math.Max(0L, value);
        }

        // Real seconds played. Written by PlayTimeTracker.
        public double PlayTimeSeconds
        {
            get => playTimeSeconds;
            set => playTimeSeconds = Math.Clamp(value, 0d, MaxPlayTimeSeconds);
        }

        // Guild gold (never negative). Written by GuildManager.
        public long Gold
        {
            get => gold;
            set => gold = Math.Max(0L, value);
        }

        // Guild reputation. Written by GuildManager (clamped there to the configured range).
        public int Reputation
        {
            get => reputation;
            set => reputation = value;
        }

        // Cumulative rank points. Written by GuildManager.
        public long RankPoints
        {
            get => rankPoints;
            set => rankPoints = Math.Max(0L, value);
        }

        // Guild rank (also shown in the slot header). Written by GuildManager.
        public GuildRank Rank
        {
            get => rank;
            set => rank = value;
        }

        // Open requests on the board. Written by RequestManager.
        public IReadOnlyList<SavedRequest> Requests => requests ?? (IReadOnlyList<SavedRequest>)Array.Empty<SavedRequest>();

        public void SetRequests(IEnumerable<SavedRequest> items) => requests = items != null ? new List<SavedRequest>(items) : new List<SavedRequest>();

        // Next request instance id (keeps ids unique across saves). Written by RequestManager.
        public int NextRequestId
        {
            get => nextRequestId;
            set => nextRequestId = Math.Max(1, value);
        }

        // True once the new guild's starting batch has been posted. Written by RequestManager.
        public bool RequestsInitialized
        {
            get => requestsInitialized;
            set => requestsInitialized = value;
        }

        // The roster. Written by AdventurerManager.
        public IReadOnlyList<SavedAdventurer> Adventurers => adventurers ?? (IReadOnlyList<SavedAdventurer>)Array.Empty<SavedAdventurer>();

        public void SetAdventurers(IEnumerable<SavedAdventurer> items) =>
            adventurers = items != null ? new List<SavedAdventurer>(items) : new List<SavedAdventurer>();

        // Next adventurer id (keeps ids unique across saves). Written by AdventurerManager.
        public int NextAdventurerId
        {
            get => nextAdventurerId;
            set => nextAdventurerId = Math.Max(1, value);
        }

        // True once the new guild's starting adventurers have joined. Written by AdventurerManager.
        public bool AdventurersInitialized
        {
            get => adventurersInitialized;
            set => adventurersInitialized = value;
        }

        // Posted quests (waiting or in progress). Written by QuestManager.
        public IReadOnlyList<SavedQuest> Quests => quests ?? (IReadOnlyList<SavedQuest>)Array.Empty<SavedQuest>();

        public void SetQuests(IEnumerable<SavedQuest> items) => quests = items != null ? new List<SavedQuest>(items) : new List<SavedQuest>();

        // Next quest id (keeps ids unique across saves). Written by QuestManager.
        public int NextQuestId
        {
            get => nextQuestId;
            set => nextQuestId = Math.Max(1, value);
        }

        public bool IsValid =>
            version is >= 1 and <= CurrentVersion &&
            totalMinutes >= 0 &&
            playTimeSeconds is >= 0d and <= MaxPlayTimeSeconds &&
            gold >= 0 && rankPoints >= 0 &&
            Enum.IsDefined(typeof(GuildRank), rank);

        // A deep copy marked as the current version — the starting point of every save, so a system that's
        // absent this session keeps its last saved values instead of being written as defaults.
        public GameSaveData CloneAsCurrent()
        {
            var copy = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(this));
            copy.version = CurrentVersion;
            return copy;
        }
    }
}