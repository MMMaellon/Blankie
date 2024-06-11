
#include "../FastFur-Function-Wind.cginc"


fragInput vert(meshData v)
{
	fragInput o = (fragInput)0;

	//--------------------------------------------------------------------------------
	// Single-Pass Stereo Instancing support

	UNITY_SETUP_INSTANCE_ID(v);
	//UNITY_INITIALIZE_OUTPUT(fragInput, o); // Redundant, because we are already clearing it to 0.
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);


	//--------------------------------------------------------------------------------
	// How far away are we?
	float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
#if defined (USING_STEREO_MATRICES)
	float3 viewVector = (unity_StereoWorldSpaceCameraPos[0].xyz + unity_StereoWorldSpaceCameraPos[1].xyz) * 0.5 - worldPos;
#else
	float3 viewVector = _WorldSpaceCameraPos.xyz - worldPos;
#endif
	float viewDistance = length(viewVector) + _OverrideDistanceBias;

	// 1 = VR Camera, 2 = Desktop Camera, 3 = Screenshot
    if (_VRChatCameraMode > 0.5)
	{
        // Screenshot
        if(_VRChatCameraMode > 2.5) viewDistance = 0;
        // Default in-game camera photo resolutions
		else if(_ScreenParams.y == 1080) viewDistance = 0.0;
		else if(_ScreenParams.y == 1440) viewDistance = 0.0;
		else if(_ScreenParams.y == 2160) viewDistance = 0.0;
		else if(_ScreenParams.y == 4320) viewDistance = 0.0;
	}

    viewDistance = max(0.0, viewDistance);

#if defined(FUR_SKIN_LAYER)
	o.pos = UnityWorldToClipPos(o.worldPos.xyz);
	float3 worldNormal = UnityObjectToWorldNormal(v.normal);
	float3 viewDir = normalize(viewVector);
#else

    // The FURFADEIN will be 1 at min range, and 0 at max range
    o.FURFADEIN = saturate(1.0 - ((viewDistance - FUR_MINRANGE) / (_MaxDistance - FUR_MINRANGE)));

    // How many layers do we need to render? We don't want this equation approaching infinity,
    // we want it approaching 128, hence the multiplication by 0.96875 to act as a limit.
    // Also, we want to start adding a second layer at the max range, so we add 0.99 so that
    // we are right at the limit. This will also hold the minimum thickness of the last layer
    // at 0.5 of the total fur thickness.
    float maxLayers = min(FUR_MAXLAYERS, (1.0 / (1.0 - (0.97875 * o.FURFADEIN))) + 0.99);

	// If we don't need to render this layer, abort.
	if (1.0 + (FUR_MAXLAYERS - FUR_LAYER) > maxLayers)
	{
        o.FURCULLTEST = 1e9;
		o.pos = float4(0, 0, 1e9, 1);
		return o;
	}

    // Which way is this vertex pointing?
	float3 worldNormal = UnityObjectToWorldNormal(v.normal);
	float3 viewDir = normalize(viewVector);
	// -1 = Directly facing the camera, 1 = Directly facing away from the camera
	o.FURCULLTEST = dot(worldNormal.xyz, -viewDir);
    // Believe it or not, it is much faster to NOT cull the vertex if it's facing the wrong way.
    // I have no idea how/why, but the difference is around 25%. My best-guess is that this culling
    // creates too many oddly-shaped triangles which somehow pass the GPUs culling algorithm.
    /*
	// If this vertex is facing too far away from the camera, abort.
	if (o.FURCULLTEST >= 0.5)
	{
        o.FURCULLTEST = 1e9;
		o.pos = float4(0, 0, 1e9, 1);
		return o;
	}
    */


	//--------------------------------------------------------------------------------
	// Get height samples. We're loading the data directly, without any sampler or
	// filtering.
	int4 uvInt = int4(floor(frac(v.uv0) * _FurShapeMap_TexelSize.zw), 0, 0);
	float4 furShape = _FurShapeMap.Load(uvInt);

	//--------------------------------------------------------------------------------
	// Apply optional height masks
	if (_FurShapeMask1Bits > 0)
	{
		int4 uvIntMask = int4(floor(frac(v.uv0) * _FurShapeMask1_TexelSize.zw), 0, 0);
		float4 furMask = _FurShapeMask1.Load(uvIntMask);
		if (_FurShapeMask1Bits & 1) furShape.z = min(furShape.z, furMask.x);
		if (_FurShapeMask1Bits & 2) furShape.z = min(furShape.z, furMask.y);
		if (_FurShapeMask1Bits & 4) furShape.z = min(furShape.z, furMask.z);
		if (_FurShapeMask1Bits & 8) furShape.z = min(furShape.z, furMask.w);
	}
	if (_FurShapeMask2Bits > 0)
	{
		int4 uvIntMask = int4(floor(frac(v.uv0) * _FurShapeMask2_TexelSize.zw), 0, 0);
		float4 furMask = _FurShapeMask2.Load(uvIntMask);
		if (_FurShapeMask2Bits & 1) furShape.z = min(furShape.z, furMask.x);
		if (_FurShapeMask2Bits & 2) furShape.z = min(furShape.z, furMask.y);
		if (_FurShapeMask2Bits & 4) furShape.z = min(furShape.z, furMask.z);
		if (_FurShapeMask2Bits & 8) furShape.z = min(furShape.z, furMask.w);
	}
	if (_FurShapeMask3Bits > 0)
	{
		int4 uvIntMask = int4(floor(frac(v.uv0) * _FurShapeMask3_TexelSize.zw), 0, 0);
		float4 furMask = _FurShapeMask3.Load(uvIntMask);
		if (_FurShapeMask3Bits & 1) furShape.z = min(furShape.z, furMask.x);
		if (_FurShapeMask3Bits & 2) furShape.z = min(furShape.z, furMask.y);
		if (_FurShapeMask3Bits & 4) furShape.z = min(furShape.z, furMask.z);
		if (_FurShapeMask3Bits & 8) furShape.z = min(furShape.z, furMask.w);
	}
	if (_FurShapeMask4Bits > 0)
	{
		int4 uvIntMask = int4(floor(frac(v.uv0) * _FurShapeMask4_TexelSize.zw), 0, 0);
		float4 furMask = _FurShapeMask4.Load(uvIntMask);
		if (_FurShapeMask4Bits & 1) furShape.z = min(furShape.z, furMask.x);
		if (_FurShapeMask4Bits & 2) furShape.z = min(furShape.z, furMask.y);
		if (_FurShapeMask4Bits & 4) furShape.z = min(furShape.z, furMask.z);
		if (_FurShapeMask4Bits & 8) furShape.z = min(furShape.z, furMask.w);
	}
#endif


	//--------------------------------------------------------------------------------
	// Convert coordinates to world space
	float scale = 0.95 * length(mul((float3x3) unity_ObjectToWorld, v.normal));


#if !defined(FUR_SKIN_LAYER)
	float3 windVector = 0;
	float3 windTurbulence = 0;
	if (_EnableWind > 0 && _WindSpeed > 0) windVector = calculateWind(o.worldPos.xyz, v.uv0, windTurbulence);
#endif


	//--------------------------------------------------------------------------------
	// Apply layer spacing and gravity
#if defined(FUR_SKIN_LAYER)
	o.FUR_Z = 0;
#else
    // Each new layer starts at the skin and moves outwards. As the layers increase, the
    // top layer will approach (but never actually reach) 1.0
    o.FUR_MAXZ = furShape.z;
	o.FUR_Z = furShape.z * (maxLayers - ((1.0 + FUR_MAXLAYERS) - FUR_LAYER)) / (maxLayers + 2.0);
#endif
	o.worldPos.xyz = mul(unity_ObjectToWorld, v.vertex);
#if !defined(FUR_SKIN_LAYER)
	float worldThickness = length(mul((float3x3) unity_ObjectToWorld, v.normal)) * _ScaleCalibration * _FurShellSpacing;
	o.worldPos.xyz += normalize(worldNormal - (float3(0, _FurGravitySlider * 0.75, 0) + windVector + windTurbulence) * o.FUR_Z) * worldThickness * o.FUR_Z;
#endif
	o.pos = UnityWorldToClipPos(o.worldPos.xyz);
	o.uv.xy = v.uv0;


	//--------------------------------------------------------------------------------
	// Baked lighting
#if defined(LIGHTMAP_ON)
	float2 lightmapUV = v.uv0 * unity_LightmapST.xy + unity_LightmapST.zw;
	o.lightData1.rgb = DecodeLightmap(UNITY_SAMPLE_TEX2D_LOD(unity_Lightmap, lightmapUV, 0));
#endif


	//--------------------------------------------------------------------------------
    // Process fog
#if defined(FUR_USE_FOG)
	UNITY_TRANSFER_FOG(o, o.pos);
#endif



	//--------------------------------------------------------------------------------
	// Spherical harmonics lights
	o.lightData1.rgb += ShadeSH9(float4(worldNormal.xyz, 1));


	// Check to see which way the vertex is pointing, and if it's too far away, stop
	// calculating any more lighting.
	// -1 = Directly facing the camera, 1 = Directly facing away from the camera
	if (o.FURCULLTEST > 0.35) return o;
	


	// If the fur is facing the camera, it won't have sub-surface scattering
	float scatterAtten = 0;
	if (_SubsurfaceScattering > 0)
	{
        scatterAtten = saturate(dot(viewDir, worldNormal.xyz));
		float diff = max(0, (1 - (scatterAtten * 3))) * 0.4;
		o.lightData1.rgb += ShadeSH9(float4(-viewDir, 1)).rgb * diff * _SubsurfaceScattering;
	}


	//--------------------------------------------------------------------------------
	// "Important" world space lights
	// Note: Since there is no ForwardAdd pass, in UltraLight version it will always be DIRECTIONAL
	float lightOn = _LightColor0.a;


	if (lightOn > 0)
	{
	    float3 lightDir = _WorldSpaceLightPos0.xyz;


		// Diffuse (Lambertian)
		o.MAINLIGHTDOT = dot(worldNormal.xyz, lightDir);
		//o.lightData1.rgb += _LightColor0.rgb * max(0, dot(worldNormal.xyz, _WorldSpaceLightPos0.xyz));


		// Simulate subsurface scattering. This isn't accurate at all, but accurate != fast.
		// The basic idea is that if the viewer is behind the avatar, facing towards the light,
		// light will be visible from fur that is perpendicular to the viewer.
		if (_SubsurfaceScattering > 0)
		{
			float scatterStrength = dot(viewDir, -_WorldSpaceLightPos0.xyz);// Is the camera looking past our position and into the light?
			scatterStrength -= scatterAtten * 1.25;// Is the fur facing the camera? If so, the light gets blocked.

			o.lightData1.rgb += _LightColor0.rgb * max(0, saturate(scatterStrength) * _SubsurfaceScattering);
		}


#if !defined(FUR_SKIN_LAYER)
		// Anisotropic reflections. This isn't even close to being correct, but doing things correctly means
		// doing it mostly in the fragment shader, which cuts frame rate by about 1/3, so it's not an option.
		if (_FurAnisotropicEnable > 0)
		{
        	float4 worldTangent = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);
	        float3 worldBinormal = cross(worldNormal.xyz, worldTangent.xyz) * (worldTangent.w * unity_WorldTransformParams.w);

			// Get the direction that the hair is pointing.
			float3 hairVector = (float3(furShape.xy, 0) * 2 - 1) * _FurCombStrength;
			hairVector.z = sqrt(1 - dot(hairVector.xy, hairVector.xy));
			hairVector = normalize(hairVector.x * worldTangent + hairVector.y * worldBinormal + hairVector.z * normalize(worldNormal.xyz) * (1.001 - _FurAnisoFlat));
			hairVector = normalize(hairVector - (windVector + float3(0, _FurGravitySlider, 0)));

			// If the light hits the hair at right angle, it reflects outwards in a disc.
			// As it hits at steeper angles, that disc turns into a cone. However, the hair
			// also has a prism-like structure, so it will add an offset to the angle of reflection.
			// Combining these two factors gives us a cone-shaped angle. The closer the view
			// direction is to this angle, the brighter the reflection/refraction.

			// Start by getting the dot product of the hair and the light vectors. If the light
			// is hitting at a right angle, the result will be 0. If it is hitting the hair straight
			// on the result will be -1.
			float lightDot = dot(hairVector, lightDir);

			// The light will reflect off at an angle that is biased towards the hair tip due to
			// the structure of the hair. The property sliders control the amount of bias. We are
			// just adding a linear bias, which isn't correct, but it's simple to calculate.
			float2 anisoAngle = lightDot + float2(_FurAnisoReflectAngle, _FurAnisoRefractAngle) * 0.01111;

			// Now get the dot product of the hair and the view angle. If the negative of the product
			// (negative because we are viewing from the opposite direction) is close to of the
			// reflect/refract angles, then the light will be visible.
			float hairAndViewDot = -dot(hairVector, -viewDir);

			// Glossiness determines how wide the possible view angle difference is.
			float2 anisoGloss = float2(_FurAnisoReflectGloss, _FurAnisoRefractGloss);
			float2 anisoSetting = float2(_FurAnisotropicReflect, _FurAnisotropicRefract);
			float2 glossFactor = 0.35 + (anisoGloss * anisoGloss);
			float2 anisoStrength = saturate((1 - abs(anisoAngle - hairAndViewDot)) - (0.65 + anisoGloss * 0.2)) * glossFactor;
			o.ANISOTROPICBOTH = (anisoStrength * anisoSetting);
			o.ANISOTROPICANGLE = (anisoAngle.x - hairAndViewDot) * glossFactor.x;
        }
#endif
	}


	//--------------------------------------------------------------------------------
	// Apply the 4 vertex lights (which are always point lights). This code is copy-pasted from UnityCG.cginc.
#if defined(VERTEXLIGHT_ON)
	// to light vectors
	float4 toLightX = unity_4LightPosX0 - o.worldPos.x;
	float4 toLightY = unity_4LightPosY0 - o.worldPos.y;
	float4 toLightZ = unity_4LightPosZ0 - o.worldPos.z;
	// squared lengths
	float4 lengthSq = 0;
	lengthSq += toLightX * toLightX;
	lengthSq += toLightY * toLightY;
	lengthSq += toLightZ * toLightZ;
	// don't produce NaNs if some vertex position overlaps with the light
	lengthSq = max(lengthSq, 0.000001);

	// NdotL
	float4 ndotl = 0;
	ndotl += toLightX * worldNormal.x;
	ndotl += toLightY * worldNormal.y;
	ndotl += toLightZ * worldNormal.z;
	// correct NdotL
	float4 corr = rsqrt(lengthSq);
	ndotl = max(float4(0, 0, 0, 0), ndotl * corr);
	// attenuation
	float4 atten = 1.0 / (1.0 + lengthSq * unity_4LightAtten0);
	float4 diff = ndotl * atten;
	// final color
	o.lightData1.rgb += unity_LightColor[0].rgb * diff.x;
	o.lightData1.rgb += unity_LightColor[1].rgb * diff.y;
	o.lightData1.rgb += unity_LightColor[2].rgb * diff.z;
	o.lightData1.rgb += unity_LightColor[3].rgb * diff.w;

	if (_SubsurfaceScattering > 0 && false)
	{
		float4 cameraToLightX = _WorldSpaceCameraPos.x - unity_4LightPosX0;
		float4 cameraToLightY = _WorldSpaceCameraPos.y - unity_4LightPosY0;
		float4 cameraToLightZ = _WorldSpaceCameraPos.z - unity_4LightPosZ0;
		float4 scatterStrength = 0;
		scatterStrength += cameraToLightX * viewDir.x;
		scatterStrength += cameraToLightY * viewDir.y;
		scatterStrength += cameraToLightZ * viewDir.z;
		scatterStrength -= scatterAtten * 3;// Is the fur facing the camera? If so, the light gets blocked.

		diff = max(0, scatterStrength * atten) * 0.4;
		o.lightData1.rgb += unity_LightColor[0].rgb * diff.x;
		o.lightData1.rgb += unity_LightColor[1].rgb * diff.y;
		o.lightData1.rgb += unity_LightColor[2].rgb * diff.z;
		o.lightData1.rgb += unity_LightColor[3].rgb * diff.w;
	}
#endif


	//--------------------------------------------------------------------------------
    // All done!
	return o;
}