Shader "Custom/GroundGrid"
{
    Properties
    {
        _BaseColor   ("Base Color",       Color) = (0.07, 0.09, 0.13, 1)
        _TintColor   ("Patch Tint",       Color) = (0.10, 0.13, 0.19, 1)
        _LineColor   ("Grid Line Color",  Color) = (0.55, 0.70, 1.00, 1)
        _CellSize    ("Cell Size",        Float) = 2
        _MajorEvery  ("Major Line Every", Float) = 5
        _LineWidth   ("Line Width",       Range(0.005, 0.1)) = 0.02
        _LineAlpha   ("Minor Line Alpha", Range(0, 1)) = 0.05
        _MajorAlpha  ("Major Line Alpha", Range(0, 1)) = 0.12
        _PatchScale  ("Patch Scale",      Float) = 0.04
    }

    SubShader
    {
        Tags
        {
            "Queue"          = "Geometry"
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        ZWrite Off
        Cull Off

        Pass
        {
            Name "Main"

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _TintColor;
                float4 _LineColor;
                float  _CellSize;
                float  _MajorEvery;
                float  _LineWidth;
                float  _LineAlpha;
                float  _MajorAlpha;
                float  _PatchScale;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 worldXY    : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS);
                o.worldXY    = TransformObjectToWorld(input.positionOS).xy;
                return o;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Anti-aliased line mask: 1 on the line, 0 away from it, in screen-stable width.
            float gridLine(float2 coord, float width)
            {
                float2 d  = abs(frac(coord - 0.5) - 0.5);
                float2 fw = fwidth(coord);
                float2 w  = max(width, fw);
                float2 m  = 1.0 - smoothstep(w, w + fw, d);
                return max(m.x, m.y);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 p = i.worldXY;

                // Soft large-scale patches so the floor is not one flat color
                float n = valueNoise(p * _PatchScale) * 0.65 + valueNoise(p * _PatchScale * 2.3 + 17.0) * 0.35;
                float3 col = lerp(_BaseColor.rgb, _TintColor.rgb, smoothstep(0.25, 0.75, n));

                // Minor + major grid
                float2 cell  = p / _CellSize;
                float minor  = gridLine(cell, _LineWidth);
                float major  = gridLine(cell / _MajorEvery, _LineWidth / _MajorEvery);

                col = lerp(col, _LineColor.rgb, minor * _LineAlpha);
                col = lerp(col, _LineColor.rgb, major * _MajorAlpha);

                // Dot at every minor intersection
                float2 g   = (frac(cell + 0.5) - 0.5) * _CellSize;
                float  dot_ = 1.0 - smoothstep(0.06, 0.10, length(g));
                col = lerp(col, _LineColor.rgb, dot_ * _MajorAlpha);

                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
