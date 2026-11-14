// Who is holding saving back right now (the start tutorial); while anyone is, nothing writes the slot.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Registry of owners — saving resumes only when the last one lets go.
    public static class SaveBlockers
    {
        private static readonly HashSet<object> Owners = new();

        public static bool IsBlocked => Owners.Count > 0;

        public static void Block(object owner)
        {
            if (owner != null)
                Owners.Add(owner);
        }

        public static void Unblock(object owner) => Owners.Remove(owner);

        // NOTE: With domain reload off, a stale owner from the last Play session would block every save.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() => Owners.Clear();
    }
}