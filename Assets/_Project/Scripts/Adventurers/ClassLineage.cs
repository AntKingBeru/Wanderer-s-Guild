// Class lineage (pure): which class each advanced class comes from, and the rank + retained growth an arrival of an advanced
// class should have (as if they had maxed every class before it and advanced, by the same rule as a real advancement).
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public static class ClassLineage
    {
        private const int MaxDepth = 8;
        private static bool _warnedCycle;

        // advanced class id → the class it advances from (the first class, in database order, that lists it).
        public static Dictionary<string, ClassData> PredecessorMap(IReadOnlyList<ClassData> classes)
        {
            Dictionary<string, ClassData> map = new();
            foreach (var classData in classes)
            {
                if (!classData)
                    continue;
                foreach (var upgrade in classData.AdvancesToIds)
                {
                    if (!string.IsNullOrEmpty(upgrade))
                        map.TryAdd(upgrade, classData);
                }
            }
            return map;
        }

        // Rank and retained growth for an arrival of this class. A base class gives (F, nothing).
        // NOTE: Walks the chain back to its base class (max 8 steps — a cycle in the sheet stops there, warned once), then
        // replays each advancement forward: maxed in the previous class → advance (growth kept, no stat lower).
        public static (GuildRank rank, StatBlock retained) ArrivalProfile(ClassData classData, SpeciesData species,
                                                                         IReadOnlyDictionary<string, ClassData> predecessorOf,
                                                                         int maxLevel)
        {
            List<ClassData> chain = new() { classData };
            var current = classData;
            while (predecessorOf.TryGetValue(current.Id, out var previous))
            {
                if (chain.Count >= MaxDepth || chain.Contains(previous))
                {
                    if (!_warnedCycle)
                    {
                        _warnedCycle = true;
                        Debug.LogWarning($"[ClassLineage] The UPGRADES chain leading to '{classData.Id}' loops or is too deep; stopping there.");
                    }
                    break;
                }
                chain.Add(previous);
                current = previous;
            }

            if (chain.Count == 1) return (GuildRank.F, default);

            chain.Reverse(); // Base class first, the arriving class last.
            StatBlock retained = default;
            for (var i = 0; i < chain.Count - 1; i++)
            {
                ClassData from = chain[i], to = chain[i + 1];
                var before = AdventurerStats.Compute(from, species, maxLevel, retained);

                retained += (species.GrowthPerLevel + from.StatGrowthPerLevel) * (maxLevel - 1);
                var after = AdventurerStats.Compute(to, species, 1, retained);
                retained += Shortfall(before, after);
            }

            return (chain[^2].MaxRank, retained);
        }

        private static StatBlock Shortfall(StatBlock want, StatBlock have) =>
            new(Mathf.Max(0, want.Strength - have.Strength), Mathf.Max(0, want.Agility - have.Agility),
                Mathf.Max(0, want.Intellect - have.Intellect), Mathf.Max(0, want.Vitality - have.Vitality));
    }
}