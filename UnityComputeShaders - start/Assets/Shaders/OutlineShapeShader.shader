Shader "Unlit/OutlineShapeShader"
{
    Properties
    {
        _Color("Color", Vector) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Blend Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #define EPSILON 0.00001f

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            float4 _Color;
            float _Depth;
            float _MinDepth;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                #if UNITY_REVERSED_Z
                o.vertex.z = clamp(UNITY_NEAR_CLIP_VALUE - _Depth + _MinDepth, -1, 1);
                #else
                o.vertex.z = clamp(UNITY_NEAR_CLIP_VALUE + _Depth - EPSILON, -1, 1);
                #endif
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
}
