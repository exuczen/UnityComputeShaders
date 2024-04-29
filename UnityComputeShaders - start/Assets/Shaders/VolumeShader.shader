Shader "Unlit/VolumeShader"
{
    Properties
    {
        _MainTex("Texture", 3D) = "white" {}
        _SampleAlpha("Sample Alpha", Range(0.0, 1.0)) = 0.02
        _FragAlpha("Frag Alpha", Range(-2.0, 2.0)) = 0.0
        _StepSize("Step Size", Range(0.015, 1.0)) = 0.01
        _StepCount("Step Count", Range(1, 128)) = 1
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

        static const bool CullFront = _Cull == 1;
        static const bool CullBack = _Cull == 2;

        static const float3 LocalCameraPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1));
        static const float IsCameraAboveCrossSection = objectAboveCrossSection(LocalCameraPos, CamNear);

        //float3 WorldCrossSectionNormal;
        //float3 WorldCrossSectionPoint;
        //matrix ModelMatrix;
        //matrix ModelMatrixInv;
        //float3 ModelPosition;
        //float3 WorldCameraPosition;
        //float3 WorldCameraForward;

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

            static const float VertexRaySign = 1 - 2 * abs(_Cull - 1);

            v2f vert(appdata v)
            {
                v2f o;

                // Vertex in object space this will be the starting point of raymarching
                o.objectVertex = v.vertex;

                // Calculate vector from camera to vertex in world space
                //float3 worldVertex = mul(unity_ObjectToWorld, v.vertex).xyz;
                ////float3 localCamPos = mul(unity_WorldToObject, _WorldSpaceCameraPos);
                ////o.vertexRay = -VertexRaySign * normalize((o.objectVertex.xyz - localCamPos));
                //o.vertexRay = -VertexRaySign * (worldVertex - _WorldSpaceCameraPos);
                o.vertexRay = VertexRaySign * WorldSpaceViewDir(v.vertex);

                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                //i.vertexRay = VertexRaySign * WorldSpaceViewDir(i.objectVertex);
                
                // Start raymarching at the front surface of the object
                // Use vector from camera to object surface to get ray direction
                ////float3 rayDirection = i.vertexRay;
                ////float3 rayDirection = -normalize(ObjSpaceViewDir(i.objectVertex));
                float3 rayDirection = worldToObjectDirection(i.vertexRay);
                float3 samplePosition = i.objectVertex;
                float4 color;

                if (CullFront)
                {
                    color = blendTex3DInClipView(samplePosition, rayDirection);
                }
                else
                {
                    if (IsCameraAboveCrossSection && objectAboveCrossSection(samplePosition))
                    {
                        samplePosition = getIntersectionWithPlane(samplePosition, rayDirection, LocalCrossSectionPoint, LocalCrossSectionNormal);
                    }
                    color = blendTex3D(samplePosition, rayDirection);
                }
                color.a *= _FragAlpha;

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

            static const bool InteriorEnabled = CullBack && all(abs(LocalCameraPos) < 0.5f + CamNear);

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
                    float3 rayDirection;
                    float3 samplePosition;

                    if (IsCameraAboveCrossSection)
                    {
                        float distance;
                        float3 camPos = LocalCameraPos;
                        //float3 vertexRay = normalize(i.objectVertex.xyz - camPos);
                        float3 vertexRay = -normalize(ObjSpaceViewDir(i.objectVertex));
                        float3 planeIsecPoint = getIntersectionWithPlane(camPos, vertexRay, LocalCrossSectionPoint, LocalCrossSectionNormal, distance);
                        
                        //float3 camPos = _WorldSpaceCameraPos;
                        //float3 vertexRay = -normalize(WorldSpaceViewDir(i.objectVertex));
                        ////float3 vertexRay = normalize(mul(unity_ObjectToWorld, i.objectVertex) - camPos);
                        //float3 planeIsecPoint = getIntersectionWithPlane(camPos, vertexRay, WorldCrossSectionPoint, WorldCrossSectionNormal, distance);
                        //planeIsecPoint = mul(unity_WorldToObject, float4(planeIsecPoint, 1));
                        ////vertexRay = normalize(mul(unity_WorldToObject, vertexRay));

                        //float3 camPos = WorldCameraPosition;
                        //float3 vertexRay = normalize(/*ModelPosition +*/ mul(ModelMatrix, i.objectVertex) - camPos);
                        ////float3 vertexRay = normalize(mul(ModelMatrix, i.objectVertex.xyz) - camPos);
                        //float3 planeIsecPoint = getIntersectionWithPlane(camPos, vertexRay, WorldCrossSectionPoint, WorldCrossSectionNormal, distance);
                        //planeIsecPoint = mul(ModelMatrixInv, float4(planeIsecPoint.xyz, 1) /*- ModelPosition*/);
                        ////vertexRay = normalize(mul(ModelMatrixInv, vertexRay));

                        if (distance < 0)
                        {
                            discard;
                        }
                        rayDirection = vertexRay;
                        samplePosition = planeIsecPoint;
                    }
                    else
                    {
                        float3 vertexRay;
                        float3 camNearIsecPoint = getWorldIntersectionWithCameraNearPlane(i.objectVertex, vertexRay);
                    
                        rayDirection = worldToObjectDirection(vertexRay);
                        samplePosition = mul(unity_WorldToObject, camNearIsecPoint);
                    }
                    float4 color = blendTex3D(samplePosition, rayDirection);
                    color.a *= _FragAlpha;

                    return color;
                    //return float4(1, 0, 0, 0);
                    
                    //if (all(abs(samplePosition) < 0.5f + EPSILON))
                    //{
                    //    return tex3D(_MainTex, samplePosition + float3(0.5f, 0.5f, 0.5f));
                    //}
                    //else
                    //{
                    //    discard;
                    //    return COLOR_CLEAR;
                    //}

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
                    return COLOR_CLEAR;
                }
            }

            ENDHLSL
        }
    }
}
