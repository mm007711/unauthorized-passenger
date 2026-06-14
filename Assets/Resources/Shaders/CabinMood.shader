Shader "Hidden/GalTemplate/CabinMood"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Intensity ("Intensity", Range(0, 1)) = 0.9
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Intensity;

            float Gaussian(float value, float center, float width)
            {
                float normalized = (value - center) / max(width, 0.001);
                return exp(-(normalized * normalized));
            }

            fixed4 frag(v2f_img input) : SV_Target
            {
                float2 uv = input.uv;
                fixed4 source = tex2D(_MainTex, uv);
                float strength = saturate(_Intensity);

                float aisle = Gaussian(uv.x, 0.5, 0.32) * Gaussian(uv.y, 0.48, 0.58);
                float frontGlow = Gaussian(uv.x, 0.52, 0.2) * Gaussian(uv.y, 0.76, 0.22);
                float windowCool = smoothstep(0.52, 1.0, uv.x) * (1.0 - smoothstep(0.08, 0.72, uv.y));

                float3 pink = float3(0.96, 0.26, 0.88);
                float3 violet = float3(0.46, 0.32, 1.0);
                float3 cool = float3(0.14, 0.32, 0.78);
                float3 tint = lerp(pink, violet, saturate(frontGlow * 0.8 + windowCool * 0.35));
                tint = lerp(tint, cool, windowCool * 0.35);

                float tintAmount = (0.1 + aisle * 0.34 + frontGlow * 0.28 + windowCool * 0.18) * strength;
                float3 graded = lerp(source.rgb, source.rgb * float3(0.86, 0.72, 1.02) + tint * 0.34, saturate(tintAmount));

                float sideEdge = 1.0 - smoothstep(0.0, 0.34, min(uv.x, 1.0 - uv.x));
                float topEdge = 1.0 - smoothstep(0.0, 0.22, 1.0 - uv.y);
                float bottomEdge = 1.0 - smoothstep(0.0, 0.2, uv.y);
                float vignette = saturate(sideEdge * 0.75 + topEdge * 0.7 + bottomEdge * 0.45);
                graded = lerp(graded, graded * float3(0.2, 0.18, 0.34), vignette * 0.55 * strength);

                float aisleBloom = aisle * Gaussian(uv.y, 0.55, 0.42) * strength;
                graded += tint * aisleBloom * 0.18;

                return fixed4(saturate(graded), source.a);
            }
            ENDCG
        }
    }
}
