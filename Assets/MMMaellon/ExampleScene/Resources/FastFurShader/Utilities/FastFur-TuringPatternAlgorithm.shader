Shader "Warren's Fast Fur/Internal Utilities/Turing Fur Markings Algorithm"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _PigmentColour("Pigment Colour", Color) = (0,0,0,1)
        _BaseColour("Base Colour", Color) = (1,1,1,1)
        _TransitionalColour("Transitional Colour", Color) = (0.5,0.5,0.5,1)
        _Contrast("Contrast", Range(1, 10)) = 10
        _ActivatorHormoneRadius("Activator Hormone Radius", Range(1, 10)) = 3
        _InhibitorHormoneAdditionalRadius("Inhibitor Hormone Radius", Range(1, 10)) = 3
        _InhibitorStrength("Inhibitor Strength", Range(0, 1)) = .3
        _XMultiplier("", Float) = 0
        _YMultiplier("", Float) = 0
        _ScanRadiusX("", Float) = 0
        _ScanRadiusY("", Float) = 0
        _Sin("", Float) = 0
        _Cos("", Float) = 0
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

            float4 _PigmentColour;
            float4 _BaseColour;
            float4 _TransitionalColour;
            float _Contrast;
            float _ActivatorHormoneRadius;
            float _InhibitorHormoneAdditionalRadius;
            float _InhibitorStrength;
            float _XMultiplier;
            float _YMultiplier;
            float _ScanRadiusX;
            float _ScanRadiusY;

            v2f vert(meshData v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float invLerp(float inputA, float inputB, float input) {
                return (input - inputA) / (inputB - inputA);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float totalRange = _ActivatorHormoneRadius + _InhibitorHormoneAdditionalRadius;
                float totalActivator = 0;
                float totalInhibit = 0;
                float score = 0;
                float startScore = _MainTex.Sample(my_linear_repeat_sampler, i.uv).a;

                for (float y = -_ScanRadiusY; y <= _ScanRadiusY; y++)
                {
                    for (float x = -_ScanRadiusX; x <= _ScanRadiusX; x++)
                    {
                        float distance = sqrt(((x / _XMultiplier) * (x / _XMultiplier)) + ((y / _YMultiplier) * (y / _YMultiplier)));
                        if(distance <= totalRange)
                        {
                            float2 xy = i.uv + float2(x * _MainTex_TexelSize.x, y * _MainTex_TexelSize.y);
                            float col = _MainTex.Sample(my_linear_repeat_sampler, xy).a;

                            if (distance <= _ActivatorHormoneRadius)
                            {
                                totalActivator++;
                                score += col >= 0.5 ? 1 : 0;
                            }
                            else
                            {
                                totalInhibit++;
                                score -= col >= 0.5 ? _InhibitorStrength : 0;
                            }
                        }
                    }
                }

                float finalScore = saturate((saturate(invLerp(-_InhibitorStrength * totalInhibit, 0, score)) + saturate(invLerp(0, totalActivator, score)) -1) * pow(2, _Contrast - 1) + 0.5);
                float pigment = saturate(finalScore - 0.5) * 2;
                float base = saturate((1 - finalScore) - 0.5) * 2;
                float transition = 1 - (pigment + base);
                return(float4((_PigmentColour.rgb * pigment) + (_BaseColour.rgb * base) + (_TransitionalColour.rgb * transition), finalScore));
            }
            ENDCG
        }
    }
}
