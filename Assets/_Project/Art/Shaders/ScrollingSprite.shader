// Unlit URP sprite whose texture scrolls in whole texels (pixel-art clouds). Speed in texels/second; needs Wrap Mode U = Repeat.
Shader "Wanderer's Guild/Scrolling Sprite"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}
        _ScrollSpeed ("Scroll Speed (texels per second: X, Y)", Vector) = (3, 0, 0, 0)
        [Toggle] _SnapToTexels ("Snap To Whole Texels", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "False"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_TexelSize;   // NOTE: (1/width, 1/height, width, height) — filled in by Unity.
                float4 _ScrollSpeed;
                float _SnapToTexels;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;        // NOTE: The SpriteRenderer's Color (tint / alpha).
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            // NOTE: Whole-texel steps keep pixel art crisp; frac keeps the offset small (Repeat wraps it anyway), so UVs
            // stay precise however long the menu stays open.
            float2 ScrollOffset()
            {
                float2 texels = _Time.y * _ScrollSpeed.xy;
                texels = lerp(texels, floor(texels), _SnapToTexels);
                return frac(texels * _MainTex_TexelSize.xy);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv + ScrollOffset(); // NOTE: Every tile (and a partial last tile) shifts alike — seamless.
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
            }
            ENDHLSL
        }
    }
}