// Full-screen: pixels within the outline width (in texels) straight beside the mask get the outline colour; the rest is left
// untouched. Width = UI pixels × Render Scale, so the line is one UI pixel thick at any render scale.
Shader "Hidden/WanderersGuild/PixelOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth ("Outline Width (texels)", Float) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Outline"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #define MAX_OUTLINE_TEXELS 12

            half4 _OutlineColor;
            float _OutlineWidth;

            half Mask(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv).r;
            }

            // NOTE: The mask is the camera target's size, so one texel = _ScreenSize.zw (1/width, 1/height). Only straight
            // offsets (a cross, not a square), so convex corners keep the classic notch — the 4-neighbour rule, stretched.
            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 texel = _ScreenSize.zw;

                if (Mask(uv) > 0.5h) return 0; // Inside the object: untouched.

                int width = clamp((int)round(_OutlineWidth), 1, MAX_OUTLINE_TEXELS);
                [loop] for (int i = 1; i <= width; i++)
                {
                    float2 dx = float2(texel.x * i, 0);
                    float2 dy = float2(0, texel.y * i);
                    half neighbour = max(max(Mask(uv + dx), Mask(uv - dx)), max(Mask(uv + dy), Mask(uv - dy)));
                    if (neighbour > 0.5h) return _OutlineColor;
                }
                return 0;
            }
            ENDHLSL
        }
    }
}