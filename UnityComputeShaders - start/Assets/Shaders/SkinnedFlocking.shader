Shader "Flocking/Skinned" { // StructuredBuffer + SurfaceShader

   Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_BumpMap ("Bumpmap", 2D) = "bump" {}
		_MetallicGlossMap("Metallic", 2D) = "white" {}
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_Glossiness ("Smoothness", Range(0,1)) = 1.0
	}

   SubShader {
 
		CGPROGRAM
        #include "UnityCG.cginc"

		sampler2D _MainTex;
		sampler2D _BumpMap;
		sampler2D _MetallicGlossMap;
        struct appdata_custom {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            float4 texcoord : TEXCOORD0;
            float4 tangent : TANGENT;
 
            uint id : SV_VertexID;
            uint inst : SV_InstanceID;

            UNITY_VERTEX_INPUT_INSTANCE_ID
         };
		struct Input {
			float2 uv_MainTex;
			float2 uv_BumpMap;
			float3 worldPos;
		};
		half _Glossiness;
		half _Metallic;
		fixed4 _Color;
 
        #pragma multi_compile __ FRAME_INTERPOLATION
        #pragma surface surf Standard vertex:vert addshadow nolightmap
        #pragma instancing_options procedural:setup

        float4x4 _LookAtMatrix;
        float3 _BoidPosition;
        int _CurrentFrame;
        int _NextFrame;
        float _FrameInterpolation;
        int numOfFrames;

         #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            struct Boid
            {
                float3 position;
                float3 direction;
                float noise_offset;
                float speed;
                float frame;
                float next_frame;
	            float frame_interpolation;
                float padding;
            };

            StructuredBuffer<Boid> boidsBuffer; 
            StructuredBuffer<float4> vertexAnimation; 
         #endif

        float4x4 look_at_matrix(float3 at, float3 eye, float3 up) {
            float3 zaxis = normalize(at - eye);
            float3 xaxis = normalize(cross(up, zaxis));
            float3 yaxis = cross(zaxis, xaxis);
            return float4x4(
                xaxis.x, yaxis.x, zaxis.x, 0,
                xaxis.y, yaxis.y, zaxis.y, 0,
                xaxis.z, yaxis.z, zaxis.z, 0,
                0, 0, 0, 1
            );
        }
     
        void vert(inout appdata_custom v)
        {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                #ifdef FRAME_INTERPOLATION
                    v.vertex = lerp(vertexAnimation[v.id * numOfFrames + _CurrentFrame], vertexAnimation[v.id * numOfFrames + _NextFrame], _FrameInterpolation);

                #else
                    v.vertex = vertexAnimation[v.id * numOfFrames + _CurrentFrame];
                #endif
                v.vertex = mul(_LookAtMatrix, v.vertex);
                v.vertex.xyz += _BoidPosition;
            #endif
        }

        void setup()
        {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                _BoidPosition = boidsBuffer[unity_InstanceID].position;
                _LookAtMatrix = look_at_matrix(_BoidPosition, _BoidPosition + (boidsBuffer[unity_InstanceID].direction * -1), float3(0.0, 1.0, 0.0));
                _CurrentFrame = boidsBuffer[unity_InstanceID].frame;
                #ifdef FRAME_INTERPOLATION
                    _NextFrame = boidsBuffer[unity_InstanceID].next_frame;
                    _FrameInterpolation = boidsBuffer[unity_InstanceID].frame_interpolation;
                #endif
            #endif
        }
 
         void surf (Input IN, inout SurfaceOutputStandard o) {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			fixed4 m = tex2D (_MetallicGlossMap, IN.uv_MainTex); 
			o.Albedo = c.rgb;
			o.Alpha = c.a;
			o.Normal = UnpackNormal (tex2D (_BumpMap, IN.uv_BumpMap));
			o.Metallic = m.r;
			o.Smoothness = _Glossiness * m.a;
         }
 
         ENDCG
   }
}