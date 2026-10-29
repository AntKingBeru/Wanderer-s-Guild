// The clipboard's choices for one request — reward share, minimum rank, party size — and the terms the chosen indices make.
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class QuestTermsOptions
    {
        private readonly List<int> _shares = new();
        private readonly List<GuildRank> _ranks = new();
        private readonly List<int> _sizes = new();
        private readonly List<string> _shareLabels = new();
        private readonly List<string> _rankLabels = new();
        private readonly List<string> _sizeLabels = new();

        public IReadOnlyList<string> ShareLabels => _shareLabels;
        public IReadOnlyList<string> RankLabels => _rankLabels;
        public IReadOnlyList<string> SizeLabels => _sizeLabels;

        // Indices of the default terms in each list.
        public int DefaultShareIndex { get; }
        public int DefaultRankIndex { get; }
        public int DefaultSizeIndex { get; }

        public QuestTermsOptions(RequestData request, QuestTerms defaults, QuestSettings settings)
        {
            for (var share = settings.MinRewardShare; share <= settings.MaxRewardShare; share += Mathf.Max(1, settings.RewardShareStep))
            {
                _shares.Add(share);
                _shareLabels.Add($"{share}%");
            }

            // NOTE: One rank below to one above the request's rank (e.g. a D request → E, D, C).
            var ranks = settings.MinRankLimits(request.RecommendedRank);
            for (var rank = ranks.lowest; rank <= ranks.highest; rank++)
            {
                _ranks.Add(rank);
                _rankLabels.Add(rank.ToString());
            }

            var sizes = settings.PartySizeLimits(request.RecommendedRank);
            for (var size = sizes.x; size <= sizes.y; size++)
            {
                _sizes.Add(size);
                _sizeLabels.Add(size.ToString());
            }

            DefaultShareIndex = IndexOrFirst(_shares, defaults.RewardSharePercent);
            DefaultRankIndex = IndexOrFirst(_ranks, defaults.MinRank);
            DefaultSizeIndex = IndexOrFirst(_sizes, defaults.PartySize);
        }

        public QuestTerms Terms(int shareIndex, int rankIndex, int sizeIndex) =>
            new(_shares[Clamp(shareIndex, _shares)], _ranks[Clamp(rankIndex, _ranks)], _sizes[Clamp(sizeIndex, _sizes)]);

        private static int IndexOrFirst<T>(List<T> values, T value) => Mathf.Max(0, values.IndexOf(value));
        private static int Clamp<T>(int index, List<T> values) => Mathf.Clamp(index, 0, values.Count - 1);
    }
}