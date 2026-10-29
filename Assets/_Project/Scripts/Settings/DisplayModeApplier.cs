using System.Threading;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class DisplayModeApplier
    {
        private readonly List<DisplayInfo> _layout = new();
        private int _applyVersion;
        private bool _loggedEditorNote;

        // Monitors currently connected (at least 1).
        public int MonitorCount
        {
            get
            {
                RefreshLayout();
                return Mathf.Max(1, _layout.Count);
            }
        }

        // Applies the mode. A newer call supersedes an older one that is still waiting on a window move.
        public async Awaitable ApplyAsync(ScreenModeSnapshot mode, CancellationToken token)
        {
            var version = ++_applyVersion;

            // NOTE: The Editor's Game view ignores resolution changes and window moves; the prompt flow still works.
            if (Application.isEditor)
            {
                if (!_loggedEditorNote)
                    Debug.Log("[DisplayModeApplier] Resolution, window mode and monitor apply in builds only.");
                _loggedEditorNote = true;
                return;
            }

            RefreshLayout();
            var hasLayout = _layout.Count > 0;
            var target = hasLayout ? _layout[Mathf.Clamp(mode.MonitorIndex, 0, _layout.Count - 1)] : default;
            var size = ResolveSize(mode, target, hasLayout);
            var unityMode = ToUnity(mode.WindowMode);

            if (hasLayout && !Screen.mainWindowDisplayInfo.Equals(target))
            {
                var move = Screen.MoveMainWindowTo(target, WindowPosition(target, size, mode.WindowMode));
                while (move is { isDone: false })
                    await Awaitable.NextFrameAsync(token);

                if (version != _applyVersion)
                    return; // NOTE: A newer request arrived while moving; let it win.
            }

            // NOTE: Skip when nothing changes, to avoid a mode-switch flicker (e.g. re-applying at boot).
            if (Screen.width == size.x && Screen.height == size.y && Screen.fullScreenMode == unityMode) return;

            Screen.SetResolution(size.x, size.y, unityMode);
        }

        private void RefreshLayout()
        {
            _layout.Clear();
            Screen.GetDisplayLayout(_layout);
        }

        private static Vector2Int ResolveSize(ScreenModeSnapshot mode, DisplayInfo target, bool hasLayout)
        {
            if (mode is { Width: > 0, Height: > 0 })
                return new Vector2Int(mode.Width, mode.Height);
            return hasLayout ? new Vector2Int(target.width, target.height) : ResolutionCatalog.Native();
        }

        // NOTE: Windowed mode is centred on the target monitor; fullscreen modes start at its corner.
        private static Vector2Int WindowPosition(DisplayInfo target, Vector2Int size, WindowModeSetting windowMode)
        {
            if (windowMode != WindowModeSetting.Windowed)
                return Vector2Int.zero;
            return new Vector2Int(Mathf.Max(0, (target.width - size.x) / 2), Mathf.Max(0, (target.height - size.y) / 2));
        }

        private static FullScreenMode ToUnity(WindowModeSetting windowMode) => windowMode switch
        {
            WindowModeSetting.Fullscreen => FullScreenMode.ExclusiveFullScreen,
            WindowModeSetting.Borderless => FullScreenMode.FullScreenWindow,
            _ => FullScreenMode.Windowed
        };
    }
}