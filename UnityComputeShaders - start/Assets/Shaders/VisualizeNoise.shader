Shader "Custom/VisualizeNoise"
{
    Properties
    {
        
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Utils/NoiseSimplex.cginc"
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 wind;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.vertex.xz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 offset = (i.uv + wind.xy * _Time.y * wind.z) * wind.w;
                float noise = perlin(offset.x, offset.y);
                return fixed4(noise, noise, noise, 1);
            }
            ENDCG
        }
    }
}
