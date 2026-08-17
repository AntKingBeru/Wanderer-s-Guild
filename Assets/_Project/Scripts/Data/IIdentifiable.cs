// Contract for data assets that can be looked up by a stable string id (matches the source DB key).
namespace WanderersGuild
{
    public interface IIdentifiable
    {
        string Id { get; }
    }
}