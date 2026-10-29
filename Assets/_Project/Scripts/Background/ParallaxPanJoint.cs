// Keeps two parallax layers from vertically stacked groups joined during the camera pan (e.g. a title roof on a menu building).
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Runs before ParallaxLayer.LateUpdate, so a recalculated factor applies on the same frame.
    [DefaultExecutionOrder(-10)]
    public class ParallaxPanJoint : MonoBehaviour
    {
        [Tooltip("The layer in the upper group (e.g. the title's roof layer).")]
        [SerializeField] private ParallaxLayer upperLayer;

        [Tooltip("Pixel row of the upper layer's sprite where it meets the lower one (e.g. the roof's bottom edge), " +
                 "counted from the sprite's bottom (0).")]
        [SerializeField] private float upperJoinPixelY;

        [Tooltip("The layer in the lower group (e.g. the menu's building layer).")]
        [SerializeField] private ParallaxLayer lowerLayer;

        [Tooltip("Pixel row of the lower layer's sprite where it meets the upper one (e.g. the building's top edge), " +
                 "counted from the sprite's bottom (0).")]
        [SerializeField] private float lowerJoinPixelY;

        private SpriteRenderer _upperRenderer;
        private SpriteRenderer _lowerRenderer;
        private float _lastUpperScale = -1f;
        private float _lastLowerScale = -1f;
        private bool _isValid;
        private bool _warnedRange;

        // NOTE: Start, not Awake — every layer has captured its rest position and group by then.
        private void Start() => _isValid = Validate();

        private void OnDisable()
        {
            if (upperLayer)
                upperLayer.ClearPanFactorOverride();
            if (lowerLayer)
                lowerLayer.ClearPanFactorOverride();
            _lastUpperScale = -1f;
            _lastLowerScale = -1f;
        }

        // NOTE: The fitter rescales layers when the view changes; the join rows move with the scale, so recalculate then.
        private void LateUpdate()
        {
            if (!_isValid)
                return;

            var upperScale = upperLayer.transform.lossyScale.y;
            var lowerScale = lowerLayer.transform.lossyScale.y;
            if (Mathf.Approximately(upperScale, _lastUpperScale) && Mathf.Approximately(lowerScale, _lastLowerScale))
                return;

            _lastUpperScale = upperScale;
            _lastLowerScale = lowerScale;
            Recalculate();
        }

        // NOTE: Both layers get the same factor = (rest gap between the join rows + overlap) ÷ distance between the groups,
        // so their relative position never changes while the camera pans.
        private void Recalculate()
        {
            var travel = upperLayer.Group.PanOrigin.y - lowerLayer.Group.PanOrigin.y;

            var upperPixel = upperLayer.transform.lossyScale.y / _upperRenderer.sprite.pixelsPerUnit;
            var overlap = GameConfig.Instance.Parallax.PanJointOverlapPixels * upperPixel;
            var gap = JoinRestY(upperLayer, _upperRenderer, upperJoinPixelY) - JoinRestY(lowerLayer, _lowerRenderer, lowerJoinPixelY);

            var factor = (gap + overlap) / travel;
            if (factor is <= 0f or >= 1f && !_warnedRange)
            {
                Debug.LogWarning($"[ParallaxPanJoint] '{name}': the layers need a pan factor of {factor:0.###} " +
                                 $"(gap {gap:0.##}, travel {travel:0.##}). Between 0 and 1 is expected — check the join rows " +
                                 "and the distance between the groups.", this);
                _warnedRange = true;
            }

            upperLayer.SetPanFactorOverride(factor);
            lowerLayer.SetPanFactorOverride(factor);
        }

        // NOTE: World Y of a sprite pixel row with no parallax applied (rest position, current fitted scale).
        private static float JoinRestY(ParallaxLayer layer, SpriteRenderer renderer, float pixelY)
        {
            var parent = layer.transform.parent;
            var restWorld = parent ? parent.TransformPoint(layer.RestLocalPosition) : layer.RestLocalPosition;

            var sprite = renderer.sprite;
            return restWorld.y + (pixelY - sprite.pivot.y) / sprite.pixelsPerUnit * layer.transform.lossyScale.y;
        }

        private bool Validate()
        {
            if (!upperLayer || !lowerLayer ||
                !upperLayer.TryGetComponent(out _upperRenderer) || !_upperRenderer.sprite ||
                !lowerLayer.TryGetComponent(out _lowerRenderer) || !_lowerRenderer.sprite)
            {
                Debug.LogError($"[ParallaxPanJoint] '{name}' needs two ParallaxLayers, each with a SpriteRenderer and sprite.", this);
                return false;
            }

            if (!upperLayer.Group || !lowerLayer.Group || upperLayer.Group == lowerLayer.Group)
            {
                Debug.LogError($"[ParallaxPanJoint] '{name}': the two layers must belong to two different ParallaxGroups.", this);
                return false;
            }

            if (upperLayer.Group.PanOrigin.y - lowerLayer.Group.PanOrigin.y <= 0.001f)
            {
                Debug.LogWarning($"[ParallaxPanJoint] '{name}': the upper layer's group must sit above the lower one's " +
                                 "(vertical camera pan). Joint disabled.", this);
                return false;
            }

            // NOTE: Pointer parallax moves layers by depth — different depths would still pull the joined pair apart.
            if (!Mathf.Approximately(upperLayer.Depth, lowerLayer.Depth))
                Debug.LogWarning($"[ParallaxPanJoint] '{name}': give both layers the same depth " +
                                 $"({upperLayer.Depth:0.##} vs {lowerLayer.Depth:0.##}) so the pointer moves them together.", this);

            return true;
        }
    }
}