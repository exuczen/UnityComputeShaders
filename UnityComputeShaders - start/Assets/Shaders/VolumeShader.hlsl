// Allowed floating point inaccuracy
//#define EPSILON 0.00001f
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

float3 LocalCrossSectionNormal;
float3 LocalCrossSectionPoint;

static const float ScaledSampleAlpha = _StepSize * _SampleAlpha * lerp(2.0, 1.0, invLerp(0.015, 1.0, _StepSize));

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

float3 getObjectDeltaRay(float3 worldVertexRay)
{
    float3 rayDelta = mul(unity_WorldToObject, worldVertexRay);
    return normalize(rayDelta) * _StepSize;
}

bool objectInClipView(float3 objectPosition)
{
    float4 clipPos = UnityObjectToClipPos(objectPosition);
    float clipW = clipPos.w;
    return clipPos.z >= 0 && all(clipPos.xyz >= -clipW && clipPos.xyz <= clipW);
}

float3 getWorldIntersectionWithCameraNearPlane(float4 objectVertex, out float3 worldVertexRay)
{
    float3 camForward = unity_CameraToWorld._m02_m12_m22;
    float camNear = _ProjectionParams.y;
                    
    float3 vertexRay = -WorldSpaceViewDir(objectVertex);
    float vertexRayLength = length(vertexRay);
    float vertexFwdDist = dot(vertexRay, camForward);

    // vertexRayLength / vertexFwdDist = camNearIsecDist / camNear
    float camNearIsecDist = camNear * vertexRayLength / vertexFwdDist;

    float3 camNearIsecPoint = _WorldSpaceCameraPos + (camNearIsecDist + EPSILON) * vertexRay / vertexRayLength;
    
    worldVertexRay = vertexRay;
    return camNearIsecPoint;
}

float3 getDistanceFromPlane(float3 position, float3 planePoint, float3 planeNormal)
{
    return dot(position - planePoint, planeNormal);
}

bool isPointBelowPlane(float3 position, float3 planePoint, float3 planeNormal)
{
    return getDistanceFromPlane(position, planePoint, planeNormal) <= 0;
}

bool isPointAbovePlane(float3 position, float3 planePoint, float3 planeNormal)
{
    return !isPointBelowPlane(position, planePoint, planeNormal);
}

bool objectBelowCrossSection(float3 position)
{
    return isPointBelowPlane(position, LocalCrossSectionPoint, LocalCrossSectionNormal);
}

bool objectAboveCrossSection(float3 position)
{
    return isPointAbovePlane(position, LocalCrossSectionPoint, LocalCrossSectionNormal);
}

float3 getIntersectionWithPlane(float3 rayPoint, float3 rayDir, float3 planePoint, float3 planeNormal, out float distance)
{
    rayDir = normalize(rayDir);
    float cosAngle = dot(rayDir, planeNormal);
    
    if (abs(cosAngle) > EPSILON)
    {
        float distFromPlane = getDistanceFromPlane(rayPoint, planePoint, planeNormal);
        distance = -distFromPlane / cosAngle;
        return rayPoint + distance * rayDir;
    }
    else
    {
        distance = 0;
        return float3(0, 0, 0);
    }
}

float3 getIntersectionWithPlane(float3 rayPoint, float3 rayDir, float3 planePoint, float3 planeNormal)
{
    float distance;
    return getIntersectionWithPlane(rayPoint, rayDir, planePoint, planeNormal, distance);
}
