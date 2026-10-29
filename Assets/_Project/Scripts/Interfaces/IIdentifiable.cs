// Anything with a stable string id — how saves and the request database tool refer to authored assets.
namespace WanderersGuild
{
    public interface IIdentifiable
    {
        // Stable, unique id. Never change it once saves exist that reference it.
        string Id { get; }
    }
}