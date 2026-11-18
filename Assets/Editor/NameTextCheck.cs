// Text-length check for adventurer names on the quest note's member line: Max Name Length and every given name, + " · Lv 99".
using System;
using UnityEngine;
using WanderersGuild;
using Object = UnityEngine.Object;

namespace Editor
{
    public sealed class NameTextCheck : ITextLengthCheck
    {
        private const string LevelSuffix = " · Lv 99";

        // NOTE: A given name is never shortened (only an added family name/epithet is dropped), so each one is checked on its
        // own. GameConfig isn't a singleton outside Play Mode — it's looked up in the open scenes.
        public int Run(Action<string, Object> report)
        {
            var count = 0;
            var config = Object.FindAnyObjectByType<GameConfig>(FindObjectsInactive.Include);
            if (config != null)
            {
                var longest = new string('W', config.Adventurers.MaxNameLength) + LevelSuffix;
                TextLengthValidator.Check(TextLimits.NoteMember, longest, "GameConfig › Adventurers › Max Name Length", config, report);
                count++;
            }
            else
            {
                Debug.Log("[Text] GameConfig isn't in an open scene — the Max Name Length check was skipped.");
            }

            foreach (var list in TextLengthValidator.LoadAll<AdventurerNameList>())
            {
                foreach (var given in list.GivenNames)
                {
                    if (string.IsNullOrWhiteSpace(given))
                        continue;
                    TextLengthValidator.Check(TextLimits.NoteMember, given.Trim() + LevelSuffix, list.name, list, report);
                    count++;
                }
            }
            return count;
        }
    }
}