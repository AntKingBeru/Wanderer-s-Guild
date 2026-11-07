// Makes an avatar hoverable and clickable: name label, silhouette outline (hover or selected by a card), and a click asks
// for its floating card.
using UnityEngine;

namespace WanderersGuild
{
    [DisallowMultipleComponent]
    public class AvatarInteractable : MonoBehaviour, IWorldInteractable
    {
        private const float LabelGap = 0.1f;

        private Renderer _renderer;
        private uint _originalLayers;
        private bool _isHovered;
        private bool _isSelected;
        private bool _isOutlined;
        private int _adventurerId;
        private string _name;

        public string DisplayName => _name;

        public Vector3 LabelAnchor
        {
            get
            {
                if (!_renderer)
                    return transform.position;
                var bounds = _renderer.bounds;
                return new Vector3(bounds.center.x, bounds.max.y + LabelGap, bounds.center.z);
            }
        }

        public void Initialize(int adventurerId, string displayName, Renderer spriteRenderer)
        {
            _adventurerId = adventurerId;
            _name = displayName;
            _renderer = spriteRenderer;
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

            if (_renderer)
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