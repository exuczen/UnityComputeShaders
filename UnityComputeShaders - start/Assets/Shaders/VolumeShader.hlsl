// Allowed floating point inaccuracy
#define EPSILON 0.00001f

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
