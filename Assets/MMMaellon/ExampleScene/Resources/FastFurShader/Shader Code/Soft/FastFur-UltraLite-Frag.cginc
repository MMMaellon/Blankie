

// Inverse Lerp
float invLerp(float inputA, float inputB, float input)
{
	return (inputB - inputA) == 0 ? 1e9 : (input - inputA) / (inputB - inputA);
}




fixed4 frag(fragInput i) : SV_Target
{
    if(i.FURCULLTEST > 0.25) discard;

	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    // We don't want mip maps or any other filering for the furShape, because it creates bunch of artifacts.
	// The fur already does a good job of smoothing out any high-frequency noise.
	int4 uvInt = int4(floor(frac(i.uv) * _FurShapeMap_TexelSize.zw), 0, 0);
	float4 furShape = _FurShapeMap.Load(uvInt);

	//--------------------------------------------------------------------------------
	// Apply optional height masks
	if (_FurShapeMask1Bits > 0)
	{
		float4 furMask = _FurShapeMask1.Sample(sampler_FurShapeMap, i.uv, 0);
		if (_FurShapeMask1Bits & 1) furShape.z = min(furShape.z, furMask.x);
		if (_FurShapeMask1Bits & 2) furShape.z = min(furShape.z, furMask.y);
		if (_FurShapeMask1Bits & 4) furShape.z = min(furShape.z, furMask.z);
		if (_FurShapeMask1Bits & 8) furShape.z = min(furShape.z, furMask.w);
	}
	if (_FurShapeMask2Bits > 0)
	{
		float4 furMask = _FurShapeMask2.Sample(sampler_FurShapeMap, i.uv, 0);
		if (_FurShapeMask2Bits & 1) furShape.z = min(furShape.z, furMask.x);
		if (_FurShapeMask2Bits & 2) furShape.z = min(furShape.z, furMask.y);
		if (_FurShapeMask2Bits & 4) furShape.z = min(furShape.z, furMask.z);
		if (_FurShapeMask2Bits & 8) furShape.z = min(furShape.z, furMask.w);
	}
	if (_FurShapeMask3Bits > 0)
	{
		float4 furMask = _FurShapeMask3.Sample(sampler_FurShapeMap, i.uv, 0);
		if (_FurShapeMask3Bits & 1) furShape.z = min(furShape.z, furMask.x);
		if (_FurShapeMask3Bits & 2) furShape.z = min(furShape.z, furMask.y);
		if (_FurShapeMask3Bits & 4) furShape.z = min(furShape.z, furMask.z);
		if (_FurShapeMask3Bits & 8) furShape.z = min(furShape.z, furMask.w);
	}
	if (_FurShapeMask4Bits > 0)
	{
		float4 furMask = _FurShapeMask4.Sample(sampler_FurShapeMap, i.uv, 0);
		if (_FurShapeMask4Bits & 1) furShape.z = min(furShape.z, furMask.x);
		if (_FurShapeMask4Bits & 2) furShape.z = min(furShape.z, furMask.y);
		if (_FurShapeMask4Bits & 4) furShape.z = min(furShape.z, furMask.z);
		if (_FurShapeMask4Bits & 8) furShape.z = min(furShape.z, furMask.w);
	}

#if !defined(FUR_SKIN_LAYER)
	float thicknessSample = furShape.z;
#endif


	// Density changes need to be reduced down into 33 discrete steps, otherwise the hairs will not be visible.
	float furDensity = pow(10,round(furShape.a * 32) * 0.125 - 2) * pow(_HairDensity, 3);


	// Comb the hairs (ie. shift the uv map points), then check the hair map to see if we're on a hair
	// NOTE: The next line will create visible seams on hard edges if the combing is too strong and the
	// offset goes past the overgrooming in the textures.
#if defined(FUR_SKIN_LAYER)
	float2 furUV = i.uv;
#else
	float2 combOffsetSlope = (.498 - furShape.xy) * _FurCombStrength * _FurShellSpacing * 0.1;
	float2 furUV = i.uv + (i.FUR_Z * combOffsetSlope);
#endif
	float2 strandUV = furUV * furDensity;


	// Sample the hair map
#if defined(FUR_SKIN_LAYER)
	float4 hairStrand = _HairMap.Sample(sampler_HairMap, strandUV);
#else
	// Calculate the LOD. There is a hardware function for this, but only in shader model 4.1
	float2 dx = ddx(strandUV * _HairMap_TexelSize.z);
	float2 dy = ddy(strandUV * _HairMap_TexelSize.w);
	float hairStrandLOD = max(0, (0.5 * log2(max(dot(dx, dx), dot(dy, dy)))));// + (_HairBlur - _HairSharpen));

	float4 hairStrand = _HairMap.Sample(sampler_HairMap, strandUV);

    hairStrand.b *= 1.1;// + (min(0.5, _HairTransparency) * 0.85);
    hairStrand.b *= lerp(1.15, 3.5, saturate((hairStrandLOD - 2.0) * 0.2));

    float hairEdge = hairStrand.b * thicknessSample;

	// Are we on a visible part of a hair?
	float visibleEdge = hairEdge - i.FUR_Z;
	clip(visibleEdge);
#endif


	// We have something to render, start by getting the base colour 
	float4 albedo = _MainTex.Sample(sampler_MainTex, _HideSeams > 0.5 ? i.uv.xy : furUV) * _Color;


	// Calculate per-hair tinting (not accurate, but speed takes priority)
	if (_HairColourShift != 0)
	{
		float colourShift = saturate(furShape.z * 5) * (hairStrand.g - .498) * _HairColourShift;
		float channel1 = saturate(1 - colourShift);
		float channel2 = saturate(colourShift);
		float channel3 = saturate(-colourShift);
		albedo.rgb = albedo.rgb * channel1 + albedo.brg * channel2 + albedo.gbr * channel3;
	}

	// Apply per-hair highlights (not accurate, but speed takes priority)
	if (_HairHighlights != 0)
	{
		float highlight = saturate(furShape.z * 5) * (hairStrand.r - .498) * _HairHighlights;
		float brightness = length(albedo.rgb);
		float highlighting = ((1.1 - brightness) * highlight) + 1;// darker colours are affected more, because otherwise white fur looks dirty
		albedo.rgb *= highlighting * highlighting * max(1, highlighting) * max(1, highlighting);
	}


	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Fur occlusion is based on the depth of the fur. It affects all types of light, even direct light, because
	// it's an approximation of every type of light getting partially blocked by other hairs that are in the way.
	float furOcclusion = i.FURFADEIN * saturate(furShape.z - (i.FUR_Z + _LightPenetrationDepth));
	furOcclusion = pow(1 - (furOcclusion * _DeepFurOcclusionStrength * 0.5), 2);


	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Calculate lighting
	float3 diffuseLight = i.lightData1.rgb;
	float aniosoEnergyConservation = 0;
	float anisotropic1Reflect = 0;
	float anisotropic2Refract = 0;
	if (_FurAnisotropicEnable > 0)
	{
		// Anisotropic
		float anisoBaseStrength = (i.FUR_Z < 0.001 ? _FurAnisoSkin : (saturate((i.FUR_Z + _FurAnisoDepth * _FurAnisoDepth * _FurAnisoDepth * 0.1) * 10) * ((_FurAnisoDepth * 0.35) + (1 - _FurAnisoDepth) * pow(saturate((furShape.z + 0.5) - ((furShape.z + 0.25) - i.FUR_Z)), 2)) * 5));

		anisotropic1Reflect = anisoBaseStrength * (1 + _FurAnisoReflectMetallic);
		anisotropic2Refract = anisoBaseStrength * (1 + _FurAnisoRefractMetallic);
		aniosoEnergyConservation = ((anisotropic1Reflect * _FurAnisotropicReflect * (1 + _FurAnisoReflectGloss * 0.5)) + (anisotropic2Refract * _FurAnisotropicRefract * (1 + _FurAnisoRefractGloss * 0.5))) * 0.025;

		anisoBaseStrength *= saturate(i.MAINLIGHTDOT);
		anisotropic1Reflect *= i.ANISOTROPIC1REFLECT * saturate(i.MAINLIGHTDOT);
		anisotropic2Refract *= i.ANISOTROPIC2REFRACT * saturate(i.MAINLIGHTDOT);
	}


	float lambertLight = saturate(i.MAINLIGHTDOT);
	float diffuseIntensity = saturate(lambertLight);   
	diffuseLight += _LightColor0.rgb * diffuseIntensity * (1.0 - aniosoEnergyConservation);


	if (_FurAnisotropicEnable > 0)
	{
		float3 anisoReflectColor = ((1 - _FurAnisoReflectMetallic) + (_FurAnisoReflectMetallic * albedo.rgb)) * _FurAnisotropicReflectColor;

		if (_FurAnisoReflectIridescenceStrength > 0 && anisotropic1Reflect > 0) anisoReflectColor = (anisoReflectColor * (1 - (_FurAnisoReflectIridescenceStrength * 0.05))) + (_FurAnisoReflectIridescenceStrength * 0.05 * (
			(((0.5 + albedo.rgb) * _FurAnisotropicReflectColor) * saturate(1 - abs(i.ANISOTROPICANGLE * _FurAnisoReflectIridescenceStrength))) +
			(((0.5 + albedo.brg) * _FurAnisotropicReflectColorNeg) * saturate(-i.ANISOTROPICANGLE * _FurAnisoReflectIridescenceStrength)) +
			(((0.5 + albedo.gbr) * _FurAnisotropicReflectColorPos) * saturate(i.ANISOTROPICANGLE * _FurAnisoReflectIridescenceStrength))));

		float3 aniso1ReflectColour = (_LightColor0.rgb + (anisotropic1Reflect > 0 ? _FurAnisoReflectEmission : 0)) * anisotropic1Reflect * anisoReflectColor;
		float3 aniso2RefractColour = (_LightColor0.rgb + (anisotropic2Refract > 0 ? _FurAnisoRefractEmission : 0)) * anisotropic2Refract * _FurAnisotropicRefractColor * ((1 - _FurAnisoRefractMetallic) + (_FurAnisoRefractMetallic * albedo.rgb));
		diffuseLight += (aniso1ReflectColour + aniso2RefractColour);
    }


	// Fur Occlusion affects all types of light. It is also in addition to ambient occlusion.
	diffuseLight *= furOcclusion;



	// Calculate our 'final' (sort of) colour
	float3 finalCol = albedo.rgb * diffuseLight;


	// Apply emission
#if defined(_EMISSION)
	finalCol += (tex2D(_EmissionMap, i.uv).rgb * _EmissionColor.rgb * _EmissionMapStrength);
#endif


	// Apply fog
#if defined(FUR_USE_FOG)
	UNITY_APPLY_FOG(i.fogCoord, finalCol.rgb);
#endif


    // All done!
#if defined(FUR_SKIN_LAYER)
	return(float4(finalCol.rgb * _OverallBrightness, 1));
#else

    float alpha = saturate(invLerp(hairEdge, hairEdge * (1.0 - _HairTransparency), i.FUR_Z)) * (1.0 - _HairTransparency * 0.25);

	return(float4(finalCol.rgb * _OverallBrightness, alpha));
#endif
}