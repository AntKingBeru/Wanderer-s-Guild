// A waiting applicant: the adventurer they'd become (not in the roster) and when they leave if nobody accepts them.
namespace WanderersGuild
{
    public sealed class Applicant
    {
        public Applicant(Adventurer adventurer, long expiresAtMinutes)
        {
            Adventurer = adventurer;
            ExpiresAtMinutes = expiresAtMinutes;
        }

        public Adventurer Adventurer { get; }
        public long ExpiresAtMinutes { get; }
        public int Id => Adventurer.Id;

        public SavedApplicant ToSaved() => new(Adventurer.ToSaved(), ExpiresAtMinutes);
    }
}