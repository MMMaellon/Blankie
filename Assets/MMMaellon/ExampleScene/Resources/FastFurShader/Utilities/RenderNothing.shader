Shader "Warren's Fast Fur/Helper Shaders/Render Nothing"
{
	Properties
	{
		_Color("Albedo (set alpha to 0 in case of Standard shader fallback)", Color) = (0,0,0,0)
		[Enum(Cutout,1)] _Mode("Render Mode (set to Cutout in case of Standard shader fallback)", Float) = 1
	}
	SubShader
	{
		Tags {"VRCFallback" = "Hidden" "IgnoreProjector" = "True" }

		Pass
		{
			CGPROGRAM
 
			#pragma vertex vert
			#pragma fragment frag

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.pos = float4(-1e30,-1e30,-1e30,1);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				discard;
				return float4(0,0,0,0);
			}
			ENDCG
		}

		Pass {
			Tags { "LightMode" = "ShadowCaster" }
			CGPROGRAM
			ENDCG
		}
	}

}
