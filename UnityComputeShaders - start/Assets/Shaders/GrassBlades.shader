﻿Shader "Custom/GrassBlades"
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

        // Physically based Standard lighting model, and enable shadows on all light types   
        #pragma surface surf Standard vertex:vert addshadow fullforwardshadows
        #pragma instancing_options procedural:setup

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float _Fade;

        float _Scale;
        float4x4 _LeanMatrix;
        float3 _Position;
        
        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        struct GrassBlade
        {
            float3 position;
            float lean;
            float noise;
            float fade;
        };

        StructuredBuffer<GrassBlade> bladesBuffer; 
        #endif

        void setup()
        {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            {
                GrassBlade blade = bladesBuffer[unity_InstanceID];
                _LeanMatrix = create_matrix_xy(blade.position, blade.lean);
                _Position = blade.position;
                _Fade = blade.fade;
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

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color * _Fade;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
