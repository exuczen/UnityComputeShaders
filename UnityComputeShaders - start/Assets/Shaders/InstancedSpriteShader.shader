﻿Shader "Custom/InstancedSpriteShader" 
{
	Properties     
    {
        _MainTex("Texture", 2D) = "white" {}     
    }  

	SubShader 
	{
		Pass 
		{
			Tags
			{ 
				"Queue" = "Transparent"
				"RenderType" = "Transparent"
				"IgnoreProjector" = "True"
			}
			//Blend SrcAlpha OneMinusSrcAlpha
			Blend Off
		
			CGPROGRAM
			#pragma multi_compile_instancing

			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			#define EPSILON 0.00001f

			struct InstanceData
			{
				matrix objectToWorld;
				float3 clipPosition;
				float4 color;
				float scale;
			};

			static const float ScreenRatio = _ScreenParams.y / _ScreenParams.x;

			StructuredBuffer<InstanceData> _InstancesData;

			sampler2D _MainTex;

			struct appdata
			{
				float4 vertex : POSITION;
				half2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 position : SV_POSITION;
				float4 color : COLOR;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
		
			v2f vert(appdata v, uint vertexID: SV_VertexID, uint instanceID : SV_InstanceID)
			{
				v2f o;// = (v2f)0;

				InstanceData instance = _InstancesData[instanceID];

				/* UnityObjectToClipPos must be called and used for Graphics.RenderMeshInstanced to work */
				float4 clipPos = UnityObjectToClipPos(v.vertex);

				//float2 xy = v.vertex.xy * 2;
				float2 xy = (v.texcoord - 0.5) * 2;
				xy.y *= -1;
				xy.x *= ScreenRatio;

				float3 instanceClipPos = instance.clipPosition;
				float instanceScale = instance.scale;

				o.position = float4(instanceClipPos.xy + xy * instanceScale, instanceClipPos.z, clipPos.w);
				o.color =  instance.color;
				o.uv = v.texcoord;

				return o;
			}

			float4 frag(v2f i) : COLOR
			{
				float4 color = tex2D(_MainTex, i.uv);
				color.a = step(EPSILON, color.a);

				if (color.a  < EPSILON)
				{
					discard;
				}
				else
				{
					color.rgb *= i.color.rgb;
					color.a = i.color.a;
				}
				return color;
			}
			ENDCG
		}
	}
}