Shader "Unlit/VolumeShader"
{
    Properties
    {
        _MainTex("Texture", 3D) = "white" {}
        _SampleAlpha("Sample Alpha", Range(0.0, 1.0)) = 0.02
        _FragAlpha("Frag Alpha", Range(-3.0, 1.0)) = 0.0
        _StepSize("Step Size", Range(0.01, 1.0)) = 0.01
        _StepCount("Step Count", Range(1, 128)) = 1
        _ObjectScale("Object Scale", float) = 1
        //_CamForward("Cam Forward", Vector) = (0, 0, 1)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Integer) = 2
        //[Toggle] _InteriorEnabled("Interior Enabled", Integer) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100

        HLSLINCLUDE

        #include "UnityCG.cginc"
        #include "VolumeShader.hlsl"

        //float3 _CamForward;
        
        ENDHLSL

        Pass
        {
            Tags { "LightMode" = "Always" }
            Blend One OneMinusSrcAlpha
            //Blend OneMinusDstAlpha One
            //Blend OneMinusDstColor One
            //Blend One OneMinusSrcColor
            Cull [_Cull]

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 objectVertex : TEXCOORD0;
                float3 vertexRay : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;

                // Vertex in object space this will be the starting point of raymarching
                o.objectVertex = v.vertex;

                // Calculate vector from camera to vertex in world space
                float sign = 2 * abs(_Cull - 1) - 1;
                //float3 worldVertex = mul(unity_ObjectToWorld, v.vertex).xyz;
                ////float3 localCamPos = mul(unity_WorldToObject, _WorldSpaceCameraPos);
                ////o.vertexRay = sign * normalize((o.objectVertex.xyz - localCamPos));
                //o.vertexRay = sign * (worldVertex - _WorldSpaceCameraPos);
                o.vertexRay = -sign * WorldSpaceViewDir(v.vertex);

                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                //float sign = 2 * abs(_Cull - 1) - 1;
                //i.vertexRay = -sign * WorldSpaceViewDir(i.objectVertex);

                // Start raymarching at the front surface of the object
                float3 rayOrigin = i.objectVertex;
                // Use vector from camera to object surface to get ray direction
                ////float3 rayDirection = i.vertexRay;
                ////float3 rayDirection = -normalize(ObjSpaceViewDir(i.objectVertex));
                float3 rayDirection = mul(unity_WorldToObject, normalize(i.vertexRay));
                float3 samplePosition = rayOrigin;

                float4 color = float4(0, 0, 0, 0);

                if (_Cull == 1)
                {
                    // Raymarch through object space
                    for (int i = 0; i < _StepCount; i++)
                    {
                        // Accumulate color only within unit cube bounds
                        if (all(abs(samplePosition) < 0.5f + EPSILON))
                        {
                            if (objectInClipView(samplePosition))
                            {
                                color = blendSampleTex3D(color, rayDirection, samplePosition);
                            }
                            else
                            {
                                samplePosition += rayDirection * RayScale;
                            }
                        }
                    }
                }
                else
                {
                    // Raymarch through object space
                    for (int i = 0; i < _StepCount; i++)
                    {
                        // Accumulate color only within unit cube bounds
                        if (all(abs(samplePosition) < 0.5f + EPSILON))
                        {
                            color = blendSampleTex3D(color, rayDirection, samplePosition);
                        }
                    }
                }
                color.a = _FragAlpha;
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Interior"
            Tags { "LightMode" = "Always" }
            Blend One OneMinusSrcAlpha
            //Blend OneMinusDstAlpha One
            //Blend OneMinusDstColor One
            //Blend One OneMinusSrcColor
            Cull Front

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 objectVertex : TEXCOORD0;
                //float4 vertexRay : TEXCOORD1;
            };

            static const int InteriorEnabled = _Cull == 2;

            v2f vert(appdata v)
            {
                v2f o;

                // Vertex in object space this will be the starting point of raymarching
                o.objectVertex = v.vertex;

                //o.vertexRay.xyz = -WorldSpaceViewDir(v.vertex);
                //o.vertexRay.w = length(o.vertexRay.xyz);

                o.vertex = UnityObjectToClipPos(v.vertex);

                //float3 camForward = UNITY_MATRIX_V[2].xyz;
                //float3 camForward = UNITY_MATRIX_I_V[2].xyz;
                //float3 camForward = mul((float3x3)UNITY_MATRIX_V, float3(0, 0, 1));
                //float3 camForward = unity_CameraToWorld[2].xyz;
                //float3 camForward = mul((float3x3)unity_CameraToWorld, float3(0 ,0 ,1));
                //float3 camForward = unity_CameraToWorld._m02_m12_m22;

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                if (InteriorEnabled)
                {
                    float3 camForward = unity_CameraToWorld._m02_m12_m22;

                    float camNear = _ProjectionParams.y;
                    
                    float3 vertexRay = -WorldSpaceViewDir(i.objectVertex);
                    float vertexRayLength = length(vertexRay);
                    float vertexFwdDist = dot(vertexRay, camForward);

                    // vertexRayLength / vertexFwdDist = camNearIsecDist / camNear
                    float camNearIsecDist = camNear * vertexRayLength / vertexFwdDist;

                    vertexRay /= vertexRayLength;

                    float3 camNearIsecPoint = _WorldSpaceCameraPos + vertexRay * (camNearIsecDist + EPSILON);
                    
                    float3 rayDirection = mul(unity_WorldToObject, vertexRay);
                    float3 samplePosition = mul(unity_WorldToObject, camNearIsecPoint);

                    float4 color = float4(0, 0, 0, 0);

                    // Raymarch through object space
                    for (int i = 0; i < _StepCount; i++)
                    {
                        // Accumulate color only within unit cube bounds
                        if (all(abs(samplePosition) < 0.5f + EPSILON))
                        {
                            color = blendSampleTex3D(color, rayDirection, samplePosition);
                        }
                    }
                    color.a = _FragAlpha;
                    return color;

                    //if (all(_CamForward - camForward < EPSILON))
                    //{
                    //    return float4(1, 0, 0, 0);
                    //}
                    //else
                    //{
                    //    return float4(0, 1, 0, 0);
                    //}
                    //return float4(1, 0, 0, 0);
                }
                else
                {
                    discard;
                    return float4(0, 0, 0, 0);
                }
            }

            ENDHLSL
        }
    }
}
