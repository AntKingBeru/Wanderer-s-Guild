// The imported pool of every ClassData (built by Database › Load); finds classes by id and lists the base classes.
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    [CreateAssetMenu(menuName = "Wanderer's Guild/Class Database", fileName = "ClassDatabase")]
    public class ClassDatabase : ScriptableObject
    {
        [SerializeField] private ClassData[] classes = Array.Empty<ClassData>();

        public IReadOnlyList<ClassData> Classes => classes ?? Array.Empty<ClassData>();

        // Usable classes by id. NOTE: Null entries and empty ids are skipped; for duplicate ids the first wins.
        public Dictionary<string, ClassData> BuildLookup() => DatabaseLookup.Build(Classes, nameof(ClassDatabase));

        // Classes no other class advances to (the ones adventurers can arrive as).
        public List<ClassData> BaseClasses(IReadOnlyDictionary<string, ClassData> lookup)
        {
            HashSet<string> advanced = new();
            foreach (var data in lookup.Values)
            {
                foreach (var upgrade in data.AdvancesToIds)
                    advanced.Add(upgrade);
            }

            return lookup.Values.Where(data => !advanced.Contains(data.Id)).ToList();
        }

#if UNITY_EDITOR
        // Editor-only: the importer replaces the list after each load.
        public void SetClasses(ClassData[] items) => classes = items ?? Array.Empty<ClassData>();
#endif
    }
}