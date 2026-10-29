// URP renderer feature: draws a mask of hovered objects (one rendering-layer bit) and outlines it 1 render-pixel thick.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace WanderersGuild
{
    public class PixelOutlineFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            [Tooltip("Unlit material that writes white (shader Hidden/WanderersGuild/OutlineMask).")]
            public Material maskMaterial;

            [Tooltip("Full-screen outline material (shader Hidden/WanderersGuild/PixelOutline).")]
            public Material outlineMaterial;

            [Tooltip("Rendering-layer bit marking hovered objects. MUST match GameConfig › Interaction › Outline Rendering Layer.")]
            [Range(0, 31)] public int renderingLayer = 7;

            public Color color = Color.white;
            public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingTransparents;
        }

        public Settings settings = new();

        private MaskPass _maskPass;
        private CompositePass _compositePass;

        public override void Create()
        {
            _maskPass = new MaskPass { renderPassEvent = settings.passEvent };
            _compositePass = new CompositePass { renderPassEvent = settings.passEvent };
        }

        // NOTE: Only game cameras, only when something is hovered, only with both materials assigned.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.Game)
                return;
            if (PixelOutlineRequests.Count == 0 || !settings.maskMaterial || !settings.outlineMaterial)
                return;

            settings.outlineMaterial.SetColor(OutlineColorId, settings.color);
            _maskPass.Setup(settings.maskMaterial, 1u << settings.renderingLayer);
            _compositePass.Setup(settings.outlineMaterial);

            renderer.EnqueuePass(_maskPass);
            renderer.EnqueuePass(_compositePass);
        }

        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");

        // Hands the mask from the mask pass to the composite pass within one frame.
        private class OutlineMaskData : ContextItem
        {
            public TextureHandle Mask;
            public override void Reset() => Mask = TextureHandle.nullHandle;
        }

        // NOTE: Renders the marked objects in white into an R8 mask the size of the camera target, without depth testing
        // (the outline shows around hidden parts too — an x-ray silhouette).
        private class MaskPass : ScriptableRenderPass
        {
            private static readonly List<ShaderTagId> ShaderTags = new()
            {
                new ShaderTagId("UniversalForward"), new ShaderTagId("UniversalForwardOnly"), new ShaderTagId("SRPDefaultUnlit")
            };

            private Material _material;
            private uint _layerMask;

            private class PassData
            {
                public RendererListHandle List;
            }

            public void Setup(Material material, uint layerMask)
            {
                _material = material;
                _layerMask = layerMask;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var cameraData = frameData.Get<UniversalCameraData>();
                var renderingData = frameData.Get<UniversalRenderingData>();
                var lightData = frameData.Get<UniversalLightData>();

                var descriptor = cameraData.cameraTargetDescriptor;
                descriptor.depthStencilFormat = GraphicsFormat.None;
                descriptor.msaaSamples = 1;
                descriptor.graphicsFormat = GraphicsFormat.R8_UNorm;
                var mask = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, "_PixelOutlineMask", true);

                var drawing = RenderingUtils.CreateDrawingSettings(ShaderTags, renderingData, cameraData, lightData, SortingCriteria.CommonOpaque);
                drawing.overrideMaterial = _material;
                drawing.overrideMaterialPassIndex = 0;
                FilteringSettings filtering = new(RenderQueueRange.all, ~0, _layerMask);

                using var builder = renderGraph.AddRasterRenderPass("Pixel Outline Mask", out PassData data);
                data.List = renderGraph.CreateRendererList(new RendererListParams(renderingData.cullResults, drawing, filtering));
                builder.UseRendererList(data.List);
                builder.SetRenderAttachment(mask, 0);
                builder.SetRenderFunc((PassData passData, RasterGraphContext context) => context.cmd.DrawRendererList(passData.List));

                frameData.GetOrCreate<OutlineMaskData>().Mask = mask;
            }
        }

        // NOTE: Full-screen pass over the camera color: a pixel just outside the mask (4-neighbor) becomes the outline.
        private class CompositePass : ScriptableRenderPass
        {
            private Material _material;

            private class PassData
            {
                public TextureHandle Mask;
                public Material Material;
            }

            public void Setup(Material material) => _material = material;

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (!frameData.Contains<OutlineMaskData>())
                    return;

                var resources = frameData.Get<UniversalResourceData>();
                var mask = frameData.Get<OutlineMaskData>().Mask;
                if (!mask.IsValid())
                    return;

                using var builder = renderGraph.AddRasterRenderPass("Pixel Outline Composite", out PassData data);
                data.Mask = mask;
                data.Material = _material;
                builder.UseTexture(mask);
                builder.SetRenderAttachment(resources.activeColorTexture, 0);
                builder.SetRenderFunc((PassData passData, RasterGraphContext context) =>
                    Blitter.BlitTexture(context.cmd, passData.Mask, new Vector4(1f, 1f, 0f, 0f), passData.Material, 0));
            }
        }
    }
}