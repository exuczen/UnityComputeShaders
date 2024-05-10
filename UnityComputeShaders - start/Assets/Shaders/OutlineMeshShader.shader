Shader "Unlit/OutlineMeshShader"
{
    Properties
    {
        _Color("Color", Vector) = (1, 1, 1, 1)
        [IntRange] _DepthMlp("Depth Mlp", Range(1, 20)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Blend Off
        LOD 0

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            float4 _Color;
            int _DepthMlp;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 clipPos = i.vertex;
                float depth = clipPos.z / clipPos.w;
                return float4(_Color.xyz, _DepthMlp * depth + 0.01);
            }
            ENDCG
        }
    }
}
