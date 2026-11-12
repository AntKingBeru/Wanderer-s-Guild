// Speech rules: how important each topic is, which lines wait for the speaker to come home, and what chatter turns into.
namespace WanderersGuild
{
    public static class SpeechRules
    {
        // NOTE: Higher shows first and survives the queue cap. 0 = chatter (only joins an empty queue).
        public static int PriorityOf(SpeechTopic topic) => topic switch
        {
            SpeechTopic.Injured => 4,
            SpeechTopic.Departing or SpeechTopic.QuestSucceeded or SpeechTopic.QuestFailed => 3,
            SpeechTopic.LeveledUp or SpeechTopic.CameBack => 2,
            SpeechTopic.WellFed => 1,
            _ => 0
        };

        public static bool IsChatter(SpeechTopic topic) =>
            topic is SpeechTopic.Chatter or SpeechTopic.MealChatter or SpeechTopic.ShopChatter or SpeechTopic.RestChatter
                or SpeechTopic.Sleeping;

        // NOTE: Said on the way in — these happen while the speaker is still away, so they wait for the avatar to appear.
        public static bool WaitsForArrival(SpeechTopic topic) =>
            topic is SpeechTopic.QuestSucceeded or SpeechTopic.QuestFailed or SpeechTopic.CameBack or SpeechTopic.LeveledUp
                or SpeechTopic.Injured;

        // Only departures are said while walking out.
        public static bool AllowedWhileLeaving(SpeechTopic topic) => topic == SpeechTopic.Departing;

        // Plain chatter becomes the current activity's own kind.
        public static SpeechTopic ChatterFor(AvatarActivity activity) => activity switch
        {
            AvatarActivity.Supper => SpeechTopic.MealChatter,
            AvatarActivity.Shop => SpeechTopic.ShopChatter,
            AvatarActivity.Rest => SpeechTopic.RestChatter,
            AvatarActivity.Sleep => SpeechTopic.Sleeping,
            _ => SpeechTopic.Chatter
        };
    }
}