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
                o.vertexRay = VertexRaySign * WorldSpaceViewDir(v.vertex);

                o.vertex = UnityObjectToClipPos(v.vertex);

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                bool discarding = false;

                // Start raymarching at the front or back surface of the object
                // Use vector from camera to object surface to get ray direction
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
                        color = discarding ? colorClear : blendTex3D(samplePosition, rayDirection);
                    }
                }
                #else
                {
                    color = discarding ? colorClear : getTex3DColor(samplePosition);
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
            };

            static const bool InteriorEnabled = !CullFront && objectPointInCube(LocalCameraPos, CameraNear);

            v2f vert(appdata v)
            {
                v2f o;

                // Vertex in object space this will be the starting point of raymarching
                o.objectVertex = v.vertex;

                o.vertex = UnityObjectToClipPos(v.vertex);

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                if (InteriorEnabled)
                {
                    bool discarding = false;

                    float3 rayDirection = 0;
                    float3 samplePosition = 0;
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
                        float3 vertexRay = -normalize(ObjSpaceViewDir(i.objectVertex));
                        float3 planeIsecPoint = objectIsecWithCrossSection(camPos, vertexRay, planeIsecDist, planeDist);

                        bool planeIsecInClipView = objectInClipView(planeIsecPoint);

                        if (planeDist > 0 && planeIsecDist > 0 && planeIsecInClipView)
                        {
                            rayDirection = vertexRay;
                            samplePosition = planeIsecPoint;
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
