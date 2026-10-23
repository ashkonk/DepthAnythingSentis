Shader "Hidden/DepthAnything/Sample/BodyPixMask"
{
    Properties { _MainTex ("BodyPix packed mask", 2D) = "black" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 frag(v2f_img i) : SV_Target
            {
                float probability = tex2D(_MainTex, i.uv).a;
                return float4(probability, 0, 0, 1);
            }
            ENDCG
        }
    }
}
