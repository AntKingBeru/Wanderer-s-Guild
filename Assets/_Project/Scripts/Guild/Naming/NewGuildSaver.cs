// Writes a new guild's first save into its slot (header first, then clears the old guild's leftovers).
using System;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class NewGuildSaver
    {
        private readonly NewGuildSettings _settings;

        public NewGuildSaver(NewGuildSettings settings) => _settings = settings;

        /// <param name="guildName"></param>
        /// <param name="error">A short message for the player when saving fails; null on success.</param>
        public bool TryCreate(string guildName, out string error)
        {
            error = null;
            if (!SaveSlotManager.HasInstance)
            {
                Debug.LogError("[NewGuildSaver] No SaveSlotManager under __PersistentSystems.");
                error = "Saving isn't available.";
                return false;
            }

            var slots = SaveSlotManager.Instance;
            if (!TryResolveSlot(slots, out var slot))
            {
                error = "No free save slot.";
                return false;
            }

            var header = SaveSlotHeader.Create(guildName, _settings.StartingRank, _settings.StartingDate, 0d, DateTime.UtcNow);
            var saved = slots.Store.TryResetSlot(slot, header);
            slots.Refresh();

            if (!saved)
                error = "Couldn't save the guild. Try again.";
            return saved;
        }

        // NOTE: Normally New Game already chose the slot; a dev run straight into the Intro falls back to the first empty one.
        private static bool TryResolveSlot(SaveSlotManager slots, out int slot)
        {
            slot = GameSession.ActiveSlot;
            if (slot >= 0)
                return true;

            if (!slots.TryGetFirstEmpty(out slot))
                return false;

            Debug.LogWarning($"[NewGuildSaver] No active slot (Intro entered without New Game); using slot {slot + 1}.");
            GameSession.BeginNewGame(slot);
            return true;
        }
    }
}