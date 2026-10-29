// Continue: resumes the most recent save. Unavailable until the save system exists, so it shows greyed out.
namespace WanderersGuild
{
    public sealed class ContinueCommand : IMenuCommand
    {
        public bool IsAvailable => SaveSlotManager.HasInstance && SaveSlotManager.Instance.TryGetMostRecent(out _);
        public bool CanExecute => GameSessionLauncher.CanLaunch;

        public void Execute()
        {
            if (SaveSlotManager.Instance.TryGetMostRecent(out var slot))
                GameSessionLauncher.LoadSlot(slot);
        }
    }
}