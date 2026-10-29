// A request currently on the board: its template, when it was issued and expires (total in-game minutes), and if it's been viewed.
namespace WanderersGuild
{
    public sealed class ActiveRequest
    {
        public ActiveRequest(int instanceId, RequestData data, long issuedAtMinutes, long expiresAtMinutes, bool isSeen = false)
        {
            InstanceId = instanceId;
            Data = data;
            IssuedAtMinutes = issuedAtMinutes;
            ExpiresAtMinutes = expiresAtMinutes;
            IsSeen = isSeen;
        }

        // Unique per guild (saved); identifies this posting even if the same template returns later.
        public int InstanceId { get; }
        public RequestData Data { get; }
        public long IssuedAtMinutes { get; }
        public long ExpiresAtMinutes { get; }

        // False until the player first opens its details (the board shows NEW until then).
        public bool IsSeen { get; private set; }

        public bool IsExpiredAt(long nowMinutes) => nowMinutes >= ExpiresAtMinutes;
        public long MinutesLeft(long nowMinutes) => System.Math.Max(0L, ExpiresAtMinutes - nowMinutes);

        // True if this call changed it.
        public bool MarkSeen()
        {
            if (IsSeen)
                return false;
            IsSeen = true;
            return true;
        }

        public SavedRequest ToSaved() => new(InstanceId, Data.Id, IssuedAtMinutes, ExpiresAtMinutes, IsSeen);
    }
}