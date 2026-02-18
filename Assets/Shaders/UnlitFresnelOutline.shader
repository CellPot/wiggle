Shader "Custom/UnlitFresnelOutline"
{
    Properties
    {
        _MainColor      ("Main Color",    Color)              = (1, 1, 1, 1)
        _OutlineColor   ("Outline Color", Color)              = (0, 0, 0, 1)
        _OutlineWidth   ("Outline Width", Range(0.0, 1.0))   = 0.4
        _OutlineSharpness ("Outline Sharpness", Range(1, 32)) = 8.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Main"

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex   vert
            #pragma fragment frag

            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainColor;
                float4 _OutlineColor;
                float  _OutlineWidth;
                float  _OutlineSharpness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 viewDirWS  : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.viewDirWS  = GetWorldSpaceViewDir(positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float3 N = normalize(input.normalWS);
                float3 V = normalize(input.viewDirWS);

                // Fresnel: 0 at center, 1 at silhouette edge
                float fresnel = 1.0 - saturate(dot(N, V));

                // Remap so _OutlineWidth controls where the edge starts (0=full rim, 1=thin ring)
                // smoothstep gives a crisp band; _OutlineSharpness controls hardness
                float edgeStart = 1.0 - _OutlineWidth;
                float t = saturate((fresnel - edgeStart) / (1.0 - edgeStart + 1e-5));
                float outline = pow(t, 1.0 / max(_OutlineSharpness * 0.1, 0.001));

                half4 color = lerp(_MainColor, _OutlineColor, saturate(outline));
                return color;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
