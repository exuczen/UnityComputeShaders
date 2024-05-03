Shader "Unlit/VolumeShader"
{
    Properties
    {
        _MainTex("Texture", 3D) = "white" {}
        _SampleAlpha("Sample Alpha", Range(0.0, 1.0)) = 1.0
        _FragAlpha("Frag Alpha", Range(-2.0, 2.0)) = 1.0
        _StepSize("Step Size", Range(0.015, 1.0)) = 0.01
        [IntRange] _StepCount("Step Count", Range(1, 128)) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Integer) = 2
        [Toggle(BLEND_ENABLED)] _BlendEnabled("Blend Enabled", Integer) = 1
        [Toggle(CROSS_SECTION)] _CrossSection("Cross Section", Integer) = 0
        [Toggle(DEBUG_MODEL_VIEW)] _DebugModelView("Debug Model View", Integer) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendSrc("Blend Src", Integer) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendDst("Blend Dst", Integer) = 10
        [Enum(UnityEngine.Rendering.CompareFunction)] _ExteriorZTest("Exterior ZTest", Integer) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _InteriorZTest("Interior ZTest", Integer) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "LightMode" = "Always" }
        LOD 100

        HLSLINCLUDE

        #include "UnityCG.cginc"
        #include "VolumeShader.hlsl"

        #pragma multi_compile CULL_OFF CULL_BACK CULL_FRONT

        #pragma shader_feature __ BLEND_ENABLED
        #pragma shader_feature __ CROSS_SECTION
        #pragma shader_feature __ DEBUG_MODEL_VIEW

        #ifdef DEBUG_MODEL_VIEW
        matrix ModelMatrix;
        matrix ModelMatrixInv;
        float3 ModelPosition;
        float3 ModelCameraForward;
        float3 WorldCameraPosition;
        float3 WorldCameraForward;
        #endif

        //static const float IsCameraAboveCrossSection = objectAboveCrossSection(LocalCameraPos);
        //static const float IsCameraBelowCrossSection = !IsCameraAboveCrossSection;

        ENDHLSL

        Pass
        {
            Name "Exterior"
            Blend [_BlendSrc] [_BlendDst]
            Cull [_Cull]
            ZTest [_ExteriorZTest]

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
                bool discarding = false;

                //i.vertexRay = VertexRaySign * WorldSpaceViewDir(i.objectVertex);
                
                // Start raymarching at the front surface of the object
                // Use vector from camera to object surface to get ray direction
                ////float3 rayDirection = i.vertexRay;
                ////float3 rayDirection = -normalize(ObjSpaceViewDir(i.objectVertex));
                float3 rayDirection = worldToObjectDirection(i.vertexRay);
                float3 samplePosition = i.objectVertex;
                float4 color;
                float4 colorClear = COLOR_CLEAR;

                #ifdef CROSS_SECTION
                if (objectAboveCrossSection(samplePosition))
                {
                    // CullOff   = 0; VertexRaySign = -1
                    // CullFront = 1; VertexRaySign =  1; IsCameraAboveCrossSection;
                    // CullBack  = 2; VertexRaySign = -1; IsCameraBelowCrossSection;
                    float camDistFromPlane = objectDistanceFromCrossSection(LocalCameraPos);

                    if (VertexRaySign * camDistFromPlane > VertexRaySign * EPSILON)
                    {
                        discarding = true;
                    }
                    else
                    {
                        samplePosition = objectIsecWithCrossSection(samplePosition, rayDirection);
                    }
                }
                #endif
                #ifdef BLEND_ENABLED
                {
                    if (CullFront)
                    {
                        color = discarding ? colorClear : blendTex3DInClipView(samplePosition, rayDirection);
                    }
                    else
                    {
                        color = discarding ? colorClear :blendTex3D(samplePosition, rayDirection);
                    }
                }
                #else
                {
                    color = getTex3DColor(samplePosition);
                }
                #endif
                color.a *= _FragAlpha;

                if (all(color < EPSILON))
                {
                    discard;
                }
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Interior"
            Blend [_BlendSrc] [_BlendDst]
            Cull Front
            ZTest [_InteriorZTest]

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 objectVertex : TEXCOORD0;
                //float4 vertexRay : TEXCOORD1;
            };

            static const bool InteriorEnabled = !CullFront && objectPointInCube(LocalCameraPos, CameraNear);

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
                    bool discarding = false;

                    float3 rayDirection;
                    float3 samplePosition;
                    float4 color;
                    float4 colorClear = COLOR_CLEAR;

                    float camNearIsecDist;
                    float3 worldVertexRay;
                    float3 camNearIsecPoint = worldIsecWithCamNearPlane(i.objectVertex, worldVertexRay, camNearIsecDist);

                    camNearIsecPoint = mul(unity_WorldToObject, float4(camNearIsecPoint, 1));

                    if (objectPointInCube(camNearIsecPoint, 0))
                    {
                        #ifdef CROSS_SECTION
                        float planeIsecDist;
                        float planeDist;

                        float3 camPos = LocalCameraPos;
                        //float3 vertexRay = normalize(i.objectVertex.xyz - camPos);
                        float3 vertexRay = -normalize(ObjSpaceViewDir(i.objectVertex));
                        float3 planeIsecPoint = objectIsecWithCrossSection(camPos, vertexRay, planeIsecDist, planeDist);

                        #ifdef DEBUG_MODEL_VIEW
                        //float3 camPos = _WorldSpaceCameraPos;
                        //float3 vertexRay = -normalize(WorldSpaceViewDir(i.objectVertex));
                        ////float3 vertexRay = normalize(mul(unity_ObjectToWorld, i.objectVertex) - camPos);
                        //float3 planeIsecPoint = worldIsecWithCrossSection(camPos, vertexRay, planeIsecDist, planeDist);
                        //planeIsecPoint = mul(unity_WorldToObject, float4(planeIsecPoint, 1));
                        ////vertexRay = normalize(mul(unity_WorldToObject, vertexRay));

                        //float3 camPos = WorldCameraPosition;
                        //float3 vertexRay = normalize(/*ModelPosition +*/ mul(ModelMatrix, i.objectVertex) - camPos);
                        ////float3 vertexRay = normalize(mul(ModelMatrix, i.objectVertex.xyz) - camPos);
                        //float3 planeIsecPoint = worldIsecWithCrossSection(camPos, vertexRay, planeIsecDist, planeDist);
                        //planeIsecPoint = mul(ModelMatrixInv, float4(planeIsecPoint.xyz, 1) /*- ModelPosition*/);
                        ////vertexRay = normalize(mul(ModelMatrixInv, vertexRay));
                        #endif

                        bool planeIsecInClipView = objectInClipView(planeIsecPoint);

                        if (planeDist > 0 && planeIsecDist > 0 && planeIsecInClipView)
                        {
                            rayDirection = vertexRay;
                            samplePosition = planeIsecPoint;

                            //color = float4(1, 0, 0, 0);
                        }
                        else
                        #endif
                        {
                            rayDirection = worldToObjectDirection(worldVertexRay);
                            samplePosition = camNearIsecPoint;

                            #ifdef CROSS_SECTION
                            {
                                discarding = objectAboveCrossSection(samplePosition);
                            }
                            #endif
                            //color = float4(0, 0, 1, 0);
                        }
                    }
                    else
                    {
                        discarding = true;
                    }
                    #ifdef BLEND_ENABLED
                    {
                        color = discarding ? colorClear : blendTex3D(samplePosition, rayDirection);
                    }
                    #else
                    {
                        color = discarding ? colorClear : getTex3DColor(samplePosition);
                    }
                    #endif
                    color.a *= _FragAlpha;

                    #ifdef DEBUG_MODEL_VIEW
                    {
                        //float3 localCameraForward = mul(unity_WorldToObject, float4(unity_CameraToWorld._m02_m12_m22, 0));
                        //float3 modelCameraForward = mul(ModelMatrixInv, float4(unity_CameraToWorld._m02_m12_m22, 0));
                        //float3 modelCameraForward = mul(ModelMatrixInv, float4(WorldCameraForward, 0));

                        //if (all(ModelCameraForward - LocalCameraForward < EPSILON))
                        //{
                        //    color = float4(1, 0, 0, 0);
                        //}
                        //else
                        //{
                        //    color = float4(0, 1, 0, 0);
                        //}
                    }
                    #endif

                    if (all(color < EPSILON))
                    {
                        discard;
                    }
                    return color;
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
