// A clickable guild object (desk, quest board, lounge…): its name, the screen it opens, its colliders, and its outline (hover
// or the tutorial's pin). Registered in GuildInteractables while enabled.
using UnityEngine;

namespace WanderersGuild
{
    [DisallowMultipleComponent]
    public class GuildInteractable : MonoBehaviour, IWorldInteractable
    {
        [Tooltip("Shown in the label above the object while hovered.")]
        [SerializeField] private string displayName = "Reception Desk";

        [Tooltip("The screen a click opens (also how the tutorial finds this object).")]
        [SerializeField] private MenuPanelId opens = MenuPanelId.ReceptionDesk;

        [Tooltip("Extra height above the object's top for the label anchor (units).")]
        [SerializeField] private float labelHeight = 0.1f;

        private Renderer[] _renderers;
        private uint[] _originalLayers;
        private Collider[] _colliders;
        private bool _isHovered;
        private bool _isPinned;
        private bool _isOutlined;

        public string DisplayName => displayName;
        public MenuPanelId Opens => opens;

        // Every renderer's bounds together (the tutorial frames this on screen).
        public Bounds WorldBounds => CombinedBounds();

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

            _colliders = GetComponentsInChildren<Collider>(true);
            if (_colliders.Length == 0)
                _colliders = new[] { AddFittedBox() };

            var layer = GameConfig.Instance.Interaction.InteractableLayer;
            foreach (var target in _colliders)
                target.gameObject.layer = layer;
        }

        private void OnEnable() => GuildInteractables.Register(this);

        // NOTE: Never leave a model outlined, or the outline passes running, after it's disabled or destroyed.
        private void OnDisable()
        {
            GuildInteractables.Unregister(this);
            _isHovered = false;
            _isPinned = false;
            RefreshOutline();
        }

        // Hover outline (GuildInteractionController).
        public void SetHighlighted(bool highlighted)
        {
            _isHovered = highlighted;
            RefreshOutline();
        }

        // NOTE: The tutorial's outline — it stays while hover comes and goes.
        public void SetPinned(bool pinned)
        {
            _isPinned = pinned;
            RefreshOutline();
        }

        // True if the ray hits one of this object's own colliders (the tutorial's world gate — nothing in front can take it).
        public bool IsHitBy(Ray ray, float maxDistance)
        {
            if (_colliders == null)
                return false;

            foreach (var target in _colliders)
            {
                if (target != null && target.enabled && target.gameObject.activeInHierarchy && target.Raycast(ray, out _, maxDistance))
                    return true;
            }
            return false;
        }

        public void Interact(GamePanelHost panels)
        {
            if (panels != null)
                panels.Open(opens);
        }

        // NOTE: Outlined while hovered OR pinned; the outline bit and the outline-pass count change only when that flips.
        private void RefreshOutline()
        {
            var outlined = _isHovered || _isPinned;
            if (outlined == _isOutlined || _renderers == null)
                return;
            _isOutlined = outlined;

            var bit = GameConfig.Instance.Interaction.OutlineBit;
            for (var i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                    _renderers[i].renderingLayerMask = outlined ? _originalLayers[i] | bit : _originalLayers[i];
            }

            if (outlined)
                PixelOutlineRequests.Add();
            else
                PixelOutlineRequests.Remove();
        }

        private Bounds CombinedBounds()
        {
            Bounds bounds = new(transform.position, Vector3.zero);
            if (_renderers == null)
                return bounds;

            var first = true;
            foreach (var target in _renderers)
            {
                if (target == null)
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