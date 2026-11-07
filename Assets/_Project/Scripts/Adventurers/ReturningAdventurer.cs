// Brings recalled adventurers home: anyone Returning whose return time has passed becomes Idle (and arrives at the guild).
using System.Collections.Generic;

namespace WanderersGuild
{
    // NOTE: Called from QuestManager's tick. A return time of 0 (edited save) is already past, so they come home at once.
    public static class ReturningAdventurers
    {
        private static readonly List<Adventurer> Due = new();

        public static void ReleaseDue(long nowMinutes)
        {
            if (!AdventurerManager.HasInstance)
                return;

            Due.Clear();
            foreach (var adventurer in AdventurerManager.Instance.Adventurers)
            {
                if (adventurer.Status == AdventurerStatus.Returning && nowMinutes >= adventurer.ReturningUntilMinutes)
                    Due.Add(adventurer);
            }

            // NOTE: Collected first — the events below may touch the roster's listeners.
            foreach (var adventurer in Due)
            {
                adventurer.FinishReturn();
                GameEventsRelay.OnAdventurerChanged.Invoke(adventurer);
                GameEventsRelay.OnAdventurerReturned.Invoke(adventurer);
            }
            Due.Clear();
        }
    }
}