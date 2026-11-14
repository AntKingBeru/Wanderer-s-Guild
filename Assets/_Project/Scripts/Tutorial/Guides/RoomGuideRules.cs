// When a room guide applies: its room can be built now (rank, unlocked and under its limit, affordable if asked), and the
// first room of its kind (placed or finished).
using System.Linq;

namespace WanderersGuild
{
    public static class RoomGuideRules
    {
        // NOTE: FacilityRules.LockReason covers the facility's own unlock rank and its limit (an office already built → locked).
        public static bool CanBuildNow(RoomGuide guide)
        {
            if (guide.Facility == null || !FacilityManager.HasInstance || !GuildManager.HasInstance)
                return false;

            var guild = GuildManager.Instance;
            var rank = guild.Rank.Rank;
            if (rank < guide.MinRank)
                return false;
            if (FacilityRules.LockReason(guide.Facility, FacilityManager.Instance.Rooms, rank) != null)
                return false;

            return !guide.NeedsGold || guild.Wallet.Gold >= guide.Facility.CostFor(1);
        }

        // The first room of this facility (under construction or finished); null if there's none.
        public static PlacedFacility FirstPlaced(FacilityData facility) =>
            facility == null || !FacilityManager.HasInstance
                ? null
                : FacilityManager.Instance.Rooms.FirstOrDefault(room => room.Data == facility);
    }
}