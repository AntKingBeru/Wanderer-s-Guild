// The daily schedule (pure): what an avatar should be doing at a minute of the day — sleep, supper, shopping or leisure.
namespace WanderersGuild
{
    public static class AvatarSchedule
    {
        private const int MinutesPerDay = 24 * 60;

        /// <param name="minuteOfDay">0…1439.</param>
        /// <param name="settings"></param>
        /// <param name="hasTavern">A finished tavern exists (otherwise there's no supper to go to).</param>
        /// <param name="hasArmory">A finished armory exists.</param>
        /// <param name="supperHour"></param>
        /// <param name="shoppingHour"></param>
        // NOTE: Sleep wins (it's night); supper beats shopping if the windows ever overlap.
        public static AvatarActivity Desired(int minuteOfDay, AvatarSettings settings, bool hasTavern, bool hasArmory,
            int supperHour, int shoppingHour)
        {
            if (InRange(minuteOfDay, settings.SleepStartHour * 60, settings.SleepEndHour * 60)) return AvatarActivity.Sleep;

            if (hasTavern && InWindow(minuteOfDay, supperHour * 60, settings.SupperLeadMinutes, settings.SupperStayMinutes))
                return AvatarActivity.Supper;

            if (hasArmory && InWindow(minuteOfDay, shoppingHour * 60, settings.ShopLeadMinutes, settings.ShopStayMinutes))
                return AvatarActivity.Shop;

            return AvatarActivity.Leisure;
        }

        private static bool InWindow(int minute, int anchor, int lead, int stay) =>
            InRange(minute, Wrap(anchor - lead), Wrap(anchor + stay));

        // NOTE: [start, end) on a 24h circle — start > end wraps past midnight; start == end means never.
        private static bool InRange(int minute, int start, int end) =>
            start <= end ? minute >= start && minute < end : minute >= start || minute < end;

        private static int Wrap(int minute) => ((minute % MinutesPerDay) + MinutesPerDay) % MinutesPerDay;
        
        // True during sleep hours (outside lodgers are away then).
        public static bool IsSleepTime(int minuteOfDay, AvatarSettings settings) =>
            InRange(minuteOfDay, settings.SleepStartHour * 60, settings.SleepEndHour * 60);
    }
}