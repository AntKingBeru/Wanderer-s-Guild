// Generates "The <adjective> <noun>" party names, unique among the parties that exist now.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public static class PartyNames
    {
        private const int Attempts = 30;

        // NOTE: Random tries first, then a numbered fallback, so it always returns a unique name.
        public static string Generate(PartySettings settings, ICollection<string> taken, int partyId)
        {
            IReadOnlyList<string> adjectives = settings.NameAdjectives, nouns = settings.NameNouns;
            if (adjectives.Count > 0 && nouns.Count > 0)
            {
                for (var i = 0; i < Attempts; i++)
                {
                    var name = $"The {adjectives[Random.Range(0, adjectives.Count)]} {nouns[Random.Range(0, nouns.Count)]}";
                    if (!taken.Contains(name))
                        return name;
                }
            }
            return $"Party #{partyId}";
        }
    }
}