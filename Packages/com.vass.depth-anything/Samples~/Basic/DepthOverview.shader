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
            float4 _OutputSize; // width, height, outer margin, panel gap
            float4 frag(v2f_img i) : SV_Target
            {
                float2 pixel = i.uv * _OutputSize.xy;
                float3 backdrop = float3(.008, .013, .023);
                float panelWidth = (_OutputSize.x - 2 * _OutputSize.z - 2 * _OutputSize.w) / 3;
                float panelHeight = _OutputSize.y - 2 * _OutputSize.z;
                float localX = pixel.x - _OutputSize.z;
                float index = floor(localX / (panelWidth + _OutputSize.w));
                float2 uv = float2((localX - index * (panelWidth + _OutputSize.w)) / panelWidth, (pixel.y - _OutputSize.z) / panelHeight);
                if (index < 0 || index > 2 || any(uv < 0) || any(uv > 1)) return float4(backdrop, 1);
                float panelAspect = panelWidth / panelHeight;
                float2 fit = float2(min(1, _InputAspect / panelAspect), min(1, panelAspect / _InputAspect));
                uv = (uv - .5) / fit + .5;
                if (any(uv < 0) || any(uv > 1)) return float4(backdrop, 1);
                if (index < .5)
                {
                    return float4(tex2D(_Input, uv).rgb, 1);
                }
                return index < 1.5 ? tex2D(_Depth, uv) : tex2D(_Relief, uv);
            }
            ENDCG
        }
    }
}
