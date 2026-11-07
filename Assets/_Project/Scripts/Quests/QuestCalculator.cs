// Quest maths (pure): default/clamped terms, who qualifies, appeal, duration, difficulty, member/party power, success chance.
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
        public static float Appeal(Adventurer adventurer, Quest quest, QuestSettings settings) =>
            Appeal(adventurer, quest, settings, quest.GoldPerMember);

        // The same appeal test at a given gold per member (e.g. after a potion share cut).
        public static float Appeal(Adventurer adventurer, Quest quest, QuestSettings settings, int goldPerMember)
        {
            var expectation = Mathf.Max(1f, settings.ExpectedGoldPerLevel * adventurer.Level);
            var offered = goldPerMember * (adventurer.Class.HasAffinity(quest.Request.Category) ? settings.AffinityAcceptFactor : 1f);
            return offered / expectation;
        }

        public static int DurationDays(RequestData request, QuestSettings settings) =>
            settings.BaseDays(request.Category) + (int)request.RecommendedRank / settings.RanksPerExtraDay;

        // Per-member difficulty of the request's rank × its recommended party size.
        public static float Difficulty(RequestData request, QuestSettings settings) =>
            settings.DifficultyPerMember(request.RecommendedRank) * request.RecommendedPartySize;

        // NOTE: Weighted stats (incl. gear) × affinity bonus × the supper bonus they carry (Well fed).
        public static float MemberPower(Adventurer adventurer, QuestCategory category, QuestSettings settings)
        {
            var power = settings.Weights(category).Apply(adventurer.Stats);
            if (adventurer.Class.HasAffinity(category))
                power *= 1f + settings.AffinityPowerBonus;
            return power * (1f + adventurer.WellFedBonus);
        }

        public static float PartyPower(IEnumerable<Adventurer> members, QuestCategory category, QuestSettings settings)
        {
            return members.Sum(member => MemberPower(member, category, settings));
        }

        // chanceAtEven at power = difficulty, moving chancePerRatio per 1.0 of power ÷ difficulty; plus a flat bonus
        // (potions) added before clamping.
        public static float SuccessChance(float partyPower, float difficulty, QuestSettings settings, float flatBonus = 0f)
        {
            if (difficulty <= 0f)
                return settings.MaxChance;

            var ratio = partyPower / difficulty;
            var chance = settings.ChanceAtEven + (ratio - 1f) * settings.ChancePerRatio + flatBonus;
            return Mathf.Clamp(chance, settings.MinChance, settings.MaxChance);
        }
    }
}