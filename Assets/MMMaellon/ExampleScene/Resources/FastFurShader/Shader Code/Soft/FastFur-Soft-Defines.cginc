//--------------------------------------------------------------------------------
// Maximum distance
float _MaxDistance;
float _HairTransparency;
#if defined (FUR_SOFT)
#define FUR_MINRANGE 0.05
#define FUR_MAXLAYERS 16.0
#endif
#if defined (FUR_SOFTLITE)
#define FUR_MINRANGE 0.05
#define FUR_MAXLAYERS 16.0
#endif


//--------------------------------------------------------------------------------
// Detect VR
#if defined(UNITY_SINGLE_PASS_STEREO) || defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
#define USING_STEREO_MATRICES
#endif


//--------------------------------------------------------------------------------
// Main Skin Settings
Texture2D _MainTex;
SamplerState sampler_MainTex;
float4 _MainTex_TexelSize;
float _SelectedUV;
float4 _Color;
float _AlbedoHueShift;
float _AlbedoHueShiftCycle;
float _AlbedoHueShiftRate;
float _Mode;
float _SmoothnessTextureChannel;
float _GlossyReflections;

// Metallic
sampler2D _MetallicGlossMap;
float4 _MetallicGlossMap_TexelSize;
float _Metallic;

// Occlusion map
Texture2D _OcclusionMap;
SamplerState sampler_OcclusionMap;
float4 _OcclusionMap_TexelSize;
float _OcclusionStrength;

// AudioLink
Texture2D <float4> _AudioTexture;
float4 _AudioTexture_TexelSize;

float _AudioLinkEnable;
float _AudioLinkStrength;

float _AudioLinkDynamicStrength;
float _AudioLinkDynamicSpeed;
float _AudioLinkDynamicFiltering;

float _AudioLinkStaticFiltering;
float _AudioLinkStaticTips;
float _AudioLinkStaticMiddle;
float _AudioLinkStaticRoots;
float _AudioLinkStaticSkin;

// Decals
UNITY_DECLARE_TEX2D(_DecalTexture);
float _DecalEnable;
float4 _DecalColor;
float2 _DecalPosition;
float _DecalAlbedoStrength;
float _DecalEmissionStrength;
float _DecalScale;
float _DecalRotation;
float _DecalTiled;
float _DecalAudioLinkEnable0;
float _DecalAudioLinkLayers0;
float4 _DecalAudioLinkBassColor0;
float4 _DecalAudioLinkLowMidColor0;
float4 _DecalAudioLinkHighMidColor0;
float4 _DecalAudioLinkTrebleColor0;
float _DecalLumaGlowEnable0;
float _DecalLumaGlow0;
float _DecalHueShift0;
float _DecalHueShiftCycle0;
float _DecalHueShiftRate0;

UNITY_DECLARE_TEX2D_NOSAMPLER(_DecalTexture1);
float _DecalEnable1;
float4 _DecalColor1;
float2 _DecalPosition1;
float _DecalAlbedoStrength1;
float _DecalEmissionStrength1;
float _DecalScale1;
float _DecalRotation1;
float _DecalTiled1;
float _DecalAudioLinkEnable1;
float _DecalAudioLinkLayers1;
float4 _DecalAudioLinkBassColor1;
float4 _DecalAudioLinkLowMidColor1;
float4 _DecalAudioLinkHighMidColor1;
float4 _DecalAudioLinkTrebleColor1;
float _DecalLumaGlowEnable1;
float _DecalLumaGlow1;
float _DecalHueShift1;
float _DecalHueShiftCycle1;
float _DecalHueShiftRate1;

UNITY_DECLARE_TEX2D_NOSAMPLER(_DecalTexture2);
float _DecalEnable2;
float4 _DecalColor2;
float2 _DecalPosition2;
float _DecalAlbedoStrength2;
float _DecalEmissionStrength2;
float _DecalScale2;
float _DecalRotation2;
float _DecalTiled2;
float _DecalAudioLinkEnable2;
float _DecalAudioLinkLayers2;
float4 _DecalAudioLinkBassColor2;
float4 _DecalAudioLinkLowMidColor2;
float4 _DecalAudioLinkHighMidColor2;
float4 _DecalAudioLinkTrebleColor2;
float _DecalLumaGlowEnable2;
float _DecalLumaGlow2;
float _DecalHueShift2;
float _DecalHueShiftCycle2;
float _DecalHueShiftRate2;

UNITY_DECLARE_TEX2D_NOSAMPLER(_DecalTexture3);
float _DecalEnable3;
float4 _DecalColor3;
float2 _DecalPosition3;
float _DecalAlbedoStrength3;
float _DecalEmissionStrength3;
float _DecalScale3;
float _DecalRotation3;
float _DecalTiled3;
float _DecalAudioLinkEnable3;
float _DecalAudioLinkLayers3;
float4 _DecalAudioLinkBassColor3;
float4 _DecalAudioLinkLowMidColor3;
float4 _DecalAudioLinkHighMidColor3;
float4 _DecalAudioLinkTrebleColor3;
float _DecalLumaGlowEnable3;
float _DecalLumaGlow3;
float _DecalHueShift3;
float _DecalHueShiftCycle3;
float _DecalHueShiftRate3;

// UV Discard
float _EnableUDIMDiscardOptions;
float _UDIMDiscardUV;
float _UDIMDiscardRow3_0;
float _UDIMDiscardRow3_1;
float _UDIMDiscardRow3_2;
float _UDIMDiscardRow3_3;
float _UDIMDiscardRow2_0;
float _UDIMDiscardRow2_1;
float _UDIMDiscardRow2_2;
float _UDIMDiscardRow2_3;
float _UDIMDiscardRow1_0;
float _UDIMDiscardRow1_1;
float _UDIMDiscardRow1_2;
float _UDIMDiscardRow1_3;
float _UDIMDiscardRow0_0;
float _UDIMDiscardRow0_1;
float _UDIMDiscardRow0_2;
float _UDIMDiscardRow0_3;

// Misc
float _Cutoff;
float _HideSeams;
float _TiltEdges;
float _TwoSided;
float4 _BackfaceColor;
float4 _BackfaceEmission;

// Supplied by VR Chat
float _VRChatCameraMode;
float _VRChatMirrorMode;


//--------------------------------------------------------------------------------
// Fur Shaping
Texture2D _FurShapeMap;
SamplerState sampler_FurShapeMap;
float4 _FurShapeMap_TexelSize;

Texture2D _FurShapeMask1;
float4 _FurShapeMask1_TexelSize;
Texture2D _FurShapeMask2;
float4 _FurShapeMask2_TexelSize;
Texture2D _FurShapeMask3;
float4 _FurShapeMask3_TexelSize;
Texture2D _FurShapeMask4;
float4 _FurShapeMask4_TexelSize;

uint _FurShapeMask1Bits;
uint _FurShapeMask2Bits;
uint _FurShapeMask3Bits;
uint _FurShapeMask4Bits;

float _FurShellSpacing;
float _FurMinHeight;
float _BodyShrinkOffset;
float _BodyExpansion;
float _BodyResizeCutoff;

float _FurCombStrength;
float _FurClipping;

float _HairDensityThreshold;

float _ScaleCalibration;


//--------------------------------------------------------------------------------
// Hairs
Texture2D _HairMap;
SamplerState sampler_HairMap;
float4 _HairMap_TexelSize;

Texture2D _HairMapCoarse;
float _CoarseMapActive;

float _HairMapAlphaFilter;
float _HairMapMipFilter;
float _HairMapMediumAlphaFilter;
float _HairMapMediumMipFilter;
float _HairMapMediumStrength;
float _HairMapCoarseAlphaFilter;
float _HairMapCoarseMipFilter;
float _HairMapCoarseStrength;

float _HairSharpen;
float _HairBlur;

float4 _HairMapScaling3;
float4 _HairMapScaling4;
float4 _HairMapScaling5;

float _HairCurlsActive;
float _HairCurlXWidth;
float _HairCurlYWidth;
float _HairCurlXTwists;
float _HairCurlYTwists;
float _HairCurlXYOffset;

float _HairHighlights;
float _HairColourShift;

float _AdvancedHairColour;
float4 _HairRootColour;
float4 _HairMidColour;
float4 _HairTipColour;
float _HairRootAlbedo;
float _HairMidAlbedo;
float _HairTipAlbedo;
float _HairRootMarkings;
float _HairMidMarkings;
float _HairTipMarkings;
float _HairColourMinHeight;

float _HairRootPoint;
float _HairMidLowPoint;
float _HairMidHighPoint;
float _HairTipPoint;

float _HairDensity;

float _HairClipping;


//--------------------------------------------------------------------------------
// Fur Markings
sampler2D _MarkingsMap;

float4 _MarkingsColour;
float _FurMarkingsActive;

float _MarkingsHeight;
float _MarkingsVisibility;
float _MarkingsDensity;
float _MarkingsContrast;
float _MarkingsRotation;

float _MarkingsMapPositiveCutoff;
float _MarkingsMapNegativeCutoff;


//--------------------------------------------------------------------------------
// Physically Based Shading
#if defined(_NORMALMAP)
sampler2D _BumpMap;
float _BumpScale;
#endif


#if defined(FUR_SKIN_LAYER)
float _PBSSkin;
float _PBSSkinStrength;


#if defined(_SPECGLOSSMAP)
sampler2D _SpecGlossMap;
#endif

float _Glossiness;
float _GlossMapScale;
#endif


//--------------------------------------------------------------------------------
// MatCap
#if defined(FUR_SKIN_LAYER)
float _MatcapEnable;
sampler2D _Matcap;
float4 _MatcapColor;
sampler2D _MatcapMask;
float _MatcapTextureChannel;
float _MatcapAdd;
float _MatcapReplace;
float _MatcapEmission;
float _MatcapSpecular;
#endif


//--------------------------------------------------------------------------------
// Lighting
sampler2D _EmissionMap;
float3 _EmissionColor;
float _EmissionMapStrength;

float _EmissionHueShift;
float _EmissionHueShiftCycle;
float _EmissionHueShiftRate;

float _AlbedoEmission;
float _OverallBrightness;

float _EmissionMapAudioLinkEnable;
float _EmissionMapAudioLinkLayers;
float4 _EmissionMapAudioLinkBassColor;
float4 _EmissionMapAudioLinkLowMidColor;
float4 _EmissionMapAudioLinkHighMidColor;
float4 _EmissionMapAudioLinkTrebleColor;
float _EmissionMapLumaGlowEnable;
float _EmissionMapLumaGlow;

float _ExtraLightingEnable;
float _ExtraLighting;
float _ExtraLightingRim;
float3 _ExtraLightingColor;
float _ExtraLightingMode;

float _FallbackLightEnable;
float3 _FallbackLightColor;
float _FallbackLightStrength;
float _FallbackLightDirection;
float _FallbackLightAngle;

float _MaxBrightness;
float _SoftenBrightness;
float3 _WorldLightReColour;
float _WorldLightReColourStrength;

float _FurShadowCastSize;
float _SoftenShadows;

float _LightPenetrationDepth;
float _DeepFurOcclusionStrength;
float _ProximityOcclusion;
float _ProximityOcclusionRange;
float _LightWraparound;
float _SubsurfaceScattering;

float _FurAnisotropicEnable;
float _FurAnisotropicReflect;
float _FurAnisoReflectAngle;
float _FurAnisoReflectGloss;
float _FurAnisoReflectMetallic;
float4 _FurAnisotropicReflectColor;
float4 _FurAnisotropicReflectColorNeg;
float4 _FurAnisotropicReflectColorPos;
float _FurAnisoReflectIridescenceStrength;
float _FurAnisoReflectEmission;
float _FurWindShimmer;
float _FurAnisoFlat;

float _FurAnisotropicRefract;
float _FurAnisoRefractAngle;
float _FurAnisoRefractGloss;
float _FurAnisoRefractMetallic;
float4 _FurAnisotropicRefractColor;
float _FurAnisoRefractEmission;
float _FurAnisoDepth;
float _FurAnisoSkin;


//--------------------------------------------------------------------------------
// Toon Shading
float _ToonShading;
float4 _ToonColour1;
float4 _ToonColour2;
float4 _ToonColour3;
float4 _ToonColour4;
float4 _ToonColour5;
float4 _ToonColour6;
float4 _ToonColour7;
float4 _ToonColour8;
float4 _ToonColour9;

float _ToonHueRGB;
float _ToonHueGBR;
float _ToonHueBRG;
float _ToonHueRBG;
float _ToonHueGRB;
float _ToonHueBGR;
float _ToonBrightness;
float _ToonWhiten;

float _ToonLighting;
float4 _ToonLightingHigh;
float4 _ToonLightingMid;
float4 _ToonLightingShadow;
float _ToonLightingBlend;
float _ToonLightingHighLevel;
float _ToonLightingHighSoftEdge;
float _ToonLightingShadowLevel;
float _ToonLightingShadowSoftEdge;


//--------------------------------------------------------------------------------
// Dynamic Movements
float _HairStiffness;
float _AudioLinkHairVibration;
float _FurGravitySlider;
UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
float _FurTouchStrength;
float _FurTouchThreshold;
float _EnableWind;
float _WindSpeed;
float _WindDirection;
float _WindAngle;
float4 _WindDirectionActual;
float _WindTurbulenceStrength;
float _WindGustsStrength;
float _MovementStrength;
float _VelocityX;
float _VelocityY;
float _VelocityZ;


//--------------------------------------------------------------------------------
// Debugging
#if defined(FUR_DEBUGGING)
uint _FurDebugDistance;
uint _FurDebugTopLayer;
uint _FurDebugUpperLimit;
uint _FurDebugDepth;
uint _FurDebugVerticies;
uint _FurDebugMipMap;
uint _FurDebugHairMap;
uint _FurDebugContact;
uint _FurDebugLength;
uint _FurDebugDensity;
uint _FurDebugCombing;
uint _FurDebugQuality;
#endif

int _DebuggingLog;// This is here so I have an easy toggle to use during testing

float _OverrideScale;
float _OverrideQualityBias;
float _OverrideDistanceBias;
float _TS1;
float _TS2;
float _TS3;


//--------------------------------------------------------------------------------
// In order to calculate shadows properly, we may need a temporary structure
#if defined(FUR_USE_SHADOWS) && (defined(SHADOWS_SCREEN) || defined(SHADOWS_DEPTH) || defined(SHADOWS_CUBE))
struct shadowStruct
{
	float4 pos : SV_POSITION;
	UNITY_SHADOW_COORDS(0)
};
#endif


//--------------------------------------------------------------------------------
// Pack a bunch of attributes into different channels.
#define FUR_Z uv.z
#define FURFADEIN uv.a
#define FURCULLTEST worldPos.a
#define FUR_MAXZ furData1.x


#define SUBSURFACESTRENGTH lightData1.w
#define ANISOTROPICBOTH lightData2.xy
#define ANISOTROPIC1REFLECT lightData2.x
#define ANISOTROPIC2REFRACT lightData2.y
#define ANISOTROPICANGLE lightData2.z
#define MAINLIGHTDOT lightData2.w


//--------------------------------------------------------------------------------
// The structure for the vertex shader input data
struct meshData
{
	float4 vertex : POSITION;
	float3 normal : NORMAL;
	float4 tangent : TANGENT;

	float2 uv0 : TEXCOORD0;
#if defined(LIGHTMAP_ON) || !defined(FUR_SOFTLITE)
	float2 uv1 : TEXCOORD1;
#endif
#if !defined(FUR_SOFTLITE)
	float2 uv2 : TEXCOORD2;
	float2 uv3 : TEXCOORD3;
#endif

	UNITY_VERTEX_INPUT_INSTANCE_ID
};


// The structure for the fragment shader input data
struct fragInput
{
	float4 pos : SV_POSITION;
	float4 worldPos : TEXCOORD0;
	float3 worldNormal : TEXCOORD1;
#if defined(_NORMALMAP)
	float4 worldTangent : TEXCOORD7;
#endif

	float4 uv : TEXCOORD2;

    float furData1 : TEXCOORD3;

	centroid float4 lightData1 : TEXCOORD4;// We need to use 'centroid' interpolation, otherwise MSAA causes pixel 'fireflies'
    centroid float4 lightData2 : TEXCOORD5;

#if defined(FUR_USE_FOG)
	UNITY_FOG_COORDS(6)
#endif

#if defined(FUR_DEBUGGING) && !defined(FUR_SKIN_LAYER)
	float4 vertDist : TEXCOORD8;
#endif

#if defined(FUR_USE_SHADOWS) && (defined(SHADOWS_SCREEN) || defined(SHADOWS_DEPTH) || defined(SHADOWS_CUBE))
	UNITY_SHADOW_COORDS(9)
#endif

	UNITY_VERTEX_OUTPUT_STEREO
};
