Shader "Hidden/GalTemplate/DialogueFocus"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Intensity ("Intensity", Range(0, 1)) = 0
        _BlurSize ("Blur Size", Range(0, 8)) = 3
        _Darken ("Darken", Range(0, 1)) = 0.18
        _Tint ("Tint", Color) = (0.09, 0.055, 0.14, 0.18)
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
            float4 _MainTex_TexelSize;
            float _Intensity;
            float _BlurSize;
            float _Darken;
            float4 _Tint;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 stepUv = _MainTex_TexelSize.xy * _BlurSize;
                fixed4 original = tex2D(_MainTex, i.uv);
                fixed4 blur = original * 0.24;
                blur += tex2D(_MainTex, i.uv + float2(stepUv.x, 0)) * 0.12;
                blur += tex2D(_MainTex, i.uv - float2(stepUv.x, 0)) * 0.12;
                blur += tex2D(_MainTex, i.uv + float2(0, stepUv.y)) * 0.12;
                blur += tex2D(_MainTex, i.uv - float2(0, stepUv.y)) * 0.12;
                blur += tex2D(_MainTex, i.uv + stepUv) * 0.07;
                blur += tex2D(_MainTex, i.uv - stepUv) * 0.07;
                blur += tex2D(_MainTex, i.uv + float2(stepUv.x, -stepUv.y)) * 0.07;
                blur += tex2D(_MainTex, i.uv + float2(-stepUv.x, stepUv.y)) * 0.07;

                float intensity = saturate(_Intensity);
                fixed4 color = lerp(original, blur, intensity);
                float luminance = dot(color.rgb, float3(0.299, 0.587, 0.114));
                color.rgb = lerp(float3(luminance, luminance, luminance), color.rgb, lerp(1.0, 0.76, intensity));
                color.rgb = lerp(color.rgb, _Tint.rgb, _Tint.a * intensity);
                color.rgb *= 1.0 - _Darken * intensity;
                return color;
            }
            ENDCG
        }
    }
}
