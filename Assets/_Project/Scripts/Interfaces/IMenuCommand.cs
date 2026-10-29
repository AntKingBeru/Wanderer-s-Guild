// Contract for a main-menu action (Command pattern): whether it's offered, whether it can run right now, and what it does.
namespace WanderersGuild
{
    public interface IMenuCommand
    {
        //False shows the button greyed out (e.g. Continue with no saves). Evaluated when the menu appears.
        bool IsAvailable { get; }

        //Checked on every activation (e.g. false while a scene transition is running).
        bool CanExecute { get; }

        void Execute();
    }
}