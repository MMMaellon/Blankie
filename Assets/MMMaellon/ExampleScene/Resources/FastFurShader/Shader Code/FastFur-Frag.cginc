// The fragment shader needs to figure out if it is part of a visible hair. The steps to determine this are:
//   - How thick, and what direction is the combing of the fur?
//   - Multiply by the current height and the combing to determine where to look on the hair map
//   - Is there a hair there? Is it tall enough?
//   
// Once all of the above is complete and we've confirmed that we need to render a fragment of a hair, we then
// need to apply all of the various colouring, lighting, etc...
#if !defined (PREPASS)
#include "FastFur-Function-ToonShading.cginc"
#endif


#if defined(FUR_SKIN_LAYER) && defined(FASTFUR_TWOSIDED)
fixed4 frag(fragInput i, fixed facing : VFACE) : SV_Target
#elif defined(FUR_SKIN_LAYER)
fixed4 frag(fragInput i) : SV_Target
#else
fixed4 frag(fragInput i, inout uint coverage : SV_Coverage) : SV_Target
#endif
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

// Get the furshape so we know the thickness and directionality of the fur.
float zPos = frac(i.ZDATA * 0.1) * 10;// This will actually be the skin if this is the skin layer
float skinZ = saturate(floor(i.ZDATA * 0.01) * 0.01);// This will actually be 0 if this is the skin layer

#if !defined(FUR_SKIN_LAYER)
	if (zPos > 1.05 || zPos <= skinZ + 0.002) discard;
#endif

	float4 furShape = _FurShapeMap.Sample(sampler_FurShapeMap, i.uv, 0);
	float msaaSamples = 1.0;
	if (_HairTransparency > 0) msaaSamples = GetRenderTargetSampleCount();


#if defined(SKIN_ONLY)
	furShape.z = 0;
#else
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
#endif

	float thicknessSample = furShape.z * (1 - skinZ);
	thicknessSample *= furShape.z < _FurMinHeight ? 0.0 : 1.0;


	// Density changes need to be reduced down into 33 discrete steps, otherwise the hairs will not be visible.
	float furDensity = pow(10,round(furShape.a * 32) * 0.125 - 2) * pow(_HairDensity, 3);

	// If the density has changed, that will cause a ridge. This happens because the video card will fail to calulate
	// the correct mip-map level. It will think the fur is much further away than it actually is.
	bool densityChange = !(ddx(furDensity) == 0 && ddy(furDensity) == 0);


	// Comb the hairs (ie. shift the uv map points), then check the hair map to see if we're on a hair
	// NOTE: The next line will create visible seams on hard edges if the combing is too strong and the
	// offset goes past the overgrooming in the textures.
#if defined(FUR_SKIN_LAYER)
	float2 furUV = i.uv;
#else
	float2 curls = float2(0,0);
	if (_HairCurlsActive > 0)
	{
		curls = float2(_HairCurlXTwists, _HairCurlYTwists) * 6.2832 * zPos;
		curls.y += _HairCurlXYOffset * 0.03491;
		curls = sin(curls) * float2(_HairCurlXWidth, _HairCurlYWidth) / (furDensity * 35);
	}
	float2 combOffsetSlope = (.498 - furShape.xy) * _FurCombStrength * _FurShellSpacing * 0.1;
	float2 furUV = i.uv + curls + (zPos * combOffsetSlope);
#endif
	float2 strandUV = furUV * furDensity;


	//--------------------------------------------------------------------------------
	// Sample the hair maps
	// 
	// This is not a simple task. We can't just take a sample and be done with it, because
	// the hair height will blend into mush by the time the GPU reaches mip map 2, which isn't very
	// far away. To prevent this, the shader counter-acts the effects of mip maps for the height channel.
	//
	// However, the technique to do this is different for the fine and the coarse hair maps. The fine map
	// works best with the old alpha filtering, but the coarse maps work better with the newer
	// mip-level filtering.

	// Calculate the LOD.
	float hairStrandLOD = _HairMap.CalculateLevelOfDetail(sampler_HairMap, strandUV);


	// Add some psuedo-random rotation to the hair map. This prevents visible tiling patterns, but causes some split-hair lines.
	// The 0.641 seems to be around the limit where anything less starts to show the tiling patterns, even with rotation. The
	// smaller the number, the less split-hair lines.
	float rotateHairAngle = dot(round(strandUV * 0.641), float2(1.468, 1.332));
	float hairCos = cos(rotateHairAngle);
	float hairSin = sin(rotateHairAngle);
	strandUV = mul(strandUV, float2x2(hairCos, -hairSin, hairSin, hairCos));


	// If there was a fur density change, use a default LOD of 4.0, otherwise there will be a noticable seam along the density change.
	float hairStrandLODClamped = densityChange ? 4.0 : hairStrandLOD + 0.5;

	// Sample the hair maps
	//float4 hairStrand = _HairMap.SampleBias(sampler_HairMap, strandUV, _HairBlur - _HairSharpen);
	//float4 coarseStrand = _HairMapCoarse.SampleBias(sampler_HairMap, strandUV, _HairBlur - _HairSharpen);
	float4 hairStrand = _HairMap.SampleLevel(sampler_HairMap, strandUV, hairStrandLODClamped + _HairBlur - _HairSharpen);
	hairStrand.ba = saturate(hairStrand.ba - 0.01) * 1.0101010101;
#if !defined(FUR_SKIN_LAYER)
	float4 coarseStrand = _HairMapCoarse.SampleLevel(sampler_HairMap, strandUV, hairStrandLODClamped + _HairBlur - _HairSharpen);
	coarseStrand.ba = saturate(coarseStrand.ba - 0.01) * 1.0101010101;
#else
	float4 coarseStrand = float4(0.5, 0.5, 0, 1);
#endif


	//--------------------------------------------------------------------------------
	//Sample the fur markings
	float3 furMarkings = 0;
	float hairEdge = zPos;
	float furEdge = zPos;
	float markingsMultiplier = 1;

	if (_FurMarkingsActive > 0)
	{
		// Find the UV location
		float rotateSin = sin(radians(_MarkingsRotation));
		float rotateCos = cos(radians(_MarkingsRotation));
		float2 markingsUV = furUV * (_MarkingsDensity * _MarkingsDensity);
		markingsUV = mul(markingsUV, float2x2(rotateCos, -rotateSin, rotateSin, rotateCos));

		// Take a raw sample, calculate the height offset, then multiply the colour tint
		furMarkings = tex2D(_MarkingsMap, markingsUV).rgb;
#if !defined(FUR_SKIN_LAYER)
		float markingSample = (furMarkings.r * 0.30 + furMarkings.g * 0.59 + furMarkings.b * 0.11);

		furMarkings = furMarkings.rgb * _MarkingsColour;

		// Re-scale the markingSample so that it is always 0-1
		markingSample = (markingSample - _MarkingsMapNegativeCutoff) / (_MarkingsMapPositiveCutoff - _MarkingsMapNegativeCutoff);

		// Flip it if this is a negative offset
		if (_MarkingsHeight > 0) markingSample = 1.0 - markingSample;

		// Calculate the multiplier
		markingsMultiplier = 1.0 - (markingSample * abs(_MarkingsHeight));
#endif
	}



	//--------------------------------------------------------------------------------
	// Alpha filtering
	//--------------------------------------------------------------------------------
	// Apply a multiplication to the length, because MipMaps cause hair samples to get blurred with skin samples.
	// The multiplier is calculated by dividing the hair length by the alpha channel, which is always set to 1 for hairs.
	// This isn't a perfect correction, but it still dramatically improves the render quality.
	// 
	// A side-effect of this is that normal trilinear filter will also get multiplied, which will cause the hairs to
	// appear thicker when further away. This is also a benefit, making the hairs more visible further away, even
	// though it's not technically realistic.
	// 
	// Unfortunately, all this scaling sometimes results in errors that causes "fireflies" and other random high-height
	// pixels, which we need to deal with.

	// If the calibration is lower than the length, or there was a density change, that will cause an error, so don't scale it
	//hairStrand.a = (hairStrand.a < hairStrand.b || densityChange) ? 1 : hairStrand.a;
	//coarseStrand.a = (coarseStrand.a < coarseStrand.b || densityChange) ? 1 : coarseStrand.a;
	hairStrand.a = (hairStrand.a < hairStrand.b) ? 1 : hairStrand.a;
	coarseStrand.a = (coarseStrand.a < coarseStrand.b) ? 1 : coarseStrand.a;

	// The first filter that catches a lot of these stray pixels is to have a soft half-strength cutoff at higher MipMap levels.
	// The MAX_LOD threshold is higher, up to a maximum of double its value, as we get further away.
#define PIXEL_FILTER_MAX_LOD float2(8, 8)
	float2 filter1 = max(0.5, saturate((PIXEL_FILTER_MAX_LOD * (2 - i.FURFADEIN)) - hairStrandLODClamped));
	// The second filter is to add a slight bias to our equation. The larger the bias, the less "shimmery" pixels will make the cut.
#define PIXEL_FILTER_BIAS float2(0.1, 1.0)
	// Apply the filters. Together they catch about 90% of the glitchy pixels
	float2 alphaRescale = max(1, filter1 * ((1 + PIXEL_FILTER_BIAS) / max(0.1, (float2(hairStrand.a, coarseStrand.a) + PIXEL_FILTER_BIAS))));
	// The filtering makes the hairs a bit shorter, though, so re-scale everything to roughly the same height
#define PIXEL_FILTER_RESCALE float2(0.12, 0.12)
	alphaRescale *= 1.0 + (saturate(0.9 - float2(hairStrand.b, coarseStrand.b)) * PIXEL_FILTER_RESCALE);

	// If we're far away, multiply the length of the hairs. Otherwise they are invisible at far distances.
	alphaRescale *= 2.0 - saturate(i.FURFADEIN);

	float2 alphaRescaleStrength = float2(_HairMapAlphaFilter, _HairMapCoarseAlphaFilter);
	alphaRescale = (alphaRescale * alphaRescaleStrength) + (1 - alphaRescaleStrength);

	//--------------------------------------------------------------------------------
	// Mip level filtering
	//--------------------------------------------------------------------------------
	// The mip maps will cause the hairs to shrink, so we apply a multiplier based on the mip map level.
	// The critical point where this is the most dramatic is between mip maps 3-5.
	//
	// Debugging mip map levels = 0 Red, 1 Yellow, 2 Green, 3 Cyan, 4 Blue, 5 Red, 6 Yellow, 7 Green, 8 Cyan
	#define RESCALE_MIPLEVEL0 float2( 0.0, 3.5)
	#define RESCALE_MIPLEVEL1 float2( 1.5, 4.5)
	#define RESCALE_MIPLEVEL2 float2( 5.0, 6.5)
	#define RESCALE_MIPLEVEL3 float2( 7.0,10.0)
	#define RESCALE_MULTIPLY0 float2( 1.0, -0.5)
	#define RESCALE_MULTIPLY1 float2( 1.0, 1.0)
	#define RESCALE_MULTIPLY2 float2( 1.85, 1.85)
	#define RESCALE_MULTIPLY3 float2( 1.85, 1.85)
	float2 mipRescale = lerp(RESCALE_MULTIPLY0, RESCALE_MULTIPLY1, saturate((hairStrandLODClamped - RESCALE_MIPLEVEL0) / (RESCALE_MIPLEVEL1 - RESCALE_MIPLEVEL0)));
	mipRescale += lerp(RESCALE_MULTIPLY1, RESCALE_MULTIPLY2, saturate((hairStrandLODClamped - RESCALE_MIPLEVEL1) / (RESCALE_MIPLEVEL2 - RESCALE_MIPLEVEL1)));
	mipRescale += lerp(RESCALE_MULTIPLY2, RESCALE_MULTIPLY3, saturate((hairStrandLODClamped - RESCALE_MIPLEVEL2) / (RESCALE_MIPLEVEL3 - RESCALE_MIPLEVEL2)));
	mipRescale -= (RESCALE_MULTIPLY1 + RESCALE_MULTIPLY2);

	float2 mipRescaleStrength = float2(_HairMapMipFilter, _HairMapCoarseMipFilter);
	mipRescale = (mipRescale * mipRescaleStrength) + (saturate(mipRescale) * (1 - mipRescaleStrength));


	//--------------------------------------------------------------------------------
	// Calculate the hair-length result
	float2 hairLength = float2(hairStrand.b, coarseStrand.b);
	hairLength.g *= _HairMapCoarseStrength;
	hairLength = (hairLength * alphaRescale * mipRescale * (1.0 + _HairClipping + ((1 - i.FURFADEIN) * 0.1)));


	// If MSAA transparency is active, then we want to boost the lengths of the hairs proportionally,
	// so that they overall hair length appears roughly the same.
	if (msaaSamples > 1.5)
	{
		hairLength *= 1.0 + (min(0.5, _HairTransparency) * 0.35);
	}


	float hairLengthFinal = saturate(max(hairLength.r, hairLength.g));


	// Are we on a visible part of a hair?
	furEdge = (skinZ + ((1 - skinZ) * markingsMultiplier)) * thicknessSample;
	hairEdge = furEdge * hairLengthFinal;
	float clipTest = hairEdge - zPos;

#if !defined(FUR_SKIN_LAYER)
#if defined(FUR_DEBUGGING) && defined(FORWARD_BASE_PASS)
	if (_FurDebugVerticies > 0.5)
	{
		float vertMaxDist = max(max(i.vertDist.x, i.vertDist.y), i.vertDist.z);
		float vertMinDist = min(min(i.vertDist.x, i.vertDist.y), i.vertDist.z);
		if (vertMaxDist < 0.98 && (vertMinDist > 0.02 || _FurDebugUpperLimit < 0.5 || i.vertDist.a > 1.0)) clip(clipTest);
	}
	else
	{
		float vertMinDist = min(min(i.vertDist.x, i.vertDist.y), i.vertDist.z);
		if (vertMinDist > 0.02 || _FurDebugUpperLimit < 0.5 || i.vertDist.a > 1.0) clip(clipTest);
	}
#endif
	clip(clipTest);
#endif


	float relativeZPos = saturate(furEdge > 0 ? zPos / furEdge : 1.0);

	//--------------------------------------------------------------------------------
	// Beyond this point, we're on a hair (or the skin)
	//--------------------------------------------------------------------------------

	// Grab the AudioLink bands, if needed
	float audioLinkStaticStrength = 0;
	float4 audioLinkBands = float4(0,0,0,0);
	float4 audioLinkDynamic = float4(0,0,0,0);
	bool audioLinkActive = false;
	if (_AudioLinkEnable > 0 && _AudioTexture_TexelSize.z > 16)
	{
		float2 baseCoordinates = float2(max(0, _AudioLinkStaticFiltering > 0 ? 16 - _AudioLinkStaticFiltering : 0), _AudioLinkStaticFiltering > 0 ? 28 : 0);
		audioLinkBands.r = _AudioTexture[uint2(baseCoordinates)].r;
		audioLinkBands.g = _AudioTexture[uint2(baseCoordinates + uint2(0,1))].r;
		audioLinkBands.b = _AudioTexture[uint2(baseCoordinates + uint2(0,2))].r;
		audioLinkBands.a = _AudioTexture[uint2(baseCoordinates + uint2(0,3))].r;

		// Filtering tends to make the lights dimmer, so boost the brightness.
		audioLinkBands *= (1 + (_AudioLinkStaticFiltering * 0.1));

		// Apply tip/root/skin strengths
#if defined(FUR_SKIN_LAYER)
		audioLinkStaticStrength = _AudioLinkStaticSkin;
#else
		audioLinkStaticStrength = (saturate(relativeZPos - 0.5) * 2 * _AudioLinkStaticTips) + (saturate(0.5 - abs(0.5 - relativeZPos)) * 2 * _AudioLinkStaticMiddle) + (saturate(0.5 - relativeZPos) * 2 * _AudioLinkStaticRoots);
#endif
		audioLinkBands *= audioLinkStaticStrength;

		if (_AudioLinkDynamicStrength > 0)
		{
			float maxOffset = 11.0 * (11.0 - _AudioLinkDynamicSpeed);
			float offset = maxOffset * saturate(relativeZPos);
			audioLinkDynamic = float4(
			   _AudioTexture[uint2(uint2(1 + offset, 0))].r,
			   _AudioTexture[uint2(uint2(1 + offset, 1))].r,
			   _AudioTexture[uint2(uint2(1 + offset, 2))].r,
			   _AudioTexture[uint2(uint2(1 + offset, 3))].r
			);

			if (_AudioLinkDynamicFiltering >= 1.0)
			{
				for (float x = 1; x <= _AudioLinkDynamicFiltering; x++)
				{
					audioLinkDynamic = max(audioLinkDynamic, float4(
					   _AudioTexture[uint2(uint2(1 + offset + x, 0))].r,
					   _AudioTexture[uint2(uint2(1 + offset + x, 1))].r,
					   _AudioTexture[uint2(uint2(1 + offset + x, 2))].r,
					   _AudioTexture[uint2(uint2(1 + offset + x, 3))].r
					) * (x + 2.0) / (_AudioLinkDynamicFiltering + 2.0));
				}
			}
		}

		audioLinkBands *= _AudioLinkStrength;
		audioLinkDynamic *= _AudioLinkStrength;

		audioLinkActive = true;
	}


	// Just a reminder to myself: if this is the skin layer, then the "skinZ" will actually be 0, while the
	// "zPos" will actually be where the skin is. This is intentional, since we want the skin to be lit like
	// fur if it isn't at position 0.
	float4 albedo = float4(1,1,1,1);
	float albedoMapStrength = 1;
	float markingsMapStrength = 1;
#if defined(_METALLICGLOSSMAP)
	float2 metallicMap = tex2D(_MetallicGlossMap, i.uv).ra;
	float metallic = metallicMap.r;
#else
	float metallic = _Metallic;
#endif
	float oneMinusMetallic = 1 - metallic;


	// For advanced hair colouring, figure out where we are relative to the tips of the hairs, then determine how strong
	// the root/middle/tip colouring should be.
	if (_AdvancedHairColour > 0.5 && clipTest >= 0.0)
	{
		// First, determine if the fur is thick enough to enable the advanced hair colouring. If so, how strong is it?
		float advancedStrength = saturate(((furShape.z - _HairColourMinHeight) / (1.0001 - _HairColourMinHeight)) * (2 - _HairColourMinHeight));

		// At far distances, we will need to blend the colour layers together
		float separation = saturate(1 - (hairStrandLOD * 0.125));
		float midLow = (_HairRootPoint + _HairMidLowPoint) * 0.5;
		float midHigh = (_HairMidHighPoint + _HairTipPoint) * 0.5;
		float3 blendLevels = float3(midLow, midHigh - midLow, 1 - midHigh) * (1 - separation);

		// Next, where are we on the length of the hair?
		float hairZ = saturate(saturate((zPos * hairLengthFinal) / (furShape.z + 0.0001)) * (1 + pow(1.25, max(1, hairStrandLOD))) * 0.5);

		// Determine the relative strengths of the root, middle, and tip, based on where we are on the hair
		float3 colourLevels = saturate(float3(1 - ((hairZ - _HairRootPoint) / (_HairMidLowPoint - _HairRootPoint)), 0, (hairZ - _HairMidHighPoint) / (_HairTipPoint - _HairMidHighPoint)));
		colourLevels.y = 1 - (colourLevels.x + colourLevels.z);
		colourLevels = (colourLevels * separation) + blendLevels;

		albedo.rgb = (_HairRootColour * colourLevels.x) + (_HairMidColour * colourLevels.y) + (_HairTipColour * colourLevels.z);
		albedoMapStrength = (dot(float3(_HairRootAlbedo, _HairMidAlbedo, _HairTipAlbedo), colourLevels) * advancedStrength) + (1 - advancedStrength);
		markingsMapStrength = (dot(float3(_HairRootMarkings, _HairMidMarkings, _HairTipMarkings), colourLevels) * advancedStrength) + (1 - advancedStrength);

		albedo.rgb = (albedo.rgb * advancedStrength) + (float3(1,1,1) * (1 - advancedStrength));
	}


#if defined (PREPASS)
	return(float4(0,0,0,0));
#endif
	// We have something to render, start by getting the base colour. We limit the mip map level, otherwise
	// edge fur get sparkles due to the mip map being super-high. 
#if defined(FUR_SKIN_LAYER)
	float4 albedoSample = _MainTex.Sample(sampler_MainTex, i.uv) * albedoMapStrength * _Color;
#else
	float albedoLOD = _MainTex.CalculateLevelOfDetailUnclamped(sampler_MainTex, _HideSeams ? i.uv : furUV);
	float4 albedoSample = _MainTex.SampleLevel(sampler_MainTex, _HideSeams ? i.uv : furUV, min(albedoLOD, 4)) * albedoMapStrength * _Color;
#endif
	albedo = (albedo * albedoMapStrength * albedoSample) + ((1 - albedoMapStrength) * albedo);


	// Test for cutout
#if defined (_ALPHATEST_ON)
	clip(albedo.a - _Cutoff);
#endif


	// What direction is the camera?
	float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);


	// Calculate per-hair tinting (not accurate, but speed takes priority)
	float3 extraEmission = float3(0,0,0);

	if (_ToonShading == 0)
	{
#if defined(FUR_SKIN_LAYER) && defined(FASTFUR_TWOSIDED)
		if (_HairColourShift != 0 && facing > 0.5)
#else
		if (_HairColourShift != 0)
#endif
		{
			float colourShift = saturate(furShape.z * 5) * (hairStrand.g - .498) * _HairColourShift;
			float channel1 = saturate(1 - colourShift);
			float channel2 = saturate(colourShift);
			float channel3 = saturate(-colourShift);
			albedo.rgb = albedo.rgb * channel1 + albedo.brg * channel2 + albedo.gbr * channel3;
		}

		// Apply per-hair highlights (not accurate, but speed takes priority)
#if defined(FUR_SKIN_LAYER) && defined(FASTFUR_TWOSIDED)
		if (_HairHighlights != 0 && facing > 0.5)
#else
		if (_HairHighlights != 0)
#endif
		{
			float highlight = saturate(furShape.z * 5) * (hairStrand.r - .498) * _HairHighlights;
			float brightness = length(albedo.rgb);
			float highlighting = ((1.1 - brightness) * highlight) + 1;// darker colours are affected more, because otherwise white fur looks dirty
			albedo.rgb *= highlighting * highlighting * max(1, highlighting) * max(1, highlighting);
		}
	}


	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Apply fur markings
	if (_FurMarkingsActive)
	{
		// Apply markings tinting
		float colourStrength = _MarkingsVisibility * markingsMapStrength * furShape.z * max(1, (1.5 - i.FURFADEIN));
		albedo.rgb *= (float3(1, 1, 1) * (1 - colourStrength)) + (furMarkings * colourStrength);
	}


	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Apply decals
	if (_DecalEnable)
	{
		float2 decalUV = (doRotation((_HideSeams ? i.uv : furUV) - _DecalPosition.xy, _DecalRotation) + (_DecalScale * 0.5)) / _DecalScale;
		if (_DecalTiled > 0 || (all(decalUV >= 0) && all(decalUV <= 1)))
		{
			float4 decal = UNITY_SAMPLE_TEX2D(_DecalTexture, decalUV) * _DecalColor;
			decal.rgb *= decal.a;

#if defined(FORWARD_BASE_PASS)
			float3 decalEmission = decal.rgb * _DecalEmissionStrength;
			if (audioLinkActive)
			{
				if (_DecalAudioLinkEnable0 > 0)
				{
					float2 strength = float2(_DecalAudioLinkLayers0 != 1 ? 1.0 : 0, _DecalAudioLinkLayers0 > 0.5 ? 1.0 : 0);
					decalEmission += ((strength.x * audioLinkBands.r) + (strength.y * audioLinkDynamic.r)) * _DecalAudioLinkBassColor0.rgb * decal.rgb;
					decalEmission += ((strength.x * audioLinkBands.g) + (strength.y * audioLinkDynamic.g)) * _DecalAudioLinkLowMidColor0.rgb * decal.rgb;
					decalEmission += ((strength.x * audioLinkBands.b) + (strength.y * audioLinkDynamic.b)) * _DecalAudioLinkHighMidColor0.rgb * decal.rgb;
					decalEmission += ((strength.x * audioLinkBands.a) + (strength.y * audioLinkDynamic.a)) * _DecalAudioLinkTrebleColor0.rgb * decal.rgb;
				}
				if (_DecalLumaGlowZone0 > 0) decalEmission += addLumaGlow(_DecalLumaGlowZone0, relativeZPos, audioLinkStaticStrength) * decal.rgb;
				if (_DecalLumaGlowGradient0 > 0) decalEmission += addLumaGlow(_DecalLumaGlowGradient0, relativeZPos, _AudioLinkStrength) * decal.rgb;
			}
			if (_DecalHueShift0 + _DecalHueShiftCycle0 > 0) decalEmission = colourShift(decalEmission, _DecalHueShift0, _DecalHueShiftCycle0, _DecalHueShiftRate0, audioLinkActive);
			extraEmission.rgb += decalEmission;
#endif
			if (_DecalHueShift0 + _DecalHueShiftCycle0 > 0) decal.rgb = colourShift(decal.rgb, _DecalHueShift0, _DecalHueShiftCycle0, _DecalHueShiftRate0, audioLinkActive);
			albedo.rgb = (decal.rgb * _DecalAlbedoStrength) + (albedo.rgb * (1 - (decal.a * _DecalAlbedoStrength)));
		}
	}
	if (_DecalEnable1)
	{
		float2 decalUV = (doRotation((_HideSeams ? i.uv : furUV) - _DecalPosition1.xy, _DecalRotation1) + (_DecalScale1 * 0.5)) / _DecalScale1;
		if (_DecalTiled1 > 0 || (all(decalUV >= 0) && all(decalUV <= 1)))
		{
			float4 decal = UNITY_SAMPLE_TEX2D_SAMPLER(_DecalTexture1, _DecalTexture, decalUV) * _DecalColor1;
			decal.rgb *= decal.a;

#if defined(FORWARD_BASE_PASS)
			float3 decalEmission = decal.rgb * _DecalEmissionStrength1;
			if (audioLinkActive)
			{
				if (_DecalAudioLinkEnable1 > 0)
				{
					float2 strength = float2(_DecalAudioLinkLayers1 != 1 ? 1.0 : 0, _DecalAudioLinkLayers1 > 0.5 ? 1.0 : 0);
					decalEmission += ((strength.x * audioLinkBands.r) + (strength.y * audioLinkDynamic.r)) * _DecalAudioLinkBassColor1.rgb * decal.rgb;
					decalEmission += ((strength.x * audioLinkBands.g) + (strength.y * audioLinkDynamic.g)) * _DecalAudioLinkLowMidColor1.rgb * decal.rgb;
					decalEmission += ((strength.x * audioLinkBands.b) + (strength.y * audioLinkDynamic.b)) * _DecalAudioLinkHighMidColor1.rgb * decal.rgb;
					decalEmission += ((strength.x * audioLinkBands.a) + (strength.y * audioLinkDynamic.a)) * _DecalAudioLinkTrebleColor1.rgb * decal.rgb;
				}
				if (_DecalLumaGlowZone1 > 0) decalEmission += addLumaGlow(_DecalLumaGlowZone1, relativeZPos, audioLinkStaticStrength) * decal.rgb;
				if (_DecalLumaGlowGradient0 > 1) decalEmission += addLumaGlow(_DecalLumaGlowGradient1, relativeZPos, _AudioLinkStrength) * decal.rgb;
			}
			if (_DecalHueShift1 + _DecalHueShiftCycle1 > 0) decalEmission = colourShift(decalEmission, _DecalHueShift1, _DecalHueShiftCycle1, _DecalHueShiftRate1, audioLinkActive);
			extraEmission += decalEmission;
#endif
			if (_DecalHueShift1 + _DecalHueShiftCycle1 > 0) decal.rgb = colourShift(decal.rgb, _DecalHueShift1, _DecalHueShiftCycle1, _DecalHueShiftRate1, audioLinkActive);
			albedo.rgb = (decal.rgb * _DecalAlbedoStrength1) + (albedo.rgb * (1 - (decal.a * _DecalAlbedoStrength1)));
		}
	}
	if (_DecalEnable2)
	{
		float2 decalUV = (doRotation((_HideSeams ? i.uv : furUV) - _DecalPosition2.xy, _DecalRotation2) + (_DecalScale2 * 0.5)) / _DecalScale2;
		if (_DecalTiled2 > 0 || (all(decalUV >= 0) && all(decalUV <= 1)))
		{
			float4 decal = UNITY_SAMPLE_TEX2D_SAMPLER(_DecalTexture2, _DecalTexture, decalUV) * _DecalColor2;
			decal.rgb *= decal.a;

#if defined(FORWARD_BASE_PASS)
			float3 decalEmission = decal.rgb * _DecalEmissionStrength2;
			if (audioLinkActive)
			{
				if (_DecalAudioLinkEnable2 > 0)
				{
					float2 strength = float2(_DecalAudioLinkLayers2 != 1 ? 1.0 : 0, _DecalAudioLinkLayers2 > 0.5 ? 1.0 : 0);
					decalEmission += ((strength.x * audioLinkBands.r) + (strength.y * audioLinkDynamic.r)) * _DecalAudioLinkBassColor2.rgb * decal.rgb;
					decalEmission += ((strength.x * audioLinkBands.g) + (strength.y * audioLinkDynamic.g)) * _DecalAudioLinkLowMidColor2.rgb * decal.rgb;
					decalEmission += ((strength.x * audioLinkBands.b) + (strength.y * audioLinkDynamic.b)) * _DecalAudioLinkHighMidColor2.rgb * decal.rgb;
					decalEmission += ((strength.x * audioLinkBands.a) + (strength.y * audioLinkDynamic.a)) * _DecalAudioLinkTrebleColor2.rgb * decal.rgb;
				}
				if (_DecalLumaGlowZone2 > 0) decalEmission += addLumaGlow(_DecalLumaGlowZone2, relativeZPos, audioLinkStaticStrength) * decal.rgb;
				if (_DecalLumaGlowGradient2 > 0) decalEmission += addLumaGlow(_DecalLumaGlowGradient2, relativeZPos, _AudioLinkStrength) * decal.rgb;
			}
			if (_DecalHueShift2 + _DecalHueShiftCycle2 > 0) decalEmission = colourShift(decalEmission, _DecalHueShift2, _DecalHueShiftCycle2, _DecalHueShiftRate2, audioLinkActive);
			extraEmission += decalEmission;
#endif
			if (_DecalHueShift2 + _DecalHueShiftCycle2 > 0) decal.rgb = colourShift(decal.rgb, _DecalHueShift2, _DecalHueShiftCycle2, _DecalHueShiftRate2, audioLinkActive);
			albedo.rgb = (decal.rgb * _DecalAlbedoStrength2) + (albedo.rgb * (1 - (decal.a * _DecalAlbedoStrength2)));
		}
	}
	if (_DecalEnable3)
	{
		float2 decalUV = (doRotation((_HideSeams ? i.uv : furUV) - _DecalPosition3.xy, _DecalRotation3) + (_DecalScale3 * 0.5)) / _DecalScale3;
		if (_DecalTiled3 > 0 || (all(decalUV >= 0) && all(decalUV <= 1)))
		{
			float4 decal = UNITY_SAMPLE_TEX2D_SAMPLER(_DecalTexture3, _DecalTexture, decalUV) * _DecalColor3;
			decal.rgb *= decal.a;

#if defined(FORWARD_BASE_PASS)
			float3 decalEmission = decal.rgb * _DecalEmissionStrength3;
			if (audioLinkActive)
			{
				if (_DecalAudioLinkEnable3 > 0)
				{
					float2 strength = float2(_DecalAudioLinkLayers3 != 1 ? 1.0 : 0, _DecalAudioLinkLayers3 > 0.5 ? 1.0 : 0);
					decalEmission += ((strength.x * audioLinkBands.r) + (strength.y * audioLinkDynamic.r)) * _DecalAudioLinkBassColor3.rgb * decal.rgb;
					decalEmission += ((strength.x * audioLinkBands.g) + (strength.y * audioLinkDynamic.g)) * _DecalAudioLinkLowMidColor3.rgb * decal.rgb;
					decalEmission += ((strength.x * audioLinkBands.b) + (strength.y * audioLinkDynamic.b)) * _DecalAudioLinkHighMidColor3.rgb * decal.rgb;
					decalEmission += ((strength.x * audioLinkBands.a) + (strength.y * audioLinkDynamic.a)) * _DecalAudioLinkTrebleColor3.rgb * decal.rgb;
				}
				if (_DecalLumaGlowZone3 > 0) decalEmission += addLumaGlow(_DecalLumaGlowZone3, relativeZPos, audioLinkStaticStrength) * decal.rgb;
				if (_DecalLumaGlowGradient3 > 0) decalEmission += addLumaGlow(_DecalLumaGlowGradient3, relativeZPos, _AudioLinkStrength) * decal.rgb;
			}
			if (_DecalHueShift3 + _DecalHueShiftCycle3 > 0) decalEmission = colourShift(decalEmission, _DecalHueShift3, _DecalHueShiftCycle3, _DecalHueShiftRate3, audioLinkActive);
			extraEmission += decalEmission;
#endif
			if (_DecalHueShift3 + _DecalHueShiftCycle3 > 0) decal.rgb = colourShift(decal.rgb, _DecalHueShift3, _DecalHueShiftCycle3, _DecalHueShiftRate3, audioLinkActive);
			albedo.rgb = (decal.rgb * _DecalAlbedoStrength3) + (albedo.rgb * (1 - (decal.a * _DecalAlbedoStrength3)));
		}
	}



	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Fur occlusion is based on the depth of the fur. It affects all types of light, even direct light, because
	// it's an approximation of every type of light getting partially blocked by other hairs that are in the way.
	float furOcclusion = i.FURFADEIN * saturate(furShape.z - (zPos + _LightPenetrationDepth));
	furOcclusion = pow(1 - (furOcclusion * _DeepFurOcclusionStrength * 0.5), 2);

	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Ambient occlusion is based on the occlusion map, further modified by proximity occlusion.
	float ambientOcclusion = 1;
#if defined(FORWARD_BASE_PASS)
	if (_OcclusionMap_TexelSize.z > 16)
	{
		ambientOcclusion = (1.0 - saturate(_OcclusionStrength)) + (pow(_OcclusionMap.Sample(sampler_OcclusionMap, i.uv).r, max(1.0, _OcclusionStrength)) * saturate(_OcclusionStrength));
	}

	// When the camera is close, add some more ambient occlusion
	if (_ProximityOcclusion > 0)
	{
		float range = length(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);
		if (range < _ProximityOcclusionRange)
		{
			float strength = ((_ProximityOcclusionRange - range) / _ProximityOcclusionRange) * _ProximityOcclusion;
			ambientOcclusion *= (1.0 - strength) * (1.0 - strength);
		}
	}
#endif



	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Calculate lighting
	float3 diffuseLight = i.lightData1.rgb * ambientOcclusion;

#if defined(POINT) || defined(SPOT) || (defined(FUR_USE_SHADOWS) && (defined(SHADOWS_SCREEN) || defined(SHADOWS_DEPTH) || defined(SHADOWS_CUBE)))
	UNITY_LIGHT_ATTENUATION(attenuation, i, i.worldPos.xyz); // This Unity macro also checks for shadows.
#else
	float attenuation = 1;
#endif

	float3 worldLight = min(_LightColor0.rgb, _MaxBrightness) * (1.0 - (_SoftenShadows * 0.1));
	bool lightOn = any(_LightColor0 >= 0.002) && _LightColor0.a >= 0.002;

	float brightness = (worldLight.r * 0.30 + worldLight.g * 0.59 + worldLight.b * 0.11);
	if (brightness > 0.75 && _SoftenBrightness > 0)
	{
		float newBrightness = (brightness * (1 - _SoftenBrightness)) + ((brightness / max(1, brightness * 0.75 + 0.5)) * _SoftenBrightness);
		worldLight *= newBrightness / brightness;
		brightness = newBrightness;
	}

	float3 worldLightDir = _WorldSpaceLightPos0.xyz;
	if (!lightOn && _FallbackLightEnable > 0)
	{
		worldLight = float3(_FallbackLightColor.rgb) * _FallbackLightStrength;
		float lightYCos = cos(radians(_FallbackLightAngle));
		worldLightDir = normalize(float3(sin(radians(_FallbackLightDirection)) * lightYCos, -sin(radians(_FallbackLightAngle)), cos(radians(_FallbackLightDirection)) * lightYCos));
		lightOn = true;
	}

	if (_WorldLightReColourStrength > 0)
	{
		worldLight = (worldLight * (1 - _WorldLightReColourStrength)) + (_WorldLightReColour.rgb * _WorldLightReColourStrength * brightness);
	}

	float anisotropic1Reflect = 0;
	float anisotropic2Refract = 0;

	if (lightOn)
	{
#if defined(POINT) || defined(SPOT)
		worldLightDir = normalize(worldLightDir - i.worldPos.xyz);
#endif

		// Lambertian light
		float wrapScale = _LightWraparound * 0.01;
		float lambertLight = saturate((i.MAINLIGHTDOT * (1.0 - wrapScale) * (oneMinusMetallic + (2.0 * metallic * pow(saturate(i.MAINLIGHTDOT), 1.0 + metallic * 15.0)))) + wrapScale);

#if !defined(FUR_SKIN_LAYER)
		// Brighten hair tips that are sticking out and catching the light
		float brightenTips = (_LightWraparound * 0.09 + i.MAINLIGHTDOT + pow(zPos, 1.5) - 1);// Brighten tips, leave the rest of the hair shadowed
		brightenTips *= max(0, 0.5 - i.MAINLIGHTDOT) * 0.15 * saturate(i.FURFADEIN + 0.5);// Multiply the brightening by how perpendicular the hair is to the light direction
		brightenTips = saturate(_LightWraparound * brightenTips);

#else
		float brightenTips = 0;
#endif
		// Subsurface scattering
		float scatterStrength = i.SUBSURFACESTRENGTH * (1 - (hairEdge - zPos));


		// Base diffuse intensity
		float diffuseIntensity = saturate(lambertLight + brightenTips) + scatterStrength;


		float aniosoEnergyConservation = 0;
		if (_FurAnisotropicEnable > 0)
		{
			// Anisotropic
			//float anisoBaseStrength = attenuation * (zPos < 0.001 ? _FurAnisoSkin : (saturate((zPos + _FurAnisoDepth * _FurAnisoDepth * _FurAnisoDepth * 0.1) * 10) * ((_FurAnisoDepth * 0.35) + (1 - _FurAnisoDepth) * pow(saturate((furShape.z + 0.5) - ((furShape.z + 0.25) - zPos)), 2)) * 5));
			float anisoBaseStrength = (zPos < 0.001 ? _FurAnisoSkin : (saturate((zPos + _FurAnisoDepth * _FurAnisoDepth * _FurAnisoDepth * 0.1) * 10) * ((_FurAnisoDepth * 0.35) + (1 - _FurAnisoDepth) * pow(saturate((furShape.z + 0.5) - ((furShape.z + 0.25) - zPos)), 2)) * 5));
			anisoBaseStrength = (i.FURFADEIN * anisoBaseStrength) + ((1.0 - i.FURFADEIN) * 0.75);
			anisoBaseStrength *= attenuation;

			anisotropic1Reflect = anisoBaseStrength * (1 + _FurAnisoReflectMetallic);
			anisotropic2Refract = anisoBaseStrength * (1 + _FurAnisoRefractMetallic);
			aniosoEnergyConservation = ((anisotropic1Reflect * _FurAnisotropicReflect * (1 + _FurAnisoReflectGloss * 0.5)) + (anisotropic2Refract * _FurAnisotropicRefract * (1 + _FurAnisoRefractGloss * 0.5))) * 0.025;

			anisoBaseStrength *= //saturate(i.MAINLIGHTDOT);
			anisotropic1Reflect *= i.ANISOTROPIC1REFLECT * saturate(i.MAINLIGHTDOT);
			anisotropic2Refract *= i.ANISOTROPIC2REFRACT * saturate(i.MAINLIGHTDOT);
		}
		diffuseLight += worldLight * diffuseIntensity * attenuation * (1.0 - aniosoEnergyConservation);
		//diffuseLight += worldLight * diffuseIntensity * attenuation * (((1.0 - aniosoEnergyConservation) * (1.0 - _TS1)) + _TS1);
	}
	// Fur Occlusion affects all types of light: ambient, direct, and anisotropic. It is also in addition to ambient occlusion.
	diffuseLight *= furOcclusion;

	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Apply Toon Shading
	if (_ToonShading > 0)
	{
		albedo = toonAlbedo(albedo);
		if (_ToonLighting > 0)
		{
			if (_FurAnisotropicEnable > 0)
			{
				float3 aniso1ReflectColour = worldLight * anisotropic1Reflect * _FurAnisotropicReflectColor * ((1 - _FurAnisoReflectMetallic) + (_FurAnisoReflectMetallic * albedo.rgb));
				float3 aniso2RefractColour = worldLight * anisotropic2Refract * _FurAnisotropicRefractColor * ((1 - _FurAnisoRefractMetallic) + (_FurAnisoRefractMetallic * albedo.rgb));
				diffuseLight += (aniso1ReflectColour + aniso2RefractColour) * 3;
			}
			diffuseLight = toonLighting(diffuseLight);
		}
	}


	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Skin-layer
#if defined(FUR_SKIN_LAYER)
	float3 PBSCol = float3(0,0,0);
	float PBSBlend = 0.0;
	float4 matCapCol = float4(0,0,0,0);
	float matCapMask = 0.0;
	float PBSCutoff = clipTest > 0.0 ? -1.0 : _PBSSkin;

	if (_PBSSkinStrength > 0 && PBSCutoff * 1.55 > furShape.z) // 1.55 ensures that this is always calculated when needed, while reducing by ~95% calculating when not needed
	{
		// This fancy looking equation makes more sense when graphed. PBSCutoff 0.0 is always fully off. PBSCutoff 1.0 is always fully on.
		// PBSCutoff 0.59 is fully on for any fur below half-height, and then gradually fades to fully off for full-height fur.
		PBSBlend = _PBSSkinStrength * saturate((1.0 - (furShape.z / tan(min(i.FURFADEIN + 0.1, PBSCutoff * 0.785398)))) + 1.0);// 0.785398 = PI / 4

#if defined(_NORMALMAP)
		float3 worldBinormal = cross(i.worldNormal, i.worldTangent.xyz) * (i.worldTangent.w * unity_WorldTransformParams.w);
		float3 normalMap = UnpackScaleNormal(tex2D(_BumpMap, i.uv), _BumpScale);
		normalMap = normalize((normalMap.x * i.worldTangent) + (normalMap.y * worldBinormal) + (normalMap.z * i.worldNormal));
#else
		float3 normalMap = normalize(i.worldNormal);
#endif

		/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// MatCap
		if (_MatcapEnable > 0.0)
		{
			if (_MatcapTextureChannel == 0) { matCapMask = 1.0; }
			else if (_MatcapTextureChannel == 1) { matCapMask = tex2D(_MatcapMask, i.uv).r; }
			else if (_MatcapTextureChannel == 2) { matCapMask = tex2D(_Matcap, i.uv).a; }
			else if (_MatcapTextureChannel == 3) { matCapMask = albedo.a; }
#if defined(_METALLICGLOSSMAP)
			else if (_MatcapTextureChannel == 4 || _MatcapTextureChannel == 5)
			{
				matCapMask = _MatcapTextureChannel == 4 ? metallicMap.y : metallicMap.r;
			}
#endif
			matCapMask *= PBSBlend;

			if (matCapMask > 0.0)
			{
				float3 viewDirUp = normalize(float3(0, 1, 0) - (viewDir * dot(viewDir, float3(0, 1, 0))));
				float3 viewDirRight = normalize(cross(viewDirUp, viewDir));

				float2 matCapUV = float2(dot(viewDirRight, normalMap), dot(viewDirUp, normalMap));
				matCapUV = (_MatcapSpecular * matCapUV) + ((1.0 - _MatcapSpecular) * i.worldNormal.xy);
				matCapUV = matCapUV * 0.5 + 0.5;

				matCapCol = tex2D(_Matcap, matCapUV) * _MatcapColor;
				albedo.rgb += matCapCol.rgb * _MatcapAdd * matCapMask;
				albedo.rgb = (albedo.rgb * (1.0 - (_MatcapReplace * matCapMask))) + (matCapCol.rgb * _MatcapReplace * matCapMask);
			}
		}


		// This is the skin, so apply the albedo hue shifting here
		if (_AlbedoHueShift + _AlbedoHueShiftCycle > 0) albedo.rgb = colourShift(albedo.rgb, _AlbedoHueShift, _AlbedoHueShiftCycle, _AlbedoHueShiftRate, audioLinkActive);


		/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// PBS Skin

#if defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A)
// REMOVED in RC.1 //		float smoothness = (_InvertSmoothness > 0.5 ? 1.0 - _GlossMapScale : _GlossMapScale) * albedo.a;
		float smoothness = albedo.a * _GlossMapScale;
#elif defined(_METALLICGLOSSMAP)
// REMOVED in RC.1 //		float smoothness = (_InvertSmoothness > 0.5 ? 1.0 - _GlossMapScale : _GlossMapScale) * metallicMap.y;
		float smoothness = metallicMap.y * _GlossMapScale;
#else
// REMOVED in RC.1 //		float smoothness = (_InvertSmoothness > 0.5 ? 1.0 - _Glossiness : _Glossiness);
		float smoothness = _Glossiness;
#endif

		float3 specularTint;
		float oneMinusReflectivity;
		float3 PBSAlbedo = DiffuseAndSpecularFromMetallic(albedo.rgb, metallic, specularTint, oneMinusReflectivity);

		UnityLight light;
		light.color = worldLight * attenuation * furOcclusion;
		light.dir = worldLightDir;
		light.ndotl = DotClamped(normalMap, light.dir);

		UnityIndirect indirectLight;
		indirectLight.diffuse = i.lightData1.rgb * ambientOcclusion * furOcclusion;
#if defined(FORWARD_BASE_PASS) && !defined(_GLOSSYREFLECTIONS_OFF)
		float3 reflectDir = reflect(-viewDir, normalMap);
		float4 reflectSample = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, reflectDir);
		indirectLight.specular = DecodeHDR(reflectSample, unity_SpecCube0_HDR) * ambientOcclusion * furOcclusion;
#else
		indirectLight.specular = 0;
#endif

		PBSCol = UNITY_BRDF_PBS(PBSAlbedo, specularTint, oneMinusReflectivity, smoothness, normalMap, viewDir, light, indirectLight);
	}
	else
#endif
	// If this isn't the skin, then apply the albedo hue shifting here
	if (_AlbedoHueShift + _AlbedoHueShiftCycle > 0) albedo.rgb = colourShift(albedo.rgb, _AlbedoHueShift, _AlbedoHueShiftCycle, _AlbedoHueShiftRate, audioLinkActive);


	// Metallic fur reflections
#if defined(FORWARD_BASE_PASS) && !defined(_GLOSSYREFLECTIONS_OFF)
	float3 reflectDir = reflect(-viewDir, i.worldNormal.xyz);
	float4 reflectSample = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, reflectDir);
	float3 reflectedLight = DecodeHDR(reflectSample, unity_SpecCube0_HDR) * ambientOcclusion * furOcclusion * metallic;
#else
	float3 reflectedLight = 0;
#endif


	// Calculate our 'final' (sort of) colour
	float3 finalCol = albedo.rgb * (diffuseLight * (1 + metallic) + reflectedLight);
#if defined(FUR_SKIN_LAYER)
	finalCol = (finalCol * (1.0 - PBSBlend)) + (PBSCol * PBSBlend);
#endif
	if (_FurAnisotropicEnable > 0 && (_ToonLighting < 0.5 || _ToonShading < 0.5))
	{
		float3 anisoReflectColor = ((1 - _FurAnisoReflectMetallic) + (_FurAnisoReflectMetallic * albedo.rgb)) * _FurAnisotropicReflectColor;

		if (_FurAnisoReflectIridescenceStrength > 0 && anisotropic1Reflect > 0) anisoReflectColor = (anisoReflectColor * (1 - (_FurAnisoReflectIridescenceStrength * 0.05))) + (_FurAnisoReflectIridescenceStrength * 0.05 * (
			(((0.5 + albedo.rgb) * _FurAnisotropicReflectColor) * saturate(1 - abs(i.ANISOTROPICANGLE * _FurAnisoReflectIridescenceStrength))) +
			(((0.5 + albedo.brg) * _FurAnisotropicReflectColorNeg) * saturate(-i.ANISOTROPICANGLE * _FurAnisoReflectIridescenceStrength)) +
			(((0.5 + albedo.gbr) * _FurAnisotropicReflectColorPos) * saturate(i.ANISOTROPICANGLE * _FurAnisoReflectIridescenceStrength))));

		float3 aniso1ReflectColour = (worldLight + (anisotropic1Reflect > 0 ? _FurAnisoReflectEmission : 0)) * anisotropic1Reflect * anisoReflectColor;
		float3 aniso2RefractColour = (worldLight + (anisotropic2Refract > 0 ? _FurAnisoRefractEmission : 0)) * anisotropic2Refract * _FurAnisotropicRefractColor * ((1 - _FurAnisoRefractMetallic) + (_FurAnisoRefractMetallic * albedo.rgb));
		finalCol += aniso1ReflectColour + aniso2RefractColour;


		//finalCol += (aniso1ReflectColour + aniso2RefractColour) * (1 - _TS1);

		//float3 specular = _TS1 * i.specularLight.rgb * (0.2 + zPos * 0.8) * (_TS3 + albedo.rgb);
		//float energyConservation = (specular.r * 0.30 + specular.g * 0.59 + specular.b * 0.11);
		//finalCol.rgb = finalCol.rgb * (1 - energyConservation) + specular;

	}


	// Apply emission
#if defined(FORWARD_BASE_PASS)
#if defined(_EMISSION)
	float3 emissionMap = tex2D(_EmissionMap, _HideSeams ? i.uv : furUV).rgb * _EmissionColor.rgb;
	float3 emissionColour = emissionMap * _EmissionMapStrength;

	if (audioLinkActive)
	{
		if (_EmissionMapAudioLinkEnable > 0)
		{
			float2 strength = float2(_EmissionMapAudioLinkLayers != 1 ? 1.0 : 0, _EmissionMapAudioLinkLayers > 0.5 ? 1.0 : 0);
			emissionColour += ((strength.x * audioLinkBands.r) + (strength.y * audioLinkDynamic.r)) * _EmissionMapAudioLinkBassColor.rgb * emissionMap.rgb;
			emissionColour += ((strength.x * audioLinkBands.g) + (strength.y * audioLinkDynamic.g)) * _EmissionMapAudioLinkLowMidColor.rgb * emissionMap.rgb;
			emissionColour += ((strength.x * audioLinkBands.b) + (strength.y * audioLinkDynamic.b)) * _EmissionMapAudioLinkHighMidColor.rgb * emissionMap.rgb;
			emissionColour += ((strength.x * audioLinkBands.a) + (strength.y * audioLinkDynamic.a)) * _EmissionMapAudioLinkTrebleColor.rgb * emissionMap.rgb;
		}
		if (_EmissionMapLumaGlowZone > 0) emissionColour += addLumaGlow(_EmissionMapLumaGlowZone, relativeZPos, audioLinkStaticStrength) * emissionMap.rgb;
		if (_EmissionMapLumaGlowGradient > 0) emissionColour += addLumaGlow(_EmissionMapLumaGlowGradient, relativeZPos, _AudioLinkStrength) * emissionMap.rgb;
	}

	if (_AlbedoEmission > 0) emissionColour += albedo.rgb * _AlbedoEmission;
	if (_EmissionHueShift + _EmissionHueShiftCycle > 0) emissionColour = colourShift(emissionColour, _EmissionHueShift, _EmissionHueShiftCycle, _EmissionHueShiftRate, audioLinkActive);

	finalCol += emissionColour;
#endif
#if defined(FUR_SKIN_LAYER)
	finalCol += matCapCol * matCapMask * _MatcapEmission;
#endif
	finalCol += extraEmission;
#endif


	// Apply fog
	UNITY_APPLY_FOG(i.fogCoord, finalCol.rgb);


	// Apply back-facing colour
#if defined(FUR_SKIN_LAYER) && defined(FASTFUR_TWOSIDED)
	if (facing < 0.5)
	{
		finalCol.rgb = (finalCol.rgb * _BackfaceColor.rgb) + _BackfaceEmission.rgb;
	}
#endif


	// Apply debugging colours
#if defined(FUR_DEBUGGING)
	// If debugging is on, limit the brightness so that it doesn't wash out the debugging colours
	finalCol = saturate(finalCol);
#endif
#if defined(FUR_DEBUGGING) && defined(FORWARD_BASE_PASS)
	float3 debugColour[12] = {
		float3(1  ,0  ,0),// Editor
		float3(1  ,0.5,0),// VR
		float3(1  ,1  ,0),// Desktop
		float3(0.5,1  ,0),
		float3(0  ,1  ,0),// VR Viewfinder
		float3(0  ,1  ,0.5),
		float3(0  ,1  ,1),// Desktop Viewfinder
		float3(0  ,0.5,1),
		float3(0  ,0  ,1),// Camera Photo
		float3(0.5,0  ,1),
		float3(1  ,0  ,1),// Screenshot
		float3(1  ,0  ,0.5)// Stream Camera
	};
	finalCol = _FurDebugDistance && i.VISIBLELAYERS > 0 ? debugColour[floor(i.VISIBLELAYERS + 1.001) % 12] * .25 + finalCol * .75 : finalCol;
	finalCol = _FurDebugMipMap ? debugColour[(((int)hairStrandLOD) * 2) % 10] * .25 + finalCol * .75 : finalCol;
	finalCol = _FurDebugHairMap && clipTest > 0 ? (hairLengthFinal > hairLength.x ? (hairLengthFinal > hairLength.y ? debugColour[8] * .25 + finalCol * .75 : debugColour[4] * .25 + finalCol * .75) : debugColour[0] * .25 + finalCol * .75) : finalCol;
#if !defined(FUR_SKIN_LAYER)
	finalCol = _FurDebugDepth ? debugColour[11 * (i.vertDist.a / i.VISIBLELAYERS)] * .25 + finalCol * .75 : finalCol;
#endif
	finalCol = _FurDebugLength ? debugColour[(uint)round(furShape.z * 64) % (uint)12] * .25 + finalCol * .75 : finalCol;
	finalCol = _FurDebugDensity ? debugColour[(uint)round(furShape.a * 32) % (uint)12] * .25 + finalCol * .75 : finalCol;
	finalCol = _FurDebugCombing ? float3(furShape.rg, 0) * 0.75 + finalCol * 0.25 : finalCol;
	if (_FurDebugQuality)
	{
	#if defined (USING_STEREO_MATRICES)
		float quality = _V4QualityVR;
	#else
		float quality = _VRChatCameraMode < -0.5 ? _V4QualityEditor : _V4Quality2D;
		if (_VRChatMirrorMode > 0.5)
		{
			quality = _VRChatMirrorMode > 1.5 ? _V4Quality2DMirror : _V4QualityVRMirror;
		}
		else
		{
			if (_VRChatCameraMode > 2.5)
			{
				quality = _V4QualityScreenshot;
			}
			else if (_VRChatCameraMode > 0.5)
			{
				quality = (_ScreenParams.y == 720) ? _V4QualityCameraView : (_ScreenParams.y == 1080) ? _V4QualityCameraPhoto : _V4QualityStreamCamera;
			}
		}
	#endif
		if (quality > 9) quality -= 2;
		if (quality > 9) quality -= 2;
		finalCol = (debugColour[quality % 12] * .25) + (finalCol * .75);
	}

#if !defined(FUR_SKIN_LAYER)
	if (_FurDebugVerticies)
	{
		float vertDist = max(max(i.vertDist.x, i.vertDist.y), i.vertDist.z);
		finalCol.rgb = (vertDist < 0.98 ? finalCol.rgb : float3(0.5, zPos > 0.98 ? 0 : 0.5, 0.5));
	}
#endif

#if defined(USING_STEREO_MATRICES)
	if(_FurDebugStereo > 0.5) finalCol += unity_StereoEyeIndex > 0.5 ? float3(0.1,0,0) : float3(0,0,0.1);
#endif

	if(_FurDebugContact > 0.5)
	{
		float contactActive = saturate(((_FurTouchThreshold + _OcclusionMap.Sample(sampler_OcclusionMap, i.uv).r) - 1.0) * 10.0);
		float3 contactColour = float3((1.0 - contactActive) * 0.25, contactActive * 0.25, (i.lightData1.b >= 1.00) ? 0.5 : 0.0);

		// If the _CameraDepthTexture isn't valid, create a checkerboard pattern
		if(SAMPLE_RAW_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(0.5, 0.5, 4, 0)).b > 0.01)
		{
			bool checkerBoard = ((i.pos.x % 64 >= 32) == (i.pos.y % 64 >= 32)) ? 0.5 : 0.0;
			if(checkerBoard) contactColour = (contactColour * 0.25) + float3(0.5, 0, 0.0);
		}

		finalCol = (finalCol * 0.5) + contactColour;
	}

#endif // FUR_DEBUGGING && FORWARD_BASE_PASS


#if !defined(FUR_SKIN_LAYER)
	// If MSAA is being used, then we can set the sub-pixel coverage manually, which effectively
	// gives us levels of transparency! The hairs can't use the MSAA anyway, since MSAA only helps
	// with smoothing geometric edges, and the hairs are not polygons.
	if (msaaSamples > 1.5)
	{
		float alpha = invLerp(hairEdge, hairEdge * (1.0 - (_HairTransparency * 0.9)), zPos);

		// This isn't 'real' transparency, since the layers don't blend together. So we add
		// a little extra alpha for 'dark' hairs, to break up the transparency levels.
		// 0.33 is the cutoff for 'dark' hairs, 2.5 is how much of a boost they get.
		alpha *= (1.0 + max(0.0, (0.33 - hairStrand.r) * 2.5));

		// Apply some dithering up-close to reduce banding.
		float2 ditherMask = (int)(i.uv * 8192.0) & 1;
		float dither = (ditherMask.x + (ditherMask.y * -2.0) + 0.5) * saturate(relativeZPos * (i.FURFADEIN - 0.80) * 5.0);
		alpha += dither / msaaSamples;

		// Because the fake transparency doesn't stack, darken it further the deeper the fur is.
		alpha += saturate(furEdge - zPos) * 0.25;

		// If there are only 2 samples, then we try to fake an intermediate level by having 0->0.5 
		// be the first sub-pixel, then 0.5->0.75 switches to the other sub-pixel, then 0.75->1.0 is both.
		// The idea is that the first two levels can combine to full coverage. Visually, this creates a
		// crossover area that is sometimes darker, sometimes lighter, depending on overlap.
		if (msaaSamples == 2) coverage = max(1, alpha * 3.99);

		// For any other number of samples, we simply turn on a percentage of the sub-pixels based on alpha.
		// Doing more complex blending can create smoother bands, but it also creates artifacts as well.
		else coverage = (2 << (uint)(alpha * msaaSamples * 0.999)) - 1;
	}
#endif


	return(float4(finalCol.rgb * _OverallBrightness, 1));
}
