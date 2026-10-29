// Persistent registry of save slots: re-reads them, answers "most recent / first empty", and deletes slots.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public class SaveSlotManager : Singleton<SaveSlotManager>
    {
        private readonly List<SaveSlotInfo> _slots = new();
        private SaveSlotStore _store;
        private SaveThumbnailCache _thumbnails;

        public IReadOnlyList<SaveSlotInfo> Slots => _slots;

        // The disk store — the in-game save writer (later session) writes headers/thumbnails through it.
        public SaveSlotStore Store => _store;

        // True when any slot holds something — loadable or unreadable. Gates the Load Game panel,
        // which is also where unreadable saves get deleted. (Continue uses TryGetMostRecent: loadable only.)
        public bool HasAnySave
        {
            get
            {
                return _slots.Any(slot => slot.State != SaveSlotState.Empty);
            }
        }

        protected override void OnSingletonAwake()
        {
            _store = new SaveSlotStore(GameConfig.Instance.SaveSlots);
            _thumbnails = new SaveThumbnailCache(_store);
            GameEventsRelay.OnSceneLoadCompleted.AddListener(OnSceneLoadCompleted);
            Refresh();
        }

        protected override void OnDestroy()
        {
            GameEventsRelay.OnSceneLoadCompleted.RemoveListener(OnSceneLoadCompleted);
            _thumbnails?.Clear(); // NOTE: Null on a destroyed duplicate.
            base.OnDestroy();
        }

        // Re-reads every slot header from disk, drops cached thumbnails, then raises OnSaveSlotsChanged.
        public void Refresh()
        {
            _thumbnails.Clear();
            _slots.Clear();

            for (var i = 0; i < _store.SlotCount; i++)
                _slots.Add(_store.ReadSlot(i));

            GameEventsRelay.OnSaveSlotsChanged.Invoke();
        }

        // Out-of-range indices read as Empty.
        public SaveSlotInfo GetSlot(int index) =>
            index >= 0 && index < _slots.Count ? _slots[index] : new SaveSlotInfo(index, SaveSlotState.Empty, null);

        // The loadable slot with the latest real-world save time (Continue's target).
        public bool TryGetMostRecent(out int index)
        {
            index = -1;
            var newest = long.MinValue;

            foreach (var slot in _slots.Where(slot => slot.State == SaveSlotState.Ready && slot.Header.SavedAtUtcTicks > newest))
            {
                newest = slot.Header.SavedAtUtcTicks;
                index = slot.Index;
            }
            return index >= 0;
        }

        // The lowest-numbered empty slot (New Game's target). Corrupted slots don't count as empty.
        public bool TryGetFirstEmpty(out int index)
        {
            foreach (var slot in _slots.Where(slot => slot.State == SaveSlotState.Empty))
            {
                index = slot.Index;
                return true;
            }

            index = -1;
            return false;
        }

        // Cached thumbnail of a loadable slot, or null. Valid until the next Refresh.
        public Texture2D GetThumbnail(int index) =>
            GetSlot(index).State == SaveSlotState.Ready ? _thumbnails.Get(index) : null;

        public bool Delete(int index)
        {
            if (index < 0 || index >= _slots.Count)
                return false;

            var deleted = _store.TryDelete(index);
            Refresh(); // NOTE: Re-read even on failure — the folder may be partly gone.
            return deleted;
        }

        // NOTE: Returning to the menu from a game (or the debug menu) may have changed the files.
        private void OnSceneLoadCompleted(SceneId scene)
        {
            if (scene == SceneId.MainMenu)
                Refresh();
        }
    }
}