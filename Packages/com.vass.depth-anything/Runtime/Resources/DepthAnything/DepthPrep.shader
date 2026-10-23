Shader "Hidden/DepthAnything/Prepare"
{
    Properties { _MainTex ("Image", 2D) = "white" {} }
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
            float _Linearised;
            float4 frag(v2f_img i) : SV_Target
            {
                float3 c = saturate(tex2D(_MainTex, i.uv).rgb);
                if (_Linearised > 0.5) c = LinearToGammaSpace(c);
                return float4((c - float3(0.485, 0.456, 0.406)) / float3(0.229, 0.224, 0.225), 1);
            }
            ENDCG
        }
    }
}
