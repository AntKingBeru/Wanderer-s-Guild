// Pure resolver: matches a party's combined stats against a quest and produces a QuestOutcome.

using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public static class QuestResolver
{
    public static QuestOutcome Resolve(Quest quest, IReadOnlyList<Adventurer> party, System.Random rng)
    {
        var config = GameConfig.Instance.Resolution;
        var diffIndex = (int)quest.Difficulty;

        var threshold = diffIndex >= 0 && diffIndex < config.difficultyStatThreshold.Length
            ? Mathf.Max(1, config.difficultyStatThreshold[diffIndex])
            : 1;

        var partyTotal = party.Sum(a => a.Stats.Total);
        
        var overUnderRatio = (partyTotal - threshold) / (float)threshold;
        var chance = config.baseSuccessChance + overUnderRatio * 10f * config.chancePerTenPercent;
        chance = Mathf.Clamp01(chance);

        var success = rng.NextDouble() < chance;
        
        var survivorCount = Mathf.Max(1, party.Count);
        var baseXp = config.experiencePerDifficulty * (diffIndex + 1);

        if (success)
        {
            var adventurerShare = quest.RewardGold - quest.GuildCut;
            var perSurvivor = adventurerShare / survivorCount;
            return new QuestOutcome(true, quest.GuildCut, perSurvivor,
                                    baseXp, config.rankProgressPerSuccess, config.reputationOnSuccess);
        }

        var failXp = baseXp * Mathf.Clamp(config.failureExperienceFraction, 0, 100) / 100;
        return new QuestOutcome(false, 0, 0, failXp, 0, config.reputationOnFailure);
    }
    
    public static bool RollCasualty(Quest quest, int partyTotal, System.Random rng)
    {
        var config = GameConfig.Instance.Resolution;
        var diffIndex = (int)quest.Difficulty;
        var threshold = diffIndex >= 0 && diffIndex < config.difficultyStatThreshold.Length
            ? Mathf.Max(1, config.difficultyStatThreshold[diffIndex]) : 1;
        
        var shortfall = Mathf.Max(0f, (threshold - partyTotal) / (float)threshold);
        var deathChance = Mathf.Clamp01(config.baseDeathChanceOnFailure * (1f + shortfall * 2f));
        return rng.NextDouble() < deathChance;
    }
}