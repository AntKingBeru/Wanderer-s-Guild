namespace WanderersGuild
{
    public interface IMenuPanelHost
    {
        // True while a panel or the menu is mid-transition. Open/Close requests are ignored meanwhile.
        bool IsBusy { get; }

        bool HasOpenPanel { get; }

        bool HasPanel(MenuPanelId id);

        void Open(MenuPanelId id);

        void Close(MenuPanelId id);
    }
}