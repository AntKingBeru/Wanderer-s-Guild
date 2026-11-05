// Creates new adventurers — random arrivals and the fixed starting party — with name, level, wealth and their class's starting gear.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class AdventurerFactory
    {
        private readonly AdventurerSettings _settings;
        private readonly IReadOnlyList<SpeciesData> _species;
        private readonly IReadOnlyList<ClassData> _baseClasses;
        private readonly IReadOnlyDictionary<string, EquipmentData> _equipment;
        private readonly AdventurerNameGenerator _names;
        private readonly List<ClassData> _eligible = new();
        private readonly HashSet<string> _warnedGear = new();

        public AdventurerFactory(AdventurerSettings settings, IReadOnlyList<SpeciesData> species, IReadOnlyList<ClassData> baseClasses,
                                 IReadOnlyDictionary<string, EquipmentData> equipment)
        {
            _settings = settings;
            _species = species;
            _baseClasses = baseClasses;
            _equipment = equipment;
            _names = new AdventurerNameGenerator(settings.NameList, settings.EpithetChance);
        }

        // A random arrival: random species, random base class unlocked at this guild rank, level by reputation.
        // False when no species exists or no base class is unlocked.
        public bool TryCreate(int id, GuildRank guildRank, ReputationLevel reputation, long nowMinutes,
                              ICollection<string> takenNames, out Adventurer adventurer)
        {
            adventurer = null;

            _eligible.Clear();
            foreach (var classData in _baseClasses)
            {
                if (classData.RequiredGuildRank <= guildRank)
                    _eligible.Add(classData);
            }

            if (_species.Count == 0 || _eligible.Count == 0)
                return false;

            var species = _species[Random.Range(0, _species.Count)];
            var chosen = _eligible[Random.Range(0, _eligible.Count)];
            var level = Random.Range(1, _settings.MaxArrivalLevel(reputation) + 1);

            adventurer = Build(id, _names.Next(takenNames), species, chosen, level, nowMinutes);
            return true;
        }

        // One of the new guild's starting adventurers. False if the spec has no species or class.
        public bool TryCreateStarting(int id, StartingAdventurerSpec spec, long nowMinutes, ICollection<string> takenNames,
                                      out Adventurer adventurer)
        {
            adventurer = null;
            if (!spec.IsValid)
                return false;

            var name = !string.IsNullOrWhiteSpace(spec.Name) && !takenNames.Contains(spec.Name.Trim())
                ? spec.Name.Trim()
                : _names.Next(takenNames);
            var level = Mathf.Clamp(spec.Level, 1, _settings.Experience.MaxLevel);

            adventurer = Build(id, name, spec.Species, spec.Class, level, nowMinutes);
            return true;
        }

        private Adventurer Build(int id, string name, SpeciesData species, ClassData classData, int level, long nowMinutes)
        {
            var wealthRange = _settings.WealthPerLevel;
            var wealth = Random.Range(wealthRange.x, wealthRange.y + 1) * level;
            var experience = _settings.Experience.ThresholdFor(level);

            return new Adventurer(id, name, species, classData, experience, GuildRank.F, wealth, AdventurerStatus.Idle,
                                  nowMinutes, default, _settings.Experience, equipment: StartingGear(classData));
        }

        // NOTE: Unknown ids are warned once each and skipped (the adventurer arrives without that item).
        private EquipmentData[] StartingGear(ClassData classData)
        {
            var ids = classData.StartingGearIds;
            var gear = new EquipmentData[ids.Count];
            for (var i = 0; i < ids.Count; i++)
            {
                if (_equipment.TryGetValue(ids[i], out gear[i]))
                    continue;
                if (_warnedGear.Add(ids[i]))
                    Debug.LogWarning($"[AdventurerFactory] Class '{classData.Id}' starting gear '{ids[i]}' isn't in the equipment database.");
            }
            return gear;
        }
    }
}