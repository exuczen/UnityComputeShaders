﻿// StableFluids - A GPU implementation of Jos Stam's Stable Fluids on Unity
// https://github.com/keijiro/StableFluids

Shader "Custom/StableFluids"
{
    Properties
    {
        _MainTex("", 2D) = ""
        _VelocityField("", 2D) = ""
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    sampler2D _MainTex;
    float4 _MainTex_TexelSize; // (1.0 / width, 1.0 / height, width, height)

    sampler2D _VelocityField;

    float2 _ForceOrigin;
    float _ForceExponent;

    half4 frag_advect(v2f_img i) : SV_Target
    {
        // Time parameters
        float time = _Time.y;
        float deltaTime = unity_DeltaTime.x;

        // Aspect ratio coefficients
        float2 aspect = float2(_MainTex_TexelSize.y * _MainTex_TexelSize.z, 1); // (width / height, 1)
        float2 aspect_inv = float2(_MainTex_TexelSize.x * _MainTex_TexelSize.w, 1); // (height / width, 1)

        // Color advection with the velocity field
        float2 delta = tex2D(_VelocityField, i.uv).xy * aspect_inv * deltaTime;
        float3 color = tex2D(_MainTex, i.uv - delta).xyz;

        // Dye (injection color)
        float3 dye = saturate(sin(time * float3(2.72, 5.12, 4.98)) + 0.5);

        // Blend dye with the color from the buffer.  
        float2 pos = (i.uv - 0.5) * aspect;
        float amp = exp(-_ForceExponent * distance(_ForceOrigin, pos));
        color = lerp(color, dye, saturate(amp * 100));

        return half4(color, 1);

        //return half4(tex2D(_MainTex, i.uv).rgb, 1);
    }

    half4 frag_render(v2f_img i) : SV_Target
    {
        half3 rgb = tex2D(_MainTex, i.uv).rgb;
        //half3 rgb = tex2D(_VelocityField, i.uv).xyx;

        //half3 rgb0 = tex2D(_MainTex, i.uv).rgb;
        //half3 rgb1 = tex2D(_VelocityField, i.uv).xyx;
        //half3 rgb = rgb0 * rgb1;

        return half4(rgb, 1);
    }

    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_advect
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_render
            ENDCG
        }
    }
}
