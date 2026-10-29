// The guild's reputation: clamped to the configured range, mapped to a level and a 0–1 pacing value. Raises change events.
using UnityEngine;

namespace WanderersGuild
{
    public sealed class GuildReputation
    {
        private readonly ReputationSettings _settings;

        public int Value { get; private set; }
        public ReputationLevel Level { get; private set; }

        // 0 at the minimum, 1 at the maximum — the pacing lever other systems read (arrivals, requests).
        public float Normalized => Mathf.InverseLerp(_settings.Minimum, _settings.Maximum, Value);

        public GuildReputation(ReputationSettings settings, int startingValue)
        {
            _settings = settings;
            Value = Mathf.Clamp(startingValue, settings.Minimum, settings.Maximum);
            Level = settings.LevelOf(Value);
        }

        /// <param name="delta"></param>
        /// <param name="source">What caused it (kept for logging and later reactions).</param>
        public void Change(int delta, ReputationSource source)
        {
            if (delta == 0)
                return;

            // NOTE: The event carries the applied change — at the cap, +10 may apply 0 and raise nothing.
            var next = Mathf.Clamp(Value + delta, _settings.Minimum, _settings.Maximum);
            var applied = next - Value;
            if (applied == 0)
                return;

            Value = next;
            GameEventsRelay.OnReputationChanged.Invoke(Value, applied);

            var level = _settings.LevelOf(Value);
            if (level == Level)
                return;

            Level = level;
            GameEventsRelay.OnReputationLevelChanged.Invoke(level);
        }

        // Raises the current value and level with no change (scene start).
        public void PublishInitial()
        {
            GameEventsRelay.OnReputationChanged.Invoke(Value, 0);
            GameEventsRelay.OnReputationLevelChanged.Invoke(Level);
        }
    }
}