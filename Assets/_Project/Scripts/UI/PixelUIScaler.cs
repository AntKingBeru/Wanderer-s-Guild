using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public class PixelUIScaler : Singleton<PixelUIScaler>
    {
        // NOTE: PanelSettings are assets. Originals are restored on destroy so Play Mode never dirties them.
        private readonly Dictionary<PanelSettings, float> _originalScales = new();
        private int _lastWidth = -1;
        private int _lastHeight = -1;
        private int _scaleCap;

        // Current integer UI scale (1 UI pixel = this many screen pixels).
        public int CurrentScale { get; private set; } = 1;

        // The largest scale that fits the current screen; the upper bound for the UI scale override.
        public int MaxScaleForScreen => CalculateScale(Screen.width, Screen.height, GameConfig.Instance.UIScale.ReferenceResolution);

        protected override void OnSingletonAwake()
        {
            foreach (var panel in GameConfig.Instance.UIScale.PixelPanels)
            {
                if (!panel || _originalScales.ContainsKey(panel))
                    continue;

                if (panel.scaleMode != PanelScaleMode.ConstantPixelSize)
                    Debug.LogWarning($"[PixelUIScaler] '{panel.name}' should use Scale Mode: Constant Pixel Size.", panel);

                _originalScales.Add(panel, panel.scale);
            }

            Apply();
        }

        // 0 = automatic; otherwise the maximum scale to use (still never above what fits).
        public void SetScaleCap(int cap)
        {
            _scaleCap = Mathf.Max(0, cap);
            Apply();
        }

        // NOTE: Unity has no resize event; polls and only reapplies when the screen size changes.
        private void Update()
        {
            if (Screen.width == _lastWidth && Screen.height == _lastHeight)
                return;
            Apply();
        }

        protected override void OnDestroy()
        {
            foreach (var pair in _originalScales.Where(pair => pair.Key))
            {
                pair.Key.scale = pair.Value;
            }

            base.OnDestroy();
        }

        private void Apply()
        {
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;

            var automatic = CalculateScale(_lastWidth, _lastHeight, GameConfig.Instance.UIScale.ReferenceResolution);
            CurrentScale = _scaleCap > 0 ? Mathf.Min(_scaleCap, automatic) : automatic;

            foreach (var panel in _originalScales.Keys)
                panel.scale = CurrentScale;
        }

        // Largest whole number N where the base resolution × N fits the screen; never below 1.
        public static int CalculateScale(int screenWidth, int screenHeight, Vector2Int reference)
        {
            var horizontal = screenWidth / Mathf.Max(1, reference.x);
            var vertical = screenHeight / Mathf.Max(1, reference.y);
            return Mathf.Max(1, Mathf.Min(horizontal, vertical));
        }
    }
}