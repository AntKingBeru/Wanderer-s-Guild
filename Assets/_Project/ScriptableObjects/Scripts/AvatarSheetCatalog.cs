// Which sprite sheet each species + class combination uses: most specific entry wins, then the default sheet.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    [CreateAssetMenu(menuName = "Wanderer's Guild/Avatar Sheet Catalog", fileName = "AvatarSheetCatalog")]
    public class AvatarSheetCatalog : ScriptableObject
    {
        [Tooltip("Exact species + class beats species-only / class-only, which beat 'anyone'. Ties: the first entry wins.")]
        [SerializeField] private AvatarSheetEntry[] entries = Array.Empty<AvatarSheetEntry>();

        [Tooltip("Used when no entry matches. Empty = a generated placeholder.")]
        [SerializeField] private AdventurerSpriteSheet defaultSheet;

        public IReadOnlyList<AvatarSheetEntry> Entries => entries ?? Array.Empty<AvatarSheetEntry>();

        // The best sheet for this combination, or the default (null if neither exists).
        public AdventurerSpriteSheet Find(SpeciesData species, ClassData classData)
        {
            AdventurerSpriteSheet best = null;
            var bestScore = -1;
            foreach (var entry in Entries)
            {
                var score = entry.Specificity(species, classData);
                if (score <= bestScore)
                    continue;

                best = entry.Sheet;
                bestScore = score;
            }
            return best ? best : defaultSheet;
        }
    }
}