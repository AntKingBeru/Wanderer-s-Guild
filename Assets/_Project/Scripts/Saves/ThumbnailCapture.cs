// Renders a camera (no UI — UI Toolkit doesn't draw through it) into a small target and box-downsamples it to a thumbnail.
using UnityEngine;
using UnityEngine.Rendering;

namespace WanderersGuild
{
    public static class ThumbnailCapture
    {
        // The caller owns the returned texture and must Destroy it.
        public static Texture2D Capture(Camera camera, Vector2Int renderSize, Vector2Int thumbnailSize)
        {
            var width = Mathf.Max(1, renderSize.x);
            var height = Mathf.Max(1, renderSize.y);

            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            target.filterMode = FilterMode.Point;
            var previousActive = RenderTexture.active;
            Texture2D full = null;

            try
            {
                Render(camera, target);

                RenderTexture.active = target;
                full = new Texture2D(width, height, TextureFormat.RGBA32, false);
                full.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                full.Apply(false);

                return Downsample(full, thumbnailSize);
            }
            finally
            {
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
                if (full)
                    Object.Destroy(full);
            }
        }

        // NOTE: The camera's aspect is forced to the target's for the render (else a 21:9 screen squeezes into 16:9).
        // URP renders on demand through a render request; Camera.Render is the fallback.
        private static void Render(Camera camera, RenderTexture target)
        {
            var previousAspect = camera.aspect;
            camera.aspect = (float)target.width / target.height;

            try
            {
                RenderPipeline.StandardRequest request = new() { destination = target };
                if (RenderPipeline.SupportsRenderRequest(camera, request))
                {
                    RenderPipeline.SubmitRenderRequest(camera, request);
                    return;
                }

                var previousTarget = camera.targetTexture;
                camera.targetTexture = target;
                camera.Render();
                camera.targetTexture = previousTarget;
            }
            finally
            {
                camera.aspect = previousAspect;
                camera.ResetAspect(); // Back to following the screen.
            }
        }

        // NOTE: Each thumbnail pixel is the average of the source block it covers (5×5 for 320×180 → 64×36).
        private static Texture2D Downsample(Texture2D source, Vector2Int size)
        {
            var targetWidth = Mathf.Max(1, size.x);
            var targetHeight = Mathf.Max(1, size.y);
            var input = source.GetPixels32();
            var output = new Color32[targetWidth * targetHeight];

            for (var y = 0; y < targetHeight; y++)
            {
                var y0 = y * source.height / targetHeight;
                var y1 = Mathf.Max(y0 + 1, (y + 1) * source.height / targetHeight);

                for (var x = 0; x < targetWidth; x++)
                {
                    var x0 = x * source.width / targetWidth;
                    var x1 = Mathf.Max(x0 + 1, (x + 1) * source.width / targetWidth);

                    int r = 0, g = 0, b = 0, count = 0;
                    for (var sy = y0; sy < y1; sy++)
                    {
                        for (var sx = x0; sx < x1; sx++)
                        {
                            var pixel = input[sy * source.width + sx];
                            r += pixel.r;
                            g += pixel.g;
                            b += pixel.b;
                            count++;
                        }
                    }

                    output[y * targetWidth + x] = new Color32((byte)(r / count), (byte)(g / count), (byte)(b / count), 255);
                }
            }

            Texture2D thumbnail = new(targetWidth, targetHeight, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            thumbnail.SetPixels32(output);
            thumbnail.Apply(false);
            return thumbnail;
        }
    }
}