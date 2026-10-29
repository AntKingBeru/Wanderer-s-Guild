// Creates new adventurers: random species, random base class unlocked at the guild's rank, level by reputation, wealth, name.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class AdventurerFactory
    {
        private readonly AdventurerSettings _settings;
        private readonly IReadOnlyList<SpeciesData> _species;
        private readonly IReadOnlyList<ClassData> _baseClasses;
        private readonly AdventurerNameGenerator _names;
        private readonly List<ClassData> _eligible = new();

        public AdventurerFactory(AdventurerSettings settings, IReadOnlyList<SpeciesData> species, IReadOnlyList<ClassData> baseClasses)
        {
            _settings = settings;
            _species = species;
            _baseClasses = baseClasses;
            _names = new AdventurerNameGenerator(settings.NameList, settings.EpithetChance);
        }

        // False when no species exists or no base class is unlocked at this guild rank.
        public bool TryCreate(int id, GuildRank guildRank, ReputationLevel reputation, long nowMinutes,
                              ICollection<string> takenNames, out Adventurer adventurer)
        {
            adventurer = null;

            _eligible.Clear();
            foreach (var data in _baseClasses)
            {
                if (data.RequiredGuildRank <= guildRank)
                    _eligible.Add(data);
            }

            if (_species.Count == 0 || _eligible.Count == 0)
                return false;

            var species = _species[Random.Range(0, _species.Count)];
            var classData = _eligible[Random.Range(0, _eligible.Count)];
            var level = Random.Range(1, _settings.MaxArrivalLevel(reputation) + 1);

            var wealthRange = _settings.WealthPerLevel;
            var wealth = Random.Range(wealthRange.x, wealthRange.y + 1) * level;
            var experience = _settings.Experience.ThresholdFor(level);

            adventurer = new Adventurer(id, _names.Next(takenNames), species, classData, experience, GuildRank.F, wealth,
                                        AdventurerStatus.Idle, nowMinutes, default, _settings.Experience);
            return true;
        }
    }
}