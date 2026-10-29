// One authored request (imported from the Requests sheet): title, objective, category, rank, source, party size, reward, expiry.
using UnityEngine;

namespace WanderersGuild
{
    [CreateAssetMenu(menuName = "Wanderer's Guild/Request Data", fileName = "RequestData")]
    public class RequestData : ScriptableObject, IIdentifiable
    {
        [Header("Identity")]
        [Tooltip("Stable, unique id (also the asset's file name) — saves refer to requests by it. Never change it once players have saves.")]
        [SerializeField] private string id;
        [Tooltip("Short title shown on the request board (sheet column NAME).")]
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string objective;

        [Header("Classification")]
        [SerializeField] private QuestCategory category;
        [SerializeField] private GuildRank recommendedRank;
        [SerializeField] private RequestSource source;

        [Tooltip("Adventurers this request is meant for (input to the calculated difficulty). " +
                 "HLD party limits: 2–5 at lower ranks, 3–7 at higher ranks.")]
        [SerializeField, Range(1, 7)] private int recommendedPartySize = 3;

        [Header("Reward & Timing")]
        [SerializeField] private RewardData reward;
        [Tooltip("Days from generation until this request expires if unfulfilled.")]
        [SerializeField, Min(1)] private int expirationDays = 7;

        public string Id => id;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? id : displayName;
        public string Objective => objective;
        public QuestCategory Category => category;
        public GuildRank RecommendedRank => recommendedRank;
        public RequestSource Source => source;
        public int RecommendedPartySize => recommendedPartySize;
        public RewardData Reward => reward;
        public int ExpirationDays => expirationDays;

#if UNITY_EDITOR
        // Editor-only: the sheet importer fills or updates this asset in place (its GUID never changes).
        public void ApplyImport(in RequestImportRow row)
        {
            id = row.Id;
            displayName = row.DisplayName;
            objective = row.Objective;
            category = row.Category;
            recommendedRank = row.RecommendedRank;
            source = row.Source;
            recommendedPartySize = Mathf.Clamp(row.RecommendedPartySize, 1, 7);
            reward = row.Reward;
            expirationDays = Mathf.Max(1, row.ExpirationDays);
        }
#endif
    }
}