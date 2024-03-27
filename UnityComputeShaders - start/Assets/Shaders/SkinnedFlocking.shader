Shader "Flocking/Skinned" 
{ // StructuredBuffer + SurfaceShader

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
        #include "UnityCG.cginc"
        #include "Utils/Math.cginc"
        #include "Utils/Surface.hlsl"

        #pragma multi_compile __ FRAME_INTERPOLATION
        #pragma surface surf Standard vertex:vert addshadow nolightmap
        #pragma instancing_options procedural:setup

        struct appdata_custom 
        {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            float4 texcoord : TEXCOORD0;
            float4 tangent : TANGENT;
 
            uint id : SV_VertexID;
            uint inst : SV_InstanceID;

            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
 
        float4x4 _Matrix;
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
            float frame;
        };

        StructuredBuffer<Boid> boidsBuffer; 
        StructuredBuffer<float4> vertexAnimation; 
        #endif

        void setup()
        {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            {
                Boid boid = boidsBuffer[unity_InstanceID];
                _Matrix = create_matrix(boid.position, boid.direction, float3(0.0, 1.0, 0.0));
                _CurrentFrame = boid.frame;
                #ifdef FRAME_INTERPOLATION
                {
                    _NextFrame = _CurrentFrame + 1;
                    if (_NextFrame >= numOfFrames)
                    {
                        _NextFrame = 0;
                    }
                    _FrameInterpolation = frac(boid.frame);
                }
                #endif
            }
            #endif
        }
     
        void vert(inout appdata_custom v)
        {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            {
                int voffset = v.id * numOfFrames;
                #ifdef FRAME_INTERPOLATION
                {
                    v.vertex = lerp(vertexAnimation[voffset + _CurrentFrame], vertexAnimation[voffset + _NextFrame], _FrameInterpolation);
                }
                #else
                {
                    v.vertex = vertexAnimation[voffset + _CurrentFrame];
                }
                #endif
                v.vertex = mul(_Matrix, v.vertex);
            }
            #endif
        }

        ENDCG
    }
}