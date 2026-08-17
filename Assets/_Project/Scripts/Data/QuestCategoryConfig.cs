// Authored SO mapping each QuestCategory to its rank gate, base time limit, and potion.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    [CreateAssetMenu(fileName = "QuestCategoryConfig", menuName = "Wanderer's Guild/Config/Quest Categories", order = 0)]
    public class QuestCategoryConfig : ScriptableObject
    {
        [System.Serializable]
        public struct CategoryInfo
        {
            public QuestCategory category;
            public Rank requiredGuildRank;
            public float baseTimeLimitHours;
            public string potionId;
        }

        [SerializeField] private List<CategoryInfo> categories = new();

        private Dictionary<QuestCategory, CategoryInfo> _lookup;

        private void OnEnable() => _lookup = null;

        private void BuildLookup()
        {
            _lookup = new Dictionary<QuestCategory, CategoryInfo>(categories.Count);
            foreach (var info in categories)
                _lookup[info.category] = info;
        }

        // Returns metadata for a category; false if it hasn't been authored.
        public bool TryGet(QuestCategory category, out CategoryInfo info)
        {
            if (_lookup == null)
                BuildLookup();
            info = default;
            return _lookup != null && _lookup.TryGetValue(category, out info);
        }

        public Rank GetRequiredRank(QuestCategory category)
            => TryGet(category, out var info) ? info.requiredGuildRank : Rank.F;
    }
}