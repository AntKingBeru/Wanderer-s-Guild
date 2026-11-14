// Every enabled guild object (desk, quest board, lounge, office…), found by the screen it opens — they spawn with their rooms.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Registry — GuildInteractable joins in OnEnable and leaves in OnDisable. Destroyed entries are pruned on lookup.
    public static class GuildInteractables
    {
        private static readonly List<GuildInteractable> All = new();

        public static void Register(GuildInteractable interactable)
        {
            if (interactable != null && !All.Contains(interactable))
                All.Add(interactable);
        }

        public static void Unregister(GuildInteractable interactable) => All.Remove(interactable);

        // The object that opens this screen (null = none). Newest first, so a respawned room's object wins over a leftover.
        public static GuildInteractable Find(MenuPanelId opens)
        {
            for (var i = All.Count - 1; i >= 0; i--)
            {
                var interactable = All[i];
                if (interactable == null)
                {
                    All.RemoveAt(i);
                    continue;
                }

                if (interactable.Opens == opens)
                    return interactable;
            }
            return null;
        }

        // NOTE: With domain reload off, last session's objects would linger.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() => All.Clear();
    }
}