Shader "Unlit/SphericalReflectionShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Direction("Direction", Integer) = 1
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

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Direction;

            static const float M_PI = 3.14159265359;
            static const float M_2PI = 6.28318530718;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float3 r = normalize(v.vertex.xyz);
                float fi = atan2(r.z, r.x);
                float fiSign = sign(fi);
                if (_Direction < 0)
                {
                    fi = -fiSign * M_PI + fi;
                }
                float U = 0.5 + fi / M_2PI;
                float V = 0.5 + _Direction * asin(r.y)/M_PI;
                v.uv = float2(U,V);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                //o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}
