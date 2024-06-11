Shader "Warren's Fast Fur/Special Variants/Fast Fur - Soft UltraLite"
{
	Properties
	{


		//--------------------------------------------------------------------------------
		// Main Skin Settings
		[ToggleUI] _MainMapsGroup("Main Render Settings", Int) = 0
		[NOSCALEOFFSET] _MainTex("Albedo Map", 2D) = "white" {}
		[HDR]_Color("Albedo Colour", Color) = (1,1,1,1)
		[ToggleUI] _HideSeams("Aggressively Hide Albedo UV Seams", Int) = 0
		_MaxDistance("Maximum Distance", Range(2, 8)) = 8

		_HairTransparency("Hair Transparency", Range(0, 1)) = 0.35

   
		//--------------------------------------------------------------------------------
		// Dynamic Movements
		[ToggleUI] _DynamicsGroup("Gravity, Wind, and Movement", Int) = 0
		_FurGravitySlider("Gravity Strength", Range(0, 1)) = 0.35
		[ToggleUI] _WindGroup("Wind Settings", Int) = 0
		[ToggleUI]_EnableWind("Enable Wind", Int) = 0
		_WindSpeed("Wind Speed", Range(0, 1)) = .35
		_WindDirection("Wind Horizontal Direction", Range(0, 360)) = 90
		_WindAngle("Wind Vertical Angle", Range(-90, 90)) = -10
		_WindTurbulenceStrength("Turbulence", Range(0, 10)) = 1.7
		_WindGustsStrength("Gusts", Range(0, 10)) = 2.5

		//--------------------------------------------------------------------------------
		// Fur Shaping
		[ToggleUI] _FurShapeGroup("Fur Thickness and Overall Shape", Int) = 0
		[NOSCALEOFFSET]_FurShapeMap("Fur Shape Data Map", 2D) = "grey" {}
		[NOSCALEOFFSET]_FurShapeMask1("Height Masks 1", 2D) = "white" {}
		[NOSCALEOFFSET]_FurShapeMask2("Height Masks 2", 2D) = "white" {}
		[NOSCALEOFFSET]_FurShapeMask3("Height Masks 3", 2D) = "white" {}
		[NOSCALEOFFSET]_FurShapeMask4("Height Masks 4", 2D) = "white" {}
		_FurShapeMask1Bits("Height Masks 1 Bitfield", Int) = 0
		_FurShapeMask2Bits("Height Masks 2 Bitfield", Int) = 0
		_FurShapeMask3Bits("Height Masks 3 Bitfield", Int) = 0
		_FurShapeMask4Bits("Height Masks 4 Bitfield", Int) = 0
		[NOSCALEOFFSET]_FurGroomingMask("Fur Grooming: Copy Map", 2D) = "grey" {}
		_ScaleCalibration("Fur Thickness Calibration", Float) = -1
		_FurShellSpacing("Fur Thickness", Range(0.01, 1)) = 0.5
		_FurCombStrength("Combing Strength", Range(0, 1)) = 0.35


		//--------------------------------------------------------------------------------
		// Hairs
		[ToggleUI] _HairsGroup("Individual Hairs", Int) = 0
		[NOSCALEOFFSET]_HairMap("Hair Pattern Map", 2D) = "grey" {}

		_HairDensity("Hair Density", Range(0.1, 6)) = 3.5

		[Enum(Blurry (Box Filter),0,Sharp (Kaiser Filter),1)] _HairMipType("Texture Filter Type", Int) = 1

		[ToggleUI] _HairsColourGroup("Hair Colouring", Int) = 0
		_HairColourShift("Hair Colour Shift", Range(-5, 5)) = 0.2
		_HairHighlights("Hair Highlights", Range(-5, 5)) = 0.5


		// Hairs Generator
		[ToggleUI] _GenerateHairGroup("Hair Pattern Map Generator", Int) = 0
		
		_GenGuardHairs("# of Guard Hairs", Range(0, 5000)) = 150
		[Enum(Uniform Thickness,0,Slightly Tapered,1,Heavily Tapered,2)]_GenGuardHairsTaper("Guard Hair Shape", Int) = 1
		_GenGuardHairMinHeight("Guard Hair Min Height", Range(0, 1)) = .7
		_GenGuardHairMaxHeight("Guard Hair Max Height", Range(0, 1)) = 1
		_GenGuardHairMinColourShift("Guard Hair Min Colour Shift", Range(-1, 1)) = -1
		_GenGuardHairMaxColourShift("Guard Hair Max Colour Shift", Range(-1, 1)) = 1
		_GenGuardHairMinHighlight("Guard Hair Min Highlight", Range(-1, 1)) = -1
		_GenGuardHairMaxHighlight("Guard Hair Max Highlight", Range(-1, 1)) = 1
		[IntRange]_GenGuardHairMaxOverlap("Guard Hair Max Overlap", Range(0, 10)) = 0

		_GenMediumHairs("# of Medium Hairs", Range(0, 10000)) = 250
		[Enum(Uniform Thickness,0,Slightly Tapered,1,Heavily Tapered,2)]_GenMediumHairsTaper("Medium Hair Shape", Int) = 0
		_GenMediumHairMinHeight("Medium Hair Min Height", Range(0, 1)) = .7
		_GenMediumHairMaxHeight("Medium Hair Max Height", Range(0, 1)) = 1
		_GenMediumHairMinColourShift("Medium Hair Min Colour Shift", Range(-1, 1)) = -1
		_GenMediumHairMaxColourShift("Medium Hair Max Colour Shift", Range(-1, 1)) = 1
		_GenMediumHairMinHighlight("Medium Hair Min Highlight", Range(-1, 1)) = -1
		_GenMediumHairMaxHighlight("Medium Hair Max Highlight", Range(-1, 1)) = 1
		[IntRange]_GenMediumHairMaxOverlap("Medium Hair Max Overlap", Range(0, 10)) = 0

		_GenFineHairs("# of Fine Hairs", Range(0, 20000)) = 5000
		[Enum(Uniform Thickness,0,Slightly Tapered,1,Heavily Tapered,2)]_GenFineHairsTaper("Fine Hair Shape", Int) = 0
		_GenFineHairMinHeight("Fine Hair Min Height", Range(0, 1)) = .7
		_GenFineHairMaxHeight("Fine Hair Max Height", Range(0, 1)) = 1
		_GenFineHairMinColourShift("Fine Hair Min Colour Shift", Range(-1, 1)) = -1
		_GenFineHairMaxColourShift("Fine Hair Max Colour Shift", Range(-1, 1)) = 1
		_GenFineHairMinHighlight("Fine Hair Min Highlight", Range(-1, 1)) = -1
		_GenFineHairMaxHighlight("Fine Hair Max Highlight", Range(-1, 1)) = 1
		[IntRange]_GenFineHairMaxOverlap("Fine Hair Max Overlap", Range(0, 10)) = 0


		//--------------------------------------------------------------------------------
		// Lighting
		[ToggleUI]_LightingGroup("Lighting and Emission", Int) = 0

		_OverallBrightness("Brightness Multiplier", Range(0, 4)) = 1

		[NOSCALEOFFSET]_EmissionMap("Emission Map", 2D) = "white" {}
		[HDR]_EmissionColor("Emission Colour", Color) = (1,1,1,1)
		_EmissionMapStrength("Emission Map Strength", Range(0.0, 2.0)) = 1

		// Fur Lighting
		[ToggleUI]_FurLightingGroup("Supplemental Fur Lighting", Int) = 0
		_SubsurfaceScattering("Subsurface Scattering", Range(0, 1)) = 0.5

		// Anisotropic
		[ToggleUI]_AnisotropicGroup("Anisotropic Fur Gloss", Int) = 0
		[ToggleUI]_FurAnisotropicEnable("Enable Anisotropic Lighting", Int) = 1
		_FurAnisotropicReflect("Surface Reflections", Range(0, 2)) = 0.5
		_FurAnisoReflectAngle("Reflection Angle", Range(0, 45)) = 1
		_FurAnisoReflectGloss("Reflection Gloss", Range(0, 1)) = 0.5
		_FurAnisoReflectMetallic("Metallic Reflections", Range(0, 1)) = 0.5
		[HDR]_FurAnisotropicReflectColor("Reflection Base Tint", Color) = (1,1,1,1)
		[HDR]_FurAnisotropicReflectColorNeg("Reflection Red Shift", Color) = (1,0,0,1)
		[HDR]_FurAnisotropicReflectColorPos("Reflection Blue Shift", Color) = (0,0,1,1)
		_FurAnisoReflectIridescenceStrength("Iridescence Strength", Range(0, 20)) = 10
		_FurAnisoReflectEmission("Reflection Emission", Range(0, 2)) = 0
		_FurAnisotropicRefract("Internal Refractions", Range(0, 2)) = 0.5
		_FurAnisoRefractAngle("Refraction Angle", Range(0, 45)) = 6
		_FurAnisoRefractGloss("Refraction Gloss", Range(0, 1)) = 0.5
		_FurAnisoRefractMetallic("Metallic Refractions", Range(0, 1)) = 1.0
		[HDR]_FurAnisotropicRefractColor("Refraction Tint", Color) = (1,1,1,1)
		_FurAnisoRefractEmission("Refraction Emission", Range(0, 2)) = 0
		_FurAnisoDepth("Anisotropy Depth into Fur", Range(0, 1)) = 0.5
		_FurAnisoSkin("Apply Anisotropy to Skin", Range(0, 1)) = 0
		_FurWindShimmer("Wind Shimmering", Range(0, 0.5)) = 0.1
		_FurAnisoFlat("Flatten Anisotropic Hair Angle", Range(0, 1)) = 0


		// Occlusion and Shadow
		[ToggleUI]_OcclusionGroup("Occlusion and Shadow", Int) = 0
		_DeepFurOcclusionStrength("Deep Fur Occlusion Strength", Range(0, 2)) = 0.35
		_LightPenetrationDepth("Light Penetration Depth", Range(0, 1)) = 0.1


		// Debugging
		[ToggleUI] _DebuggingGroup("Debugging and Internal Information", Int) = 0
		[ToggleUI] _DebuggingLog("Enable Debugging in Console", Int) = 0
		_OverrideDistanceBias("Override Distance Bias (DEFAULT: 0)", Float) = 0
		_TS1("Test Slider", Range(0, 1)) = 0
		_TS2("Test Slider", Range(0, 20)) = 0
		_TS3("Test Slider", Range(-1, 1)) = 0


		// Texture Utilities
		[ToggleUI] _UtilitiesGroup("Fur Shape Data Map Utilities", Int) = 0
		[NOSCALEOFFSET] _UtilitySourceMap("Source Map", 2D) = "black" {}
		[NOSCALEOFFSET] _UtilityTargetMap("Target Map", 2D) = "black" {}

		[Enum(Copy a Channel from Source to Target,0,Apply Source Mask to Target,1,Rescale Combing Strength,2,Rescale Length,3,Rescale Density,4,Fill Target Channel,5)] _UtilityFunction("Operation", Int) = 0
		[ToggleUI] _UtilityInvert("Invert the Channel", Int) = 0
		[Enum(Set Target Channel to 0 if Mask is Below Threshold,0,Set Target Channel to 0 if Mask is Above Threshold,1)] _UtilityMaskType("Mask Type", Int) = 0
		_UtilityMaskThreshold("Mask Threshold", Range(0, 1)) = 0.01
		[Enum(Red (Comb X),0,Green (Comb Y),1,Blue (Length),2,Alpha (Density),3)] _UtilitySourceMask("Source Mask Channel", Int) = 2
		[Enum(Red (Comb X),0,Green (Comb Y),1,Blue (Length),2,Alpha (Density),3)] _UtilitySourceChannel("Source Channel", Int) = 0
		[Enum(Red (Comb X),0,Green (Comb Y),1,Blue (Length),2,Alpha (Density),3)] _UtilityTargetChannel("Target Channel", Int) = 0
		[Enum(256 x 256,0,512 x 512,1,1024 x 1024,2,2048 x 2048,3)] _UtilityNewResolution("New Texture Resolution", Int) = 2
		[PowerSlider(10.0)] _UtilityReScale("Re-Scale Factor", Range(0.5, 2)) = 0.75
		_UtilityValue("Fill Value", Range(0, 1)) = 0
	}


	SubShader
	{
		// Unity will try to combine static meshes into batches, which won't work if the calibration
		// scale is different, so we need to disable it. Projected effects also won't work, since the
		// surface of the fur isn't flat, so we disable those as well.
		Tags { "DisableBatching" = "true" "IgnoreProjector" = "True" "Queue" = "AlphaTest"}


		Pass
		{
			Tags {"LightMode" = "ForwardBase"}
			Cull Off
			CGPROGRAM
			
			#define FUR_SOFTLITE
			#define FUR_SKIN_LAYER
			
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			#pragma shader_feature _EMISSION
			#pragma shader_feature_local FUR_DEBUGGING

			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			#include "FastFur-Soft-Defines.cginc"
			#include "FastFur-UltraLite-Vert.cginc"
			#include "FastFur-UltraLite-Frag.cginc"

			ENDCG
		}


		Pass
		{
			Tags {"Queue" = "Transparent" "RenderType"="Transparent" "LightMode" = "ForwardBase"}
			//Cull Off
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite On
			ZTest Less

			CGPROGRAM

			#define FUR_SOFTLITE
			#define FUR_LAYER 1.0
			
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			#pragma shader_feature _EMISSION
			#pragma shader_feature_local FUR_DEBUGGING

			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			#include "FastFur-Soft-Defines.cginc"
			#include "FastFur-UltraLite-Vert.cginc"
			#include "FastFur-UltraLite-Frag.cginc"

			ENDCG
		}


		Pass
		{
			Tags {"Queue" = "Transparent" "RenderType"="Transparent" "LightMode" = "ForwardBase"}
			//Cull Off
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite On
			ZTest Less

			CGPROGRAM

			#define FUR_SOFTLITE
			#define FUR_LAYER 2.0
			
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			#pragma shader_feature _EMISSION
			#pragma shader_feature_local FUR_DEBUGGING

			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			#include "FastFur-Soft-Defines.cginc"
			#include "FastFur-UltraLite-Vert.cginc"
			#include "FastFur-UltraLite-Frag.cginc"

			ENDCG
		}


		Pass
		{
			Tags {"Queue" = "Transparent" "RenderType"="Transparent" "LightMode" = "ForwardBase"}
			//Cull Off
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite On
			ZTest Less

			CGPROGRAM

			#define FUR_SOFTLITE
			#define FUR_LAYER 3.0
			
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			#pragma shader_feature _EMISSION
			#pragma shader_feature_local FUR_DEBUGGING

			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			#include "FastFur-Soft-Defines.cginc"
			#include "FastFur-UltraLite-Vert.cginc"
			#include "FastFur-UltraLite-Frag.cginc"

			ENDCG
		}


		Pass
		{
			Tags {"Queue" = "Transparent" "RenderType"="Transparent" "LightMode" = "ForwardBase"}
			//Cull Off
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite On
			ZTest Less

			CGPROGRAM

			#define FUR_SOFTLITE
			#define FUR_LAYER 4.0
			
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			#pragma shader_feature _EMISSION
			#pragma shader_feature_local FUR_DEBUGGING

			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			#include "FastFur-Soft-Defines.cginc"
			#include "FastFur-UltraLite-Vert.cginc"
			#include "FastFur-UltraLite-Frag.cginc"

			ENDCG
		}


		Pass
		{
			Tags {"Queue" = "Transparent" "RenderType"="Transparent" "LightMode" = "ForwardBase"}
			//Cull Off
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite On
			ZTest Less

			CGPROGRAM

			#define FUR_SOFTLITE
			#define FUR_LAYER 5.0
			
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			#pragma shader_feature _EMISSION
			#pragma shader_feature_local FUR_DEBUGGING

			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			#include "FastFur-Soft-Defines.cginc"
			#include "FastFur-UltraLite-Vert.cginc"
			#include "FastFur-UltraLite-Frag.cginc"

			ENDCG
		}


		Pass
		{
			Tags {"Queue" = "Transparent" "RenderType"="Transparent" "LightMode" = "ForwardBase"}
			//Cull Off
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite On
			ZTest Less

			CGPROGRAM

			#define FUR_SOFTLITE
			#define FUR_LAYER 6.0
			
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			#pragma shader_feature _EMISSION
			#pragma shader_feature_local FUR_DEBUGGING

			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			#include "FastFur-Soft-Defines.cginc"
			#include "FastFur-UltraLite-Vert.cginc"
			#include "FastFur-UltraLite-Frag.cginc"

			ENDCG
		}


		Pass
		{
			Tags {"Queue" = "Transparent" "RenderType"="Transparent" "LightMode" = "ForwardBase"}
			//Cull Off
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite On
			ZTest Less

			CGPROGRAM

			#define FUR_SOFTLITE
			#define FUR_LAYER 7.0
			
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			#pragma shader_feature _EMISSION
			#pragma shader_feature_local FUR_DEBUGGING

			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			#include "FastFur-Soft-Defines.cginc"
			#include "FastFur-UltraLite-Vert.cginc"
			#include "FastFur-UltraLite-Frag.cginc"

			ENDCG
		}


		Pass
		{
			Tags {"Queue" = "Transparent" "RenderType"="Transparent" "LightMode" = "ForwardBase"}
			//Cull Off
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite On
			ZTest Less

			CGPROGRAM

			#define FUR_SOFTLITE
			#define FUR_LAYER 8.0
			
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			#pragma shader_feature _EMISSION
			#pragma shader_feature_local FUR_DEBUGGING

			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			#include "FastFur-Soft-Defines.cginc"
			#include "FastFur-UltraLite-Vert.cginc"
			#include "FastFur-UltraLite-Frag.cginc"

			ENDCG
		}


		Pass
		{
			Tags {"Queue" = "Transparent" "RenderType"="Transparent" "LightMode" = "ForwardBase"}
			//Cull Off
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite On
			ZTest Less

			CGPROGRAM

			#define FUR_SOFTLITE
			#define FUR_LAYER 9.0
			
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			#pragma shader_feature _EMISSION
			#pragma shader_feature_local FUR_DEBUGGING

			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			#include "FastFur-Soft-Defines.cginc"
			#include "FastFur-UltraLite-Vert.cginc"
			#include "FastFur-UltraLite-Frag.cginc"

			ENDCG
		}


		Pass
		{
			Tags {"Queue" = "Transparent" "RenderType"="Transparent" "LightMode" = "ForwardBase"}
			//Cull Off
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite On
			ZTest Less

			CGPROGRAM

			#define FUR_SOFTLITE
			#define FUR_LAYER 10.0
			
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			#pragma shader_feature _EMISSION
			#pragma shader_feature_local FUR_DEBUGGING

			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			#include "FastFur-Soft-Defines.cginc"
			#include "FastFur-UltraLite-Vert.cginc"
			#include "FastFur-UltraLite-Frag.cginc"

			ENDCG
		}


		Pass
		{
			Tags {"Queue" = "Transparent" "RenderType"="Transparent" "LightMode" = "ForwardBase"}
			//Cull Off
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite On
			ZTest Less

			CGPROGRAM

			#define FUR_SOFTLITE
			#define FUR_LAYER 11.0
			
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			#pragma shader_feature _EMISSION
			#pragma shader_feature_local FUR_DEBUGGING

			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			#include "FastFur-Soft-Defines.cginc"
			#include "FastFur-UltraLite-Vert.cginc"
			#include "FastFur-UltraLite-Frag.cginc"

			ENDCG
		}


		Pass
		{
			Tags {"Queue" = "Transparent" "RenderType"="Transparent" "LightMode" = "ForwardBase"}
			//Cull Off
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite On
			ZTest Less

			CGPROGRAM

			#define FUR_SOFTLITE
			#define FUR_LAYER 12.0
			
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			#pragma shader_feature _EMISSION
			#pragma shader_feature_local FUR_DEBUGGING

			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			#include "FastFur-Soft-Defines.cginc"
			#include "FastFur-UltraLite-Vert.cginc"
			#include "FastFur-UltraLite-Frag.cginc"

			ENDCG
		}


		Pass
		{
			Tags {"Queue" = "Transparent" "RenderType"="Transparent" "LightMode" = "ForwardBase"}
			//Cull Off
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite On
			ZTest Less

			CGPROGRAM

			#define FUR_SOFTLITE
			#define FUR_LAYER 13.0
			
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			#pragma shader_feature _EMISSION
			#pragma shader_feature_local FUR_DEBUGGING

			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			#include "FastFur-Soft-Defines.cginc"
			#include "FastFur-UltraLite-Vert.cginc"
			#include "FastFur-UltraLite-Frag.cginc"

			ENDCG
		}


		Pass
		{
			Tags {"Queue" = "Transparent" "RenderType"="Transparent" "LightMode" = "ForwardBase"}
			//Cull Off
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite On
			ZTest Less

			CGPROGRAM

			#define FUR_SOFTLITE
			#define FUR_LAYER 14.0
			
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			#pragma shader_feature _EMISSION
			#pragma shader_feature_local FUR_DEBUGGING

			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			#include "FastFur-Soft-Defines.cginc"
			#include "FastFur-UltraLite-Vert.cginc"
			#include "FastFur-UltraLite-Frag.cginc"

			ENDCG
		}


		Pass
		{
			Tags {"Queue" = "Transparent" "RenderType"="Transparent" "LightMode" = "ForwardBase"}
			//Cull Off
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite On
			ZTest Less

			CGPROGRAM

			#define FUR_SOFTLITE
			#define FUR_LAYER 15.0
			
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			#pragma shader_feature _EMISSION
			#pragma shader_feature_local FUR_DEBUGGING

			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			#include "FastFur-Soft-Defines.cginc"
			#include "FastFur-UltraLite-Vert.cginc"
			#include "FastFur-UltraLite-Frag.cginc"

			ENDCG
		}


		Pass
		{
			Tags {"Queue" = "Transparent" "RenderType"="Transparent" "LightMode" = "ForwardBase"}
			//Cull Off
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite On
			ZTest Less

			CGPROGRAM

			#define FUR_SOFTLITE
			#define FUR_LAYER 16.0

			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			#pragma shader_feature _EMISSION
			#pragma shader_feature_local FUR_DEBUGGING

			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			#include "FastFur-Soft-Defines.cginc"
			#include "FastFur-UltraLite-Vert.cginc"
			#include "FastFur-UltraLite-Frag.cginc"

			ENDCG
		}


		//--------------------------------------------------------------------------------
		// Shadow pass - Custom
		UsePass "Warren's Fast Fur/Fast Fur - Lite/Fur Shadows"

		// ------------------------------------------------------------------
		// Extracts information for lightmapping, GI (emission, albedo, ...)
		// This pass it not used during regular rendering.
		Pass
		{
			Name "META"
			Tags { "LightMode"="Meta" }

			//Cull Off

			CGPROGRAM
			#pragma vertex vert_meta
			#pragma fragment frag_meta

			#pragma shader_feature _EMISSION

			#include "UnityStandardMeta.cginc"
			ENDCG
		}
	}
	CustomEditor "WarrensFastFur.CustomShaderGUI"
}
