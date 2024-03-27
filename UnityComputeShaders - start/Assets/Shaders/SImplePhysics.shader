Shader "Physics/Simple" 
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
        Cull Off

		CGPROGRAM
        #include "Utils/Surface.hlsl"
        
        #pragma surface surf Standard vertex:vert addshadow nolightmap
        #pragma instancing_options procedural:setup

        float3 _BallPosition;
        float _Radius;
        
        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        struct Ball
        {
            float3 position;
            float3 velocity;
            float4 color;
        };

        StructuredBuffer<Ball> ballsBuffer; 
        #endif

        void setup()
        {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            {
                _Color = ballsBuffer[unity_InstanceID].color;
                _BallPosition = ballsBuffer[unity_InstanceID].position;
            }
            #endif
        }
        
        void vert(inout appdata_full v, out Input data)
        {
            UNITY_INITIALIZE_OUTPUT(Input, data);

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            {
                v.vertex.xyz *= _Radius;
                v.vertex.xyz += _BallPosition;
            }
            #endif
        }
 
        ENDCG
    }
}