Shader "Warren's Fast Fur/Internal Utilities/Turing Fur Markings Mutations"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _MutationRate("Mutation Rate", Range(0, 0.5)) = .25
        _Seed("Random Seed", Vector) = (0,0,0,0)
    }



        SubShader
        {
            Tags { "RenderType" = "Opaque" }

            Cull Off

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                struct meshData
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                };

                struct v2f
                {
                    float4 vertex : SV_POSITION;
                    float2 uv : TEXCOORD0;
                };

                SamplerState my_linear_repeat_sampler;

                UNITY_DECLARE_TEX2D(_MainTex);
                float4 _MainTex_TexelSize;

                float _MutationRate;
                float4 _Seed;

                v2f vert(meshData v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;
                    return o;
                }

                float random(float2 p) { return frac(cos(dot(p, float2(23.14069263277926, 2.665144142690225))) * 12345.6789); }

                fixed4 frag(v2f i) : SV_Target
                {
                    float2 offset = float2(random(i.uv * _Seed.ba) * _MainTex_TexelSize.x, random(i.uv * _Seed.ab) * _MainTex_TexelSize.y);
                    float4 col = _MainTex.Sample(my_linear_repeat_sampler, i.uv);
                    if(random(i.uv * _Seed.rg) < _MutationRate) col = col.a > 0.5 ? float4(col.rgb, 0.45) : float4(col.rgb, 0.55);
                    return(col);
                }
                ENDCG
            }
        }
}
