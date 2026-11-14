// A tutorial target in the world — the guild object that opens a given screen (desk, quest board, lounge): outlined and framed.
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    // NOTE: The object is looked up on every use (it's part of the spawned Guild Hall, which can be respawned).
    public sealed class TutorialWorldTarget : ITutorialTarget
    {
        private readonly TutorialWorldProjector _projector;
        private GuildInteractable _pinned;
        private bool _isEmphasised;

        public TutorialWorldTarget(MenuPanelId opens, TutorialWorldProjector projector)
        {
            Opens = opens;
            _projector = projector;
        }

        // The screen the object opens — how it's found.
        public MenuPanelId Opens { get; }

        // The object right now (null if none exists yet).
        public GuildInteractable Interactable => GuildInteractables.Find(Opens);

        public bool IsAvailable => Interactable != null;

        public VisualElement Element => null;

        public bool TryGetRect(out Rect panelRect)
        {
            panelRect = default;
            var interactable = Interactable;
            SyncPin(interactable);
            return interactable != null && _projector != null && _projector.TryProject(interactable.WorldBounds, out panelRect);
        }

        public void SetEmphasis(bool on)
        {
            _isEmphasised = on;
            SyncPin(on ? Interactable : null);
        }

        // NOTE: Keeps the outline on whichever object is current — a new one gets it, the old one lets go (a destroyed one fails
        // the Unity null check and is just forgotten). Runs every frame through TryGetRect while the step is up.
        private void SyncPin(GuildInteractable current)
        {
            var wanted = _isEmphasised ? current : null;
            if (wanted == _pinned)
                return;

            if (_pinned != null)
                _pinned.SetPinned(false);

            _pinned = wanted;
            if (_pinned != null)
                _pinned.SetPinned(true);
        }
    }
}