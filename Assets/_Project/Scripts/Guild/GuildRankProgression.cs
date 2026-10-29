// Guild Rank from cumulative rank points: crossing a threshold ranks up at once (one event per rank). Raises progress events.
using UnityEngine;

namespace WanderersGuild
{
    public sealed class GuildRankProgression
    {
        private readonly GuildRankSettings _settings;

        public GuildRank Rank { get; private set; }
        public long Points { get; private set; }
        public bool IsHighestRank => Rank == _settings.Highest;

        // Progress from the current rank's threshold to the next (0–1); 1 at the highest rank.
        public float Progress
        {
            get
            {
                if (!_settings.TryGetNext(Rank, out var next))
                    return 1f;

                var from = _settings.PointsFor(Rank);
                var to = _settings.PointsFor(next);
                return Mathf.Clamp01((float)(Points - from) / (to - from));
            }
        }

        // NOTE: A loaded rank is never lowered (points are raised to its threshold if the table changed). If the points
        // now qualify for more, the rank catches up silently — no banner for something that happened before this load.
        public GuildRankProgression(GuildRankSettings settings, GuildRank rank, long points)
        {
            _settings = settings;
            Rank = rank;
            Points = System.Math.Max(points, settings.PointsFor(rank));

            while (settings.TryGetNext(Rank, out var next) && Points >= settings.PointsFor(next))
                Rank = next;
        }

        public void AddPoints(long points)
        {
            if (points <= 0)
                return;

            Points = points > long.MaxValue - Points ? long.MaxValue : Points + points;

            // NOTE: One big gain can cross several thresholds — rank up one step at a time, an event for each.
            while (_settings.TryGetNext(Rank, out var next) && Points >= _settings.PointsFor(next))
            {
                Rank = next;
                GameEventsRelay.OnGuildRankUp.Invoke(Rank);
            }

            GameEventsRelay.OnRankProgressChanged.Invoke(Rank, Progress);
        }

        // Raises the current rank and progress (scene start).
        public void PublishInitial() => GameEventsRelay.OnRankProgressChanged.Invoke(Rank, Progress);
    }
}