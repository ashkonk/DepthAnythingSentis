Shader "DepthAnything/Sample/Display"
{
    Properties { _MainTex ("Normalized depth and mask", 2D) = "black" {} }
    SubShader
    {
        ZTest Always ZWrite Off Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 frag(v2f_img i) : SV_Target
            {
                float2 d = tex2D(_MainTex, i.uv).rg;
                float3 c = lerp(float3(.025, .045, .075), float3(.94, .97, 1), d.r);
                return float4(lerp(float3(.025, .045, .075), c, d.g), 1);
            }
            ENDCG
        }
    }
}
