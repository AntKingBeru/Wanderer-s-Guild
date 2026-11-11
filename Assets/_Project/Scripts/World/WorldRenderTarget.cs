// Makes the camera render the world into a small point-filtered render texture (pixel look), resized to match the canvas.
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Runs before WorldViewPresenter reads the texture. Created at the reference size in Awake; the presenter resizes it
    // to the canvas once layout is known (and whenever the canvas changes).
    [DefaultExecutionOrder(-40)]
    [RequireComponent(typeof(Camera))]
    public class WorldRenderTarget : MonoBehaviour
    {
        private const int MinSize = 16;

        private Camera _camera;

        // The world image.
        public RenderTexture Texture { get; private set; }

        // The live texture size in pixels (= the canvas size in UI pixels once laid out).
        public Vector2Int Size => Texture ? new Vector2Int(Texture.width, Texture.height) : GameConfig.Instance.WorldView.RenderSize;

        private void Awake()
        {
            _camera = GetComponent<Camera>();

            // NOTE: Unity's default camera background has alpha 0. Empty world pixels would then be transparent, and the
            // UI would draw them over an uncleared screen (the "smudge"). Force it opaque.
            var background = _camera.backgroundColor;
            background.a = 1f;
            _camera.backgroundColor = background;

            Texture = CreateTexture(GameConfig.Instance.WorldView.RenderSize);
            _camera.targetTexture = Texture;
        }

        // Recreates the texture at a new size (no-op if unchanged) and raises OnWorldViewResized.
        // NOTE: Order matters — new texture, camera retargeted, listeners (the UI) swap to it, and only then the old one is
        // released, so nothing ever renders into or samples a freed texture.
        public void Resize(Vector2Int size)
        {
            size = new Vector2Int(Mathf.Max(MinSize, size.x), Mathf.Max(MinSize, size.y));
            if (Texture is not null && Texture.width == size.x && Texture.height == size.y)
                return;

            var old = Texture;
            Texture = CreateTexture(size);
            _camera.targetTexture = Texture;
            _camera.ResetAspect();

            GameEventsRelay.OnWorldViewResized.Invoke(size);
            Release(old);
        }

        // NOTE: Detach first, so the camera never renders into a released texture.
        private void OnDestroy()
        {
            if (_camera is not null && _camera.targetTexture == Texture)
                _camera.targetTexture = null;

            Release(Texture);
            Texture = null;
        }

        private static RenderTexture CreateTexture(Vector2Int size)
        {
            RenderTexture texture = new(size.x, size.y, 24, RenderTextureFormat.ARGB32)
            {
                name = $"WorldView {size.x}x{size.y}",
                filterMode = FilterMode.Point,
                useMipMap = false,
                antiAliasing = 1
            };
            texture.Create();
            return texture;
        }

        private static void Release(RenderTexture texture)
        {
            if (texture is null)
                return;
            texture.Release();
            Destroy(texture);
        }
    }
}