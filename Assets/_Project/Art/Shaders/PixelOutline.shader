// Full-screen: pixels just outside the mask (4-neighbour) get the outline colour; everything else is left untouched.
Shader "Hidden/WanderersGuild/PixelOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
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

            half4 _OutlineColor;

            half Mask(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv).r;
            }

            // NOTE: The mask is the camera target's size (320×180), so one texel = _ScreenSize.zw (1/width, 1/height).
            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 texel = _ScreenSize.zw;

                if (Mask(uv) > 0.5h) return 0; // Inside the object: untouched.

                half neighbour = max(max(Mask(uv + float2(texel.x, 0)), Mask(uv - float2(texel.x, 0))),
                                     max(Mask(uv + float2(0, texel.y)), Mask(uv - float2(0, texel.y))));
                return neighbour > 0.5h ? _OutlineColor : 0;
            }
            ENDHLSL
        }
    }
}