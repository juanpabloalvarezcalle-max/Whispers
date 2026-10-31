Shader "Whispers/PickupGlow"
{
    Properties
    {
        _Tint ("Color y pulso", Color) = (1, 0.7, 0.3, 0.3)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "PickupGlow"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Bordes difusos, haz más ancho junto al objeto y más fino hacia arriba.
                float taper = lerp(0.48, 0.16, input.uv.y);
                float edge = 1.0 - smoothstep(taper * 0.62, taper, abs(input.uv.x * 2.0 - 1.0));
                float rise = smoothstep(0.0, 0.08, input.uv.y);
                float fadeTop = 1.0 - smoothstep(0.54, 1.0, input.uv.y);
                float alpha = edge * rise * fadeTop;
                return half4(_Tint.rgb, _Tint.a * alpha);
            }
            ENDHLSL
        }
    }
}
