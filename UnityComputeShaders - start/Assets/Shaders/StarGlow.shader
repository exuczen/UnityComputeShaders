﻿Shader "ImageEffect/StarGlow"
{
    Properties
    {
        [HideInInspector]
        _MainTex("Texture", 2D) = "white" {}

        //[KeywordEnum(ADDITIVE, SCREEN, COLORED_ADDITIVE, COLORED_SCREEN, DEBUG)]
        //_COMPOSITE_TYPE("Composite Type", Float) = 0

        _BrightnessSettings("(Threshold, Intensity, Attenuation, -)", Vector) = (0.8, 1.0, 0.95, 0.0)
    }
    SubShader
    {
        CGINCLUDE

        #include "UnityCG.cginc"

        sampler2D _MainTex;
        float4    _MainTex_ST;
        float4    _MainTex_TexelSize;
        float4    _BrightnessSettings;

        #define BRIGHTNESS_THRESHOLD _BrightnessSettings.x
        #define INTENSITY            _BrightnessSettings.y
        #define ATTENUATION          _BrightnessSettings.z

        ENDCG

        Pass //Step 0
        {
            CGPROGRAM

            #pragma vertex vert_img
            #pragma fragment frag

            fixed4 frag(v2f_img input) : SV_Target
            {
                return tex2D(_MainTex, input.uv);
            }

            ENDCG
        }

        Pass //Step 1
        {
            CGPROGRAM

            #pragma vertex vert_img
            #pragma  fragment frag
            
            fixed4 frag(v2f_img input) : SV_Target
            {
                float4 color = tex2D(_MainTex, input.uv);
                //return step(BRIGHTNESS_THRESHOLD, (color.r + color.g + color.b) / 3.0f) * color;
                return max(color - BRIGHTNESS_THRESHOLD, 0) * INTENSITY;
            }

            ENDCG
        }

        Pass //Step 2
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma  fragment frag
            
            int _Iteration;
            float2 _Offset;

            struct v2f_starglow
            {
                float4 pos      : SV_POSITION;
                half2 uv        : TEXCOORD0;
                half power      : TEXCOORD1;
                half2 offset    : TEXCOORD2;
            };

            v2f_starglow vert(appdata_img v)
            {
                v2f_starglow o;

                o.pos       = UnityObjectToClipPos(v.vertex);
                o.uv        = v.texcoord;
                o.power     = 1 << ((_Iteration - 1) << 1); //pow(4, _Iteration - 1);
                o.offset    = _MainTex_TexelSize.xy * _Offset * o.power;

                return o;
            }

            float4 frag(v2f_starglow input) : SV_TARGET
            {
                half4 color = 0;
                half2 uv = input.uv;

                for (int j = 0; j < 4; j++)
                {
                    color += saturate(tex2D(_MainTex, uv) * pow(ATTENUATION, input.power * j));
                    uv += input.offset;
                }
                return color;
            }
            ENDCG
        }

        Pass //Step 3
        {
            Blend OneMinusDstColor One
            
            CGPROGRAM

            #pragma vertex vert_img
            #pragma  fragment frag
            
            fixed4 frag(v2f_img input) : SV_Target
            {
                return tex2D(_MainTex, input.uv);
            }

            ENDCG
        }

        Pass //Step 4
        {
            CGPROGRAM

            #pragma vertex vert_img
            #pragma  fragment frag
            //#pragma multi_compile _COMPOSITE_TYPE_ADDITIVE _COMPOSITE_TYPE_SCREEN _COMPOSITE_TYPE_COLORED_ADDITIVE _COMPOSITE_TYPE_COLORED_SCREEN _COMPOSITE_TYPE_DEBUG
            
            sampler2D _CompositeTex;
            float4 _CompositeColor;

            fixed4 frag(v2f_img input) : SV_Target
            {
                float4 mainColor      = tex2D(_MainTex,      input.uv);
                float4 compositeColor = tex2D(_CompositeTex, input.uv);

                //#if defined(_COMPOSITE_TYPE_COLORED_ADDITIVE) || defined(_COMPOSITE_TYPE_COLORED_SCREEN)

                compositeColor.rgb = (compositeColor.r + compositeColor.g + compositeColor.b) * 0.3333 * _CompositeColor;

                //#endif

                //#if defined(_COMPOSITE_TYPE_SCREEN) || defined(_COMPOSITE_TYPE_COLORED_SCREEN)

                //return saturate(mainColor + compositeColor - saturate(mainColor * compositeColor));

                //#elif defined(_COMPOSITE_TYPE_ADDITIVE) || defined(_COMPOSITE_TYPE_COLORED_ADDITIVE)

                return saturate(mainColor + compositeColor);

                //#else

                //return compositeColor;

                //#endif
            }

            ENDCG
        }
    }
}