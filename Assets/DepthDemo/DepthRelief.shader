Shader "DepthAnything/Sample/Relief"
{
    Properties
    {
        _MainTex ("Color", 2D) = "white" {}
        _DepthTex ("Normalized depth and mask", 2D) = "white" {}
        _Strength ("Relief", Float) = .8
        _Aspect ("Aspect", Float) = 1.5
        _CutEdges ("Cut depth discontinuities", Float) = 1
    }
    SubShader
    {
        Cull Off ZTest LEqual ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex, _DepthTex;
            float _Strength, _Aspect, _CutEdges;
            struct Varyings { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert(appdata_base v)
            {
                Varyings o;
                float depth = tex2Dlod(_DepthTex, float4(v.texcoord.xy, 0, 0)).r;
                v.vertex.x *= _Aspect;
                v.vertex.z -= (depth - .5) * _Strength;
                o.position = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord.xy;
                return o;
            }
            float4 frag(Varyings i) : SV_Target
            {
                float2 d = tex2D(_DepthTex, i.uv).rg;
                clip(d.g - .35);
                // Hide stretched connections at sharp depth discontinuities.
                // Full-frame relief can contain large foreground/background jumps.
                // The mask-aware path already fills its outline: preserve interior body pixels.
                if (_CutEdges > .5) clip(.12 - abs(ddx(d.r)) - abs(ddy(d.r)));
                return float4(tex2D(_MainTex, i.uv).rgb, 1);
            }
            ENDCG
        }
    }
}
