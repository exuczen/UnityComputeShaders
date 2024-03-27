sampler2D _MainTex;
sampler2D _BumpMap;
sampler2D _MetallicGlossMap;

half _Glossiness;
half _Metallic;
fixed4 _Color;

struct Input
{
    float2 uv_MainTex;
    float2 uv_BumpMap;
    float3 worldPos;
};

void surf(Input IN, inout SurfaceOutputStandard o)
{
    fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
    fixed4 m = tex2D(_MetallicGlossMap, IN.uv_MainTex);
    o.Albedo = c.rgb;
    o.Alpha = c.a;
    o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
    o.Metallic = m.r;
    o.Smoothness = _Glossiness * m.a;
}
