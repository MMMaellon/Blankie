// Perform the toon colour substitution, based on the closest colour in RGB colour space.
// RGB isn't really accurate (YUV would be better), but it's the fastest. There's a lot of
// copy-paste here, but for speed reasons I'd rather avoid loops or function calls.
float4 toonAlbedo (float4 albedo)
{
	float score = length(albedo.rgb - _ToonColour1);
	float3 chosenColour = _ToonColour1;

	float nextScore = length(albedo.rgb - _ToonColour2);
	chosenColour = nextScore > score ? chosenColour : _ToonColour2;
	score = min(score, nextScore);

	nextScore = length(albedo.rgb - _ToonColour3);
	chosenColour = nextScore > score ? chosenColour : _ToonColour3;
	score = min(score, nextScore);

	nextScore = length(albedo.rgb - _ToonColour4);
	chosenColour = nextScore > score ? chosenColour : _ToonColour4;
	score = min(score, nextScore);

	nextScore = length(albedo.rgb - _ToonColour5);
	chosenColour = nextScore > score ? chosenColour : _ToonColour5;
	score = min(score, nextScore);

	nextScore = length(albedo.rgb - _ToonColour6);
	chosenColour = nextScore > score ? chosenColour : _ToonColour6;
	score = min(score, nextScore);

	nextScore = length(albedo.rgb - _ToonColour7);
	chosenColour = nextScore > score ? chosenColour : _ToonColour7;
	score = min(score, nextScore);

	nextScore = length(albedo.rgb - _ToonColour8);
	chosenColour = nextScore > score ? chosenColour : _ToonColour8;
	score = min(score, nextScore);

	nextScore = length(albedo.rgb - _ToonColour9);
	chosenColour = nextScore > score ? chosenColour : _ToonColour9;
	albedo.rgb = chosenColour;

	// Post-effects
	/////////////////////////////////////////////////
	// Hue-shifting. To move channels, we specify each channels' new source. So to shift RGB->GBR, we multiply by albedo.brg, because the red channel's source is the blue channel, green's source is red, blue's source is green. 
	albedo.rgb = (albedo.rgb * _ToonHueRGB) + (albedo.brg * _ToonHueGBR) + (albedo.gbr * _ToonHueBRG) + (albedo.rbg * _ToonHueRBG) + (albedo.grb * _ToonHueGRB) + (albedo.bgr * _ToonHueBGR);
	// Brightness
	albedo.rgb += albedo.rgb * _ToonBrightness;
	// Whiten
	albedo.rgb = max(albedo.rgb, _ToonWhiten * 0.1);
	/////////////////////////////////////////////////

	return(albedo);
}



// Toon-lighting
float3 toonLighting (float3 lighting)
{
	float lightLevel = lighting.r * 0.30 + lighting.g * 0.59 + lighting.b * 0.11;

	if(lightLevel >= _ToonLightingHighLevel)
	{
		lighting = _ToonLightingHigh;
	}
	else if(lightLevel <= _ToonLightingShadowLevel)
	{
		lighting = _ToonLightingShadow ;
	}
	else
	{
		float split = saturate(reset(_ToonLightingShadowLevel, _ToonLightingShadowLevel + (_ToonLightingShadowSoftEdge * 0.5), 0, 1, lightLevel));
		lighting = (_ToonLightingShadow * (1 - split)) + (_ToonLightingMid * split);
		split = saturate(reset(_ToonLightingHighLevel - (_ToonLightingHighSoftEdge * 0.25), _ToonLightingHighLevel, 0, 1, lightLevel));
		lighting = (lighting * (1 - split)) + (_ToonLightingHigh * split);
	}

	return(lighting);
}
