Shader "Custom/DelaunayLines" 
{
	Properties     
    {
        _MainTex("Texture", 2D) = "white" {}
		_HalfRes("Half Resolution", Integer) = 0
		_AngularPairsStride("Angular Pairs Stride", Integer) = 0
		_Scale("Scale", Float) = 0
    }

	SubShader 
	{
		Pass 
		{
			Tags
			{ 
				//"Queue" = "Transparent"
				//"RenderType" = "Transparent"
				//"IgnoreProjector" = "True"
				"RenderType" = "Opaque"
			}
			//Blend SrcAlpha OneMinusSrcAlpha
			//ZWrite Off
			LOD 100
			
			CGPROGRAM
			#include "UnityCG.cginc"

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing

			struct Particle
			{
				int2 position;
				float endTime;
				float4 color;
				uint randomSeed;
				bool active;
			};

			StructuredBuffer<Particle> particlesBuffer;
			Buffer<int> angularPairBuffer;
		
			struct v2f
			{
				float4 position : SV_POSITION;
				float4 color : COLOR;
				//float2 uv: TEXCOORD0;
			};

			sampler2D _MainTex;
			int _HalfRes;
			int _AngularPairsStride;
			float _Scale;
		
			v2f vert(uint vertex_id : SV_VertexID, uint instance_id : SV_InstanceID)
			{
				v2f o; //= (v2f)0;
				
				int i = (instance_id / _AngularPairsStride);
				int j = vertex_id * (instance_id % _AngularPairsStride);
				int k = i * _AngularPairsStride + j;
				float2 xy = (particlesBuffer[angularPairBuffer[k]].position - int2(_HalfRes, _HalfRes)) * _Scale;

				//o.position = float4(instance_id, 0, 0, 1) + vertex_id * float4(1, 1, 1, 1);
				o.position = float4(xy.x, xy.y, 0, 1);
				o.position = UnityObjectToClipPos(o.position);
				o.color = float4(1, 0, 0, 1);
				
				return o;
			}

			float4 frag(v2f i) : COLOR
			{
				//fixed4 color = tex2D(_MainTex, i.uv) * i.color;
				fixed4 color = i.color;
				return color;
			}
			ENDCG
		}
	}
	FallBack Off
}
