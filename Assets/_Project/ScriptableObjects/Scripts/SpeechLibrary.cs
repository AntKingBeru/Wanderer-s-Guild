// The lines adventurers say, by topic. A new asset comes filled with the default lines (ASCII only — the pixel font's safe set).
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    [CreateAssetMenu(menuName = "Wanderer's Guild/Speech Library", fileName = "SpeechLibrary")]
    public class SpeechLibrary : ScriptableObject
    {
        // NOTE: Field initializer = the defaults a freshly created asset gets. Edit freely in the Inspector.
        [SerializeField] private SpeechLineSet[] sets =
        {
            new(SpeechTopic.Departing, "Off we go!", "Wish us luck!", "Adventure awaits!", "Swords ready!", "Back soon!"),
            new(SpeechTopic.QuestSucceeded, "We did it!", "Easy coin!", "Another job done.", "Pay day!", "Not a scratch!"),
            new(SpeechTopic.QuestFailed, "That went badly...", "Next time...", "We'll be back.", "Rough day.", "Don't ask."),
            new(SpeechTopic.CameBack, "Home at last.", "Good to be back.", "Missed this place.", "Finally, home."),
            new(SpeechTopic.LeveledUp, "I feel stronger!", "Level up!", "Getting better!", "Did you see that?"),
            new(SpeechTopic.Injured, "Ow... my ribs.", "Need a bed.", "Just a scratch...", "That one hurt."),
            new(SpeechTopic.WellFed, "Delicious!", "What a stew!", "So full...", "Best meal in town!", "More bread?"),
            new(SpeechTopic.Chatter, "Nice day.", "Seen my boots?", "Hmm...", "Any good quests?", "I need a drink.",
                "This place is growing!"),
            new(SpeechTopic.MealChatter, "Pass the salt.", "Smells good!", "Is there more?", "Mmm..."),
            new(SpeechTopic.ShopChatter, "Shiny...", "Too pricey.", "I'll take that one!", "Just looking."),
            new(SpeechTopic.RestChatter, "Ugh...", "Need... rest.", "Everything hurts.", "Water, please."),
            new(SpeechTopic.Sleeping, "Zzz...", "Zzz... zz...", "*snore*", "Mmh... treasure...")
        };

        // This topic's lines (empty if none). NOTE: The first set with lines wins if a topic is listed twice.
        public IReadOnlyList<string> LinesFor(SpeechTopic topic)
        {
            if (sets == null)
                return Array.Empty<string>();

            foreach (var set in sets)
            {
                if (set.Topic == topic && set.Lines.Count > 0)
                    return set.Lines;
            }
            return Array.Empty<string>();
        }
    }
}