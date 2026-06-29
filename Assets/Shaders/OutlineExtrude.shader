Shader "Wanderer/OutlineExtrude"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0.40, 0.80, 1.0, 1.0)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.03
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+1" }

        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct attributes
            {
                float4 position_os : POSITION;
                float3 normal_os : NORMAL;
            };
            struct varyings
            {
                float4 position_hcs : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 outline_color;
                float outline_width;
            CBUFFER_END

            varyings vert (attributes IN)
            {
                varyings OUT;
                float scale = length(float3(GetObjectToWorldMatrix()[0].xyz));
                float width = scale > 0.0001 ? outline_width / scale : outline_width;
                float3 pos_os = IN.position_os.xyz + normalize(IN.normal_os) * width;
                OUT.position_hcs = TransformObjectToHClip(pos_os);
                return OUT;
            }

            half4 frag (varyings IN) : SV_Target
            {
                return outline_color;
            }
            ENDHLSL
        }
    }
}