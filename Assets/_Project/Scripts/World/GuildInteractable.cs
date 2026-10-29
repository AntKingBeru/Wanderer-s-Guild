// A clickable guild object (desk, quest board, lounge): its name, the screen it opens, its colliders, and its hover outline.
using UnityEngine;

namespace WanderersGuild
{
    [DisallowMultipleComponent]
    public class GuildInteractable : MonoBehaviour
    {
        [Tooltip("Shown in the label above the object while hovered.")]
        [SerializeField] private string displayName = "Reception Desk";

        [Tooltip("The screen a click opens.")]
        [SerializeField] private MenuPanelId opens = MenuPanelId.ReceptionDesk;

        [Tooltip("Extra height above the object's top for the label anchor (units).")]
        [SerializeField] private float labelHeight = 0.1f;

        private Renderer[] _renderers;
        private uint[] _originalLayers;
        private bool _isHighlighted;

        public string DisplayName => displayName;
        public MenuPanelId Opens => opens;

        // World point the name label sits above: the top-center of the combined renderer bounds.
        public Vector3 LabelAnchor
        {
            get
            {
                var bounds = CombinedBounds();
                return new Vector3(bounds.center.x, bounds.max.y + labelHeight, bounds.center.z);
            }
        }

        // NOTE: Colliders go on the Interactable layer; a model without one gets a box fitted to its renderers
        // (approximate for rotated children — add your own collider where it matters).
        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _originalLayers = new uint[_renderers.Length];
            for (var i = 0; i < _renderers.Length; i++)
                _originalLayers[i] = _renderers[i].renderingLayerMask;

            var colliders = GetComponentsInChildren<Collider>(true);
            if (colliders.Length == 0)
                colliders = new[] { AddFittedBox() };

            var layer = GameConfig.Instance.Interaction.InteractableLayer;
            foreach (var collider in colliders)
                collider.gameObject.layer = layer;
        }

        // Turns the outline bit on or off on every renderer (originals restored exactly).
        public void SetHighlighted(bool highlighted)
        {
            if (_isHighlighted == highlighted)
                return;
            _isHighlighted = highlighted;

            var bit = GameConfig.Instance.Interaction.OutlineBit;
            for (var i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i])
                    _renderers[i].renderingLayerMask = highlighted ? _originalLayers[i] | bit : _originalLayers[i];
            }

            if (highlighted) PixelOutlineRequests.Add();
            else PixelOutlineRequests.Remove();
        }

        // NOTE: Never leave a model outlined, or the outline passes running, after it's disabled or destroyed.
        private void OnDisable() => SetHighlighted(false);

        private Bounds CombinedBounds()
        {
            Bounds bounds = new(transform.position, Vector3.zero);
            var first = true;
            foreach (var target in _renderers)
            {
                if (!target)
                    continue;
                if (first)
                {
                    bounds = target.bounds;
                    first = false;
                }
                else
                    bounds.Encapsulate(target.bounds);
            }
            return bounds;
        }

        private Collider AddFittedBox()
        {
            var bounds = CombinedBounds();
            var box = gameObject.AddComponent<BoxCollider>();
            var scale = transform.lossyScale;

            box.center = transform.InverseTransformPoint(bounds.center);
            box.size = new Vector3(bounds.size.x / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
                                   bounds.size.y / Mathf.Max(0.0001f, Mathf.Abs(scale.y)),
                                   bounds.size.z / Mathf.Max(0.0001f, Mathf.Abs(scale.z)));
            return box;
        }
    }
}