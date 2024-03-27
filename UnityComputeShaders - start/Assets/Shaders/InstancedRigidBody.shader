Shader "Physics/InstancedRigidBody" 
{ 

    Properties
    {
	    _Color ("Color", Color) = (1,1,1,1)
	    _MainTex ("Albedo (RGB)", 2D) = "white" {}
	    _BumpMap ("Bumpmap", 2D) = "bump" {}
	    _MetallicGlossMap("Metallic", 2D) = "white" {}
	    _Metallic ("Metallic", Range(0,1)) = 0.0
	    _Glossiness ("Smoothness", Range(0,1)) = 1.0
	}

    SubShader 
    {
		CGPROGRAM
        #include "Utils/Math.cginc"
        #include "Utils/Surface.hlsl"
 
        #pragma surface surf Standard vertex:vert addshadow nolightmap
        #pragma instancing_options procedural:setup

        float4x4 _Matrix;
        
        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        struct RigidBody
        {
	        float3 position;
	        float4 quaternion;
	        float3 velocity;
	        float3 angularVelocity;
	        int particleIndex;
	        int particleCount;
        };

        StructuredBuffer<RigidBody> rigidBodiesBuffer; 
        #endif

        void setup()
        {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            {
                RigidBody body = rigidBodiesBuffer[unity_InstanceID];
                //_Matrix = create_matrix(body.position, body.quaternion);
                _Matrix = quaternion_to_matrix(body.quaternion, body.position);
            }
            #endif
        }

        void vert(inout appdata_full v, out Input data)
        {
            UNITY_INITIALIZE_OUTPUT(Input, data);

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            {
                v.vertex = mul(_Matrix, v.vertex);
            }
            #endif
        }

        ENDCG
    }
}
