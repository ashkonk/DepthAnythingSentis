Shader "Hidden/DepthAnything/Post"
{
    Properties { _MainTex ("Depth", 2D) = "white" {} }
    SubShader
    {
        ZTest Always ZWrite Off Cull Off
        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex, _CurrentMask, _EstimateMask, _Stats, _Range;
        float4 _MainTex_TexelSize;
        float _LastMip, _Retain, _Masked, _FillOffset;
        float4 Stats(v2f_img i) : SV_Target
        {
            float d = tex2D(_MainTex, i.uv).r;
            float a = tex2D(_CurrentMask, i.uv).r;
            float b = tex2D(_EstimateMask, i.uv).r;
            float m = _Masked > .5 ? saturate(a * 2 - .5) * saturate(b * 1.5 - .25) : 1;
            return float4(d * m, m, d * d * m, 0);
        }
        float4 Range(v2f_img i) : SV_Target
        {
            float3 s = tex2Dlod(_Stats, float4(.5, .5, 0, _LastMip)).rgb;
            float4 previous = tex2D(_MainTex, float2(.5, .5));
            if (s.y < .002) return float4(0, 1, 0, 0);
            float mean = s.x / s.y;
            float spread = sqrt(max(s.z / s.y - mean * mean, 1e-6));
            spread = max(spread, .02 * abs(mean) + .001);
            float2 limits = float2(mean - 2 * spread, mean + 2 * spread);
            return float4(lerp(limits, previous.xy, previous.z > .5 ? _Retain : 0), 1, 0);
        }
        float4 Seed(v2f_img i) : SV_Target
        {
            float3 s = tex2D(_MainTex, i.uv).rgb;
            float d = s.x / max(s.y, 1e-5);
            float trust = s.y, band = s.y, nearest = d;
            [unroll] for (int y = -3; y <= 3; y++)
                [unroll] for (int x = -3; x <= 3; x++)
                {
                    float3 n = tex2D(_MainTex, i.uv + float2(x, y) * _MainTex_TexelSize.xy).rgb;
                    if (abs(x) <= 2 && abs(y) <= 2) trust = min(trust, n.y);
                    band = min(band, n.y);
                    if (n.y > .6) nearest = max(nearest, n.x / n.y);
                }
            float3 r = tex2D(_Range, float2(.5, .5)).rgb;
            float scale = 1 / max(r.y - r.x, .001);
            bool behind = r.z > .5 && (d - r.x) * scale < -.2;
            bool blurred = r.z > .5 && band < .6 && (nearest - d) * scale > .3;
            return trust > .6 && !behind && !blurred ? float4(d, 1, 0, 0) : 0;
        }
        float4 Fill(v2f_img i) : SV_Target
        {
            float2 f = tex2D(_MainTex, i.uv).rg;
            if (f.y > .5) return float4(f, 0, 0);
            float2 sum = 0;
            [unroll] for (int y = -1; y <= 1; y++)
                [unroll] for (int x = -1; x <= 1; x++)
                {
                    if (x == 0 && y == 0) continue;
                    float2 n = tex2D(_MainTex, i.uv + float2(x, y) * _FillOffset * _MainTex_TexelSize.xy).rg;
                    if (n.y > .5) sum += float2(n.x, 1);
                }
            return sum.y > 0 ? float4(sum.x / sum.y, 1, 0, 0) : 0;
        }
        float4 Normalize(v2f_img i) : SV_Target
        {
            float2 f = tex2D(_MainTex, i.uv).rg;
            float3 r = tex2D(_Range, float2(.5, .5)).rgb;
            float d = _Masked > .5 && f.y < .5 ? (r.x + r.y) * .5 : f.x;
            float mask = _Masked > .5 ? smoothstep(.25, .75, tex2D(_CurrentMask, i.uv).r) : 1;
            return float4(saturate((d - r.x) / max(r.y - r.x, .001)), mask * r.z, 0, 1);
        }
        ENDCG
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Stats
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Range
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Seed
            #pragma target 3.0
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Fill
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Normalize
            ENDCG
        }
    }
}
