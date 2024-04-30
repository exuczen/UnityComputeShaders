﻿// Allowed floating point inaccuracy
//#define EPSILON 0.00001f
//#define DEBUG_BLEND_DISABLED
//#define DEBUG_MODEL_VIEW
#define COLOR_CLEAR float4(0, 0, 0, 0);

#include "Utils/Math.cginc"
#include "Utils/Geometry.cginc"

struct appdata
{
    float4 vertex : POSITION;
};

sampler3D _MainTex;
float4 _MainTex_ST;
float _SampleAlpha;
float _FragAlpha;
float _StepSize;
int _StepCount;
int _Cull;

float3 WorldCrossSectionNormal;
float3 WorldCrossSectionPoint;

float3 LocalCrossSectionNormal;
float3 LocalCrossSectionPoint;

#ifdef DEBUG_MODEL_VIEW
matrix ModelMatrix;
matrix ModelMatrixInv;
float3 ModelPosition;
float3 ModelCameraForward;
float3 WorldCameraPosition;
float3 WorldCameraForward;
#endif

static const float3 LocalCameraPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1));
static const float3 LocalCameraForward = normalize(mul(unity_WorldToObject, float4(unity_CameraToWorld._m02_m12_m22, 0)).xyz);

static const float CameraNear = _ProjectionParams.y;

static const float ScaledSampleAlpha = _StepSize * _SampleAlpha * lerp(2.0, 0.75, invLerp(0.015, 1.0, _StepSize));

static const bool CullFront = _Cull == 1;
static const bool CullBack = _Cull == 2;

float4 blendUpper(float4 color, float4 newColor)
{
    newColor.rgb += (1.0 - color.a) * newColor.a * newColor.rgb;
    newColor.a += (1.0 - color.a) * newColor.a;
    return newColor;
}

float4 blendUnder(float4 color, float4 newColor)
{
    color.rgb += (1.0 - color.a) * newColor.a * newColor.rgb;
    color.a += (1.0 - color.a) * newColor.a;
    return color;
}

float4 blendSampleTex3D(float4 color, float3 rayDelta, inout float3 samplePosition)
{
    float4 sampledColor = tex3D(_MainTex, samplePosition + float3(0.5f, 0.5f, 0.5f));
    sampledColor.a *= ScaledSampleAlpha;
    color = blendUnder(color, sampledColor);

    samplePosition += rayDelta;
    
    return color;
}

float3 worldToObjectDirection(float3 worldVertexRay)
{
    float3 direction = mul(unity_WorldToObject, worldVertexRay);
    return normalize(direction);
}

bool objectInClipView(float3 objectPosition)
{
    float4 clipPos = UnityObjectToClipPos(objectPosition);
    float clipW = clipPos.w;
    return clipPos.z >= 0 && all(clipPos.xyz >= -clipW && clipPos.xyz <= clipW);
}

float getDistanceFromPlane(float3 position, float3 planePoint, float3 planeNormal)
{
    return dot(position - planePoint, planeNormal);
}

bool isPointBelowPlane(float3 position, float3 planePoint, float3 planeNormal, float epsilon = 0)
{
    return getDistanceFromPlane(position, planePoint, planeNormal) <= epsilon;
}

bool isPointAbovePlane(float3 position, float3 planePoint, float3 planeNormal, float epsilon = 0)
{
    return !isPointBelowPlane(position, planePoint, planeNormal, epsilon);
}

bool objectBelowCrossSection(float3 position, float epsilon = EPSILON)
{
    return isPointBelowPlane(position, LocalCrossSectionPoint, LocalCrossSectionNormal, epsilon);
}

bool objectAboveCrossSection(float3 position, float epsilon = EPSILON)
{
    return isPointAbovePlane(position, LocalCrossSectionPoint, LocalCrossSectionNormal, epsilon);
}

float3 getIntersectionWithPlane(float3 rayPoint, float3 rayDir, float3 planePoint, float3 planeNormal, out float isecDist, out float planeDist)
{
    //rayDir = normalize(rayDir);
    float cosAngle = dot(rayDir, planeNormal);
    
    if (abs(cosAngle) > EPSILON)
    {
        planeDist = getDistanceFromPlane(rayPoint, planePoint, planeNormal);
        isecDist = -planeDist / cosAngle;
        return rayPoint + isecDist * rayDir;
    }
    else
    {
        planeDist = 0;
        isecDist = 0;
        return float3(0, 0, 0);
    }
}

float3 getIntersectionWithPlane(float3 rayPoint, float3 rayDir, float3 planePoint, float3 planeNormal)
{
    float isecDist, planeDist;
    return getIntersectionWithPlane(rayPoint, rayDir, planePoint, planeNormal, isecDist, planeDist);
}

float3 objectIsecWithCrossSection(float3 rayPoint, float3 rayDir, out float isecDist, out float planeDist)
{
    return getIntersectionWithPlane(rayPoint, rayDir, LocalCrossSectionPoint, LocalCrossSectionNormal, isecDist, planeDist);
}

float3 objectIsecWithCrossSection(float3 rayPoint, float3 rayDir)
{
    float isecDist, planeDist;
    return getIntersectionWithPlane(rayPoint, rayDir, LocalCrossSectionPoint, LocalCrossSectionNormal, isecDist, planeDist);
}

float3 worldIsecWithCrossSection(float3 rayPoint, float3 rayDir, out float isecDist, out float planeDist)
{
    return getIntersectionWithPlane(rayPoint, rayDir, WorldCrossSectionPoint, WorldCrossSectionNormal, isecDist, planeDist);
}

float3 worldIsecWithCamNearPlane(float4 objectVertex, out float3 worldVertexRay, out float distance)
{
    float3 camForward = unity_CameraToWorld._m02_m12_m22;
    float camNear = _ProjectionParams.y;
                    
    float3 vertexRay = -WorldSpaceViewDir(objectVertex);
    float vertexRayLength = length(vertexRay);
    float vertexFwdDist = dot(vertexRay, camForward);

    // vertexRayLength / vertexFwdDist = camNearIsecDist / camNear
    float camNearIsecDist = camNear * vertexRayLength / vertexFwdDist;

    float3 camNearIsecPoint = _WorldSpaceCameraPos + (camNearIsecDist + EPSILON) * vertexRay / vertexRayLength;
    
    distance = camNearIsecDist;
    worldVertexRay = vertexRay;
    return camNearIsecPoint;
}

float4 getTex3DColor(float3 samplePosition, bool belowCrossSection)
{
    if (all(abs(samplePosition) < 0.5f + EPSILON))
    {
        if (!belowCrossSection || objectBelowCrossSection(samplePosition))
        {
            return tex3D(_MainTex, samplePosition + float3(0.5f, 0.5f, 0.5f));
        }
        else
        {
            return COLOR_CLEAR;
        }
    }
    else
    {
        return COLOR_CLEAR;
    }
}

float4 blendTex3D(float3 samplePosition, float3 rayDirection)
{
#ifdef DEBUG_BLEND_DISABLED
    return getTex3DColor(samplePosition, true);
#else
    float4 color = COLOR_CLEAR;
    float3 rayDelta = rayDirection * _StepSize;
    
    // Raymarch through object space
    for (int i = 0; i < _StepCount; i++)
    {
        // Accumulate color only within unit cube bounds
        if (all(abs(samplePosition) < 0.5f + EPSILON))
        {
            if (objectBelowCrossSection(samplePosition))
            {
                color = blendSampleTex3D(color, rayDelta, samplePosition);
            }
            else
            {
                samplePosition += rayDelta;
            }
        }
    }
    return color;
#endif
}

float4 blendTex3DInClipView(float3 samplePosition, float3 rayDirection)
{
#ifdef DEBUG_BLEND_DISABLED
    return getTex3DColor(samplePosition, true);
#else
    float4 color = COLOR_CLEAR;
    float3 rayDelta = rayDirection * _StepSize;

    // Raymarch through object space
    for (int i = 0; i < _StepCount; i++)
    {
        // Accumulate color only within unit cube bounds
        if (all(abs(samplePosition) < 0.5f + EPSILON))
        {
            if (objectBelowCrossSection(samplePosition) && objectInClipView(samplePosition))
            {
                color = blendSampleTex3D(color, rayDelta, samplePosition);
            }
            else
            {
                samplePosition += rayDelta;
            }
        }
    }
    return color;
#endif
}
