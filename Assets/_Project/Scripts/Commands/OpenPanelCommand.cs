using System;

namespace WanderersGuild
{
    public sealed class OpenPanelCommand : IMenuCommand
    {
        private readonly IMenuPanelHost _host;
        private readonly MenuPanelId _panel;
        private readonly Func<bool> _isAvailable;

        /// <param name="host"></param>
        /// <param name="panel"></param>
        /// <param name="isAvailable">Extra availability rule (e.g. Load Game needs a save). Null = always.</param>
        public OpenPanelCommand(IMenuPanelHost host, MenuPanelId panel, Func<bool> isAvailable = null)
        {
            _host = host;
            _panel = panel;
            _isAvailable = isAvailable;
        }

        public bool IsAvailable => _host != null && _host.HasPanel(_panel) && (_isAvailable == null || _isAvailable());
        public bool CanExecute => _host is { IsBusy: false, HasOpenPanel: false };

        public void Execute() => _host.Open(_panel);
    }
}