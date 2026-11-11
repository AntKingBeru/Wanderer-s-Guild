// Display strings for rooms: card title, state with time left, next-level line, effects line, and the button labels.
using System.Text;

namespace WanderersGuild
{
    public static class FacilityFormatter
    {
        // "Bedroom · Lv 1/3" ("Lv —" while a new room is still being built).
        public static string Title(PlacedFacility room)
        {
            var level = room.Level > 0 ? room.Level.ToString() : "—";
            return $"{room.Data.DisplayName} · Lv {level}/{room.Data.MaxLevel}";
        }

        // "Complete", "Under construction: 2d left" or "Upgrading to Lv 2: 1d left".
        public static string State(PlacedFacility room, long nowMinutes)
        {
            var left = RequestFormatter.TimeLeft(System.Math.Max(0L, room.FinishAtMinutes - nowMinutes));
            return room.State switch
            {
                FacilityState.Building => $"Under construction: {left} left",
                FacilityState.Upgrading => $"Upgrading to Lv {room.TargetLevel}: {left} left",
                _ => "Complete"
            };
        }

        // "Next: Lv 2 · 400g · 3 days · Rank E", "Highest level", or empty while a new room is being built.
        public static string Next(PlacedFacility room)
        {
            if (room.State == FacilityState.Building)
                return string.Empty;
            if (room.TargetLevel >= room.Data.MaxLevel && room.Level >= room.Data.MaxLevel)
                return "Highest level";

            var next = (room.State == FacilityState.Upgrading ? room.TargetLevel : room.Level) + 1;
            if (next > room.Data.MaxLevel)
                return "Highest level";

            var data = room.Data;
            return $"Next: Lv {next} · {data.CostFor(next):N0}g · {Days(data.BuildDaysFor(next))} · Rank {data.UnlockRankFor(next)}";
        }

        // One line per effect: "Capacity +2 · Lv 2: Capacity +4"; "When built: Capacity +2" while being built;
        // empty if the facility has no effects.
        public static string Effects(PlacedFacility room)
        {
            StringBuilder text = new();
            foreach (var effect in room.Data.Effects)
            {
                if (effect is null)
                    continue;
                if (text.Length > 0)
                    text.Append('\n');

                if (room.Level == 0)
                {
                    text.Append("When built: ").Append(effect.Describe(1));
                    continue;
                }

                text.Append(effect.Describe(room.Level));
                var next = room.Level + 1;
                if (next <= room.Data.MaxLevel)
                    text.Append(" · Lv ").Append(next).Append(": ").Append(effect.Describe(next));
            }
            return text.ToString();
        }

        // "Upgrade (400g)", or just "Upgrade" when there's no next level.
        public static string UpgradeLabel(PlacedFacility room)
        {
            var next = room.Level + 1;
            return next <= room.Data.MaxLevel ? $"Upgrade ({room.Data.CostFor(next):N0}g)" : "Upgrade";
        }

        public static string DemolishLabel(FacilityCheck check) => check.Allowed ? $"Demolish (+{check.Gold:N0}g)" : "Demolish";

        public static string Days(int days) => days == 1 ? "1 day" : $"{days} days";
    }
}