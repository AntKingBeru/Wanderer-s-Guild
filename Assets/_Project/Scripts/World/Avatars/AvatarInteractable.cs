// Makes an avatar hoverable and clickable: name label, silhouette outline, and a click asks for its floating card.
using UnityEngine;

namespace WanderersGuild
{
    [DisallowMultipleComponent]
    public class AvatarInteractable : MonoBehaviour, IWorldInteractable
    {
        private const float LabelGap = 0.1f;

        private Renderer _renderer;
        private uint _originalLayers;
        private bool _isHighlighted;
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

        // Turns the outline bit on/off on the sprite (the mask clips it to the sprite's silhouette).
        public void SetHighlighted(bool highlighted)
        {
            if (_isHighlighted == highlighted)
                return;
            _isHighlighted = highlighted;

            if (_renderer)
            {
                var bit = GameConfig.Instance.Interaction.OutlineBit;
                _renderer.renderingLayerMask = highlighted ? _originalLayers | bit : _originalLayers;
            }

            if (highlighted)
                PixelOutlineRequests.Add();
            else
                PixelOutlineRequests.Remove();
        }

        public void Interact(GamePanelHost panels) => GameEventsRelay.OnAdventurerCardRequested.Invoke(_adventurerId);

        // NOTE: Never leave the outline passes running after the avatar is despawned.
        private void OnDisable() => SetHighlighted(false);
    }
}