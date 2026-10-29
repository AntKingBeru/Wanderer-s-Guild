using System;
using UnityEngine;

namespace WanderersGuild
{
    [Serializable]
    public class SaveSlotHeader
    {
        public const int CurrentVersion = 1;

        // NOTE: Generous upper bound (~114 years) — anything above means a corrupted file.
        private const double MaxPlayTimeSeconds = 3.6e9;

        [SerializeField] private int version = CurrentVersion;
        [SerializeField] private string guildName = string.Empty;
        [SerializeField] private GuildRank rank;
        [SerializeField] private GameDateStamp date;
        [SerializeField] private double playTimeSeconds;
        [SerializeField] private long savedAtUtcTicks;

        public int Version => version;
        public string GuildName => guildName;
        public GuildRank Rank => rank;
        public GameDateStamp Date => date;
        public double PlayTimeSeconds => playTimeSeconds;
        public long SavedAtUtcTicks => savedAtUtcTicks;

        // Only call after IsValid — out-of-range ticks would throw.
        public DateTime SavedAtLocal => new DateTime(savedAtUtcTicks, DateTimeKind.Utc).ToLocalTime();

        // False for anything a corrupted or hand-edited file could produce.
        public bool IsValid =>
            version is >= 1 and <= CurrentVersion &&
            !string.IsNullOrWhiteSpace(guildName) &&
            Enum.IsDefined(typeof(GuildRank), rank) &&
            date.IsValid &&
            playTimeSeconds is >= 0d and <= MaxPlayTimeSeconds &&
            savedAtUtcTicks > 0 && savedAtUtcTicks <= DateTime.MaxValue.Ticks;

        // NOTE: JsonUtility needs the parameterless constructor, so new headers come from this factory.
        public static SaveSlotHeader Create(string guildName, GuildRank rank, GameDateStamp date,
                                            double playTimeSeconds, DateTime savedAtUtc) => new()
        {
            guildName = guildName?.Trim() ?? string.Empty,
            rank = rank,
            date = date,
            playTimeSeconds = Math.Max(0d, playTimeSeconds),
            savedAtUtcTicks = savedAtUtc.ToUniversalTime().Ticks
        };
    }
}