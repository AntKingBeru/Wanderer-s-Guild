// Art asset: one 12×12 icon per quest category (shown on pinned quest notes); a missing icon falls back to letters.
using System;
using System.Linq;
using UnityEngine;

namespace WanderersGuild
{
    [CreateAssetMenu(menuName = "Wanderer's Guild/Quest Category Icons", fileName = "QuestCategoryIcons")]
    public class QuestCategoryIcons : ScriptableObject
    {
        [Tooltip("12×12 sprites (Point filter, no compression). One entry per category; the first entry for a category wins.")]
        [SerializeField] private CategoryIconEntry[] icons = Array.Empty<CategoryIconEntry>();

        // The category's icon, or null if none is assigned.
        public Sprite IconFor(QuestCategory category)
        {
            return (from entry in icons ?? Array.Empty<CategoryIconEntry>() where entry.Category == category select entry.Icon).FirstOrDefault();
        }
    }
}