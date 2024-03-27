Shader "Flocking/Instanced"
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

        float4x4 _LookAtMatrix;
        float4x4 _Matrix;
        float3 _BoidPosition;

        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        struct Boid
        {
            float3 position;
            float3 direction;
            float noise_offset;
        };

        StructuredBuffer<Boid> boidsBuffer; 
        #endif
     
        void setup()
        {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            {
                Boid boid = boidsBuffer[unity_InstanceID];
                //_BoidPosition = boid.position;
                //_LookAtMatrix = look_at_matrix(boid.direction, float3(0.0, 1.0, 0.0));
                _Matrix = create_matrix(boid.position, boid.direction, float3(0.0, 1.0, 0.0));
            }
            #endif
        }
     
        void vert(inout appdata_full v, out Input data)
        {
            UNITY_INITIALIZE_OUTPUT(Input, data);

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            {
                //v.vertex = mul(_LookAtMatrix, v.vertex);
                //v.vertex.xyz += _BoidPosition;
                v.vertex = mul(_Matrix, v.vertex);
            }
            #endif
        }

        ENDCG
   }
}