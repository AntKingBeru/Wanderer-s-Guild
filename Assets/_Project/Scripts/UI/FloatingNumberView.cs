// HUD floating numbers: "+60g" / "-1" appear beside a counter, drift down-left and fade; pooled; simultaneous ones stack.
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class FloatingNumberView
    {
        private sealed class Float
        {
            public OutlinedText Text;
            public Tween Tween;
            public float StartedAt;
        }

        private readonly VisualElement _layer;
        private readonly GameObject _linkTarget;
        private readonly List<Float> _pool = new();
        private readonly Dictionary<VisualElement, (float time, int count)> _recent = new();

        private FloatingNumberView(VisualElement layer, GameObject linkTarget, int poolSize)
        {
            _layer = layer;
            _linkTarget = linkTarget;

            for (var i = 0; i < poolSize; i++)
            {
                OutlinedText text = new() { pickingMode = PickingMode.Ignore };
                text.AddToClassList("hud-float");
                text.style.display = DisplayStyle.None;
                _layer.Add(text);
                _pool.Add(new Float { Text = text });
            }
        }

        public static FloatingNumberView Bind(VisualElement root, GameObject linkTarget, int poolSize)
        {
            var layer = root.Q<VisualElement>("hud-floats");
            return layer != null ? new FloatingNumberView(layer, linkTarget, Mathf.Max(1, poolSize)) : null;
        }

        // Starts a float just left of the anchor's window. Skipped while the anchor has no valid bounds yet.
        public void Spawn(string text, Color color, VisualElement anchor, GuildHudSettings settings)
        {
            var bounds = anchor.worldBound;
            var layerWidth = _layer.contentRect.width;
            if (float.IsNaN(bounds.x) || bounds.width <= 0f || float.IsNaN(layerWidth) || layerWidth <= 0f)
                return;

            var topLeft = _layer.WorldToLocal(new Vector2(bounds.xMin, bounds.yMin));
            var row = NextRow(anchor, settings);
            var item = Take();

            item.Text.Text = text;
            item.Text.SetFillColor(color);
            item.Text.style.right = Mathf.Round(layerWidth - topLeft.x + settings.FloatGapPixels);
            item.Text.style.top = Mathf.Round(topLeft.y + row * settings.FloatStackPixels);
            item.Text.style.display = DisplayStyle.Flex;
            item.StartedAt = Time.unscaledTime;

            Apply(item, 0f, settings.FloatDrift);
            item.Tween = DOVirtual.Float(0f, 1f, settings.FloatDuration, t => Apply(item, t, settings.FloatDrift))
                .SetEase(Ease.Linear)
                .OnComplete(() => item.Text.style.display = DisplayStyle.None)
                .SetUpdate(true)
                .SetLink(_linkTarget);
        }

        // NOTE: Changes arriving within the stack window step down one row each (up to N rows, then wrap).
        private int NextRow(VisualElement anchor, GuildHudSettings settings)
        {
            var now = Time.unscaledTime;
            var count = _recent.TryGetValue(anchor, out var last) && now - last.time < settings.FloatStackWindow
                ? last.count + 1
                : 0;

            _recent[anchor] = (now, count);
            return count % Mathf.Max(1, settings.FloatStackRows);
        }

        // NOTE: A free float if there is one; otherwise the oldest running one is recycled.
        private Float Take()
        {
            var oldest = _pool[0];
            foreach (var item in _pool)
            {
                if (!item.Tween.IsActive())
                    return item;
                if (item.StartedAt < oldest.StartedAt)
                    oldest = item;
            }

            oldest.Tween.Kill();
            return oldest;
        }

        // NOTE: Whole-pixel drift down-left; fully visible for the first 60%, then fades out.
        private static void Apply(Float item, float t, Vector2 drift)
        {
            item.Text.style.translate = new Translate(-Mathf.Round(drift.x * t), Mathf.Round(drift.y * t));
            item.Text.style.opacity = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
        }
    }
}