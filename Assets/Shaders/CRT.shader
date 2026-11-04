Shader "Pinball/CRT"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Curvature ("Curvature", Range(0, 0.5)) = 0.03
        _Scanline ("Scanline", Range(0, 1)) = 0.06
        _Vignette ("Vignette", Range(0, 2)) = 0.2
        _Aberration ("Aberration", Range(0, 5)) = 0.1
        _Noise ("Noise", Range(0, 1)) = 0.01
        _Brightness ("Brightness", Range(0.5, 2)) = 1.0
        _Flash ("Flash", Range(0, 1)) = 0
        _NoiseSeed ("NoiseSeed", Float) = 0
        _Dim ("Dim", Range(0, 1)) = 0
        _Saturation ("Saturation", Range(0, 2)) = 1.1
    }
    SubShader
    {
        Tags { "Queue" = "Overlay" "RenderType" = "Opaque" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float _Curvature, _Scanline, _Vignette, _Aberration, _Noise, _Brightness, _Flash, _NoiseSeed, _Dim, _Saturation;
            float4 _Texel;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color;
                return o;
            }

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 c = uv * 2.0 - 1.0;
                c += c * (c.yx * c.yx) * _Curvature;
                uv = c * 0.5 + 0.5;
                float inside = step(0.0, uv.x) * step(uv.x, 1.0) * step(0.0, uv.y) * step(uv.y, 1.0);

                float2 px = _Texel.xy;
                float2 dir = (uv - 0.5) * 2.0;

                float3 col;
                col.r = tex2D(_MainTex, uv + dir * px * _Aberration).r;
                col.g = tex2D(_MainTex, uv).g;
                col.b = tex2D(_MainTex, uv - dir * px * _Aberration).b;

                float3 glow = 0;
                glow += tex2D(_MainTex, uv + float2(px.x * 1.6, 0)).rgb;
                glow += tex2D(_MainTex, uv - float2(px.x * 1.6, 0)).rgb;
                glow += tex2D(_MainTex, uv + float2(0, px.y * 1.6)).rgb;
                glow += tex2D(_MainTex, uv - float2(0, px.y * 1.6)).rgb;
                col += glow * 0.02;

                // CRT scanlines on output pixels so they stay crisp at any resolution
                float row = frac(i.uv.y * _ScreenParams.y * 0.5);
                float scanMask = smoothstep(0.0, 0.55, row);
                col *= 1.0 - _Scanline * (1.0 - scanMask);

                // vignette
                float vig = 1.0 - _Vignette * pow(saturate(length(c * 0.95)), 2.6);
                col *= saturate(vig);

                // grain - sampled at source texel centers so it never aliases into blobs
                float2 gp = floor(uv * _Texel.zw);
                float n = hash(gp + frac(_NoiseSeed) * 61.0) - 0.5;
                col += n * _Noise;

                // slight tube tint
                col = lerp(col, col * float3(1.02, 1.0, 0.97), 0.6);

                float lum = dot(col, float3(0.299, 0.587, 0.114));
                col = lerp(float3(lum, lum, lum), col, _Saturation);

                col *= _Brightness * inside * (1.0 - _Dim);
                col = lerp(col, float3(0.98, 0.95, 0.86), _Flash);
                col *= i.color.rgb;

                return fixed4(saturate(col), 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
