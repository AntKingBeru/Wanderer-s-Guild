// Makes an avatar hoverable and clickable: name label (above the figure's head), silhouette outline (hover or selected by a
// card), and a click asks for its floating card.
using UnityEngine;

namespace WanderersGuild
{
    [DisallowMultipleComponent]
    public class AvatarInteractable : MonoBehaviour, IWorldInteractable
    {
        private const float LabelGap = 0.1f;

        private Renderer _renderer;
        private AvatarSpriteView _view;
        private uint _originalLayers;
        private bool _isHovered;
        private bool _isSelected;
        private bool _isOutlined;
        private int _adventurerId;
        private string _name;

        public string DisplayName => _name;
        
        // True while the pointer is over it (the name label shows above the head — speech bubbles hide meanwhile).
        public bool IsHovered => _isHovered;

        // NOTE: The figure's head, not the quad's top — short species (dwarves) have empty headroom in their frame.
        public Vector3 LabelAnchor => _view != null ? _view.HeadTop + Vector3.up * LabelGap : transform.position;

        public void Initialize(int adventurerId, string displayName, Renderer spriteRenderer, AvatarSpriteView view)
        {
            _adventurerId = adventurerId;
            _name = displayName;
            _renderer = spriteRenderer;
            _view = view;
            _originalLayers = spriteRenderer.renderingLayerMask;
        }

        // Hover outline (the interaction controller).
        public void SetHighlighted(bool highlighted)
        {
            _isHovered = highlighted;
            ApplyOutline();
        }

        // Selected by a card (e.g. a room card outlining its residents) — independent of hover.
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            ApplyOutline();
        }

        public void Interact(GamePanelHost panels) => GameEventsRelay.OnAdventurerCardRequested.Invoke(_adventurerId);

        // NOTE: One outline (and one outline-pass request) whether hovered, selected or both.
        private void ApplyOutline()
        {
            var outlined = _isHovered || _isSelected;
            if (outlined == _isOutlined)
                return;
            _isOutlined = outlined;

            if (_renderer != null)
            {
                var bit = GameConfig.Instance.Interaction.OutlineBit;
                _renderer.renderingLayerMask = outlined ? _originalLayers | bit : _originalLayers;
            }

            if (outlined)
                PixelOutlineRequests.Add();
            else
                PixelOutlineRequests.Remove();
        }

        // NOTE: Never leave the outline passes running after the avatar is despawned.
        private void OnDisable()
        {
            _isHovered = false;
            _isSelected = false;
            ApplyOutline();
        }
    }
}