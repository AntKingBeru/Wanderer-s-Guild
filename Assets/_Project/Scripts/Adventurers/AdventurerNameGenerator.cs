// Picks a name for a new adventurer: a random first name, sometimes with an epithet; unique within the roster.
using System.Collections.Generic;

namespace WanderersGuild
{
    public sealed class AdventurerNameGenerator
    {
        private const int MaxAttempts = 20;
        private const string FallbackName = "Adventurer";

        private readonly AdventurerNameList _list;
        private readonly float _epithetChance;

        public AdventurerNameGenerator(AdventurerNameList list, float epithetChance)
        {
            _list = list;
            _epithetChance = epithetChance;
        }

        // NOTE: Random tries first; if every try collides, a number is appended to the last candidate ("Bran 2").
        public string Next(ICollection<string> takenNames)
        {
            var candidate = FallbackName;
            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                candidate = Random();
                if (!takenNames.Contains(candidate))
                    return candidate;
            }

            for (var suffix = 2; ; suffix++)
            {
                var numbered = $"{candidate} {suffix}";
                if (!takenNames.Contains(numbered))
                    return numbered;
            }
        }

        private string Random()
        {
            if (!_list || _list.FirstNames.Count == 0)
                return FallbackName;

            var name = _list.FirstNames[UnityEngine.Random.Range(0, _list.FirstNames.Count)];
            if (_list.Epithets.Count > 0 && UnityEngine.Random.value < _epithetChance)
                name = $"{name} {_list.Epithets[UnityEngine.Random.Range(0, _list.Epithets.Count)]}";
            return name;
        }
    }
}