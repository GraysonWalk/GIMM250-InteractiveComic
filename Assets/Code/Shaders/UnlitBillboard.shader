// Original shader from https://gist.github.com/kaiware007/8ebad2d28638ff83b6b74970a4f70c9a
// Adapted to URP with instructions from https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@12.0/manual/writing-shaders-urp-basic-unlit-structure.html
// And using code from https://gist.github.com/kaiware007/8ebad2d28638ff83b6b74970a4f70c9a?permalink_comment_id=4464811#gistcomment-4464811

Shader "Unlit/Billboard"
{
	Properties
	{
		[MainTexture] _BaseMap ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "DisableBatching" = "True" "RenderPipeline" = "UniversalPipeline" }

		ZWrite Off
		Cull Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			HLSLPROGRAM 
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct Attributes
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float2 uv : TEXCOORD0;
				float4 pos : SV_POSITION;
			};

			TEXTURE2D(_BaseMap);
			SAMPLER(sampler_BaseMap);

			CBUFFER_START(UnityPerMaterial)
			float4 _BaseMap_ST;
			CBUFFER_END
			
			Varyings vert (Attributes IN)
			{
				Varyings OUT;
				OUT.pos = TransformObjectToHClip(IN.vertex.xyz);
				OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);

				float3 vpos = TransformObjectToWorldDir(IN.vertex, false);
				float3 worldCoord = GetObjectToWorldMatrix()._m03_m13_m23;
				float3 viewPos = TransformWorldToView(worldCoord) + vpos;
				OUT.pos = TransformWViewToHClip(viewPos);


				return OUT;
			}
			
			half4 frag (Varyings IN) : SV_Target
			{
				half4 texel = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
				return texel;
			}
			ENDHLSL
		}
	}
}