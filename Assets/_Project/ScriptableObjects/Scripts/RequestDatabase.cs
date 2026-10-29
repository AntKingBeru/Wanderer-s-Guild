// The imported pool of every RequestData (built by Database › Load); filters out unusable entries for the generator.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    [CreateAssetMenu(menuName = "Wanderer's Guild/Request Database", fileName = "RequestDatabase")]
    public class RequestDatabase : ScriptableObject
    {
        [SerializeField] private RequestData[] requests = Array.Empty<RequestData>();

        public IReadOnlyList<RequestData> Requests => requests ?? Array.Empty<RequestData>();

        // Usable requests by id. NOTE: Null entries and empty ids are skipped; for duplicate ids the first wins.
        public Dictionary<string, RequestData> BuildLookup()
        {
            Dictionary<string, RequestData> byId = new();

            foreach (var request in Requests)
            {
                if (!request)
                    continue;

                if (string.IsNullOrWhiteSpace(request.Id))
                {
                    Debug.LogWarning($"[RequestDatabase] '{request.name}' has no id and is skipped.", request);
                    continue;
                }

                if (!byId.TryAdd(request.Id, request))
                    Debug.LogWarning($"[RequestDatabase] Duplicate id '{request.Id}' on '{request.name}'; skipped.", request);
            }

            return byId;
        }

#if UNITY_EDITOR
        // Editor-only: the importer replaces the list after each load.
        public void SetRequests(RequestData[] items) => requests = items ?? Array.Empty<RequestData>();
#endif
    }
}