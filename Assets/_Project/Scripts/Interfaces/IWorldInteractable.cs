// Something in the world that can be hovered (outline + name label) and clicked (Strategy: each kind decides what a click does).
using UnityEngine;

namespace WanderersGuild
{
    public interface IWorldInteractable
    {
        string DisplayName { get; }

        // World point the name label sits above.
        Vector3 LabelAnchor { get; }

        void SetHighlighted(bool highlighted);

        // The click: open a panel, show a card…
        void Interact(GamePanelHost panels);
    }
}