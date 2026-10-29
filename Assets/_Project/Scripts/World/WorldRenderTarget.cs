// Makes the camera render the world into a small point-filtered render texture (pixel look); releases it on destroy.
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Runs before WorldViewPresenter reads the texture (it uses it in Start).
    [DefaultExecutionOrder(-40)]
    [RequireComponent(typeof(Camera))]
    public class WorldRenderTarget : MonoBehaviour
    {
        private Camera _camera;

        // The world image (GameConfig › World View › Render Size).
        public RenderTexture Texture { get; private set; }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            var size = GameConfig.Instance.WorldView.RenderSize;

            // NOTE: Unity's default camera background has alpha 0. Empty world pixels would then be transparent, and the
            // UI would draw them over an uncleared screen (the "smudge"). Force it opaque.
            var background = _camera.backgroundColor;
            background.a = 1f;
            _camera.backgroundColor = background;

            Texture = new RenderTexture(size.x, size.y, 24, RenderTextureFormat.ARGB32)
            {
                name = "WorldView",
                filterMode = FilterMode.Point,
                useMipMap = false,
                antiAliasing = 1
            };
            Texture.Create();
            _camera.targetTexture = Texture;
        }

        // NOTE: Detach first, so the camera never renders into a released texture.
        private void OnDestroy()
        {
            if (_camera && _camera.targetTexture == Texture)
                _camera.targetTexture = null;

            if (!Texture)
                return;
            Texture.Release();
            Destroy(Texture);
            Texture = null;
        }
    }
}