// Fallback Pipeling and Turbo Pipeline
//
// This code has been replaced with the V3 Super Pipeline, or at least, it would be replaced if AMD GPUs
// did not crash. This code may also be useful for situations where tesselation doesn't work. For example,
// apparently CVR has an option to limit tesellation, probably intended as a speed optimization, but it
// breaks the faster Super Pipeline 3.
//
// That all being said, this old code is still pretty damn fast. Its main drawback is lower resolution.


// Each geometry shader instance is responsible for creating and offsetting multiple shells.
// There seems to be an optimal speed balancing point between the extremes of one instance per shell or all shells in one instance.
// I've used trial-and-error testing to try to find the fastest combinations.
// 
// In addition to offsetting the 3 input vertexes further the higher the shell they are on, this also applies the effects of gravity.


[maxvertexcount(GEOM_MAXVERTS)]
[instance(GEOM_INSTANCES)]

void geom(triangle hullGeomInput IN[3], inout TriangleStream<fragInput> tristream, uint InstanceID : SV_GSInstanceID)
{
	float visibleLayers = max(max(IN[0].VISIBLELAYERS, IN[1].VISIBLELAYERS), IN[2].VISIBLELAYERS);
	float baseLayer = InstanceID + 1;

	// NOTE: Since 4.0.0, the layers are actually ordered in reverse, since that makes it easier to figure
	// out the top layer. Also, there is no layer 0, the top layer is layer 1, so we need to be aware of that.

	if (baseLayer > floor(visibleLayers)) return;

	//if (all(IN[0].worldPos == IN[1].worldPos || IN[0].worldPos == IN[2].worldPos || IN[1].worldPos == IN[2].worldPos)) return;



#if defined(PIPELINE1)
	// Cull backwards-facing triangles if all 3 vertexes are pointing too far away from the camera
	float minCull = min(min(IN[0].FURCULLTEST, IN[1].FURCULLTEST), IN[2].FURCULLTEST);
	if (minCull >= HULL_BACKFACE_CULLING) return;

	// Is this triangle on the screen?
	// The vertex shader calculates its min/max possible xy positions for all points along the fur thickness, and
	// clamps the result between -1 and 1. Finding the min/max of all 3 vertices gives us the min/max of the whole
	// triangle. If we then subtract the max from the min, and the result is non-zero, then the axis is on-screen.
	// If both the x any y axis are on-screen, the triangle is visible. This errors on the side of inclusion.
	float2 screenPosMin = min(min(IN[0].screenPosMin, IN[1].screenPosMin), IN[2].screenPosMin);
	float2 screenPosMax = max(max(IN[0].screenPosMax, IN[1].screenPosMax), IN[2].screenPosMax);
	bool onScreen = all(abs(screenPosMin - screenPosMax) > 0.0001);
	if (!onScreen) return;
#endif

	fragInput o[3];
	o[0] = (fragInput)0;

	UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o[0]);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN[0]);

	o[1] = o[0];
	o[2] = o[0];

#if defined(FUR_DEBUGGING)
	o[0].vertDist = float4(1, 0, 0, 0);
	o[1].vertDist = float4(0, 1, 0, 0);
	o[2].vertDist = float4(0, 0, 1, 0);
#endif

	for (int y = 0; y < 3; y++)
	{
#if defined(FUR_SKIN_LAYER) && defined(_NORMALMAP)
		o[y].worldTangent = IN[y].worldTangent;
#endif
		o[y].uv = IN[y].uv;
#if !defined(PREPASS)
		o[y].lightData1 = IN[y].lightData1;
		o[y].lightData2 = IN[y].lightData2;

		//o[y].diffuseLight = IN[y].diffuseLight;
		//o[y].specularLight = IN[y].specularLight;
#endif
#if defined(FUR_USE_SHADOWS) && (defined(SHADOWS_SCREEN) || defined(SHADOWS_DEPTH) || defined(SHADOWS_CUBE))
		o[y]._ShadowCoord = IN[y]._ShadowCoord;
#endif
	}


	// What's the furthest layer distance?
	float maxMaxZFur = max(max(IN[0].MAX_Z, IN[1].MAX_Z), IN[2].MAX_Z);


	// View direction    
	float3 viewDirection = float3(0, 0, 0);
#if defined (USING_STEREO_MATRICES)
	viewDirection = normalize((unity_StereoWorldSpaceCameraPos[0].xyz + unity_StereoWorldSpaceCameraPos[1].xyz) * 0.5 - (0.333333333333 * (IN[0].worldPos.xyz + IN[1].worldPos.xyz + IN[2].worldPos.xyz)));
#else
	viewDirection = normalize(_WorldSpaceCameraPos.xyz - (0.333333333333 * (IN[0].worldPos.xyz + IN[1].worldPos.xyz + IN[2].worldPos.xyz)));
#endif


	// Calculate layer spacing
	float skinZ = min(min(IN[0].SKIN_Z, IN[1].SKIN_Z), IN[2].SKIN_Z);
	float3 maxZFur = { IN[0].MAX_Z, IN[1].MAX_Z, IN[2].MAX_Z };
	float3 fadeIn = { IN[0].FURFADEIN, IN[1].FURFADEIN, IN[2].FURFADEIN };
	// Enforce a minimum of 25% of the max. This prevents the fur layers from intersecting each other when they touch skin.
	maxZFur = saturate(max(maxZFur, maxMaxZFur * 0.25));


	// Check for extreme angles. The fur needs a reasonable mip map level, otherwise it wont know where to put the hairs. If a triangle is
	// being viewed at an angle that is too sharp, then tilt the layers slightly to reduce the mip map level.
	float3 surfaceNormal = normalize(cross(IN[0].worldPos.xyz - IN[1].worldPos.xyz, IN[1].worldPos.xyz - IN[2].worldPos.xyz));
	// If we are directly facing the triangle, viewAngle will be -1. If we are viewing it directly sideways, viewAngle will be 0. If we are behind it, viewAngle will be postive.
	float viewAngle = dot(viewDirection, surfaceNormal);
	float3 tilt = float3(0, 0, 0);
	float tiltFactor = (0.65 - abs(viewAngle)) * _TiltEdges;
	float tiltThreshold = visibleLayers;
	if (tiltFactor > 0)
	{
		float maxDistance = max(max(IN[0].VIEWDISTANCE, IN[1].VIEWDISTANCE), IN[2].VIEWDISTANCE);
		float minDistance = min(min(IN[0].VIEWDISTANCE, IN[1].VIEWDISTANCE), IN[2].VIEWDISTANCE);
		float3 viewDistance = float3(IN[0].VIEWDISTANCE, IN[1].VIEWDISTANCE, IN[2].VIEWDISTANCE);
		// The furthest vertex will have a tilt of 1, and the closest will have a tilt of 0
		tilt = saturate((viewDistance - minDistance) / (maxDistance - minDistance));
		// Offset the tilt, so that the back and front move in opposite directions (this will also divide it in half)
		tilt -= 0.5;
		// Scale it to the correct thickness
		tilt *= tiltFactor * (maxZFur - skinZ) * 1;

		// Flipping instantly between front/back tilting looks bad, so we want to blend layers between the two instead
		tiltThreshold = saturate((viewAngle * 25) + 0.5) * visibleLayers;
		if (tiltThreshold < visibleLayers) tiltThreshold = floor(tiltThreshold);
	}


	// The order that the layers are rendered has a pretty dramatic effect on speed (~20%).
	//
	// The fastest seems to be slicing things up, with the first instance doing layers 0, 8, 16, 24, then the next instance doing layers
	// 1, 9, 17, 25, and so forth. Outside-in seems only marginally faster than inside-out (~0.33%) using this method.
	// 
	// Why? I don't know. My best guess is that it splits the workload more evenly, so the instances tend to all finish at the same time
	// when less than the maximum number of layers are being rendered.

	float startLayer = baseLayer;
	float stopLayer = visibleLayers;

	for (float layer = startLayer; layer <= stopLayer; layer += GEOM_INSTANCES)
	{
		// NOTE: Since 4.0.0, the layers are actually ordered in reverse, since that makes it easier to figure
		// out the top layer. Also, there is no layer 0, the top layer is layer 1, so we need to be aware of that.d

		// The 0.75 exponent makes it so that the layer spacing is a bit more spread out near the skin and a bit more compressed further away.
		//float portion = min(0.998, pow(1 - (layer <= tiltThreshold ? layer / tiltThreshold : (layer - tiltThreshold) / max(1, floor(visibleLayers) - tiltThreshold)), saturate(skinZ + 0.75)));
		float portion = min(0.998, 1 - (layer <= tiltThreshold ? layer / tiltThreshold : (layer - tiltThreshold) / max(1, floor(visibleLayers) - tiltThreshold)));

		// If we are behind the triangle, tilt it the opposite direction so we can see more of the backside. The tiltThreshold gradually blends the layers between the two.
		float3 posZ = skinZ + (max(0, (layer < tiltThreshold ? tilt : -tilt) * saturate((portion - 0.07) * 3)) + ((maxZFur - skinZ) * portion * (1 - (abs(tilt) * portion * 0.75)))) * saturate((fadeIn + 0.05) * 2.0);

		float3 zData = posZ + round(skinZ * 100) * 100;
		float3 bend = pow(_HairStiffness * 0.85 + ((1 - _HairStiffness) * min(1, posZ)), 2);

		float posZArray[3] = { posZ.x, posZ.y, posZ.z };
		float bendArray[3] = { bend.x, bend.y, bend.z };
		float zDataArray[3] = { zData.x, zData.y, zData.z };

		// Pass all the data to the fragment shader. (Note: I've tried unrolling the loop to see if there is a speed increase. There is none.)
		for (int y = 0; y < 3; y++)
		{
			float3 adjustedNormal = IN[y].worldNormal;

			adjustedNormal = normalize(adjustedNormal - (((saturate(IN[y].FURFADEIN * 5.0) * IN[y].windEffect.xyz) + float3(0, _FurGravitySlider, 0)) * bendArray[y]));

			o[y].worldNormal = adjustedNormal;
#if defined(FUR_DEBUGGING)
			o[y].worldPos = float4(IN[y].worldPos.xyz + adjustedNormal * IN[y].WORLDTHICKNESS * posZArray[y], IN[y].VISIBLELAYERS);
			o[y].vertDist.a = layer;
#else
			o[y].worldPos = IN[y].worldPos.xyz + adjustedNormal * IN[y].WORLDTHICKNESS * posZArray[y];
#endif

			o[y].pos = UnityWorldToClipPos(o[y].worldPos.xyz);
			o[y].ZDATA = zDataArray[y];

			UNITY_TRANSFER_FOG(o[y], o[y].pos);
#if defined(FUR_DEBUGGING)
			if (_FurDebugTopLayer < 0.5 || layer <= 1.0) tristream.Append(o[y]);
		}
		if (_FurDebugTopLayer < 0.5 || layer <= 1.0) tristream.RestartStrip();
#else
			tristream.Append(o[y]);
	}
		tristream.RestartStrip();
#endif
}
}