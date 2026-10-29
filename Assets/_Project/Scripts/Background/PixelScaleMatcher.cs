// Keeps a sprite rig at the same pixel size as a reference background layer and places it by background pixel coordinates.
using UnityEngine;

namespace WanderersGuild
{
    [RequireComponent(typeof(ParallaxGroup))]
    public class PixelScaleMatcher : MonoBehaviour
    {
        [Tooltip("A layer of the background this rig stands in. Its scale (set by ParallaxBackgroundFitter) is copied.")]
        [SerializeField] private SpriteRenderer reference;

        [Tooltip("The background group the pixel position is measured from (its position = the centre of its frame).")]
        [SerializeField] private Transform backgroundOrigin;

        [Tooltip("Where the rig's sprite pivot sits, in background pixels from the frame's bottom-left, e.g. (210, 0) of 320×180.")]
        [SerializeField] private Vector2 pixelPosition;

        private ParallaxLayer[] _layers;
        private float _lastScale = -1f;

        private void Awake()
        {
            _layers = GetComponentsInChildren<ParallaxLayer>(true);

            if (!reference || !reference.sprite || !backgroundOrigin)
            {
                Debug.LogError($"[PixelScaleMatcher] '{name}' needs a reference SpriteRenderer (with a sprite) and a background origin.", this);
                enabled = false;
            }
        }

        // NOTE: The fitter rescales the background in Update when the view changes; this follows in LateUpdate.
        private void LateUpdate()
        {
            var scale = reference.transform.lossyScale.x;
            if (Mathf.Approximately(scale, _lastScale))
                return;

            _lastScale = scale;
            Apply(scale);
        }

        // NOTE: The layers are scaled, never this root — pointer parallax offsets are written in the root's local units.
        private void Apply(float scale)
        {
            foreach (var layer in _layers)
                layer.transform.localScale = new Vector3(scale, scale, 1f);

            var frame = reference.sprite;
            var unitsPerPixel = scale / frame.pixelsPerUnit;
            var fromCentre = pixelPosition - frame.rect.size * 0.5f;

            var position = backgroundOrigin.position + new Vector3(fromCentre.x * unitsPerPixel, fromCentre.y * unitsPerPixel, 0f);
            position.z = transform.position.z;
            transform.position = position;
        }

        [ContextMenu("Reapply (Play Mode)")]
        private void Reapply()
        {
            _lastScale = -1f;
        }
    }
}