// Shows the world render texture full-canvas under the HUD, keeps the texture sized to the canvas, maps screen ↔ view pixels
// and tells whether the pointer is over any UI.
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    [RequireComponent(typeof(UIDocument))]
    public class WorldViewPresenter : MonoBehaviour
    {
        private const string LiveClass = "world-view--live";

        [SerializeField] private WorldRenderTarget target;

        private VisualElement _view;

        // This document's root (the hover label lives here, above the world image).
        public VisualElement DocumentRoot { get; private set; }

        // The live world-texture size in pixels — use this, not the config's reference size.
        public Vector2Int RenderSize => target ? target.Size : GameConfig.Instance.WorldView.RenderSize;
        
        // World-texture pixels per UI pixel (GameConfig › World View › Render Scale). Multiply offsets that are meant in
        // UI pixels by this before adding them to view pixels.
        public int PixelScale => GameConfig.Instance.WorldView.RenderScale;

        private void OnEnable()
        {
            DocumentRoot = GetComponent<UIDocument>().rootVisualElement;
            _view = DocumentRoot?.Q<VisualElement>("world-view");
            if (_view == null)
            {
                Debug.LogError("[WorldViewPresenter] Missing 'world-view' in the UXML.", this);
                return;
            }

            _view.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            GameEventsRelay.OnWorldViewResized.AddListener(OnResized);
        }

        private void OnDisable()
        {
            _view?.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            GameEventsRelay.OnWorldViewResized.RemoveListener(OnResized);
        }

        // NOTE: The opaque background is added here (runtime only). In Edit mode the element stays transparent, so the
        // Game view shows the camera directly.
        private void Start()
        {
            if (_view == null || target?.Texture is null)
            {
                Debug.LogError("[WorldViewPresenter] No world render target to show.", this);
                return;
            }

            _view.AddToClassList(LiveClass);
            ShowTexture();
            MatchCanvas();
        }

        // NOTE: Only the texture's contents change each frame, which doesn't dirty the element — repaint it explicitly.
        private void LateUpdate() => _view?.MarkDirtyRepaint();

        // Screen position (Input System: y up from the bottom) → world-view pixel (y down from the top).
        // False outside the view (letterbox) or before layout.
        public bool TryScreenToView(Vector2 screenPosition, out Vector2 viewPixel)
        {
            viewPixel = default;
            if (!TryPanelPoint(screenPosition, out var panelPoint))
                return false;

            var local = _view.WorldToLocal(panelPoint);
            var rect = _view.contentRect;
            if (float.IsNaN(rect.width) || rect.width <= 0f || rect.height <= 0f)
                return false;

            var size = RenderSize;
            viewPixel = new Vector2(local.x / rect.width * size.x, local.y / rect.height * size.y);
            return viewPixel is { x: >= 0f, y: >= 0f } && viewPixel.x <= size.x && viewPixel.y <= size.y;
        }

        // World-view pixel → this element's local space (for placing elements over the world image).
        public Vector2 ViewToLocal(Vector2 viewPixel)
        {
            var rect = _view.contentRect;
            var size = RenderSize;
            return new Vector2(viewPixel.x / size.x * rect.width, viewPixel.y / size.y * rect.height);
        }

        // True if any pickable UI element is under the pointer. Every document shares this panel, so one pick covers
        // the HUD and all panels.
        public bool IsPointerOverUI(Vector2 screenPosition) =>
            TryPanelPoint(screenPosition, out var panelPoint) && _view.panel.Pick(panelPoint) != null;

        private void OnGeometryChanged(GeometryChangedEvent _) => MatchCanvas();

        // NOTE: Runtime only (Edit mode shows the camera directly). Render Scale texture pixels per UI pixel; unchanged sizes are
        // skipped by the target.
        private void MatchCanvas()
        {
            if (!Application.isPlaying || target is null || _view == null)
                return;

            var rect = _view.contentRect;
            if (float.IsNaN(rect.width) || rect.width <= 0f || rect.height <= 0f)
                return;

            var scale = PixelScale;
            target.Resize(new Vector2Int(Mathf.RoundToInt(rect.width) * scale, Mathf.RoundToInt(rect.height) * scale));
        }

        private void OnResized(Vector2Int _) => ShowTexture();

        private void ShowTexture()
        {
            if (_view != null && target?.Texture is not null)
                _view.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(target.Texture));
        }

        // NOTE: Input System screen positions have y up from the bottom; panel coordinates have y down from the top.
        private bool TryPanelPoint(Vector2 screenPosition, out Vector2 panelPoint)
        {
            panelPoint = default;
            if (_view?.panel == null)
                return false;

            panelPoint = RuntimePanelUtils.ScreenToPanel(_view.panel, new Vector2(screenPosition.x, Screen.height - screenPosition.y));
            return true;
        }
    }
}