using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    [RequireComponent(typeof(ParallaxGroup))]
    public class ParallaxBackgroundFitter : MonoBehaviour
    {
        private readonly List<FitEntry> _entries = new();
        private ParallaxGroup _group;
        private float _lastAspect = -1f;
        private float _lastOrthographicSize = -1f;
        private bool _warnedPerspective;
        private bool _warnedSpacing;

        private void Awake()
        {
            _group = GetComponent<ParallaxGroup>();
            if (!_group)
            {
                Debug.LogError($"[ParallaxBackgroundFitter] '{name}' needs a ParallaxGroup on the same object.", this);
                enabled = false;
                return;
            }

            // NOTE: Parallax offsets and fitted sizes assume an unscaled group root.
            if (transform.lossyScale != Vector3.one)
                Debug.LogWarning($"[ParallaxBackgroundFitter] '{name}' must stay at scale 1; the fitter scales the layers.", this);

            CollectLayers();
        }

        private void Update()
        {
            var camera = _group.TargetCamera;
            if (!camera)
                return;

            if (Mathf.Approximately(camera.aspect, _lastAspect) && Mathf.Approximately(camera.orthographicSize, _lastOrthographicSize))
                return;

            _lastAspect = camera.aspect;
            _lastOrthographicSize = camera.orthographicSize;
            Fit(camera);
        }
        
        [ContextMenu("Refit Now (Play Mode)")]
        private void RefitNow()
        {
            if (Application.isPlaying && _group && _group.TargetCamera)
                Fit(_group.TargetCamera);
        }

        private void CollectLayers()
        {
            foreach (var layer in GetComponentsInChildren<ParallaxLayer>(true))
            {
                if (!layer.TryGetComponent(out SpriteRenderer spriteRenderer) || !spriteRenderer.sprite)
                {
                    Debug.LogWarning($"[ParallaxBackgroundFitter] '{layer.name}' needs a SpriteRenderer with a sprite on the same object.", layer);
                    continue;
                }

                _entries.Add(new FitEntry(layer, spriteRenderer));
            }
        }
        
        private void Fit(Camera camera)
        {
            if (!camera.orthographic)
            {
                if (!_warnedPerspective)
                    Debug.LogWarning("[ParallaxBackgroundFitter] Requires an orthographic camera; skipping.", this);
                _warnedPerspective = true;
                return;
            }

            var settings = GameConfig.Instance.Parallax;
            var viewHeight = camera.orthographicSize * 2f;
            var viewWidth = viewHeight * camera.aspect;
            var bridgeDistance = _group.BridgeDistance;

            WarnIfTooClose(bridgeDistance, viewWidth, settings);

            // NOTE: The shared scale is the largest any layer needs, so pixel size stays uniform across the group.
            var sharedScale = _entries.Aggregate(0f, (current, entry) => Mathf.Max(current, entry.RequiredScale(viewWidth, viewHeight, settings.MaxShift)));

            foreach (var entry in _entries)
            {
                var scale = settings.UniformLayerScale
                    ? sharedScale
                    : entry.RequiredScale(viewWidth, viewHeight, settings.MaxShift);

                entry.Apply(scale, viewWidth, bridgeDistance, settings);
            }
        }
        
        // NOTE: Below this distance the neighbor background is visible from this group's rest view.
        private void WarnIfTooClose(float bridgeDistance, float viewWidth, ParallaxSettings settings)
        {
            if (_warnedSpacing || bridgeDistance <= 0f)
                return;

            var minimumDistance = (viewWidth + 2f * settings.MaxShift.x) / (1f - settings.PanStrength);
            if (bridgeDistance >= minimumDistance)
                return;

            Debug.LogWarning($"[ParallaxBackgroundFitter] '{name}' is {bridgeDistance:0.0} units from its neighbour; " +
                             $"at this aspect it needs at least {minimumDistance:0.0}.", this);
            _warnedSpacing = true;
        }
        
        private sealed class FitEntry
        {
            private readonly ParallaxLayer _layer;
            private readonly SpriteRenderer _renderer;
            private readonly Vector2 _tileSize;

            public FitEntry(ParallaxLayer layer, SpriteRenderer renderer)
            {
                _layer = layer;
                _renderer = renderer;

                var sprite = renderer.sprite;
                _tileSize = sprite.rect.size / sprite.pixelsPerUnit;
            }

            private bool IsTiled => _renderer.drawMode == SpriteDrawMode.Tiled;

            // NOTE: Height always decides the scale; width is solved by widening (tiled) or a bigger scale (simple).
            public float RequiredScale(float viewWidth, float viewHeight, Vector2 maxShift)
            {
                var heightScale = (viewHeight + 2f * maxShift.y * _layer.Depth) / _tileSize.y;
                if (IsTiled)
                    return heightScale;

                var widthScale = CoverWidth(viewWidth, maxShift) / _tileSize.x;
                return Mathf.Max(heightScale, widthScale);
            }

            public void Apply(float scale, float viewWidth, float bridgeDistance, ParallaxSettings settings)
            {
                _renderer.transform.localScale = new Vector3(scale, scale, 1f);
                if (!IsTiled)
                    return;

                // NOTE: Exact width, rendered as partial tiles by Tiled/Continuous, so the two groups meet mid-pan
                // with no gap or overlap, and never intrude into each other's rest view.
                var bridgeWidth = bridgeDistance * (1f - settings.PanStrength * _layer.Depth);
                var worldWidth = Mathf.Max(CoverWidth(viewWidth, settings.MaxShift), bridgeWidth);
                _renderer.size = new Vector2(worldWidth / scale, _tileSize.y);
            }

            private float CoverWidth(float viewWidth, Vector2 maxShift) => viewWidth + 2f * maxShift.x * _layer.Depth;
        }
    }
}