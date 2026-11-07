// Full-screen grade driven by RevealGrade.cs. Lives in Resources so builds include it even though no material references it.
// Pass 0 composites the look; pass 1 is the box filter used to build the blurred copy in _BlurTex.
Shader "Hidden/MysticCircus/RevealGrade"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _BlurTex ("Blurred source", 2D) = "black" {}
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

            sampler2D _MainTex, _BlurTex;
            float4 _MainTex_TexelSize;
            float _Exposure, _Saturation, _Soft, _Grain, _Aberration, _Vignette, _Lift;
            float4 _Tint;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                float2 centre = uv - 0.5;

                float3 colour;
                colour.r = tex2D(_MainTex, uv + centre * _Aberration).r;
                colour.g = tex2D(_MainTex, uv).g;
                colour.b = tex2D(_MainTex, uv - centre * _Aberration).b;

                float2 blurUv = uv;
                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0) blurUv.y = 1 - blurUv.y;
                #endif
                float3 blur = tex2D(_BlurTex, blurUv).rgb;
                // Glow: bright parts bleed outward, then a little overall softness.
                colour = max(colour, lerp(colour, blur * 1.15, _Soft));
                colour = lerp(colour, blur, _Soft * 0.35);

                colour *= _Exposure * _Tint.rgb;
                float luma = dot(colour, float3(0.2126, 0.7152, 0.0722));
                colour = lerp(luma.xxx, colour, _Saturation);

                // Lift, vignette and grain are judged by eye, so they work on display values:
                // in linear light the same amount of grain would swamp the shadows.
                #ifndef UNITY_COLORSPACE_GAMMA
                colour = LinearToGammaSpace(saturate(colour));
                #endif
                colour = colour * (1 - _Lift) + _Lift;
                colour *= saturate(1 - _Vignette * dot(centre, centre) * 2.4);
                float noise = frac(sin(dot(uv * _ScreenParams.xy + _Time.y * 61.0, float2(12.9898, 78.233))) * 43758.5453) - 0.5;
                colour = saturate(colour + noise * _Grain);
                #ifndef UNITY_COLORSPACE_GAMMA
                colour = GammaToLinearSpace(colour);
                #endif
                return fixed4(colour, 1);
            }
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 d = _MainTex_TexelSize.xy;
                float3 c = tex2D(_MainTex, i.uv + d * float2(-1, -1)).rgb;
                c += tex2D(_MainTex, i.uv + d * float2(1, -1)).rgb;
                c += tex2D(_MainTex, i.uv + d * float2(-1, 1)).rgb;
                c += tex2D(_MainTex, i.uv + d * float2(1, 1)).rgb;
                return fixed4(c * 0.25, 1);
            }
            ENDCG
        }
    }
}
