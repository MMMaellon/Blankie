Shader "Warren's Fast Fur/Internal Utilities/Density Check"
{
    Properties
    {

    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        Cull Off

        Pass
        {
            CGPROGRAM

            #include "UnityCG.cginc"
            #include "FastFur-Functions.cginc"

            #pragma target 4.5
            #pragma require geometry

            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            float4x4 _FurMeshMatrix;
            int _SelectedUV = 0;

            struct meshData
            {
                float4 vertex : POSITION;
                float2 uv[4] : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float  relativeDensity : TEXCOORD2;
            };

            v2f vert(meshData v)
            {
                v2f o;

                float2 uv = v.uv[_SelectedUV].xy;

                // We're rendering to a flat UV mapped texture, not the screen, so we need to pass the UV map coordinates instead of the 3D vertex position
                float x = uv.x * 2 - 1;
                float y = (1 - uv.y) * 2 - 1;
                o.pos = float4(x % 1, y % 1, 0, 1);

                o.worldPos = mul(_FurMeshMatrix, v.vertex);
                o.uv = uv;
                o.relativeDensity = 1;

                return o;
            }


            [maxvertexcount(3)]
            void geom(triangle v2f IN[3], inout TriangleStream<v2f> tristream)
            {
                v2f o = (v2f)0;

                // Calculate the relative pixel density, based upon how far away the vertexes are in the uv map compared to the world
                float relativeDensity[3];
                relativeDensity[0] = distance(IN[0].uv % 1, IN[1].uv % 1) / distance(IN[0].worldPos, IN[1].worldPos);
                relativeDensity[1] = distance(IN[0].uv % 1, IN[2].uv % 1) / distance(IN[0].worldPos, IN[2].worldPos);
                relativeDensity[2] = distance(IN[1].uv % 1, IN[2].uv % 1) / distance(IN[1].worldPos, IN[2].worldPos);
                float minRelativeDensity = 10000;
                //float totalRelativeDensity = 0;
                for (int i = 0; i < 3; i++)
                {
                    minRelativeDensity = min(minRelativeDensity, relativeDensity[i]);
                    //totalRelativeDensity += relativeDensity[i];
                }
                // Weighting to the minimum density (ie. most stretched-out UV map pixels) and ignoring the average seems to give the best overall results.
                o.relativeDensity = minRelativeDensity;

                for (i = 0; i < 3; i++)
                {
                    o.uv = IN[i].uv;
                    o.pos = IN[i].pos;
                    o.worldPos = IN[i].worldPos;
                    tristream.Append(o);
                }

                tristream.RestartStrip();
            }


            fixed4 frag(v2f i) : SV_Target
            {
                // Pack the density so that 0.01 -> 0, 1 -> 0.5, 100 -> 1
                float relativeDensity = saturate((log10(i.relativeDensity) + 2) * 0.25);
                return(fixed4(relativeDensity, relativeDensity, 1, 1));
            }

            ENDCG
        }
    }
}
