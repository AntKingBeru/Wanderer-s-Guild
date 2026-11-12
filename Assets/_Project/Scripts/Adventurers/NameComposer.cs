// Builds one random name from a name list: a given name, then maybe a family name, otherwise maybe an epithet — never both,
// and the extra is left off if the full name would pass the length limit.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public static class NameComposer
    {
        // NOTE: given = the bare given name (what the generator checks for repeats); full = what the adventurer is called.
        // False if the list is missing or has no given names.
        public static bool TryCompose(AdventurerNameList list, int maxLength, out string given, out string full)
        {
            given = full = null;
            if (list == null || list.GivenNames.Count == 0)
                return false;

            given = Pick(list.GivenNames);
            if (string.IsNullOrWhiteSpace(given))
                return false;

            given = given.Trim();
            full = given;

            var extra = PickExtra(list);
            if (!string.IsNullOrWhiteSpace(extra))
            {
                var withExtra = $"{given} {extra.Trim()}";
                if (withExtra.Length <= maxLength)
                    full = withExtra;
            }
            return true;
        }

        // NOTE: One roll decides: [0, family) → family name, [family, family + epithet) → epithet, else nothing.
        private static string PickExtra(AdventurerNameList list)
        {
            var roll = Random.value;
            if (roll < list.FamilyNameChance)
                return list.FamilyNames.Count > 0 ? Pick(list.FamilyNames) : null;
            if (roll < list.FamilyNameChance + list.EpithetChance)
                return list.Epithets.Count > 0 ? Pick(list.Epithets) : null;
            return null;
        }

        private static string Pick(IReadOnlyList<string> options) => options[Random.Range(0, options.Count)];
    }
}