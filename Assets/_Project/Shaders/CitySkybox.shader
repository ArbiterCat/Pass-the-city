Shader "SvoiPut/CitySkybox"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.08, 0.10, 0.13, 1)
        _HorizonColor ("Horizon Color", Color) = (0.34, 0.27, 0.23, 1)
        _GroundColor ("Ground Color", Color) = (0.04, 0.045, 0.05, 1)
        _CloudColor ("Cloud Color", Color) = (0.55, 0.52, 0.50, 1)
        _SunColor ("Sun Color", Color) = (1.0, 0.78, 0.55, 1)
        _SunDir ("Sun Direction", Vector) = (0.2, 0.3, 0.9, 0)
        _SunSize ("Sun Size", Range(0.0002, 0.02)) = 0.0025
        _CloudScale ("Cloud Scale", Range(0.5, 8)) = 2.4
        _CloudCover ("Cloud Cover", Range(0, 1)) = 0.52
        _Exposure ("Exposure", Range(0.2, 2)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            fixed4 _TopColor;
            fixed4 _HorizonColor;
            fixed4 _GroundColor;
            fixed4 _CloudColor;
            fixed4 _SunColor;
            float4 _SunDir;
            float _SunSize;
            float _CloudScale;
            float _CloudCover;
            float _Exposure;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float hash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float noise3(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float n000 = hash13(i + float3(0, 0, 0));
                float n100 = hash13(i + float3(1, 0, 0));
                float n010 = hash13(i + float3(0, 1, 0));
                float n110 = hash13(i + float3(1, 1, 0));
                float n001 = hash13(i + float3(0, 0, 1));
                float n101 = hash13(i + float3(1, 0, 1));
                float n011 = hash13(i + float3(0, 1, 1));
                float n111 = hash13(i + float3(1, 1, 1));
                float nx00 = lerp(n000, n100, f.x);
                float nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x);
                float nx11 = lerp(n011, n111, f.x);
                return lerp(lerp(nx00, nx10, f.y), lerp(nx01, nx11, f.y), f.z);
            }

            float fbm(float3 p)
            {
                float sum = 0;
                float amp = 0.5;
                for (int k = 0; k < 5; k++)
                {
                    sum += noise3(p) * amp;
                    p *= 2.07;
                    amp *= 0.5;
                }
                return sum;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 dir = normalize(i.dir);
                float h = dir.y;

                float3 col;
                if (h > 0)
                {
                    col = lerp(_HorizonColor.rgb, _TopColor.rgb, pow(saturate(h), 0.55));
                }
                else
                {
                    col = lerp(_HorizonColor.rgb, _GroundColor.rgb, pow(saturate(-h * 3.5), 0.75));
                }

                float2 plane = dir.xz / max(0.16, abs(dir.y) + 0.28);
                float cloudNoise = fbm(float3(plane * _CloudScale, 2.1));
                float cloud = smoothstep(_CloudCover, _CloudCover + 0.22, cloudNoise);
                cloud *= smoothstep(-0.02, 0.18, h);
                col = lerp(col, _CloudColor.rgb, cloud * 0.72);

                float haze = exp(-abs(h) * 11.0) * 0.18;
                col = lerp(col, _CloudColor.rgb, haze);

                float3 sunDir = normalize(_SunDir.xyz);
                float sunDot = saturate(dot(dir, sunDir));
                float disc = smoothstep(1.0 - _SunSize, 1.0, sunDot);
                float glow = pow(sunDot, 12.0) * 0.28 + pow(sunDot, 3.0) * 0.05;
                col += _SunColor.rgb * (disc * 1.4 + glow);

                col = lerp(col, _CloudColor.rgb, cloud * 0.12);
                return fixed4(col * _Exposure, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
