Shader "SvoiPut/SmokeParticles"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (0.34, 0.32, 0.30, 1)
        _Softness ("Softness", Range(0.5, 6)) = 3.2
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Tint;
            float _Softness;

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                fixed4 tint = _Tint * i.color;

                // Мягкий круг: плотное ядро, растворяющийся край.
                float2 centered = i.uv - 0.5;
                float distanceFromCenter = length(centered) * 2.0;
                float soft = saturate(1.0 - distanceFromCenter);
                float shape = pow(soft, _Softness);

                fixed3 color = tint.rgb;
                fixed alpha = tint.a * shape * tex.a;
                return fixed4(color, alpha);
            }
            ENDCG
        }
    }

    FallBack Off
}
