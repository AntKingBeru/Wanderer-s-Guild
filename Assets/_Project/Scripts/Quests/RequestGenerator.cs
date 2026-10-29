// Picks requests for a batch: eligible by rank (guild rank + allowance) and reputation-gated source, not already posted.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class RequestGenerator
    {
        private readonly RequestSettings _settings;
        private readonly IReadOnlyCollection<RequestData> _pool;
        private readonly List<RequestData> _candidates = new();

        public RequestGenerator(RequestSettings settings, IReadOnlyCollection<RequestData> pool)
        {
            _settings = settings;
            _pool = pool;
        }

        // Up to <paramref name="count"/> distinct eligible templates, chosen at random (fewer if not enough exist).
        public List<RequestData> Pick(int count, GuildRank guildRank, ReputationLevel reputation, RequestBoard board)
        {
            _candidates.Clear();
            foreach (var template in _pool)
            {
                if (IsEligible(template, guildRank, reputation) && !board.ContainsTemplate(template.Id))
                    _candidates.Add(template);
            }

            // NOTE: Partial Fisher–Yates — each pick is uniform and never repeats.
            List<RequestData> picked = new();
            var picks = Mathf.Min(count, _candidates.Count);
            for (var i = 0; i < picks; i++)
            {
                var swap = Random.Range(i, _candidates.Count);
                (_candidates[i], _candidates[swap]) = (_candidates[swap], _candidates[i]);
                picked.Add(_candidates[i]);
            }
            return picked;
        }

        private bool IsEligible(RequestData template, GuildRank guildRank, ReputationLevel reputation) =>
            (int)template.RecommendedRank <= (int)guildRank + _settings.RankAllowance &&
            _settings.IsSourceAllowed(template.Source, reputation);
    }
}