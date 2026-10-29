// A system that writes its state into the game save (Memento-style snapshot, captured all in the same frame).
namespace WanderersGuild
{
    public interface ISaveContributor
    {
        // Copies this system's current state into the save data. Must not keep a reference to it.
        void WriteTo(GameSaveData data);
    }
}