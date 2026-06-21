Shader "GalTemplate/CharacterBillboard"
{
    Properties
    {
        _MainTex ("Character Texture", 2D) = "white" {}
        _Opacity ("Opacity", Range(0, 1)) = 0.92
        _WhiteCutoff ("White Key Cutoff", Range(0, 1)) = 0.93
        _WhiteSoftness ("White Key Softness", Range(0.001, 0.2)) = 0.055
        _BlackCutoff ("Black Key Cutoff", Range(0, 0.3)) = 0.035
        _BlackSoftness ("Black Key Softness", Range(0.001, 0.2)) = 0.035
        _MoodTint ("Mood Tint", Color) = (0.72, 0.58, 1, 1)
        _MoodBlend ("Mood Blend", Range(0, 1)) = 0.14
        _MoodCounter ("Mood Counter", Range(0, 1)) = 0.34
        _EdgeDarkness ("Edge Darkness", Range(0, 1)) = 0.18
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.12
        _PixelSize ("Character Pixel Size", Range(1, 24)) = 1
        _PixelRefinement ("Character Pixel Refinement", Range(1, 4)) = 2
        _ZTest ("ZTest", Float) = 4
    }

    SubShader
    {
        Tags
        {
            "Queue" = "AlphaTest+40"
            "RenderType" = "TransparentCutout"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Cull Off
            Lighting Off
            ZWrite On
            ZTest [_ZTest]
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float _Opacity;
            float _WhiteCutoff;
            float _WhiteSoftness;
            float _BlackCutoff;
            float _BlackSoftness;
            float4 _MoodTint;
            float _MoodBlend;
            float _MoodCounter;
            float _EdgeDarkness;
            float _RimStrength;
            float _PixelSize;
            float _PixelRefinement;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.pos = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 uv = input.uv;
                float effectivePixelSize = max(1.0, _PixelSize / max(1.0, _PixelRefinement));
                if (effectivePixelSize > 1.01)
                {
                    float2 pixelGrid = max(float2(1.0, 1.0), _MainTex_TexelSize.zw / effectivePixelSize);
                    uv = (floor(uv * pixelGrid) + 0.5) / pixelGrid;
                }

                fixed4 source = tex2D(_MainTex, uv);
                float maxChannel = max(source.r, max(source.g, source.b));
                float minChannel = min(source.r, min(source.g, source.b));
                float saturation = maxChannel - minChannel;
                float luminance = dot(source.rgb, float3(0.299, 0.587, 0.114));

                float whiteKey = smoothstep(_WhiteCutoff, _WhiteCutoff + _WhiteSoftness, minChannel);
                whiteKey *= 1.0 - smoothstep(0.045, 0.2, saturation);

                float blackKey = 1.0 - smoothstep(_BlackCutoff, _BlackCutoff + _BlackSoftness, maxChannel);
                blackKey *= 1.0 - smoothstep(0.02, 0.14, saturation);

                float alpha = source.a * _Opacity * (1.0 - saturate(whiteKey + blackKey));
                clip(alpha - 0.01);

                float2 centeredUv = abs(input.uv - 0.5) * 2.0;
                float edge = smoothstep(0.28, 1.0, max(centeredUv.x, centeredUv.y));
                float verticalShade = smoothstep(0.15, 1.0, 1.0 - input.uv.y);

                float3 color = source.rgb;
                float3 counterColor = color * float3(0.92, 0.99, 1.08);
                color = lerp(color, counterColor, _MoodCounter);

                float3 moodColor = color * lerp(float3(1.0, 1.0, 1.0), _MoodTint.rgb, 0.3) + _MoodTint.rgb * 0.08;
                color = lerp(color, moodColor, _MoodBlend);
                color = lerp(color, color * 0.72 + _MoodTint.rgb * 0.08, edge * _EdgeDarkness);
                color += _MoodTint.rgb * _RimStrength * edge * smoothstep(0.32, 0.86, luminance);
                color = lerp(color, color * 0.88, verticalShade * 0.08);

                return fixed4(saturate(color), alpha);
            }
            ENDCG
        }
    }
}
