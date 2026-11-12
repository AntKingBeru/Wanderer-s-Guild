// Creates new adventurers — random arrivals (base classes + unlocked advanced classes) and the fixed starting party — with
// name, level, wealth, their class's starting gear, and (advanced classes) the rank and growth of their earlier classes.
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace WanderersGuild
{
    public sealed class AdventurerFactory
    {
        private readonly AdventurerSettings _settings;
        private readonly IReadOnlyList<SpeciesData> _species;
        private readonly IReadOnlyList<ClassData> _baseClasses;
        private readonly IReadOnlyList<ClassData> _allClasses;
        private readonly Dictionary<string, ClassData> _predecessorOf;
        private readonly Func<string, bool> _isUnlocked;
        private readonly IReadOnlyDictionary<string, EquipmentData> _equipment;
        private readonly AdventurerNameGenerator _names;
        private readonly List<ClassData> _eligible = new();
        private readonly HashSet<string> _warnedGear = new();

        /// <param name="settings"></param>
        /// <param name="species"></param>
        /// <param name="baseClasses"></param>
        /// <param name="allClasses"></param>
        /// <param name="isUnlocked">Whether an advanced class id has been unlocked (asked at arrival time, not here).</param>
        /// <param name="equipment"></param>
        public AdventurerFactory(AdventurerSettings settings, IReadOnlyList<SpeciesData> species, IReadOnlyList<ClassData> baseClasses,
                                 IReadOnlyList<ClassData> allClasses, Func<string, bool> isUnlocked,
                                 IReadOnlyDictionary<string, EquipmentData> equipment)
        {
            _settings = settings;
            _species = species;
            _baseClasses = baseClasses;
            _allClasses = allClasses;
            _predecessorOf = ClassLineage.PredecessorMap(allClasses);
            _isUnlocked = isUnlocked;
            _equipment = equipment;
            _names = new AdventurerNameGenerator(settings.NameList, settings.EpithetChance);
        }

        // A random arrival: random species, a random eligible class (base, or an unlocked advanced class — each gated
        // by its required guild rank, all equally likely), level by reputation. False when no species or no class qualifies.
        public bool TryCreate(int id, GuildRank guildRank, ReputationLevel reputation, long nowMinutes,
                              ICollection<string> takenNames, out Adventurer adventurer)
        {
            adventurer = null;
            FillEligible(guildRank);
            if (_species.Count == 0 || _eligible.Count == 0)
                return false;

            var species = _species[Random.Range(0, _species.Count)];
            var chosen = _eligible[Random.Range(0, _eligible.Count)];
            var level = Random.Range(1, _settings.MaxArrivalLevel(reputation) + 1);

            var (rank, retained) = ClassLineage.ArrivalProfile(chosen, species, _predecessorOf, _settings.Experience.MaxLevel);
            adventurer = Build(id, _names.Next(takenNames), species, chosen, level, nowMinutes, rank, retained);
            return true;
        }

        // One of the new guild's starting adventurers (always as specified: rank F, no retained growth).
        // False if the spec has no species or class.
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

            adventurer = Build(id, name, spec.Species, spec.Class, level, nowMinutes, GuildRank.F, default);
            return true;
        }

        // NOTE: Base classes are always in the pool; an advanced class only once it's unlocked (someone advanced into it).
        private void FillEligible(GuildRank guildRank)
        {
            _eligible.Clear();
            foreach (var classData in _baseClasses)
            {
                if (classData.RequiredGuildRank <= guildRank) _eligible.Add(classData);
            }

            foreach (var classData in _allClasses)
            {
                if (classData == null || !_predecessorOf.ContainsKey(classData.Id))
                    continue;
                if (classData.RequiredGuildRank <= guildRank && _isUnlocked != null && _isUnlocked(classData.Id))
                    _eligible.Add(classData);
            }
        }

        private Adventurer Build(int id, string name, SpeciesData species, ClassData classData, int level, long nowMinutes,
                                 GuildRank rank, StatBlock retained)
        {
            var wealthRange = _settings.WealthPerLevel;
            var wealth = Random.Range(wealthRange.x, wealthRange.y + 1) * level;
            var experience = _settings.Experience.ThresholdFor(level);

            return new Adventurer(id, name, species, classData, experience, rank, wealth, AdventurerStatus.Idle,
                                  nowMinutes, retained, _settings.Experience, equipment: StartingGear(classData));
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