// Mood text for the UI: level names, "Mood: Content (62)" with what the level does, and the reasons, biggest first.
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: ASCII only ("-" and "..."), the pixel font's safe set. Sorts the reasons list it's given (the caller's scratch).
    public static class MoodFormatter
    {
        private static readonly StringBuilder Builder = new();

        public static string LevelName(SatisfactionLevel level) => level.ToString();

        // "Mood: Unhappy (34) · wants +20% pay".
        public static string Line(MoodSnapshot mood, SatisfactionSettings settings)
        {
            var line = $"Mood: {LevelName(mood.Level)} ({Mathf.RoundToInt(mood.Score)})";
            var effect = Effect(mood.Level, settings);
            return effect.Length > 0 ? $"{line} · {effect}" : line;
        }

        // What the level does right now (empty for Content).
        public static string Effect(SatisfactionLevel level, SatisfactionSettings settings) => level switch
        {
            SatisfactionLevel.Happy => $"+{Mathf.RoundToInt(settings.HappyPowerBonus * 100f)}% power",
            SatisfactionLevel.Unhappy => $"wants +{ExtraPercent(settings.UnhappyAppeal)}% pay",
            SatisfactionLevel.Miserable => $"wants +{ExtraPercent(settings.MiserableAppeal)}% pay, may quit",
            _ => string.Empty
        };

        // One reason per line, biggest effect first; at most `max`, then "...and N more". Values rounding to 0 are skipped.
        public static string Reasons(List<MoodEntry> entries, int max)
        {
            if (entries == null)
                return string.Empty;

            entries.Sort((a, b) => Mathf.Abs(b.Value).CompareTo(Mathf.Abs(a.Value)));
            Builder.Clear();
            var shown = 0;
            var hidden = 0;
            foreach (var entry in entries)
            {
                var points = Mathf.RoundToInt(entry.Value);
                if (points == 0)
                    continue;
                if (shown >= max)
                {
                    hidden++;
                    continue;
                }

                if (shown > 0)
                    Builder.Append('\n');
                Builder.Append(points > 0 ? "+" : "-").Append(Mathf.Abs(points)).Append(' ').Append(ReasonName(entry.Reason));
                shown++;
            }

            if (hidden > 0)
                Builder.Append($"\n...and {hidden} more");
            return shown == 0 ? "Nothing on their mind" : Builder.ToString();
        }

        public static string ReasonName(MoodReason reason) => reason switch
        {
            MoodReason.NewArrival => "New here",
            MoodReason.AteSupper => "Good supper",
            MoodReason.WentHungry => "Went hungry",
            MoodReason.QuestSucceeded => "Quest success",
            MoodReason.QuestFailed => "Quest failed",
            MoodReason.GoodPay => "Good pay",
            MoodReason.PoorPay => "Poor pay",
            MoodReason.Injured => "Injured",
            MoodReason.InfirmaryCare => "Infirmary care",
            MoodReason.PromotionPassed => "Promoted",
            MoodReason.ExamFailed => "Failed an exam",
            MoodReason.ClassAdvanced => "New class",
            MoodReason.TrialFailed => "Failed a trial",
            MoodReason.PartyFormed => "New party",
            MoodReason.PartyBrokeUp => "Party broke up",
            MoodReason.Recalled => "Recalled",
            MoodReason.LodgingOutside => "Sleeping outside",
            MoodReason.OwnBed => "Own bed",
            MoodReason.IdleTooLong => "Bored, no quest",
            MoodReason.Debug => "Debug",
            _ => reason.ToString()
        };

        // 1.2 → 20.
        private static int ExtraPercent(float appeal) => Mathf.RoundToInt((appeal - 1f) * 100f);
    }
}