// Builds an id → asset lookup from an imported database, skipping null entries, empty ids and duplicates (warned).
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public static class DatabaseLookup
    {
        public static Dictionary<string, T> Build<T>(IReadOnlyList<T> items, string databaseName) where T : Object, IIdentifiable
        {
            Dictionary<string, T> byId = new();

            foreach (var item in items)
            {
                if (!item)
                    continue;

                if (string.IsNullOrWhiteSpace(item.Id))
                {
                    Debug.LogWarning($"[{databaseName}] '{item.name}' has no id and is skipped.", item);
                    continue;
                }

                if (!byId.TryAdd(item.Id, item))
                    Debug.LogWarning($"[{databaseName}] Duplicate id '{item.Id}' on '{item.name}'; skipped.", item);
            }

            return byId;
        }
    }
}