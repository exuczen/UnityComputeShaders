Shader "Flocking/Fish" 
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
        #include "Utils/Surface.hlsl"
        #include "Utils/Math.cginc"
 
        #pragma surface surf Standard vertex:vert addshadow nolightmap
        #pragma instancing_options procedural:setup

        float3 _BoidPosition;
        float _FinOffset;
        float4x4 _Matrix;
        
        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        struct Boid
        {
            float3 position;
            float3 direction;
            float noise_offset;
            float theta;
        };

        StructuredBuffer<Boid> boidsBuffer;
        #endif

        void setup()
        {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            {
                Boid boid = boidsBuffer[unity_InstanceID];
                //Convert the boid theta value to a value between -1 and 1
                //Hint: use sin and save the value as _FinOffset
                _FinOffset = sin(boid.theta);
                _Matrix = create_matrix(boid.position, boid.direction, float3(0.0, 1.0, 0.0));
            }
            #endif
        }
     
        void vert(inout appdata_full v, out Input data)
        {
            UNITY_INITIALIZE_OUTPUT(Input, data);

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            {
                if (v.vertex.z < -0.2)
                {
                    //If v.vertex.z is less than -0.2 then this is a tail vertex
                    //The sin curve between 3π/2 and 2π ramps up from -1 to 0
                    //Use this curve plus 1, ie a curve from 0 to 1 to control the strength of the swish  
                    //Apply the value you calculate as an offset to v.vertex.x 

                    float delta = (-0.2 - v.vertex.z) / 0.2;
                    
                    v.vertex.x += _FinOffset * (1.0 - cos(delta * UNITY_HALF_PI)) * 0.3;
                }
                v.vertex = mul(_Matrix, v.vertex);
            }
            #endif
        }
 
        ENDCG
    }
}
