using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class SaveThumbnailCache
    {
        private readonly SaveSlotStore _store;
        private readonly Dictionary<int, Texture2D> _textures = new();

        public SaveThumbnailCache(SaveSlotStore store) => _store = store;

        // NOTE: A missing thumbnail is cached too (as null), so the disk isn't hit again on every list refresh.
        public Texture2D Get(int index)
        {
            if (_textures.TryGetValue(index, out var cached))
                return cached;

            var loaded = _store.LoadThumbnail(index);
            _textures[index] = loaded;
            return loaded;
        }

        public void Clear()
        {
            foreach (var texture in _textures.Values.Where(texture => texture))
            {
                Object.Destroy(texture);
            }

            _textures.Clear();
        }
    }
}