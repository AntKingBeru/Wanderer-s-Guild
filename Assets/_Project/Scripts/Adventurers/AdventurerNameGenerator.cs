// Names new adventurers from their species' list (or the common one): unique among adventurers and applicants, preferring
// given names nobody has and that weren't handed out recently.
using System;
using System.Collections.Generic;

namespace WanderersGuild
{
    public sealed class AdventurerNameGenerator
    {
        private const int PreferredAttempts = 30; // NOTE: Stage 1 — unique full name, given name unused and not recent.
        private const int UniqueAttempts = 30;    // NOTE: Stage 2 — just a unique full name.
        private const string FallbackName = "Adventurer";

        private readonly AdventurerNameList _common;
        private readonly int _maxLength;
        private readonly int _recentCount;
        private readonly Queue<string> _recent = new();
        private readonly HashSet<string> _avoid = new(StringComparer.OrdinalIgnoreCase);

        /// <param name="common">Fallback list for species without their own.</param>
        /// <param name="maxLength">Longest full name (a family name or epithet past it is left off).</param>
        /// <param name="recentCount">How many recently given names to avoid (0 = none).</param>
        public AdventurerNameGenerator(AdventurerNameList common, int maxLength, int recentCount)
        {
            _common = common;
            _maxLength = Math.Max(1, maxLength);
            _recentCount = Math.Max(0, recentCount);
        }

        // NOTE: Stage 1, then stage 2, then the last candidate numbered ("Bran 2") — always terminates.
        public string Next(SpeciesData species, ICollection<string> takenNames)
        {
            var list = species != null && species.NameList != null ? species.NameList : _common;
            FillAvoid(takenNames);

            string lastGiven = null, lastFull = null;
            for (var attempt = 0; attempt < PreferredAttempts + UniqueAttempts; attempt++)
            {
                if (!NameComposer.TryCompose(list, _maxLength, out var given, out var full))
                    break;

                lastGiven = given;
                lastFull = full;
                if (takenNames.Contains(full))
                    continue;
                if (attempt < PreferredAttempts && _avoid.Contains(given))
                    continue;
                return Accept(given, full);
            }

            var baseName = lastFull ?? FallbackName;
            for (var suffix = 2; ; suffix++)
            {
                var numbered = $"{baseName} {suffix}";
                if (!takenNames.Contains(numbered))
                    return Accept(lastGiven ?? FallbackName, numbered);
            }
        }

        // NOTE: Given names of everyone on the roster / waiting, plus the recent ones.
        private void FillAvoid(ICollection<string> takenNames)
        {
            _avoid.Clear();
            foreach (var name in takenNames)
                _avoid.Add(AdventurerFormatter.GivenName(name));
            foreach (var given in _recent)
                _avoid.Add(given);
        }

        // NOTE: Remembers the given name so the next arrivals (declined applicants included) don't repeat it soon.
        private string Accept(string given, string full)
        {
            if (_recentCount > 0)
            {
                _recent.Enqueue(given);
                while (_recent.Count > _recentCount)
                    _recent.Dequeue();
            }
            return full;
        }
    }
}