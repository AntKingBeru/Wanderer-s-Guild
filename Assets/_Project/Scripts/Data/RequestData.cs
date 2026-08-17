// Authored SO defining a request template. Runtime requests are spawned from these (in the Quest layer).
using UnityEngine;

namespace WanderersGuild
{
    [CreateAssetMenu(fileName = "RequestData", menuName = "Wanderer's Guild/Data/Request", order = 2)]
    public class RequestData : ScriptableObject, IIdentifiable
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField, TextArea] private string objective;

        [Header("Classification")]
        [SerializeField] private QuestCategory category;
        [SerializeField] private Difficulty difficulty;
        [SerializeField] private Rank recommendedRank;
        [SerializeField] private RequestSource source;

        [Header("Reward & Timing")]
        [SerializeField] private RewardData reward;
        [Tooltip("Days from generation until this request expires if unfulfilled.")]
        [SerializeField] private int expirationDays = 7;

        public string Id => id;
        public string Objective => objective;
        public QuestCategory Category => category;
        public Difficulty Difficulty => difficulty;
        public Rank RecommendedRank => recommendedRank;
        public RequestSource Source => source;
        public RewardData Reward => reward;
        public int ExpirationDays => expirationDays;

        // Runtime-only factory for placeholder data before real assets exist.
        public static RequestData CreatePlaceholder(string id, string objective, QuestCategory category,
            Difficulty difficulty, Rank rank, RequestSource source, int gold, int expirationDays)
        {
            var so = CreateInstance<RequestData>();
            so.id = id;
            so.objective = objective;
            so.category = category;
            so.difficulty = difficulty;
            so.recommendedRank = rank;
            so.source = source;
            so.reward = new RewardData { gold = gold };
            so.expirationDays = expirationDays;
            return so;
        }
    }
}