// This is usually called a 'remap' in most shader code, but I work in HVAC and this
// is a 'reset' to me, so I'm sticking with what I know. ;)

float reset(float inputA, float inputB, float outputA, float outputB, float input)
{
	return lerp(outputA, outputB, (input - inputA) / (inputB - inputA));
}


// Inverse Lerp
float invLerp(float inputA, float inputB, float input)
{
	return (inputB - inputA) == 0 ? 1e9 : (input - inputA) / (inputB - inputA);
}


// 2D Rotation
float2 doRotation(float2 xy, float angle)
{
	float myRadians = radians(angle);

	float mySin = sin(myRadians);
	float myCos = cos(myRadians);

	return(float2(xy.x * myCos - xy.y * mySin, xy.x * mySin + xy.y * myCos));
}


// Colour shift. 0 = no shift, 1 = rgb -> brg, 2 = rgb -> gbr
float3 colourShift(float3 colour, float shift, float cycle, float speed, bool audioLinkActive)
{
	if (cycle == 1) shift += _Time.z * speed * 20;
	if (audioLinkActive)
	{
		if (cycle == 2) shift += dot(_AudioTexture[uint2(16, 28)], float4(0.0001, 0.1024, 104.8576, 107374.1824)) * speed;
		if (cycle == 3) shift += dot(_AudioTexture[uint2(16, 29)], float4(0.0001, 0.1024, 104.8576, 107374.1824)) * speed;
		if (cycle == 4) shift += dot(_AudioTexture[uint2(16, 30)], float4(0.0001, 0.1024, 104.8576, 107374.1824)) * speed;
		if (cycle == 5) shift += dot(_AudioTexture[uint2(16, 31)], float4(0.0001, 0.1024, 104.8576, 107374.1824)) * speed;
	}

	if (shift >= 0) shift = radians(shift) % 6.283185;
	else shift = 6.283185 - abs(radians(shift) % 6.283185);

	// Shift the hue by doing a 3-dimensional rotation. This is simpler and faster than converting to HSV, shifting, then
	// converting back to RGB. It will give a different result, but after going down a deep rabbit hole about colour spaces
	// and colour theory it turns out doing the hue shift in HSV isn't any more "correct" than doing it this way. Human
	// hue perception is vastly different than HSV, it's just that doing it in HSV is how everybody else has been doing it,
	// so it has become "correct" by convention. I'd rather have speed than convention.
	float cosAngle = cos(shift);
	return colour * cosAngle + cross(0.57735, colour) * sin(shift) + 0.57735 * dot(0.57735, colour) * (1.0 - cosAngle);
}


// Find the closest voronoi cell centre. xy will contain the distance to the centre of the cell, zw will contain the cell's centre
float4 voronoiCell(float2 uv)
{
    float gridDivisions = _TS2 * 100.0;
	float3 closestCellCentre = float3(uv, 1e29);
	for (float x = -1.0; x <= 1.0; x++)
	{
		for (float y = -1.0; y <= 1.0; y++)
		{
			float2 gridCentre = (round(uv * gridDivisions) + float2(x, y)) / gridDivisions;
            float2 cellCentre = gridCentre + ((frac(float2(dot(gridCentre, float2(1234.243, 1844.854)), dot(gridCentre, float2(1375.432, 1942.764)))) - 0.5) / gridDivisions);
            float distance = length(uv - cellCentre);

            closestCellCentre = distance < closestCellCentre.z ? float3(cellCentre, distance) : closestCellCentre;
        }
	}

	return(float4(closestCellCentre.xy - uv, closestCellCentre.xy));
}


// Rather than requiring people to install AudioLink, I have recreated this funtion here
float4 AudioLinkLerp(float2 xy)
{
	return lerp(_AudioTexture[uint2(xy)], _AudioTexture[uint2(xy + uint2(1, 0))], frac(xy.x));
}

// Apply the AudioLink colour strip
float3 getColourStrip(float direction, float2 decalUV)
{
	float audioUV = direction == 1 ? decalUV.y : direction == 2 ? (1.0 - decalUV.y) : direction == 3 ? (1.0 - decalUV.x) : decalUV.x;
	return (AudioLinkLerp(float2(audioUV * 128.0, 24)).rgb);
}

// Add Luma Glow
float3 addLumaGlow(float zone, float zPos, float strength)
{
	if (zone == 1) return(_AudioTexture[uint2(0, 23)].rgb * strength);
	if (zone == 2) return(_AudioTexture[uint2(1, 23)].rgb * strength);
	if (zone == 3) return(_AudioTexture[uint2(2, 23)].rgb * strength);
	if (zone == 4) return(_AudioTexture[uint2(3, 23)].rgb * strength);
	if (zone == 5) return(AudioLinkLerp(float2(zPos * 127.0, 59)).rgb * strength);
	if (zone == 6) return(AudioLinkLerp(float2(zPos * 127.0, 58)).rgb * strength);
	return(AudioLinkLerp(float2(zPos, 57)).rgb * strength);
}


