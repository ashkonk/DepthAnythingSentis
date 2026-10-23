Shader "DepthAnything/Sample/Overview"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _Input, _Depth, _Relief;
            float _InputAspect;
            float4 frag(v2f_img i) : SV_Target
            {
                float2 pixel = i.uv * float2(1800, 452);
                float3 backdrop = float3(.008, .013, .023);
                float panelWidth = 570.6667;
                float localX = pixel.x - 24;
                float index = floor(localX / (panelWidth + 20));
                float2 uv = float2((localX - index * (panelWidth + 20)) / panelWidth, (pixel.y - 35.7778) / 380.4444);
                if (index < 0 || index > 2 || any(uv < 0) || any(uv > 1)) return float4(backdrop, 1);
                if (index < .5)
                {
                    float2 fit = float2(min(1, _InputAspect / 1.5), min(1, 1.5 / _InputAspect));
                    uv = (uv - .5) / fit + .5;
                    if (any(uv < 0) || any(uv > 1)) return float4(backdrop, 1);
                    return float4(tex2D(_Input, uv).rgb, 1);
                }
                return index < 1.5 ? tex2D(_Depth, uv) : tex2D(_Relief, uv);
            }
            ENDCG
        }
    }
}
