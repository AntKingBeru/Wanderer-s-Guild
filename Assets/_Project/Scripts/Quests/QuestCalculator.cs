// Quest maths (one place, all tunable in GameConfig › Quests): terms limits, appeal, duration, difficulty, power, success chance.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public static class QuestCalculator
    {
        // Defaults for a request: default share, its recommended rank and party size (clamped to the limits).
        public static QuestTerms DefaultTerms(RequestData request, QuestSettings settings) =>
            ClampTerms(new QuestTerms(settings.DefaultRewardShare, request.RecommendedRank, request.RecommendedPartySize),
                       request, settings);

        // Snaps share to its step and range, minimum rank to one below…one above the request's rank,
        // party size to the quest rank's limits.
        public static QuestTerms ClampTerms(QuestTerms terms, RequestData request, QuestSettings settings)
        {
            var ranks = settings.MinRankLimits(request.RecommendedRank);
            var minRank = terms.MinRank < ranks.lowest ? ranks.lowest : terms.MinRank > ranks.highest ? ranks.highest : terms.MinRank;

            var sizes = settings.PartySizeLimits(request.RecommendedRank);
            return new QuestTerms(settings.SnapShare(terms.RewardSharePercent), minRank,
                                  Mathf.Clamp(terms.PartySize, sizes.x, sizes.y));
        }

        public static bool Qualifies(Adventurer adventurer, Quest quest) => adventurer.Rank >= quest.Terms.MinRank;

        // Offered gold per member ÷ the adventurer's expectation (affinity makes the offer look bigger). ≥ 1 = willing.
        public static float Appeal(Adventurer adventurer, Quest quest, QuestSettings settings)
        {
            var expectation = Mathf.Max(1f, settings.ExpectedGoldPerLevel * adventurer.Level);
            var offered = quest.GoldPerMember * (adventurer.Class.HasAffinity(quest.Request.Category) ? settings.AffinityAcceptFactor : 1f);
            return offered / expectation;
        }

        public static int DurationDays(RequestData request, QuestSettings settings) =>
            settings.BaseDays(request.Category) + (int)request.RecommendedRank / settings.RanksPerExtraDay;

        // Per-member difficulty of the request's rank × its recommended party size.
        public static float Difficulty(RequestData request, QuestSettings settings) =>
            settings.DifficultyPerMember(request.RecommendedRank) * request.RecommendedPartySize;

        public static float MemberPower(Adventurer adventurer, QuestCategory category, QuestSettings settings)
        {
            var power = settings.Weights(category).Apply(adventurer.Stats);
            return adventurer.Class.HasAffinity(category) ? power * (1f + settings.AffinityPowerBonus) : power;
        }

        public static float PartyPower(IEnumerable<Adventurer> members, QuestCategory category, QuestSettings settings)
        {
            return members.Sum(member => MemberPower(member, category, settings));
        }

        // chanceAtEven at power = difficulty, moving chancePerRatio per 1.0 of power ÷ difficulty; clamped.
        public static float SuccessChance(float partyPower, float difficulty, QuestSettings settings)
        {
            if (difficulty <= 0f) return settings.MaxChance;

            var ratio = partyPower / difficulty;
            return Mathf.Clamp(settings.ChanceAtEven + (ratio - 1f) * settings.ChancePerRatio, settings.MinChance, settings.MaxChance);
        }
    }
}