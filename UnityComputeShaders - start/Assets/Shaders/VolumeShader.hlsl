﻿// Allowed floating point inaccuracy
#define EPSILON 0.00001f

#include "Utils/Math.cginc"

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
float _ObjectScale;

static const float RayScale = _StepSize * _ObjectScale;
static const float ScaledSampleAlpha = _StepSize * _SampleAlpha * lerp(1.0, 0.5, invLerp(0.015, 1.0, _StepSize));

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

float4 blendSampleTex3D(float4 color, float3 rayDirection, inout float3 samplePosition)
{
    float4 sampledColor = tex3D(_MainTex, samplePosition + float3(0.5f, 0.5f, 0.5f));
    sampledColor.a *= ScaledSampleAlpha;
    color = blendUnder(color, sampledColor);

    samplePosition += rayDirection * RayScale;
    
    return color;
}

bool objectInClipView(float3 objectPosition)
{
    float4 clipPos = UnityObjectToClipPos(objectPosition);
    float clipW = clipPos.w;
    return clipPos.z >= 0 && all(clipPos.xyz >= -clipW && clipPos.xyz <= clipW);
}