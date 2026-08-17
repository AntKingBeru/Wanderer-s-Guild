// Generic SO collection giving id-based lookup over a set of authored data assets.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public abstract class ScriptableObjectDatabase<T> : ScriptableObject where T : ScriptableObject, IIdentifiable
    {
        [SerializeField] protected List<T> entries = new();

        private Dictionary<string, T> _lookup;

        public IReadOnlyList<T> Entries => entries;

        // Reset the cache on load so a fresh map is built after domain reloads / edits.
        protected virtual void OnEnable() => _lookup = null;

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, T>(entries.Count);
            foreach (var entry in entries.Where(entry => entry))
            {
                if (string.IsNullOrEmpty(entry.Id))
                {
                    Debug.LogWarning($"[{name}] Entry '{entry.name}' has an empty Id and was skipped.");
                    continue;
                }
                if (!_lookup.TryAdd(entry.Id, entry))
                    Debug.LogWarning($"[{name}] Duplicate Id '{entry.Id}' — only the first is kept.");
            }
        }

        // Returns the entry with this id, or null if absent.
        public T GetById(string id)
        {
            if (_lookup == null)
                BuildLookup();
            return _lookup.GetValueOrDefault(id);
        }

        public bool TryGetById(string id, out T value)
        {
            if (_lookup == null)
                BuildLookup();
            value = null;
            return _lookup != null && _lookup.TryGetValue(id, out value);
        }

#if UNITY_EDITOR
        // Editor-only entry point used by the populator tool (or the other dev's import tool).
        public void EditorSetEntries(List<T> newEntries)
        {
            entries = newEntries;
            _lookup = null;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}