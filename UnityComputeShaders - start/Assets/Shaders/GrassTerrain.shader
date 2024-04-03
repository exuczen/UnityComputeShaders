Shader "Custom/GrassTerrain"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        
		LOD 200
		Cull Off
		
        CGPROGRAM
        #include "Utils/Math.cginc"
        #include "Utils/GrassSurface.hlsl"

        // Physically based Standard lighting model, and enable shadows on all light types    
        #pragma surface surf Standard vertex:vert addshadow fullforwardshadows
        #pragma instancing_options procedural:setup

        float _Scale;
        float4x4 _LeanMatrix;
        float3 _Position;
        
        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        struct GrassClump
        {
            float3 position;
            float lean;
            float noise;
        };

        StructuredBuffer<GrassClump> clumpsBuffer; 
        #endif

        void setup()
        {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            {
                GrassClump clump = clumpsBuffer[unity_InstanceID];
                _Position = clump.position;
                _LeanMatrix = create_matrix_xy(clump.position, clump.lean);
            }
            #endif
        }

        void vert(inout appdata_full v, out Input data)
        {
            UNITY_INITIALIZE_OUTPUT(Input, data);

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            {
                v.vertex.xyz *= _Scale;
                float4 leanedVertex = mul(_LeanMatrix, v.vertex);
                v.vertex.xyz += _Position;
                v.vertex = lerp(v.vertex, leanedVertex, v.texcoord.y);
            }
            #endif
        }

        ENDCG
    }
    FallBack "Diffuse"
}
