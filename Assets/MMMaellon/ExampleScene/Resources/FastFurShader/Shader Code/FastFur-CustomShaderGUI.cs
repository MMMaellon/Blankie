#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System;

namespace WarrensFastFur
{

	public class CustomShaderGUI : ShaderGUI
	{
		#region "Defines"
		private Rect backgroundRect;
		private GUIStyle headingStyle;

		private float lastValidationTime = 0;
		private bool runningOkay = false;

		private float furThickness = 0;
		private float layerDensity = 0;
		private float normalMagnitidue = 0;

		private GameObject colliderGameObject = null;

		private enum Variant
		{
			Standard,
			Lite,
			UltraLite,
			Soft,
			SoftLite,
			SkinOnly,
			SkinOnlyLite
		}
		private Variant variant;

		private enum Pipeline
		{
			Pipeline1,
			Pipeline2,
			Pipeline3
		}
		private Pipeline pipeline = Pipeline.Pipeline2;
		private Pipeline prevPipeline = (Pipeline)(-1);

		private enum MaskSelection
		{
			NONE = 0,
			R = 1,
			G = 2,
			B = 4,
			A = 8,
			ALL = ~0
		}

		public enum SmoothnessMapChannel
		{
			SpecularMetallicAlpha,
			AlbedoAlpha,
		}

		MaterialEditor editor;
		UnityEngine.Object[] materials;
		Material targetMat;
		MaterialProperty[] properties;
		SerializedObject serializedObject;

		bool initialized = false;

		private AssetImporter normalMapTI = null;
		private AssetImporter metallicMapTI = null;
		private AssetImporter specularMapTI = null;
		private AssetImporter furDataMapTI = null;
		private AssetImporter furDataMask1TI = null;
		private AssetImporter furDataMask2TI = null;
		private AssetImporter furDataMask3TI = null;
		private AssetImporter furDataMask4TI = null;
		private AssetImporter hairDataMapTI = null;
		private AssetImporter hairDataMapCoarseTI = null;
		private AssetImporter markingsMapTI = null;
		private AssetImporter occlusionMapTI = null;

		private float[] recentToonHues = new float[6];
		private float[] recentFadePoints = new float[4];

		public GameObject furGrooming;
		#endregion

		#region "Getters and Setters"
		public MaterialProperty GetProperty(string name)
		{
			try
			{
				return FindProperty(name, properties);
			}
			catch (Exception)
			{
			}
			return null;
		}

		public float GetFloat(string name)
		{
			MaterialProperty prop = GetProperty(name);
			if (prop != null) return prop.floatValue;
			return 0;
		}

		int FindPropertyIndex(string name)
		{
			return targetMat.shader.FindPropertyIndex(name);
		}

		void SetProperty(string name, float value)
		{
			GetProperty(name).floatValue = value;
		}
		void SetProperty(string name, Vector4 value)
		{
			GetProperty(name).vectorValue = value;
		}
		void SetProperty(string name, Color value)
		{
			GetProperty(name).colorValue = value;
		}

		void GUIProperty(string name, string helpText)
		{
			try
			{
				editor.ShaderProperty(GetProperty(name), EditorGUIUtility.TrTextContent(GetProperty(name).displayName, helpText));
			}
			catch (Exception)
			{
			}
		}

		void DebugMessage(string message)
		{
			if (variant != Variant.UltraLite)
			{
				if (GetFloat("_DebuggingLog") > 0) Debug.Log("[WFFS] " + message);
			}
		}
		#endregion

		//*************************************************************************************************************************************************
		// Handle the custom UI
		//*************************************************************************************************************************************************
		public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
		{
			#region "GUI Header"

			editor = materialEditor;
			this.properties = properties;
			targetMat = (Material)editor.target;
			headingStyle = new GUIStyle(EditorStyles.foldout);
			headingStyle.fontStyle = FontStyle.Bold;
			headingStyle.fontSize += 2;

			variant = Variant.Standard;
			if (targetMat.shader.ToString().Contains("Lite")) variant = Variant.Lite;
			if (targetMat.shader.ToString().Contains("Soft")) variant = Variant.Soft;
			if (targetMat.shader.ToString().Contains("Soft Lite")) variant = Variant.SoftLite;
			if (targetMat.shader.ToString().Contains("UltraLite")) variant = Variant.UltraLite;
			if (targetMat.shader.ToString().Contains("Skin-Only")) variant = Variant.SkinOnly;
			if (targetMat.shader.ToString().Contains("Skin-Only Lite")) variant = Variant.SkinOnlyLite;

			GUIStyle titleStyle = new GUIStyle(EditorStyles.largeLabel);
			titleStyle.alignment = TextAnchor.MiddleCenter;
			string titleText = "Warren's Fast Fur Shader" +
			(
				variant == Variant.Lite ? " - Lite" :
				variant == Variant.UltraLite ? " - Ultra Lite" :
				variant == Variant.Soft ? " - Soft" :
				variant == Variant.SoftLite ? " - Soft Lite" :
				variant == Variant.SkinOnly ? " - Skin-Only" :
				variant == Variant.SkinOnlyLite ? " - Skin-Only Lite" :
				""
			);
			titleText += " - V4.1.3";

			if (variant != Variant.Soft && variant != Variant.UltraLite && variant != Variant.SoftLite)
			{
				pipeline = (Pipeline)GetFloat("_RenderPipeline");

				if (pipeline == prevPipeline)
				{
					if (pipeline == Pipeline.Pipeline1) titleText += " (Fallback Pipeline)";
					if (pipeline == Pipeline.Pipeline2) titleText += " (Turbo Pipeline)";
					if (pipeline == Pipeline.Pipeline3) titleText += " (Super Pipeline)";
				}
				else
				{
					titleText += " (Pending change...)";
					initialized = false;
				}
			}
			EditorGUILayout.LabelField(titleText, titleStyle);

			if (variant != Variant.Soft && variant != Variant.UltraLite && variant != Variant.SoftLite)
			{
				if (pipeline == Pipeline.Pipeline1)
				{
					EditorGUILayout.HelpBox("The Fallback Pipeline is slower and has lower render quality. It should only be used if the target platform does not support Hull + Domain shaders.", MessageType.Info);
				}
				if (!Application.isPlaying && pipeline == Pipeline.Pipeline3 && GetFloat("_ConfirmPipeline") > 0)
				{
					EditorGUILayout.HelpBox("CRASH WARNING: The Super Pipeline uses complex Hull + Domain calculations that currently WILL crash on some AMD cards/drivers. DO NOT USE THIS VERSION PUBLICLY!", MessageType.Error);
					if (GUILayout.Button("Click here to switch to the Turbo Pipeline"))
					{
						SetProperty("_RenderPipeline", (float)Pipeline.Pipeline2);
						initialized = false;
					}
				}
			}

			if (!Application.isPlaying && furDataMapTI == null && runningOkay)
			{
				if (GUILayout.Button("Click here to generate a blank fur shape data map"))
				{
					GenerateFurDataMap();
					initialized = false;
				}
			}
			if (!Application.isPlaying && hairDataMapTI == null && runningOkay)
			{
				if (GUILayout.Button("Click here to generate hair maps"))
				{
					GenerateHairMap();
					initialized = false;
				}
			}
			if (!Application.isPlaying && (furDataMapTI == null || hairDataMapTI == null) && runningOkay)
			{
				EditorGUILayout.HelpBox("Required data textures are missing. Click the above buttons to generate blank versions of the required textures.", MessageType.Error);
			}

			if (!runningOkay)
			{
				EditorGUILayout.HelpBox("The shader GUI has failed to initialize!", MessageType.Error);
			}
			EditorGUILayout.Space();
			#endregion

			#region "Main Group"
			//--------------------------------------------------------------------------------
			// Main Maps
			if (WFFFoldout("_MainMapsGroup"))
			{
				WFFGUIStartMainGroup();

				if (variant != Variant.Soft && variant != Variant.UltraLite && variant != Variant.SoftLite)
				{
					EditorGUI.BeginDisabledGroup(Application.isPlaying);
					GUIProperty("_RenderPipeline", "'Fallback Pipeline' is a standard geometry shader. 'Turbo Pipeline' uses a hull + domain shader to do culling. 'Super Pipeline' uses a hull + domain shader to do culling and also to generate the fur layers.");
					if (GetFloat("_RenderPipeline") == (float)Pipeline.Pipeline3)
					{
						if (GetFloat("_ConfirmPipeline") < 1)
						{
							EditorGUILayout.HelpBox("CRASH WARNING: The Super Pipeline uses complex Hull + Domain calculations that currently WILL crash on some AMD cards/drivers. DO NOT USE THIS VERSION PUBLICLY!", MessageType.Error);
							if (GUILayout.Button("Yes, I'm sure I want to enable the Super Pipeline"))
							{
								SetProperty("_ConfirmPipeline", 1);
								initialized = false;
							}
							if (GUILayout.Button("Cancel"))
							{
								SetProperty("_RenderPipeline", (float)Pipeline.Pipeline2);
								initialized = false;
							}
						}
					}
					else if (GetFloat("_ConfirmPipeline") > 0) SetProperty("_ConfirmPipeline", 0);
					EditorGUILayout.Space();
					EditorGUI.EndDisabledGroup();

					GUIProperty("_Mode", "Render mode.");
					if (GetFloat("_Mode") == 1)
					{
						GUIProperty("_Cutoff", "Alpha cutoff threshold when using Cutout rendering mode.");
					}
				}

				EditorGUI.BeginDisabledGroup(Application.isPlaying);
				editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_MainTex").displayName, "Albedo (RGB) of the skin and fur"), GetProperty("_MainTex"), GetProperty("_Color"), GetProperty("_SelectedUV"));
				EditorGUI.EndDisabledGroup();
				GUIProperty("_HideSeams", "Disable combing when sampling the albedo map. Causes hairs to change colour mid-shaft, but reduces the appearance of seams.");
				if (GetFloat("_HideSeams") > 0) EditorGUILayout.HelpBox("Individual hairs may change colour mid-shaft when 'Aggressively Hide UV Seams' is enabled. The preferred way to hide seams is to add enough overpainting to the albedo map texture.", MessageType.Info);
				if (variant != Variant.UltraLite)
				{
					GUIProperty("_AlbedoHueShift", "Spectrally shifts the albedo colour. This shift happens AFTER all other albedo calculations.");
					GUIProperty("_AlbedoHueShiftCycle", "Selects the method to animate the hue shift.");
					EditorGUI.BeginDisabledGroup(GetFloat("_AlbedoHueShiftCycle") == 0);
					GUIProperty("_AlbedoHueShiftRate", "Controls the speed of the hue shift animation.");
					EditorGUI.EndDisabledGroup();
				}

				EditorGUILayout.Space();

				bool smoothnessScaled = false;
				if (variant != Variant.UltraLite)
				{
					GUIProperty("_InvertSmoothness", "Inverts all calculations that involve 'smoothness' so that it behaves as 'roughness' instead.");
					bool useSmoothness = GetFloat("_InvertSmoothness") < 0.5;
					smoothnessScaled = (GetFloat("_SmoothnessTextureChannel") == 1);

					if (useSmoothness)
					{
						GUIProperty("_SmoothnessTextureChannel", "Selects the source of the Smoothness channel. Smoothness affects how scattered light reflections are.");
						if (smoothnessScaled)
						{
							GUIProperty("_GlossMapScale", "This multiplies the Smoothness channel. Smoothness affects how scattered light reflections are.");
						}
						else
						{
							GUIProperty("_Glossiness", "Smoothness affects how scattered light reflections are.");
						}
					}
					else
					{
						try
						{
							editor.ShaderProperty(GetProperty("_SmoothnessTextureChannel"), EditorGUIUtility.TrTextContent("Roughness Source", "Selects the source of the Roughness channel. Roughness affects how scattered light reflections are."));
							if (smoothnessScaled)
							{
								editor.ShaderProperty(GetProperty("_GlossMapScale"), EditorGUIUtility.TrTextContent("Roughness Source", "This multiplies the Roughness channel. Roughness affects how scattered light reflections are."));
							}
							else
							{
								editor.ShaderProperty(GetProperty("_Glossiness"), EditorGUIUtility.TrTextContent("Roughness", "Roughness affects how scattered light reflections are."));
							}
						}
						catch { }
					}


					if (GetProperty("_MetallicGlossMap").textureValue != null)
					{
						editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_MetallicGlossMap").displayName, "Metallic (R) and Use Smoothness or Roughness? (A)"), GetProperty("_MetallicGlossMap"));
						smoothnessScaled = true;
					}
					else
					{
						editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_MetallicGlossMap").displayName, "Metallic (R) and Use Smoothness or Roughness? (A)"), GetProperty("_MetallicGlossMap"), GetProperty("_Metallic"));
					}
					editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_OcclusionMap").displayName, "Occlusion Map"), GetProperty("_OcclusionMap"), GetProperty("_OcclusionStrength"));

					GUIProperty("_GlossyReflections", "Enables glossy reflections for the skin, and metallic reflections for both the skin and the fur.");
					EditorGUILayout.Space();
				}

				GUIProperty("_MaxDistance", "Maximum render distance of the UltraLite version of the shader.");
				if (variant == Variant.UltraLite) EditorGUILayout.Space();

				GUIProperty("_MirrorShowHide", "Allows the shader to optionally be hidden in VR Chat mirrors, or to only render in VR Chat mirrors.");
				EditorGUILayout.Space();

				//--------------------------------------------------------------------------------
				// AudioLink
				if (WFFFoldout("_AudioLinkGroup"))
				{
					WFFGUIStartSubGroup();

					GUIProperty("_AudioLinkEnable", "Turns on/off all AudioLink features.");

					EditorGUI.BeginDisabledGroup(GetFloat("_AudioLinkEnable") == 0);
					GUIProperty("_AudioLinkStrength", "Master control of the strength of the AudioLink effects.");
					EditorGUILayout.Space();
					GUIProperty("_AudioLinkDynamicStrength", "Controls the strength of dynamic light band moving from roots to tips.");
					GUIProperty("_AudioLinkDynamicSpeed", "Controls how fast does the dynamic light band moves from the roots to the tips.");
					GUIProperty("_AudioLinkDynamicFiltering", "Softens the rate at which the layers fade out.");
					EditorGUILayout.Space();
					GUIProperty("_AudioLinkStaticFiltering", "Controls how quickly the static layers react to sound. Lower settings will produce brief flashes of light. Higher settings will produce gentle light waves.");
					GUIProperty("_AudioLinkStaticTips", "Strength of the AudioLink effects on the hair tips.");
					GUIProperty("_AudioLinkStaticMiddle", "Strength of the AudioLink effects on the middle of the hairs.");
					GUIProperty("_AudioLinkStaticRoots", "Strength of the AudioLink effects on the hair roots.");
					GUIProperty("_AudioLinkStaticSkin", "Strength of the AudioLink effects on the skin.");
					EditorGUILayout.Space();
					EditorGUI.EndDisabledGroup();

					WFFGUIEndSubGroup();
				}

				EditorGUILayout.Space();


				//--------------------------------------------------------------------------------
				// Decals
				if (WFFFoldout("_DecalGroup"))
				{
					WFFGUIStartSubGroup();

					if (WFFFoldout("_Decal0Group"))
					{
						WFFGUIStartSubGroup();

						GUIProperty("_DecalEnable", "Enable decal");
						EditorGUI.BeginDisabledGroup(GetFloat("_DecalEnable") == 0);
						editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_DecalTexture").displayName, "Decal texture to apply"), GetProperty("_DecalTexture"), GetProperty("_DecalColor"));
						SetProperty("_DecalPosition", EditorGUILayout.Vector2Field("Decal 0 Position", GetProperty("_DecalPosition").vectorValue));
						if (GUILayout.Button("Click here to set decal 0 position")) SetDecal(0);
						GUIProperty("_DecalScale", "Size of the decal");
						GUIProperty("_DecalRotation", "Rotation of the decal");
						GUIProperty("_DecalTiled", "Enable tiled decal");
						EditorGUILayout.Space();
						GUIProperty("_DecalAlbedoStrength", "This affects the visibility of the Decal's albedo. It does not affect the Decal's emission or AudioLink visibility.");
						GUIProperty("_DecalEmissionStrength", "Decal emission strength");
						EditorGUILayout.Space();

						GUIProperty("_DecalHueShift0", "Spectrally shifts the decal colour. This shift happens AFTER all other colour calculations.");
						GUIProperty("_DecalHueShiftCycle0", "Selects the method to animate the hue shift.");
						EditorGUI.BeginDisabledGroup(GetFloat("_DecalHueShiftCycle0") == 0);
						GUIProperty("_DecalHueShiftRate0", "Controls the speed of the hue shift animation.");
						EditorGUI.EndDisabledGroup();
						EditorGUILayout.Space();

						GUIProperty("_DecalAudioLinkEnable0", "Enable AudioLink emission for this decal.");
						if (GetFloat("_DecalAudioLinkEnable0") > 0)
						{
							WFFGUIStartSubGroup();
							GUIProperty("_DecalAudioLinkLayers0", "Enables either the static-height emission layers, the dynamic-height emission layer, or both.");
							GUIProperty("_DecalAudioLinkBassColor0", "When bass audio is detected, the decal will be multiplied by this colour and added as emission.");
							GUIProperty("_DecalAudioLinkLowMidColor0", "When low-mid audio is detected, the decal will be multiplied by this colour and added as emission.");
							GUIProperty("_DecalAudioLinkHighMidColor0", "When high-mid audio is detected, the decal will be multiplied by this colour and added as emission.");
							GUIProperty("_DecalAudioLinkTrebleColor0", "When treble audio is detected, the decal will be multiplied by this colour and added as emission.");
							WFFGUIEndSubGroup();
						}
						GUIProperty("_DecalLumaGlowZone0", "Applies Furality Luma Glow Zone (which is the same as 'AudioLink Theme') to the decal.");
						GUIProperty("_DecalLumaGlowGradient0", "Applies Furality Luma Glow Gradient to the decal, with the gradient applied along the lengths of the hairs.");

						EditorGUI.EndDisabledGroup();

						WFFGUIEndSubGroup();
					}

					if (WFFFoldout("_Decal1Group"))
					{
						WFFGUIStartSubGroup();

						GUIProperty("_DecalEnable1", "Enable decal");
						EditorGUI.BeginDisabledGroup(GetFloat("_DecalEnable1") == 0);
						editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_DecalTexture1").displayName, "Applies a decal texture"), GetProperty("_DecalTexture1"), GetProperty("_DecalColor1"));
						SetProperty("_DecalPosition1", EditorGUILayout.Vector2Field("Decal 1 Position", GetProperty("_DecalPosition1").vectorValue));
						if (GUILayout.Button("Click here to set decal 1 position")) SetDecal(1);
						GUIProperty("_DecalScale1", "Scales the decal");
						GUIProperty("_DecalRotation1", "Rotates the decal");
						GUIProperty("_DecalTiled1", "Enable tiled decal");
						EditorGUILayout.Space();
						GUIProperty("_DecalAlbedoStrength1", "This affects the visibility of the Decal's albedo. It does not affect the Decal's emission or AudioLink visibility.");
						GUIProperty("_DecalEmissionStrength1", "Decal emission strength");
						EditorGUILayout.Space();

						GUIProperty("_DecalHueShift1", "Spectrally shifts the decal colour. This shift happens AFTER all other colour calculations.");
						GUIProperty("_DecalHueShiftCycle1", "Selects the method to animate the hue shift.");
						EditorGUI.BeginDisabledGroup(GetFloat("_DecalHueShiftCycle1") == 0);
						GUIProperty("_DecalHueShiftRate1", "Controls the speed of the hue shift animation.");
						EditorGUI.EndDisabledGroup();
						EditorGUILayout.Space();

						GUIProperty("_DecalAudioLinkEnable1", "Enable AudioLink emission for this decal.");
						if (GetFloat("_DecalAudioLinkEnable1") > 0)
						{
							WFFGUIStartSubGroup();
							GUIProperty("_DecalAudioLinkLayers1", "Enables either the static-height emission layers, the dynamic-height emission layer, or both.");
							GUIProperty("_DecalAudioLinkBassColor1", "When bass audio is detected, the decal will be multiplied by this colour and added as emission.");
							GUIProperty("_DecalAudioLinkLowMidColor1", "When low-mid audio is detected, the decal will be multiplied by this colour and added as emission.");
							GUIProperty("_DecalAudioLinkHighMidColor1", "When high-mid audio is detected, the decal will be multiplied by this colour and added as emission.");
							GUIProperty("_DecalAudioLinkTrebleColor1", "When treble audio is detected, the decal will be multiplied by this colour and added as emission.");
							WFFGUIEndSubGroup();
						}
						GUIProperty("_DecalLumaGlowZone1", "Applies Furality Luma Glow Zone (which is the same as 'AudioLink Theme') to the decal.");
						GUIProperty("_DecalLumaGlowGradient1", "Applies Furality Luma Glow Gradient to the decal, with the gradient applied along the lengths of the hairs.");

						EditorGUI.EndDisabledGroup();

						WFFGUIEndSubGroup();
					}

					if (WFFFoldout("_Decal2Group"))
					{
						WFFGUIStartSubGroup();

						GUIProperty("_DecalEnable2", "Enable decal");
						EditorGUI.BeginDisabledGroup(GetFloat("_DecalEnable2") == 0);
						editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_DecalTexture2").displayName, "Applies a decal texture"), GetProperty("_DecalTexture2"), GetProperty("_DecalColor2"));
						SetProperty("_DecalPosition2", EditorGUILayout.Vector2Field("Decal 2 Position", GetProperty("_DecalPosition2").vectorValue));
						if (GUILayout.Button("Click here to set decal 2 position")) SetDecal(2);
						GUIProperty("_DecalScale2", "Scales the decal");
						GUIProperty("_DecalRotation2", "Rotates the decal");
						GUIProperty("_DecalTiled2", "Enable tiled decal");
						EditorGUILayout.Space();
						GUIProperty("_DecalAlbedoStrength2", "This affects the visibility of the Decal's albedo. It does not affect the Decal's emission or AudioLink visibility.");
						GUIProperty("_DecalEmissionStrength2", "Decal emission strength");
						EditorGUILayout.Space();

						GUIProperty("_DecalHueShift2", "Spectrally shifts the decal colour. This shift happens AFTER all other colour calculations.");
						GUIProperty("_DecalHueShiftCycle2", "Selects the method to animate the hue shift.");
						EditorGUI.BeginDisabledGroup(GetFloat("_DecalHueShiftCycle2") == 0);
						GUIProperty("_DecalHueShiftRate2", "Controls the speed of the hue shift animation.");
						EditorGUI.EndDisabledGroup();
						EditorGUILayout.Space();

						GUIProperty("_DecalAudioLinkEnable2", "Enable AudioLink emission for this decal.");
						if (GetFloat("_DecalAudioLinkEnable2") > 0)
						{
							WFFGUIStartSubGroup();
							GUIProperty("_DecalAudioLinkLayers2", "Enables either the static-height emission layers, the dynamic-height emission layer, or both.");
							GUIProperty("_DecalAudioLinkBassColor2", "When bass audio is detected, the decal will be multiplied by this colour and added as emission.");
							GUIProperty("_DecalAudioLinkLowMidColor2", "When low-mid audio is detected, the decal will be multiplied by this colour and added as emission.");
							GUIProperty("_DecalAudioLinkHighMidColor2", "When high-mid audio is detected, the decal will be multiplied by this colour and added as emission.");
							GUIProperty("_DecalAudioLinkTrebleColor2", "When treble audio is detected, the decal will be multiplied by this colour and added as emission.");
							WFFGUIEndSubGroup();
						}
						GUIProperty("_DecalLumaGlowZone2", "Applies Furality Luma Glow Zone (which is the same as 'AudioLink Theme') to the decal.");
						GUIProperty("_DecalLumaGlowGradient2", "Applies Furality Luma Glow Gradient to the decal, with the gradient applied along the lengths of the hairs.");

						EditorGUI.EndDisabledGroup();

						WFFGUIEndSubGroup();
					}

					if (WFFFoldout("_Decal3Group"))
					{
						WFFGUIStartSubGroup();

						GUIProperty("_DecalEnable3", "Enable decal");
						EditorGUI.BeginDisabledGroup(GetFloat("_DecalEnable3") == 0);
						editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_DecalTexture3").displayName, "Applies a decal texture"), GetProperty("_DecalTexture3"), GetProperty("_DecalColor3"));
						SetProperty("_DecalPosition3", EditorGUILayout.Vector2Field("Decal 3 Position", GetProperty("_DecalPosition3").vectorValue));
						if (GUILayout.Button("Click here to set decal 3 position")) SetDecal(3);
						GUIProperty("_DecalScale3", "Scales the decal");
						GUIProperty("_DecalRotation3", "Rotates the decal");
						GUIProperty("_DecalTiled3", "Enable tiled decal");
						EditorGUILayout.Space();
						GUIProperty("_DecalAlbedoStrength3", "This affects the visibility of the Decal's albedo. It does not affect the Decal's emission or AudioLink visibility.");
						GUIProperty("_DecalEmissionStrength3", "Decal emission strength");
						EditorGUILayout.Space();

						GUIProperty("_DecalHueShift3", "Spectrally shifts the decal colour. This shift happens AFTER all other colour calculations.");
						GUIProperty("_DecalHueShiftCycle3", "Selects the method to animate the hue shift.");
						EditorGUI.BeginDisabledGroup(GetFloat("_DecalHueShiftCycle3") == 0);
						GUIProperty("_DecalHueShiftRate3", "Controls the speed of the hue shift animation.");
						EditorGUI.EndDisabledGroup();
						EditorGUILayout.Space();

						GUIProperty("_DecalAudioLinkEnable3", "Enable AudioLink emission for this decal.");
						if (GetFloat("_DecalAudioLinkEnable3") > 0)
						{
							WFFGUIStartSubGroup();
							GUIProperty("_DecalAudioLinkLayers3", "Enables either the static-height emission layers, the dynamic-height emission layer, or both.");
							GUIProperty("_DecalAudioLinkBassColor3", "When bass audio is detected, the decal will be multiplied by this colour and added as emission.");
							GUIProperty("_DecalAudioLinkLowMidColor3", "When low-mid audio is detected, the decal will be multiplied by this colour and added as emission.");
							GUIProperty("_DecalAudioLinkHighMidColor3", "When high-mid audio is detected, the decal will be multiplied by this colour and added as emission.");
							GUIProperty("_DecalAudioLinkTrebleColor3", "When treble audio is detected, the decal will be multiplied by this colour and added as emission.");
							WFFGUIEndSubGroup();
						}
						GUIProperty("_DecalLumaGlowZone3", "Applies Furality Luma Glow Zone (which is the same as 'AudioLink Theme') to the decal.");
						GUIProperty("_DecalLumaGlowGradient3", "Applies Furality Luma Glow Gradient to the decal, with the gradient applied along the lengths of the hairs.");

						EditorGUI.EndDisabledGroup();

						WFFGUIEndSubGroup();
					}

					WFFGUIEndSubGroup();
				}

				EditorGUILayout.Space();

				if (variant != Variant.UltraLite)
				{
					//--------------------------------------------------------------------------------
					// Physically Based Shading
					if (WFFFoldout("_PBSSkinGroup"))
					{
						WFFGUIStartSubGroup();

						GUIProperty("_PBSSkinStrength", "The strength of the Physically Based Shading for the skin layer.");
						GUIProperty("_PBSSkin", "How deep does the fur need to be to turn off Physically Based Shading.");
						EditorGUILayout.Space();

						EditorGUI.BeginDisabledGroup(GetFloat("_PBSSkin") == 0 || GetFloat("_PBSSkinStrength") == 0);
						editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_BumpMap").displayName, "Normal Map"), GetProperty("_BumpMap"), GetProperty("_BumpScale"));
						EditorGUILayout.Space();

						GUIProperty("_SpecularHighlights", "Specular highlights  It is only used for the skin layer.");

						EditorGUILayout.Space();
						GUIProperty("_TwoSided", "Render back-facing skin (the fur layers always render both sides)");
						EditorGUI.BeginDisabledGroup(GetFloat("_TwoSided") == 0);
						GUIProperty("_BackfaceColor", "Albedo colour of the backfacing skin");
						GUIProperty("_BackfaceEmission", "Emissive colour of the backfacing skin");
						EditorGUI.EndDisabledGroup();

						WFFGUIEndSubGroup();
					}
					EditorGUILayout.Space();

					//--------------------------------------------------------------------------------
					// MatCap
					if (WFFFoldout("_MatcapGroup"))
					{
						WFFGUIStartSubGroup();

						GUIProperty("_MatcapEnable", "Enable Material Capture.");
						bool matcapEnabled = GetFloat("_MatcapEnable") > 0;
						EditorGUI.BeginDisabledGroup(!matcapEnabled);
						editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_Matcap").displayName, "The MatCap texture is a spherical texture that is directionally projected onto the surface. It roughly simulates reflections."), GetProperty("_Matcap"), GetProperty("_MatcapColor"));
						GUIProperty("_MatcapTextureChannel", "This is the spherical MatCap texture.");
						if (matcapEnabled) EditorGUI.BeginDisabledGroup(GetFloat("_MatcapTextureChannel") != 1);
						editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_MatcapMask").displayName, "The MatCap mask is optional, and can be used to filter where the MatCap is visible."), GetProperty("_MatcapMask"));
						if (matcapEnabled) EditorGUI.EndDisabledGroup();
						EditorGUILayout.Space();
						GUIProperty("_MatcapAdd", "This blends in the MatCap by adding it to the albedo.");
						GUIProperty("_MatcapReplace", "This blends in the MatCap by replacing the albedo.");
						GUIProperty("_MatcapEmission", "This applies the MatCap as emission.");
						EditorGUILayout.Space();
						GUIProperty("_MatcapSpecular", "Should the MatCap be applied as diffuse or specular light?");

						EditorGUI.EndDisabledGroup();

						WFFGUIEndSubGroup();
					}
					EditorGUILayout.Space();


					//--------------------------------------------------------------------------------
					// UV Discard
					if (WFFFoldout("_UVDiscardGroup"))
					{
						WFFGUIStartSubGroup();

						GUIProperty("_EnableUDIMDiscardOptions", "'UV Discard' won't render parts of the mesh whose UV coordinates are within the selected ranges.");

						EditorGUI.BeginDisabledGroup(GetFloat("_EnableUDIMDiscardOptions") == 0);

						GUIProperty("_UDIMDiscardUV", "Which UV map should be used to determine UV Discard.");


						Rect UVGrid = EditorGUILayout.BeginHorizontal();
						EditorGUILayout.PrefixLabel("");
						EditorGUILayout.EndHorizontal();
						EditorGUI.DrawRect(new Rect(UVGrid.x + UVGrid.width * 0.3f + 48f, UVGrid.y, 177f, UVGrid.height), EditorGUIUtility.isProSkin ? new Color(0.05f, 0.05f, 0.05f) : new Color(0.95f, 0.95f, 0.95f));
						EditorGUI.LabelField(new Rect(UVGrid.x + UVGrid.width * 0.3f + 102f, UVGrid.y, UVGrid.width * 0.4f, UVGrid.height), "U");

						UVGrid = EditorGUILayout.BeginHorizontal();
						EditorGUILayout.PrefixLabel("");
						EditorGUILayout.EndHorizontal();
						EditorGUI.DrawRect(new Rect(UVGrid.x + UVGrid.width * 0.3f + 48f, UVGrid.y, 177f, UVGrid.height), EditorGUIUtility.isProSkin ? Color.black : Color.white);
						EditorGUI.LabelField(new Rect(UVGrid.x + UVGrid.width * 0.3f + 28f, UVGrid.y, UVGrid.width * 0.4f, UVGrid.height), "0");
						EditorGUI.LabelField(new Rect(UVGrid.x + UVGrid.width * 0.3f + 78f, UVGrid.y, UVGrid.width * 0.4f, UVGrid.height), "1");
						EditorGUI.LabelField(new Rect(UVGrid.x + UVGrid.width * 0.3f + 128f, UVGrid.y, UVGrid.width * 0.4f, UVGrid.height), "2");
						EditorGUI.LabelField(new Rect(UVGrid.x + UVGrid.width * 0.3f + 178f, UVGrid.y, UVGrid.width * 0.4f, UVGrid.height), "3");

						UVGrid = EditorGUILayout.BeginHorizontal();
						EditorGUILayout.PrefixLabel("V = 3");
						EditorGUILayout.EndHorizontal();
						SetProperty("_UDIMDiscardRow3_0", EditorGUI.Toggle(new Rect(UVGrid.x + UVGrid.width * 0.3f + 25f, UVGrid.y, UVGrid.width * 0.1f, UVGrid.height), GetFloat("_UDIMDiscardRow3_0") > 0f) ? 1 : 0);
						SetProperty("_UDIMDiscardRow3_1", EditorGUI.Toggle(new Rect(UVGrid.x + UVGrid.width * 0.3f + 75f, UVGrid.y, UVGrid.width * 0.1f, UVGrid.height), GetFloat("_UDIMDiscardRow3_1") > 0f) ? 1 : 0);
						SetProperty("_UDIMDiscardRow3_2", EditorGUI.Toggle(new Rect(UVGrid.x + UVGrid.width * 0.3f + 125f, UVGrid.y, UVGrid.width * 0.1f, UVGrid.height), GetFloat("_UDIMDiscardRow3_2") > 0f) ? 1 : 0);
						SetProperty("_UDIMDiscardRow3_3", EditorGUI.Toggle(new Rect(UVGrid.x + UVGrid.width * 0.3f + 175f, UVGrid.y, UVGrid.width * 0.1f, UVGrid.height), GetFloat("_UDIMDiscardRow3_3") > 0f) ? 1 : 0);

						UVGrid = EditorGUILayout.BeginHorizontal();
						EditorGUILayout.PrefixLabel("V = 2");
						EditorGUILayout.EndHorizontal();
						SetProperty("_UDIMDiscardRow2_0", EditorGUI.Toggle(new Rect(UVGrid.x + UVGrid.width * 0.3f + 25f, UVGrid.y, UVGrid.width * 0.1f, UVGrid.height), GetFloat("_UDIMDiscardRow2_0") > 0f) ? 1 : 0);
						SetProperty("_UDIMDiscardRow2_1", EditorGUI.Toggle(new Rect(UVGrid.x + UVGrid.width * 0.3f + 75f, UVGrid.y, UVGrid.width * 0.1f, UVGrid.height), GetFloat("_UDIMDiscardRow2_1") > 0f) ? 1 : 0);
						SetProperty("_UDIMDiscardRow2_2", EditorGUI.Toggle(new Rect(UVGrid.x + UVGrid.width * 0.3f + 125f, UVGrid.y, UVGrid.width * 0.1f, UVGrid.height), GetFloat("_UDIMDiscardRow2_2") > 0f) ? 1 : 0);
						SetProperty("_UDIMDiscardRow2_3", EditorGUI.Toggle(new Rect(UVGrid.x + UVGrid.width * 0.3f + 175f, UVGrid.y, UVGrid.width * 0.1f, UVGrid.height), GetFloat("_UDIMDiscardRow2_3") > 0f) ? 1 : 0);

						UVGrid = EditorGUILayout.BeginHorizontal();
						EditorGUILayout.PrefixLabel("V = 1");
						EditorGUILayout.EndHorizontal();
						SetProperty("_UDIMDiscardRow1_0", EditorGUI.Toggle(new Rect(UVGrid.x + UVGrid.width * 0.3f + 25f, UVGrid.y, UVGrid.width * 0.1f, UVGrid.height), GetFloat("_UDIMDiscardRow1_0") > 0f) ? 1 : 0);
						SetProperty("_UDIMDiscardRow1_1", EditorGUI.Toggle(new Rect(UVGrid.x + UVGrid.width * 0.3f + 75f, UVGrid.y, UVGrid.width * 0.1f, UVGrid.height), GetFloat("_UDIMDiscardRow1_1") > 0f) ? 1 : 0);
						SetProperty("_UDIMDiscardRow1_2", EditorGUI.Toggle(new Rect(UVGrid.x + UVGrid.width * 0.3f + 125f, UVGrid.y, UVGrid.width * 0.1f, UVGrid.height), GetFloat("_UDIMDiscardRow1_2") > 0f) ? 1 : 0);
						SetProperty("_UDIMDiscardRow1_3", EditorGUI.Toggle(new Rect(UVGrid.x + UVGrid.width * 0.3f + 175f, UVGrid.y, UVGrid.width * 0.1f, UVGrid.height), GetFloat("_UDIMDiscardRow1_3") > 0f) ? 1 : 0);
						UVGrid = EditorGUILayout.BeginHorizontal();

						EditorGUILayout.PrefixLabel("V = 0");
						EditorGUILayout.EndHorizontal();
						SetProperty("_UDIMDiscardRow0_0", EditorGUI.Toggle(new Rect(UVGrid.x + UVGrid.width * 0.3f + 25f, UVGrid.y, UVGrid.width * 0.1f, UVGrid.height), GetFloat("_UDIMDiscardRow0_0") > 0f) ? 1 : 0);
						SetProperty("_UDIMDiscardRow0_1", EditorGUI.Toggle(new Rect(UVGrid.x + UVGrid.width * 0.3f + 75f, UVGrid.y, UVGrid.width * 0.1f, UVGrid.height), GetFloat("_UDIMDiscardRow0_1") > 0f) ? 1 : 0);
						SetProperty("_UDIMDiscardRow0_2", EditorGUI.Toggle(new Rect(UVGrid.x + UVGrid.width * 0.3f + 125f, UVGrid.y, UVGrid.width * 0.1f, UVGrid.height), GetFloat("_UDIMDiscardRow0_2") > 0f) ? 1 : 0);
						SetProperty("_UDIMDiscardRow0_3", EditorGUI.Toggle(new Rect(UVGrid.x + UVGrid.width * 0.3f + 175f, UVGrid.y, UVGrid.width * 0.1f, UVGrid.height), GetFloat("_UDIMDiscardRow0_3") > 0f) ? 1 : 0);

						EditorGUI.EndDisabledGroup();

						WFFGUIEndSubGroup();
					}
					EditorGUILayout.Space();


					//--------------------------------------------------------------------------------
					// Quality Settings
					if (WFFFoldout("_QualityGroup"))
					{
						WFFGUIStartSubGroup();

						if (pipeline != Pipeline.Pipeline3 && variant == Variant.Standard)
						{
							GUIProperty("_MaximumLayers", "Limits the maximum number of fur layers that can be rendered.");
							EditorGUILayout.Space();
						}
						GUIProperty("_MinimumFPSTarget", "Lowers the render quality if the FPS drops below the target.");
						EditorGUILayout.Space();
						GUIProperty("_V4QualityEditor", "Render quality when viewed in the Unity editor.");
						EditorGUILayout.Space();
						GUIProperty("_V4QualityVR", "Render quality when viewed in a VR application.");
						GUIProperty("_V4Quality2D", "Render quality when viewed in a non-VR application.");
						EditorGUILayout.Space();
						GUIProperty("_V4QualityVRMirror", "Render quality when viewed in a VR Chat mirror, in VR mode.");
						GUIProperty("_V4Quality2DMirror", "Render quality when viewed in a VR Chat mirror, in desktop mode.");
						EditorGUILayout.Space();
						GUIProperty("_V4QualityCameraView", "Render quality when viewed in the VR Chat camera viewfinder.");
						GUIProperty("_V4QualityStreamCamera", "Render quality when viewed in the VR Chat stream camera feed.");
						GUIProperty("_V4QualityCameraPhoto", "Render quality when taking a VR Chat camera photo.");
						EditorGUILayout.HelpBox("In order for the shader to detect it is rendering a camera photo, your VR Chat camera resolution needs to be set to a Y resolution of 1080 (the default), 1440, 2160, or 4320. A resolution of 720 will be interpreted as the 'Camera Viewfinder'. All other Y resolutions will be interpreted as the 'Stream Camera'.", MessageType.Info);
						EditorGUILayout.Space();
						GUIProperty("_V4QualityScreenshot", "Render quality when taking a VR Chat screenshot (either by using the in-game 'Screenshot' button, or by pressing CTRL+F12).");
						String[] qualityProperties = { "_MaximumLayers", "_V4QualityEditor", "_V4QualityVR", "_V4Quality2D", "_V4QualityVRMirror", "_V4Quality2DMirror", "_V4QualityCameraView", "_V4QualityStreamCamera", "_V4QualityCameraPhoto", "_V4QualityScreenshot" };
						if (!CheckDefaults(qualityProperties))
						{
							if (GUILayout.Button("Click here to reset quality settings to defaults"))
							{
								SetDefaults(qualityProperties);
							}
						}

						WFFGUIEndSubGroup();
					}
				}

				WFFGUIEndMainGroup();
			}
			#endregion

			#region "Fur Shape"
			//--------------------------------------------------------------------------------
			// Fur Shape
			if (WFFFoldout("_FurShapeGroup"))
			{
				WFFGUIStartMainGroup();

				EditorGUI.BeginDisabledGroup(Application.isPlaying);
				editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_FurShapeMap").displayName, "Encoded map of fur combing (RG), height (B), and density (A)"), GetProperty("_FurShapeMap"));
				EditorGUI.EndDisabledGroup();
				if (furDataMapTI == null && !Application.isPlaying)
				{
					if (GUILayout.Button("Click here to generate a blank fur shape data map")) GenerateFurDataMap();
					EditorGUI.BeginDisabledGroup(true);
				}

				GUILayout.BeginHorizontal();
				editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_FurShapeMask1").displayName, "Contains up to 4 optional height masks"), GetProperty("_FurShapeMask1"));
				MaskSelection bits = (MaskSelection)GetFloat("_FurShapeMask1Bits");
				MaskSelection newBits = (MaskSelection)EditorGUILayout.EnumFlagsField(bits);
				if (bits != newBits) SetProperty("_FurShapeMask1Bits", (float)newBits);
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_FurShapeMask2").displayName, "Contains up to 4 optional height masks"), GetProperty("_FurShapeMask2"));
				bits = (MaskSelection)GetFloat("_FurShapeMask2Bits");
				newBits = (MaskSelection)EditorGUILayout.EnumFlagsField(bits);
				if (bits != newBits) SetProperty("_FurShapeMask2Bits", (float)newBits);
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_FurShapeMask3").displayName, "Contains up to 4 optional height masks"), GetProperty("_FurShapeMask3"));
				bits = (MaskSelection)GetFloat("_FurShapeMask3Bits");
				newBits = (MaskSelection)EditorGUILayout.EnumFlagsField(bits);
				if (bits != newBits) SetProperty("_FurShapeMask3Bits", (float)newBits);
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_FurShapeMask4").displayName, "Contains up to 4 optional height masks"), GetProperty("_FurShapeMask4"));
				bits = (MaskSelection)GetFloat("_FurShapeMask4Bits");
				newBits = (MaskSelection)EditorGUILayout.EnumFlagsField(bits);
				if (bits != newBits) SetProperty("_FurShapeMask4Bits", (float)newBits);
				GUILayout.EndHorizontal();

				editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_FurGroomingMask").displayName, "Used during Fur Grooming. It can be used as a length mask, or it can be copied from."), GetProperty("_FurGroomingMask"));

				EditorGUILayout.Space();

				if (variant != Variant.UltraLite) EditorGUILayout.LabelField("Max fur: " + Mathf.Round(furThickness * 1000f) + "mm thick, " + Mathf.Round(layerDensity * 100f) + "% density", titleStyle);
				else EditorGUILayout.LabelField("Max fur: " + Mathf.Round(furThickness * 1000f) + "mm thick", titleStyle);

				GUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				GUIProperty("_ScaleCalibration", "Calibrates the length of the fur relative to the avatar scaling");
				EditorGUI.EndDisabledGroup();
				EditorGUI.BeginDisabledGroup(Application.isPlaying);
				if (GUILayout.Button("Increase")) SetProperty("_ScaleCalibration", GetFloat("_ScaleCalibration") * 2f);
				if (GUILayout.Button("Decrease")) SetProperty("_ScaleCalibration", GetFloat("_ScaleCalibration") * 0.5f);
				if (GUILayout.Button("Reset"))
				{
					SetProperty("_ScaleCalibration", -1f);
					initialized = false;
				}
				EditorGUI.EndDisabledGroup();

				GUILayout.EndHorizontal();
				GUIProperty("_FurShellSpacing", "Spacing between each rendered layer of fur");
				GUIProperty("_FurMinHeight", "Hides any hair shorter than the cutoff");
				if (variant != Variant.UltraLite)
				{
					if (GetFloat("_FurMinHeight") < 0.01f)
					{
						EditorGUILayout.HelpBox("Setting the minimum fur height below 0.01 may cause the Fur Grooming masking to behave incorrectly, due to texture compression artifacts.", MessageType.Info);
					}
				}
				if (variant != Variant.UltraLite) EditorGUILayout.Space();
				GUIProperty("_FurCombStrength", "Base strength of fur combing");
				EditorGUILayout.Space();

				GUIProperty("_BodyShrinkOffset", "Shrinks the body proportionally to the length of the fur");
				GUIProperty("_BodyExpansion", "If enabled, the body layer will expand when far away");
				GUIProperty("_BodyResizeCutoff", "Don't resize the body if the fur thickness is below the cutoff");

				EditorGUI.EndDisabledGroup();

				WFFGUIEndMainGroup();
			}
			#endregion

			#region "Hair Map"
			//--------------------------------------------------------------------------------
			// Hairs
			if (WFFFoldout("_HairsGroup"))
			{
				WFFGUIStartMainGroup();

				editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_HairMap").displayName, "Encoded map of individual hair hightlights (R), tinting (G), and height (B)"), GetProperty("_HairMap"));
				if (variant != Variant.UltraLite) editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_HairMapCoarse").displayName, "A coarse version of the hair map that does not include fine hairs. This version of the map is used at longer ranges, or when the viewing angle is sharp."), GetProperty("_HairMapCoarse"));

				// Hair Map Generation
				if (WFFFoldout("_GenerateHairGroup"))
				{
					WFFGUIStartSubGroup();

					if (!Application.isPlaying)
					{
						if (hairDataMapTI == null)
						{
							if (GUILayout.Button("Click here to generate hair maps")) GenerateHairMap();
						}
						else
						{
							if (GUILayout.Button("Click here to generate new hair maps (will overwrite existing maps!)")) GenerateHairMap();
						}
						EditorGUILayout.Space();
					}

					GUIProperty("_GenGuardHairs", "Maximum number of Guard Hairs");
					GUIProperty("_GenGuardHairsTaper", "The shape of the Guard Hairs");
					GUIProperty("_GenGuardHairMinHeight", "Minimum Guard Hair Height");
					GUIProperty("_GenGuardHairMaxHeight", "Maximum Guard Hair Height");
					GUIProperty("_GenGuardHairMinColourShift", "Minimum Guard Hair ColourShift");
					GUIProperty("_GenGuardHairMaxColourShift", "Maximum Guard Hair ColourShift");
					GUIProperty("_GenGuardHairMinHighlight", "Minimum Guard Hair Highlight");
					GUIProperty("_GenGuardHairMaxHighlight", "Maximum Guard Hair Highlight");
					GUIProperty("_GenGuardHairMaxOverlap", "How many pixels of overlap with neighbouring hairs should be allowed?");
					{
						String[] defaultProperties = { "_GenGuardHairs", "_GenGuardHairsTaper", "_GenGuardHairMinHeight", "_GenGuardHairMaxHeight",
							"_GenGuardHairMinColourShift", "_GenGuardHairMaxColourShift", "_GenGuardHairMinHighlight", "_GenGuardHairMaxHighlight","_GenGuardHairMaxOverlap"};
						if (!CheckDefaults(defaultProperties))
						{
							if (GUILayout.Button("Click here to reset guard hair settings to defaults"))
							{
								SetDefaults(defaultProperties);
							}
						}
					}
					EditorGUILayout.Space();

					GUIProperty("_GenMediumHairs", "Maximum number of Medium Hairs");
					GUIProperty("_GenMediumHairsTaper", "The shape of the Medium Hairs");
					GUIProperty("_GenMediumHairMinHeight", "Minimum Medium Hair Height");
					GUIProperty("_GenMediumHairMaxHeight", "Maximum Medium Hair Height");
					GUIProperty("_GenMediumHairMinColourShift", "Minimum Medium Hair ColourShift");
					GUIProperty("_GenMediumHairMaxColourShift", "Maximum Medium Hair ColourShift");
					GUIProperty("_GenMediumHairMinHighlight", "Minimum Medium Hair Highlight");
					GUIProperty("_GenMediumHairMaxHighlight", "Maximum Medium Hair Highlight");
					GUIProperty("_GenMediumHairMaxOverlap", "How many pixels of overlap with neighbouring hairs should be allowed?");
					{
						String[] defaultProperties = { "_GenMediumHairs", "_GenMediumHairsTaper", "_GenMediumHairMinHeight", "_GenMediumHairMaxHeight",
							"_GenMediumHairMinColourShift", "_GenMediumHairMaxColourShift", "_GenMediumHairMinHighlight", "_GenMediumHairMaxHighlight","_GenMediumHairMaxOverlap"};
						if (!CheckDefaults(defaultProperties))
						{
							if (GUILayout.Button("Click here to reset medium hair settings to defaults"))
							{
								SetDefaults(defaultProperties);
							}
						}
					}
					EditorGUILayout.Space();

					GUIProperty("_GenFineHairs", "Maximum number of Fine Hairs");
					GUIProperty("_GenFineHairsTaper", "The shape of the Fine Hairs");
					GUIProperty("_GenFineHairMinHeight", "Minimum Fine Hair Height");
					GUIProperty("_GenFineHairMaxHeight", "Maximum Fine Hair Height");
					GUIProperty("_GenFineHairMinColourShift", "Minimum Fine Hair ColourShift");
					GUIProperty("_GenFineHairMaxColourShift", "Maximum Fine Hair ColourShift");
					GUIProperty("_GenFineHairMinHighlight", "Minimum Fine Hair Highlight");
					GUIProperty("_GenFineHairMaxHighlight", "Maximum Fine Hair Highlight");
					GUIProperty("_GenFineHairMaxOverlap", "How many pixels of overlap with neighbouring hairs should be allowed?");
					{
						String[] defaultProperties = { "_GenFineHairs", "_GenFineHairsTaper", "_GenFineHairMinHeight", "_GenFineHairMaxHeight",
							"_GenFineHairMinColourShift", "_GenFineHairMaxColourShift", "_GenFineHairMinHighlight", "_GenFineHairMaxHighlight","_GenFineHairMaxOverlap"};
						if (!CheckDefaults(defaultProperties))
						{
							if (GUILayout.Button("Click here to reset fine hair settings to defaults"))
							{
								SetDefaults(defaultProperties);
							}
						}
					}

					WFFGUIEndSubGroup();
				}


				EditorGUILayout.Space();
				GUIProperty("_HairDensity", "Base density of individual hairs");
				GUIProperty("_HairTransparency", "How see-through the hairs are.");

				EditorGUILayout.Space();
				GUIProperty("_HairClipping", "Makes hairs longer, but the geometric shells do not move, so the tops of tall hairs are clipped off");

				EditorGUILayout.Space();
				EditorGUI.BeginDisabledGroup(hairDataMapCoarseTI == null);
				GUIProperty("_HairMapCoarseStrength", "Strength (ie. height) of the Coarse Hair Map");
				EditorGUI.EndDisabledGroup();
				EditorGUILayout.Space();

				GUIProperty("_HairMipType", "Chooses either 'Box' filtering (blurry), or 'Kaiser' filtering (sharp) for the mip-maps");
				if (variant != Variant.UltraLite)
				{
					if (GetFloat("_HairSharpen") > 0 && GetFloat("_HairBlur") > 0) SetProperty("_HairBlur", 0);
					EditorGUI.BeginDisabledGroup(GetFloat("_HairBlur") > 0);
					GUIProperty("_HairSharpen", "Sharpens the appearance of the hairs, but also causes visual noise");
					EditorGUI.EndDisabledGroup();
					EditorGUI.BeginDisabledGroup(GetFloat("_HairSharpen") > 0);
					GUIProperty("_HairBlur", "Blurs the appearance of the hairs, and also reduces visual noise");
					EditorGUI.EndDisabledGroup();
				}

				EditorGUILayout.Space();
				GUIProperty("_HairStiffness", "Controls how easily hairs bend (due to gravity, wind, and movement). Note that stiff hairs will bend more at the roots than flexible hairs, thus flattening their appearance.");
				EditorGUILayout.Space();


				if (WFFFoldout("_HairsColourGroup"))
				{
					WFFGUIStartSubGroup();

					GUIProperty("_HairHighlights", "Strength of individual hair highlights");
					GUIProperty("_HairColourShift", "Strength of individual hair tinting");
					EditorGUILayout.Space();

					if (variant != Variant.UltraLite)
					{
						EditorGUILayout.Space();

						if (WFFFoldout("_AdvancedHairColourGroup"))
						{
							WFFGUIStartSubGroup();

							GUIProperty("_AdvancedHairColour", "Enable hairs to have different colours along their length");

							if (GetFloat("_AdvancedHairColour") < 0.5) EditorGUI.BeginDisabledGroup(true);
							else EditorGUILayout.HelpBox("This feature is still in development and should be considered experimental and SUBJECT TO CHANGE. It currently doesn't fade in correctly over distance.", MessageType.Warning);

							GUIProperty("_HairRootColour", "Base colour of the roots of the hairs");
							GUIProperty("_HairMidColour", "Base colour of the middle of the hairs");
							GUIProperty("_HairTipColour", "Base colour of the tips of the hairs");
							EditorGUILayout.Space();
							GUIProperty("_HairRootAlbedo", "How much does the albedo map affect the roots of the hairs");
							GUIProperty("_HairMidAlbedo", "How much does the albedo map affect the middle of the hairs");
							GUIProperty("_HairTipAlbedo", "How much does the albedo map affect the tips of the hairs");
							EditorGUILayout.Space();
							GUIProperty("_HairRootMarkings", "How much does the markings map affect the roots of the hairs");
							GUIProperty("_HairMidMarkings", "How much does the markings map affect the middle of the hairs");
							GUIProperty("_HairTipMarkings", "How much does the markings map affect the tips of the hairs");
							EditorGUILayout.Space();
							GUIProperty("_HairRootPoint", "Sets where the root of the hair starts to fade into the middle");
							GUIProperty("_HairMidLowPoint", "Sets where the middle of the hair starts to fade into the root");
							GUIProperty("_HairMidHighPoint", "Sets where the middle of the hair starts to fade into the tip");
							GUIProperty("_HairTipPoint", "Sets where the tip of the hair starts to fade into the middle");
							EditorGUILayout.Space();
							GUIProperty("_HairColourMinHeight", "Only apply advanced colouring to fur thicker than the minimum height");

							if (GetFloat("_AdvancedHairColour") > 0.5)
							{
								String[] hairColourProperties = {"_HairRootColour","_HairMidColour","_HairTipColour","_HairRootAlbedo","_HairMidAlbedo","_HairTipAlbedo",
							"_HairRootMarkings","_HairMidMarkings","_HairTipMarkings","_HairRootPoint","_HairMidLowPoint","_HairMidHighPoint","_HairTipPoint","_HairColourMinHeight"};
								if (!CheckDefaults(hairColourProperties))
								{
									if (GUILayout.Button("Click here to reset advanced hair colour settings to defaults"))
									{
										SetDefaults(hairColourProperties);
									}
								}
							}
							EditorGUI.EndDisabledGroup();

							WFFGUIEndSubGroup();
						}
					}

					WFFGUIEndSubGroup();
				}


				EditorGUILayout.Space();
				if (variant != Variant.UltraLite)
				{
					if (WFFFoldout("_HairCurlsGroup"))
					{
						WFFGUIStartSubGroup();

						GUIProperty("_HairCurlsActive", "Enables Hair Curls");
						bool curlsActive = GetFloat("_HairCurlsActive") > 0;
						EditorGUI.BeginDisabledGroup(!curlsActive);
						bool lockActive = GetFloat("_HairCurlsLockXY") > 0;
						GUIProperty("_HairCurlsLockXY", "Lock the X and Y axis settings");
						GUIProperty("_HairCurlXWidth", "How wide are the hair curls on the X axis");
						GUIProperty("_HairCurlXTwists", "How many twists are there in the X axis");
						EditorGUI.EndDisabledGroup();
						EditorGUI.BeginDisabledGroup(!curlsActive || lockActive);
						GUIProperty("_HairCurlYWidth", "How wide are the hair curls on the Y axis");
						GUIProperty("_HairCurlYTwists", "How many twists are there in the Y axis");
						EditorGUI.EndDisabledGroup();
						EditorGUI.BeginDisabledGroup(!curlsActive);
						GUIProperty("_HairCurlXYOffset", "The phase shift between the X and Y axis");
						if (lockActive)
						{
							SetProperty("_HairCurlYWidth", GetFloat("_HairCurlXWidth"));
							SetProperty("_HairCurlYTwists", GetFloat("_HairCurlXTwists"));
						}
						EditorGUI.EndDisabledGroup();

						WFFGUIEndSubGroup();
					}
					EditorGUILayout.Space();

				}

				if (variant != Variant.UltraLite)
				{
					if (WFFFoldout("_HairRenderingGroup"))
					{
						WFFGUIStartSubGroup();

						GUIProperty("_TiltEdges", "Tilts the edges of the fur geometry towards the camera. This can reduce edge artifacts on some avatars, but may cause problems on others.");
						EditorGUILayout.Space();

						GUIProperty("_HairMapAlphaFilter", "Boosts the lengths of the hairs using the alpha channel for length calibration");
						GUIProperty("_HairMapMipFilter", "Boosts the lengths of the hairs based on the mip map level");

						EditorGUILayout.Space();
						EditorGUI.BeginDisabledGroup(hairDataMapCoarseTI == null);
						GUIProperty("_HairMapCoarseAlphaFilter", "Boosts the lengths of the hairs using the alpha channel for length calibration");
						GUIProperty("_HairMapCoarseMipFilter", "Boosts the lengths of the hairs based on the mip map level");
						EditorGUI.EndDisabledGroup();

						String[] defaultProperties = { "_TiltEdges", "_HairMapAlphaFilter", "_HairMapMipFilter", "_HairMapCoarseAlphaFilter", "_HairMapCoarseMipFilter" };
						if (!CheckDefaults(defaultProperties))
						{
							if (GUILayout.Button("Click here to reset Advanced Hair Rendering Adjustments to defaults"))
							{
								SetDefaults(defaultProperties);
							}
						}

						WFFGUIEndSubGroup();
					}
				}

				EditorGUI.EndDisabledGroup();

				WFFGUIEndMainGroup();
			}
			#endregion

			#region "Fur Patterns"
			//--------------------------------------------------------------------------------
			// Tiled Fur Markings
			if (variant != Variant.UltraLite)
			{
				if (WFFFoldout("_MarkingsGroup"))
				{
					WFFGUIStartMainGroup();

					editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_MarkingsMap").displayName, "Tiled, secondary albedo (RGB) map of the fur. Does not affect the skin."), GetProperty("_MarkingsMap"), GetProperty("_MarkingsColour"));

					// Fur Markings Generation
					if (WFFFoldout("_GenerateFurGroup"))
					{
						WFFGUIStartSubGroup();

						if (!Application.isPlaying)
						{
							string buttonText = "Click here to generate random fur markings";
							if (markingsMapTI != null) buttonText = "Generate random fur markings (will overwrite existing texture!)";

							if (GUILayout.Button(buttonText))
							{
								GenerateFunctions gen = new GenerateFunctions();
								string assetPath = "Assets/FastFur_" + this.editor.target.name + "_Markings.png";
								if (markingsMapTI != null) assetPath = markingsMapTI.assetPath;

								gen.GenerateFurMarkings(this, assetPath);
								Texture2D myTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
								((Material)this.editor.target).SetTexture("_MarkingsMap", myTexture);

								CheckImports();
							}
						}
						EditorGUILayout.Space();

						GUIProperty("_PigmentColour", "Colour of pigmented cells (spots/stripes/etc...)");
						GUIProperty("_TransitionalColour", "Colour of transitional cells");
						GUIProperty("_BaseColour", "Colour of non-pigmented cells");
						GUIProperty("_MarkingsContrast", "Contrast between pigmented and non-pigmented cells");
						EditorGUILayout.Space();
						GUIProperty("_ActivatorHormoneRadius", "Inner-circle radius of pigmented cell activator hormones");
						GUIProperty("_InhibitorHormoneAdditionalRadius", "Additional outer-ring radius of pigmented cell inhibitor hormones.");
						GUIProperty("_InhibitorStrength", "Strength of inhibitor hormones");
						EditorGUILayout.Space();
						GUIProperty("_CellStretch", "Cell elliptical stretching");
						//GUIProperty("_InitialDensity","Starting density of pigmented cells");
						GUIProperty("_MutationRate", "Rate of random cell mutations");
						GUIProperty("_ActivatorCycles", "Number of cell activator cycles");
						//GUIProperty("_GrowthCycles","Number of cell growth cycles (happens after activator cycles)");

						WFFGUIEndSubGroup();
					}
					EditorGUILayout.Space();


					EditorGUI.BeginDisabledGroup(markingsMapTI == null);
					GUIProperty("_MarkingsDensity", "Tile density of the fur markings");
					GUIProperty("_MarkingsRotation", "Rotation of fur markings");
					EditorGUILayout.Space();
					GUIProperty("_MarkingsVisibility", "Visibility of the fur markings albedo");
					GUIProperty("_MarkingsHeight", "If positive, brighter coloured areas will raise the fur height, while darker coloured areas will lower the fur height");
					EditorGUILayout.Space();
					EditorGUI.EndDisabledGroup();

					WFFGUIEndMainGroup();
				}
			}
			#endregion

			#region "Lighting and Shadow"
			//--------------------------------------------------------------------------------
			// Light Settings
			if (WFFFoldout("_LightingGroup"))
			{
				WFFGUIStartMainGroup();

				GUIProperty("_OverallBrightness", "Applies a universal multiplier to the brightness level that affects everything.");
				EditorGUILayout.Space();

				if (materialEditor.EmissionEnabledProperty())
				{
					WFFGUIStartSubGroup();

					editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_EmissionMap").displayName, "Emission (RGB) of the skin and fur"), GetProperty("_EmissionMap"), GetProperty("_EmissionColor"));
					GUIProperty("_EmissionMapStrength", "Overall strength of the Emission Map");
					GUIProperty("_AlbedoEmission", "Emission that is the same colour as the Albedo Map.");
					EditorGUILayout.Space();

					if (variant != Variant.UltraLite)
					{
						GUIProperty("_EmissionHueShift", "Spectrally shifts the emission map colour. This shift happens AFTER all other colour calculations.");
						GUIProperty("_EmissionHueShiftCycle", "Selects the method to animate the hue shift.");
						EditorGUI.BeginDisabledGroup(GetFloat("_EmissionHueShiftCycle") == 0);
						GUIProperty("_EmissionHueShiftRate", "Controls the speed of the hue shift animation.");
						EditorGUI.EndDisabledGroup();
						EditorGUILayout.Space();

						GUIProperty("_EmissionMapAudioLinkEnable", "Enable AudioLink for the Emission Map.");
						if (GetFloat("_EmissionMapAudioLinkEnable") > 0)
						{
							WFFGUIStartSubGroup();
							GUIProperty("_EmissionMapAudioLinkLayers", "Enables either the static-height emission layers, the dynamic-height emission layer, or both.");
							GUIProperty("_EmissionMapAudioLinkBassColor", "When bass audio is detected, the Emission Map will be multiplied by this colour and added as addional emission.");
							GUIProperty("_EmissionMapAudioLinkLowMidColor", "When low-mid audio is detected, the Emission Map will be multiplied by this colour and added as addional emission.");
							GUIProperty("_EmissionMapAudioLinkHighMidColor", "When high-mid audio is detected, the Emission Map will be multiplied by this colour and added as addional emission.");
							GUIProperty("_EmissionMapAudioLinkTrebleColor", "When treble audio is detected, the Emission Map will be multiplied by this colour and added as addional emission.");
							WFFGUIEndSubGroup();
						}
						GUIProperty("_EmissionMapLumaGlowZone", "Applies Furality Luma Glow Zone (which is the same as 'AudioLink Theme') to the emission map.");
						GUIProperty("_EmissionMapLumaGlowGradient", "Applies Furality Luma Glow Gradient to the emission map, with the gradient applied along the lengths of the hairs.");
					}

					WFFGUIEndSubGroup();
				}
				EditorGUILayout.Space();


				if (variant != Variant.UltraLite)
				{
					if (WFFFoldout("_WorldLightGroup"))
					{
						WFFGUIStartSubGroup();

						GUIProperty("_MaxBrightness", "Applies a hard-limit to the maximum brightness of each light source.");
						GUIProperty("_SoftenBrightness", "Softens the brightness of light sources with an intensity greater than 0.75, but doesn't impose a hard-limit.");
						GUIProperty("_WorldLightReColour", "Re-colour world lighting.");
						GUIProperty("_WorldLightReColourStrength", "Re-colour strength.");
						String[] defaultProperties = { "_MaxBrightness", "_SoftenBrightness", "_WorldLightReColour", "_WorldLightReColourStrength" };
						if (!CheckDefaults(defaultProperties))
						{
							if (GUILayout.Button("Click here to reset world lighting settings to defaults"))
							{
								SetDefaults(defaultProperties);
							}
						}

						WFFGUIEndSubGroup();
					}
					EditorGUILayout.Space();


					if (WFFFoldout("_ExtraLightingGroup"))
					{
						WFFGUIStartSubGroup();

						GUIProperty("_FallbackLightEnable", "Enable a fallback directional light if a world does not have one.");
						EditorGUI.BeginDisabledGroup(GetFloat("_FallbackLightEnable") == 0);
						EditorGUILayout.Space();
						{
							GUIProperty("_FallbackLightColor", "Fallback directional lighting colour.");
							GUIProperty("_FallbackLightStrength", "Fallback directional lighting brightness.");
							GUIProperty("_FallbackLightDirection", "Fallback directional lighting horizontal direction.");
							GUIProperty("_FallbackLightAngle", "Fallback directional vertical direction.");
							String[] defaultProperties = { "_FallbackLightColor", "_FallbackLightStrength", "_FallbackLightDirection", "_FallbackLightAngle" };
							if (!CheckDefaults(defaultProperties))
							{
								if (GUILayout.Button("Click here to reset fallback lighting settings to defaults"))
								{
									SetDefaults(defaultProperties);
								}
							}
						}
						EditorGUI.EndDisabledGroup();
						EditorGUILayout.Space();
						GUIProperty("_ExtraLightingEnable", "Enable extra lighting.");
						EditorGUI.BeginDisabledGroup(GetFloat("_ExtraLightingEnable") == 0);
						EditorGUILayout.Space();
						{
							GUIProperty("_ExtraLighting", "Strength of the extra ambient lighting.");
							GUIProperty("_ExtraLightingRim", "Does the extra ambient lighting appear to come from behind or from the front?");
							GUIProperty("_ExtraLightingColor", "Extra ambient lighting colour.");
							GUIProperty("_ExtraLightingMode", "Determines how the extra ambient lighting is applied. It can be set to either always add extra light, or it can only add enough extra light when-needed to meet a minumum lighting level.");
							String[] defaultProperties = { "_ExtraLighting", "_ExtraLightingRim", "_ExtraLightingColor", "_ExtraLightingMode" };
							if (!CheckDefaults(defaultProperties))
							{
								if (GUILayout.Button("Click here to reset extra lighting settings to defaults"))
								{
									SetDefaults(defaultProperties);
								}
							}
						}
						EditorGUI.EndDisabledGroup();

						WFFGUIEndSubGroup();
					}
					EditorGUILayout.Space();
				}

				if (WFFFoldout("_AnisotropicGroup"))
				{
					WFFGUIStartSubGroup();

					GUIProperty("_FurAnisotropicEnable", "Simulates the way light reacts with individual strands of hair");
					EditorGUI.BeginDisabledGroup(GetFloat("_FurAnisotropicEnable") == 0);

					EditorGUILayout.Space();
					GUIProperty("_FurAnisotropicReflect", "Simulates light reflecting off of the front surface of the hairs");
					GUIProperty("_FurAnisoReflectAngle", "Sets the angle at which light reflects off of the front surface of the hairs");
					GUIProperty("_FurAnisoReflectGloss", "How glossy (ie. concentrated) are the anisotropic reflections");
					GUIProperty("_FurAnisoReflectMetallic", "How much does the light change colour to match the surface colour when reflected");
					GUIProperty("_FurAnisotropicReflectColor", "Applies a colour tint to the anisotropic reflected light");
					GUIProperty("_FurAnisotropicReflectColorNeg", "Applies a colour tint to the anisotropic reflected light when it is 'red-shifted' by iridescence");
					GUIProperty("_FurAnisotropicReflectColorPos", "Applies a colour tint to the anisotropic reflected light when it is 'blue-shifted' by iridescence");
					GUIProperty("_FurAnisoReflectIridescenceStrength", "How vibrant the iridescence should be");
					GUIProperty("_FurAnisoReflectEmission", "Adds emission to the anisotropic reflections");
					EditorGUILayout.Space();
					GUIProperty("_FurAnisotropicRefract", "Simulates light entering the hairs, bouncing off of the back surface, and re-emerging out the front at a refracted angle");
					GUIProperty("_FurAnisoRefractAngle", "Sets the angle at which light refracts when bouncing off of the back surface of the hairs");
					GUIProperty("_FurAnisoRefractGloss", "How glossy (ie. concentrated) are the anisotropic refractions");
					GUIProperty("_FurAnisoRefractMetallic", "How much does the light change colour to match the surface colour when refracted");
					GUIProperty("_FurAnisotropicRefractColor", "Applies a colour tint to the anisotropic refracted light");
					GUIProperty("_FurAnisoRefractEmission", "Adds emission to the anisotropic refractions");
					EditorGUILayout.Space();
					GUIProperty("_FurAnisoDepth", "How deep do anisotropic reflections go into the fur");
					GUIProperty("_FurAnisoSkin", "Adds a baseline amount of anisotropic light to both skin and fur, regardless of depth");
					GUIProperty("_FurAnisoFlat", "Changes the appearance of the anisotropic lighting by artificially flattening the hairs against the skin before calculating the anisotropic reflection/refraction angles.");

					String[] defaultProperties = {"_FurAnisotropicReflect","_FurAnisoReflectAngle","_FurAnisoReflectGloss","_FurAnisoReflectMetallic","_FurAnisotropicReflectColor","_FurAnisotropicReflectColorPos","_FurAnisotropicReflectColorNeg","_FurAnisoReflectIridescenceStrength",
						"_FurAnisoReflectEmission","_FurAnisoRefractEmission","_FurAnisotropicRefract","_FurAnisotropicRefractColor","_FurAnisoRefractAngle","_FurAnisoRefractGloss","_FurAnisoRefractMetallic","_FurAnisoDepth","_FurAnisoSkin","_FurAnisoFlat"};
					if (!CheckDefaults(defaultProperties))
					{
						if (GUILayout.Button("Click here to reset anisotropic settings to defaults"))
						{
							SetDefaults(defaultProperties);
						}
					}
					EditorGUI.EndDisabledGroup();

					WFFGUIEndSubGroup();
				}
				EditorGUILayout.Space();



				if (WFFFoldout("_FurLightingGroup"))
				{
					WFFGUIStartSubGroup();

					GUIProperty("_LightWraparound", "Simulates light passing along the surface at a steep angle and being caught by the tips of the fur");
					GUIProperty("_SubsurfaceScattering", "Simulates the light being absorbed and scattered internally before passing out of the fur, thus allowing some of it to be visible from behind");
					String[] defaultProperties = { "_LightWraparound", "_SubsurfaceScattering" };
					if (!CheckDefaults(defaultProperties))
					{
						if (GUILayout.Button("Click here to reset supplemental fur lighting settings to defaults"))
						{
							SetDefaults(defaultProperties);
						}
					}

					WFFGUIEndSubGroup();
				}
				EditorGUILayout.Space();


				if (WFFFoldout("_OcclusionGroup"))
				{
					WFFGUIStartSubGroup();

					if (variant != Variant.UltraLite)
					{
						editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_OcclusionMap").displayName, "Occlusion Map"), GetProperty("_OcclusionMap"), GetProperty("_OcclusionStrength"));
						EditorGUILayout.Space();
					}

					GUIProperty("_DeepFurOcclusionStrength", "Strength of darkening as light penetrates deeper into fur");
					GUIProperty("_LightPenetrationDepth", "How far does the light penetrate before any occlusion is applied");
					EditorGUILayout.Space();
					GUIProperty("_ProximityOcclusion", "How much does the light get occluded when the camera is close");
					GUIProperty("_ProximityOcclusionRange", "How far away can the camera be to cause occlusion");
					if (variant != Variant.UltraLite) EditorGUILayout.Space();
					if(GetFloat("_FurTouchStrength") > 0)
					{
						EditorGUI.BeginDisabledGroup(true);
						GUIProperty("_FurShadowCastSize", "How much of the fur should block light sources and cast shadows");
						EditorGUI.EndDisabledGroup();
						EditorGUILayout.HelpBox("Shadow Casting Size is forced to 0 when Touch Response is enabled.",MessageType.Info);
					}
					else GUIProperty("_FurShadowCastSize", "How much of the fur should block light sources and cast shadows");

					GUIProperty("_SoftenShadows", "Converts a percentage of world lighting into ambient lighting.");
					String[] defaultProperties = { "_DeepFurOcclusionStrength", "_LightPenetrationDepth", "_ProximityOcclusion", "_ProximityOcclusionRange", "_FurShadowCastSize", "_SoftenShadows" };
					if (!CheckDefaults(defaultProperties))
					{
						if (GUILayout.Button("Click here to reset occlusion and shadow settings to defaults"))
						{
							SetDefaults(defaultProperties);
						}
					}

					WFFGUIEndSubGroup();
				}

				WFFGUIEndMainGroup();
			}
			#endregion

			#region "Gravity, Wind, and Movement"
			//--------------------------------------------------------------------------------
			// Gravity, Wind, and Movement
			if (WFFFoldout("_DynamicsGroup"))
			{
				WFFGUIStartMainGroup();

				GUIProperty("_FurGravitySlider", "Gravity strength");
				if (variant != Variant.UltraLite)
				{
					EditorGUILayout.Space();
					GUIProperty("_FurTouchStrength", "Strength of Touch Response.");
					if(GetFloat("_FurTouchStrength") > 0 && GetFloat("_FurShadowCastSize") > 0)
					{
						EditorGUILayout.HelpBox("Shadow Casting Size is forced to 0 when Touch Response is enabled.",MessageType.Info);
					}
					EditorGUI.BeginDisabledGroup(GetFloat("_FurTouchStrength") == 0);
					GUIProperty("_FurTouchRange", "How far away should the fur start to react to touch?");
					GUIProperty("_FurTouchThreshold", "Sets the threshold for areas where Touch Response is disabled.");
					if (GetFloat("_FurTouchStrength") > 0 && occlusionMapTI == null)
					{
						EditorGUILayout.HelpBox("The fur contact detection requires a valid Occlusion Map in order to prevent it from falsely triggering in body crevices.", MessageType.Warning);
					}
					GUIProperty("_FurDebugContact", "Show which areas have contact detection enabled");
					EditorGUI.EndDisabledGroup();
				}

				EditorGUILayout.Space();
				GUIProperty("_AudioLinkHairVibration", "Vibrates the hairs if AudioLink is detected.");

				EditorGUILayout.Space();

				if (WFFFoldout("_WindGroup"))
				{
					WFFGUIStartSubGroup();

					GUIProperty("_EnableWind", "Enables wind");
					EditorGUI.BeginDisabledGroup(GetFloat("_EnableWind") == 0);
					EditorGUILayout.Space();

					GUIProperty("_WindSpeed", "Overall wind speed");
					EditorGUILayout.Space();
					GUIProperty("_WindDirection", "The horizonal direction of the wind");
					GUIProperty("_WindAngle", "The vertical direction of the wind");
					EditorGUILayout.Space();
					GUIProperty("_WindTurbulenceStrength", "Strength and frequency of random turbulence");
					GUIProperty("_FurWindShimmer", "Controls the amount that wind turbulence will cause reflective light to shimmer.");
					EditorGUILayout.Space();
					GUIProperty("_WindGustsStrength", "Strength and frequency of gusts of wind");
					EditorGUILayout.Space();

					String[] defaultProperties = { "_WindSpeed", "_WindDirection", "_WindAngle", "_WindTurbulenceStrength", "_WindGustsStrength", "_FurWindShimmer" };
					if (!CheckDefaults(defaultProperties))
					{
						if (GUILayout.Button("Click here to reset wind settings to defaults"))
						{
							SetDefaults(defaultProperties);
						}
					}
					EditorGUI.EndDisabledGroup();

					WFFGUIEndSubGroup();
				}
				EditorGUILayout.Space();

				if (variant != Variant.UltraLite)
				{
					if (WFFFoldout("_MovementGroup"))
					{
						WFFGUIStartSubGroup();

						EditorGUILayout.HelpBox("This feature allows the fur to react to movement, but unfortunately it requires some rather advanced animation controller configuration. Currently, this must be done manually for each avatar.", MessageType.Info);
						//if (GUILayout.Button("Click here to create or update VR Chat movement animations"));// CreateAnimations(properties);
						GUIProperty("_MovementStrength", "How much does the fur bend when the avatar moves");
						GUIProperty("_VelocityX", "Simulates movement in the X dimension");
						GUIProperty("_VelocityY", "Simulates movement in the Y dimension");
						GUIProperty("_VelocityZ", "Simulates movement in the Z dimension");

						WFFGUIEndSubGroup();
					}
				}

				WFFGUIEndMainGroup();
			}
			#endregion

			#region "Toon Shading"
			//--------------------------------------------------------------------------------
			// Toon Shading
			if (variant != Variant.UltraLite)
			{
				if (WFFFoldout("_ToonShadingGroup"))
				{
					WFFGUIStartMainGroup();

					GUIProperty("_ToonShading", "Simulates a toon effect by limiting the fur colours into discrete steps");
					EditorGUILayout.Space();

					if (WFFFoldout("_ToonColoursGroup"))
					{
						WFFGUIStartSubGroup();

						GUIProperty("_ToonColour1", "The original colour will be substituted by whichever colour substitution is closest");
						GUIProperty("_ToonColour2", "The original colour will be substituted by whichever colour substitution is closest");
						GUIProperty("_ToonColour3", "The original colour will be substituted by whichever colour substitution is closest");
						GUIProperty("_ToonColour4", "The original colour will be substituted by whichever colour substitution is closest");
						GUIProperty("_ToonColour5", "The original colour will be substituted by whichever colour substitution is closest");
						GUIProperty("_ToonColour6", "The original colour will be substituted by whichever colour substitution is closest");
						GUIProperty("_ToonColour7", "The original colour will be substituted by whichever colour substitution is closest");
						GUIProperty("_ToonColour8", "The original colour will be substituted by whichever colour substitution is closest");
						GUIProperty("_ToonColour9", "The original colour will be substituted by whichever colour substitution is closest");

						WFFGUIEndSubGroup();
					}
					EditorGUI.BeginDisabledGroup(GetFloat("_ToonShading") == 0);
					EditorGUILayout.Space();

					if (WFFFoldout("_ToonPostEffectsGroup"))
					{
						WFFGUIStartSubGroup();


						if (WFFFoldout("_ToonHue"))
						{
							WFFGUIStartSubGroup();

							GUIProperty("_ToonHueRGB", "Standard Colour Mix");
							GUIProperty("_ToonHueGBR", "Shifted Colour Mix");
							GUIProperty("_ToonHueBRG", "Shifted Colour Mix");
							GUIProperty("_ToonHueRBG", "Shifted Colour Mix");
							GUIProperty("_ToonHueGRB", "Shifted Colour Mix");
							GUIProperty("_ToonHueBGR", "Shifted Colour Mix");
							WFFGUIEndSubGroup();
						}

						EditorGUILayout.Space();
						GUIProperty("_ToonBrightness", "Adjusts the overall brightness");
						GUIProperty("_ToonWhiten", "Blends in some subtle white shading to dark areas");

						WFFGUIEndSubGroup();
					}
					EditorGUILayout.Space();

					if (WFFFoldout("_ToonLightingGroup"))
					{
						WFFGUIStartSubGroup();

						GUIProperty("_ToonLighting", "Simulates a toon lighting effect by reducing the lighting to 3 discrete steps");
						EditorGUI.BeginDisabledGroup(GetFloat("_ToonLighting") == 0);
						GUIProperty("_ToonLightingHigh", "The bright light colour");
						GUIProperty("_ToonLightingMid", "The normal light colour");
						GUIProperty("_ToonLightingShadow", "The shadow colour");
						EditorGUILayout.Space();
						GUIProperty("_ToonLightingHighLevel", "The threshold between normal and bright light");
						GUIProperty("_ToonLightingHighSoftEdge", "Blur the toon lighting bright transitional edge");
						GUIProperty("_ToonLightingShadowLevel", "The threshold between light and shadow");
						GUIProperty("_ToonLightingShadowSoftEdge", "Blur the toon lighting shadow transitional edge");
						EditorGUI.EndDisabledGroup();

						WFFGUIEndSubGroup();
					}
					EditorGUI.EndDisabledGroup();

					WFFGUIEndMainGroup();
				}
			}
			#endregion

			#region "Texture Utilities"
			//--------------------------------------------------------------------------------
			// Texture Utilities
			if (WFFFoldout("_UtilitiesGroup"))
			{
				WFFGUIStartMainGroup();

				editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_UtilitySourceMap").displayName, "The source map"), GetProperty("_UtilitySourceMap"));
				editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_UtilityTargetMap").displayName, "The target map"), GetProperty("_UtilityTargetMap"));
				bool sourceNull = GetProperty("_UtilitySourceMap").textureValue == null;

				TextureImporter sourceMapTI = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(GetProperty("_UtilitySourceMap").textureValue));
				TextureImporter targetMapTI = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(GetProperty("_UtilityTargetMap").textureValue));

				if (sourceMapTI != null)
				{
					if (sourceMapTI.sRGBTexture)
					{
						sourceMapTI.sRGBTexture = false;
						sourceMapTI.SaveAndReimport();
					}
				}

				if (targetMapTI != null)
				{
					if (targetMapTI.sRGBTexture)
					{
						targetMapTI.sRGBTexture = false;
						targetMapTI.SaveAndReimport();
					}
				}

				if (GetProperty("_UtilityTargetMap").textureValue == null)
				{
					EditorGUILayout.Space();
					GUIProperty("_UtilityNewResolution", "New Copy Resolution");
					EditorGUILayout.Space();
					//--------------------------------------------------------------------------------
					// Make a new texture copy
					//--------------------------------------------------------------------------------

					if (GUILayout.Button(sourceNull ? "Click here to create a blank white target texture" : "Click here to create a new copy of the source texture"))
					{
						int resolution = 256 * (int)(Mathf.Pow(2, GetFloat("_UtilityNewResolution")));
						string newAssetPath = "Assets/FastFur_" + this.editor.target.name + "_BlankMap";
						byte[] bytes;

						if (!sourceNull)
						{
							Texture sourceTexture = GetProperty("_UtilitySourceMap").textureValue;
							RenderTexture newRenderTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
							Graphics.Blit(sourceTexture, newRenderTexture);

							Texture2D outputTexture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false, true);
							RenderTexture.active = newRenderTexture;
							outputTexture.ReadPixels(new Rect(0, 0, outputTexture.width, outputTexture.height), 0, 0);
							outputTexture.Apply();
							RenderTexture.active = null;

							bytes = outputTexture.EncodeToPNG();

							string assetPath = AssetDatabase.GetAssetPath(GetProperty("_UtilitySourceMap").textureValue.GetInstanceID());
							newAssetPath = assetPath.Substring(0, assetPath.IndexOf(".", assetPath.Length - 4)) + "_Copy";
						}
						else
						{
							Texture2D outputTexture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false, true);

							var newPixels = outputTexture.GetPixels();
							var newColour = new Color(1f, 1f, 1f, 1f);
							for (int x = 0; x < newPixels.Length; x++) newPixels[x] = newColour;

							outputTexture.SetPixels(newPixels);

							bytes = outputTexture.EncodeToPNG();
						}

						int tries = 0;
						while (File.Exists(newAssetPath + ".png") && tries < 20)
						{
							newAssetPath += "_Copy";
							tries++;
						}
						if (tries < 20)
						{
							System.IO.File.WriteAllBytes(newAssetPath + ".png", bytes);
							AssetDatabase.Refresh();

							Texture2D myTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(newAssetPath + ".png");
							((Material)this.editor.target).SetTexture("_UtilityTargetMap", myTexture);
						}
					}
				}

				else if (!sourceNull)
				{
					EditorGUILayout.HelpBox("Warning! The target texture will be overwritten and converted to ARGB32 format! If an error occurs you may lose the contents permanently! DO NOT CONTINUE without making a backup first!", MessageType.Warning);

					EditorGUILayout.Space();
					GUIProperty("_UtilityFunction", "Function to perform");
					int function = (int)GetFloat("_UtilityFunction");
					EditorGUILayout.Space();

					if (function == 0)
					{
						//--------------------------------------------------------------------------------
						// Copy a channel
						//--------------------------------------------------------------------------------
						GUIProperty("_UtilityInvert", "Inverts the copy, so that 0=1 and 1=0");
						GUIProperty("_UtilitySourceChannel", "Source channel");
						GUIProperty("_UtilityTargetChannel", "Target channel");
						EditorGUILayout.Space();

						if (GUILayout.Button("Click here to copy the Source Channel to the Target Channel"))
						{
							int resolution = GetProperty("_UtilityTargetMap").textureValue.width;
							RenderTexture sourceRenderTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
							RenderTexture targetRenderTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
							Graphics.Blit(GetProperty("_UtilitySourceMap").textureValue, sourceRenderTexture);
							Graphics.Blit(GetProperty("_UtilityTargetMap").textureValue, targetRenderTexture);

							Texture2D sourceTexture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false, true);
							Texture2D targetTexture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false, true);

							sourceTexture.filterMode = FilterMode.Point;
							targetTexture.filterMode = FilterMode.Point;

							RenderTexture.active = sourceRenderTexture;
							sourceTexture.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
							sourceTexture.Apply();
							RenderTexture.active = targetRenderTexture;
							targetTexture.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
							targetTexture.Apply();
							RenderTexture.active = null;

							Color[] sourcePixels = sourceTexture.GetPixels(0, 0, resolution, resolution);
							Color[] targetPixels = targetTexture.GetPixels(0, 0, resolution, resolution);

							int sourceChannel = (int)GetFloat("_UtilitySourceChannel");
							int targetChannel = (int)GetFloat("_UtilityTargetChannel");
							bool invert = GetFloat("_UtilityInvert") > 0;
							for (int x = 0; x < resolution; x++)
							{
								for (int y = 0; y < resolution; y++)
								{
									int index = x + y * resolution;

									float value = 0;
									if (sourceChannel == 0) value = sourcePixels[index].r;
									if (sourceChannel == 1) value = sourcePixels[index].g;
									if (sourceChannel == 2) value = sourcePixels[index].b;
									if (sourceChannel == 3) value = sourcePixels[index].a;

									if (invert) value = 1 - value;

									if (targetChannel == 0) targetPixels[index].r = value;
									if (targetChannel == 1) targetPixels[index].g = value;
									if (targetChannel == 2) targetPixels[index].b = value;
									if (targetChannel == 3) targetPixels[index].a = value;
								}
							}


							Texture2D outputTexture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false, true);
							outputTexture.SetPixels(targetPixels);

							byte[] bytes = outputTexture.EncodeToPNG();
							if (bytes.Length > 128)
							{
								string assetPath = AssetDatabase.GetAssetPath(GetProperty("_UtilityTargetMap").textureValue.GetInstanceID());
								System.IO.File.WriteAllBytes(assetPath, bytes);
								AssetDatabase.Refresh();
							}
						}
					}
					else if (function == 1)
					{
						//--------------------------------------------------------------------------------
						// Apply a mask
						//--------------------------------------------------------------------------------
						GUIProperty("_UtilitySourceMask", "Source mask channel");
						GUIProperty("_UtilityMaskType", "Target channel");
						GUIProperty("_UtilityMaskThreshold", "Mask cutoff threshold");
						GUIProperty("_UtilityTargetChannel", "Target channel");
						EditorGUILayout.Space();
						if (GUILayout.Button("Click here to apply the Source Mask to the Target Channel"))
						{
							int resolution = GetProperty("_UtilityTargetMap").textureValue.width;
							RenderTexture sourceRenderTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
							RenderTexture targetRenderTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
							Graphics.Blit(GetProperty("_UtilitySourceMap").textureValue, sourceRenderTexture);
							Graphics.Blit(GetProperty("_UtilityTargetMap").textureValue, targetRenderTexture);

							Texture2D sourceTexture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false, true);
							Texture2D targetTexture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false, true);

							sourceTexture.filterMode = FilterMode.Point;
							targetTexture.filterMode = FilterMode.Point;

							RenderTexture.active = sourceRenderTexture;
							sourceTexture.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
							sourceTexture.Apply();
							RenderTexture.active = targetRenderTexture;
							targetTexture.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
							targetTexture.Apply();
							RenderTexture.active = null;

							Color[] sourcePixels = sourceTexture.GetPixels(0, 0, resolution, resolution);
							Color[] targetPixels = targetTexture.GetPixels(0, 0, resolution, resolution);

							int channel = (int)GetFloat("_UtilitySourceMask");
							float threshold = GetFloat("_UtilityMaskThreshold");
							bool ifAbove = GetFloat("_UtilityMaskType") > 0.5;
							int targetChannel = (int)GetFloat("_UtilityTargetChannel");

							for (int x = 0; x < resolution; x++)
							{
								for (int y = 0; y < resolution; y++)
								{
									int index = x + y * resolution;

									float maskValue = 0;
									if (channel == 0) maskValue = sourcePixels[index].r;
									if (channel == 1) maskValue = sourcePixels[index].g;
									if (channel == 2) maskValue = sourcePixels[index].b;
									if (channel == 3) maskValue = sourcePixels[index].a;

									if (maskValue <= threshold ^ ifAbove)
									{
										if (targetChannel == 0) targetPixels[index].r = 0;
										if (targetChannel == 1) targetPixels[index].g = 0;
										if (targetChannel == 2) targetPixels[index].b = 0;
										if (targetChannel == 3) targetPixels[index].a = 0;
									}
								}
							}

							Texture2D outputTexture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false, true);
							outputTexture.SetPixels(targetPixels);

							byte[] bytes = outputTexture.EncodeToPNG();
							if (bytes.Length > 128)
							{
								string assetPath = AssetDatabase.GetAssetPath(GetProperty("_UtilityTargetMap").textureValue.GetInstanceID());
								System.IO.File.WriteAllBytes(assetPath, bytes);
								AssetDatabase.Refresh();
							}
						}
					}
					else if (function <= 4)
					{
						//--------------------------------------------------------------------------------
						// Re-scale a channel
						//--------------------------------------------------------------------------------
						GUIProperty("_UtilityReScale", "Re-scaling factor");
						EditorGUILayout.Space();
						if (GUILayout.Button("Click here to re-scale the Source " + (function == 2 ? "Combing Strength" : function == 3 ? "Length" : "Density") + " onto the Target"))
						{
							int resolution = GetProperty("_UtilityTargetMap").textureValue.width;
							RenderTexture sourceRenderTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
							RenderTexture targetRenderTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
							Graphics.Blit(GetProperty("_UtilitySourceMap").textureValue, sourceRenderTexture);
							Graphics.Blit(GetProperty("_UtilityTargetMap").textureValue, targetRenderTexture);

							Texture2D sourceTexture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false, true);
							Texture2D targetTexture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false, true);

							sourceTexture.filterMode = FilterMode.Point;
							targetTexture.filterMode = FilterMode.Point;

							RenderTexture.active = sourceRenderTexture;
							sourceTexture.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
							sourceTexture.Apply();
							RenderTexture.active = targetRenderTexture;
							targetTexture.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
							targetTexture.Apply();
							RenderTexture.active = null;

							Color[] sourcePixels = sourceTexture.GetPixels(0, 0, resolution, resolution);
							Color[] targetPixels = targetTexture.GetPixels(0, 0, resolution, resolution);

							float scale = GetFloat("_UtilityReScale");

							for (int x = 0; x < resolution; x++)
							{
								for (int y = 0; y < resolution; y++)
								{
									int index = x + y * resolution;

									if (function == 3) targetPixels[index].b = sourcePixels[index].b * scale;
									if (function == 4) targetPixels[index].a = Mathf.Round(sourcePixels[index].a * scale * 64) / 64;
									if (function == 2)
									{
										Vector2 combing = new Vector2(sourcePixels[index].r * 2 - 1, sourcePixels[index].g * 2 - 1);
										combing = combing.normalized * Mathf.Min(1, combing.magnitude * scale);
										targetPixels[index].r = combing.x * 0.5f + 0.5f;
										targetPixels[index].g = combing.y * 0.5f + 0.5f;
									}
								}
							}

							Texture2D outputTexture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false, true);
							outputTexture.SetPixels(targetPixels);

							byte[] bytes = outputTexture.EncodeToPNG();
							if (bytes.Length > 128)
							{
								string assetPath = AssetDatabase.GetAssetPath(GetProperty("_UtilityTargetMap").textureValue.GetInstanceID());
								System.IO.File.WriteAllBytes(assetPath, bytes);
								AssetDatabase.Refresh();
							}
						}
					}
					else if (function == 5)
					{
						//--------------------------------------------------------------------------------
						// Fill Target Channel
						//--------------------------------------------------------------------------------
						GUIProperty("_UtilityTargetChannel", "Target channel");
						GUIProperty("_UtilityValue", "Value to write into the target channel");
						float value = GetFloat("_UtilityValue");
						EditorGUILayout.Space();
						if (GUILayout.Button("Click here to Fill the Target Channel"))
						{
							int resolution = GetProperty("_UtilityTargetMap").textureValue.width;

							RenderTexture targetRenderTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
							Graphics.Blit(GetProperty("_UtilityTargetMap").textureValue, targetRenderTexture);

							Texture2D targetTexture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false, true);
							targetTexture.filterMode = FilterMode.Point;

							RenderTexture.active = targetRenderTexture;
							targetTexture.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
							targetTexture.Apply();
							RenderTexture.active = null;
							Color[] targetPixels = targetTexture.GetPixels(0, 0, resolution, resolution);

							int targetChannel = (int)GetFloat("_UtilityTargetChannel");
							for (int x = 0; x < resolution; x++)
							{
								for (int y = 0; y < resolution; y++)
								{
									int index = x + y * resolution;

									if (targetChannel == 0) targetPixels[index].r = value;
									if (targetChannel == 1) targetPixels[index].g = value;
									if (targetChannel == 2) targetPixels[index].b = value;
									if (targetChannel == 3) targetPixels[index].a = value;
								}
							}

							Texture2D outputTexture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false, true);
							outputTexture.SetPixels(targetPixels);

							byte[] bytes = outputTexture.EncodeToPNG();
							if (bytes.Length > 128)
							{
								string assetPath = AssetDatabase.GetAssetPath(GetProperty("_UtilityTargetMap").textureValue.GetInstanceID());
								System.IO.File.WriteAllBytes(assetPath, bytes);
								AssetDatabase.Refresh();
							}
						}
					}
				}

				WFFGUIEndMainGroup();
			}
			#endregion

			#region "Debugging"
			//--------------------------------------------------------------------------------
			// Debugging
			if (WFFFoldout("_DebuggingGroup"))
			{
				WFFGUIStartMainGroup();

				EditorGUILayout.LabelField("Render Queue = " + targetMat.renderQueue);
				EditorGUILayout.Space();

				GUIProperty("_DebuggingLog", "Send debugging messages to the console.");
				EditorGUILayout.Space();

				GUIProperty("_FurDebugDistance", "Used for debugging. The colours indicate the number of layers being rendered.");
				GUIProperty("_FurDebugMipMap", "Used for debugging. The colours indicate which mipmap level the video card is using.");
				GUIProperty("_FurDebugHairMap", "Used for debugging. The colours indicate which hair map (fine, coarse, very coarse) is being used.");
				GUIProperty("_FurDebugVerticies", "Used for debugging. Show the locations of the vertices.");
				GUIProperty("_FurDebugTopLayer", "Used for debugging. Only render the top layer.");
				GUIProperty("_FurDebugUpperLimit", "Used for debugging. Show outline of the highest rendered layer.");
				GUIProperty("_FurDebugDepth", "Used for debugging. The colours indicate the layer depth levels.");
				GUIProperty("_FurDebugQuality", "The colours indicate quality level, as reported by VR Chat.");
				GUIProperty("_FurDebugStereo", "The colours indicate the right (red) and left (blue) eyes are being rendered.");

				GUIProperty("_FurDebugContact", "The colours indicate whether contact detection is enabled.");
				GUIProperty("_FurDebugLength", "The colours indicate different levels of the fur length data.");
				GUIProperty("_FurDebugDensity", "The colours indicate different levels of the fur density data.");
				GUIProperty("_FurDebugCombing", "The colours indicate different levels of the fur combing data.");
				EditorGUILayout.Space();

				EditorGUI.BeginDisabledGroup(true);
				//GUIProperty("_HairDensityThreshold","The height level where 75% of the hairs are shorter.");
				//GUIProperty("_MarkingsMapHash","Hash code (to check if texture has changed)");
				//GUIProperty("_MarkingsMapPositiveCutoff","The maximum height of 99% of hairs when height is positive");
				//GUIProperty("_MarkingsMapNegativeCutoff","The maximum height of 99% of hairs when height is negative");
				//GUIProperty("_FurShapeMapHash","Hash code (to check if texture has changed)");
				//GUIProperty("_HairMapScaling3","Hash Map scaling for mip map 3");
				//GUIProperty("_HairMapScaling4","Hash Map scaling for mip map 4");
				//GUIProperty("_HairMapScaling5","Hash Map scaling for mip map 5");
				//GUIProperty("_HairMapHash","Hash code (to check if texture has changed)");
				EditorGUILayout.Space();

				EditorGUI.EndDisabledGroup();

				GUIProperty("_OverrideScale", "Overrides the fur thickness scaling by applying a multiplier");
				GUIProperty("_OverrideQualityBias", "Overrides the quality by adding extra level of detail");
				GUIProperty("_OverrideDistanceBias", "Overrides the view distance by adding extra distance");
				if (variant != Variant.UltraLite)
				{
					if (GetFloat("_OverrideDistanceBias") < 0 || GetFloat("_OverrideQualityBias") > 0)
					{
						EditorGUILayout.HelpBox("Please do not use Higher Quality versions of the shader when you are in public game lobbies!", MessageType.Warning);
					}
				}
				EditorGUILayout.Space();

				GUIProperty("_TS1", "This probably does nothing. I use it when I need a slider for testing.");
				GUIProperty("_TS2", "This probably does nothing. I use it when I need a slider for testing.");
				GUIProperty("_TS3", "This probably does nothing. I use it when I need a slider for testing.");
				if (variant != Variant.UltraLite)
				{
// REMOVED in RC.1 //					editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_TestMap").displayName), GetProperty("_TestMap"));
				}

				EditorGUI.indentLevel--;
				EditorGUILayout.Space();
				WFFGUIEndMainGroup();
			}
			EditorGUILayout.Space();

			if (variant != Variant.UltraLite)
			{
				if (GetFloat("_OverrideQualityBias") != 0 || GetFloat("_OverrideDistanceBias") != 0 || GetFloat("_OverrideScale") != 1)
				{
					EditorGUILayout.HelpBox("Debugging overrides are active! Setting these incorrectly can cause visual errors and/or performance drop.", MessageType.Warning);

					if (GUILayout.Button("Click here to reset debugging overrides"))
					{
						SetProperty("_OverrideScale", 1.0f);
						SetProperty("_OverrideQualityBias", 0.0f);
						SetProperty("_OverrideDistanceBias", 0.0f);
					}
				}
			}
			else
			{
				if (GetFloat("_OverrideDistanceBias") != 0)
				{
					EditorGUILayout.HelpBox("Debugging overrides are active! Setting these incorrectly can cause visual errors and/or performance drop.", MessageType.Warning);

					if (GUILayout.Button("Click here to reset debugging overrides"))
					{
						SetProperty("_OverrideDistanceBias", 0.0f);
					}
				}

			}

			EditorGUILayout.Space();
			#endregion

			#region "Final GUI Cleanup"

			if (!Application.isPlaying || !initialized) CheckImports();
			CalculateQuickStuff();
			CheckKeywords(targetMat);
			Shader.SetGlobalFloat("_VRChatCameraMode", -1f);

			//--------------------------------------------------------------------------------
			// Check various imports and calculations. Limit this to once per second.
			if (!initialized || Mathf.Abs(Time.realtimeSinceStartup - lastValidationTime) > 1.0)
			{
				CalculateSlowStuff();

				initialized = true;
				lastValidationTime = Time.realtimeSinceStartup;
			}

			runningOkay = true;
			#endregion
		}

		#region "Generate Blank Maps"
		//*************************************************************************************************************************************************
		// Generate Hair Maps
		//*************************************************************************************************************************************************
		private void GenerateHairMap()
		{
			GenerateFunctions gen = new GenerateFunctions();

			string assetPathA = "Assets/FastFur_" + this.editor.target.name + "_HairMap.png";
			if (hairDataMapTI != null) assetPathA = hairDataMapTI.assetPath;
			string assetPathB = assetPathA.Replace(".png", "Coarse.png");
			if (hairDataMapCoarseTI != null) assetPathB = hairDataMapCoarseTI.assetPath;

			gen.GenerateHairMap(this, assetPathA, assetPathB);

			AssetDatabase.Refresh();
			Texture2D myTextureA = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPathA);
			((Material)this.editor.target).SetTexture("_HairMap", myTextureA);
			Texture2D myTextureB = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPathB);
			((Material)this.editor.target).SetTexture("_HairMapCoarse", myTextureB);

			CheckImports();
		}


		//*************************************************************************************************************************************************
		// Generate a Fur Data Map
		//*************************************************************************************************************************************************
		public void GenerateFurDataMap()
		{
			Texture2D newTexture = new Texture2D(1024, 1024, TextureFormat.ARGB32, false, true);

			var newPixels = newTexture.GetPixels();
			var newColour = new Color(0.498f, 0.498f, 0.7f, 0.498f);
			for (int x = 0; x < newPixels.Length; x++) newPixels[x] = newColour;

			newTexture.SetPixels(newPixels);
			newTexture.Apply();

			byte[] bytes = newTexture.EncodeToPNG();
			string assetPath = "Assets/FastFur_" + this.editor.target.name + "_FurShape.png";
			if (furDataMapTI != null) assetPath = furDataMapTI.assetPath;

			System.IO.File.WriteAllBytes(assetPath, bytes);

			AssetDatabase.Refresh();
			Texture2D myTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
			((Material)this.editor.target).SetTexture("_FurShapeMap", myTexture);

			CheckImports();
		}
		#endregion

		#region "Check Keywords"
		//*************************************************************************************************************************************************
		// Check if various features are on or off. If not, then use a variant that strips out the unused code.
		//*************************************************************************************************************************************************
		private void CheckKeywords(Material targetMat)
		{
			if (variant == Variant.UltraLite) return;

			if (targetMat.globalIlluminationFlags != MaterialGlobalIlluminationFlags.EmissiveIsBlack) targetMat.EnableKeyword("_EMISSION");
			else targetMat.DisableKeyword("_EMISSION");

			if (markingsMapTI != null) SetProperty("_FurMarkingsActive", 1);
			else SetProperty("_FurMarkingsActive", 0);

			if (hairDataMapCoarseTI != null && GetFloat("_HairMapCoarseStrength") > 0) SetProperty("_CoarseMapActive", 1);
			else SetProperty("_CoarseMapActive", 0);

			bool debugging = GetFloat("_FurDebugDistance") == 1;
			debugging |= GetFloat("_FurDebugVerticies") == 1;
			debugging |= GetFloat("_FurDebugMipMap") == 1;
			debugging |= GetFloat("_FurDebugHairMap") == 1;
			debugging |= GetFloat("_FurDebugDensity") == 1;
			debugging |= GetFloat("_FurDebugLength") == 1;
			debugging |= GetFloat("_FurDebugDepth") == 1;
			debugging |= GetFloat("_FurDebugTopLayer") == 1;
			debugging |= GetFloat("_FurDebugUpperLimit") == 1;
			debugging |= GetFloat("_FurDebugCombing") == 1;
			debugging |= GetFloat("_FurDebugQuality") == 1;
			debugging |= GetFloat("_FurDebugStereo") == 1;
			debugging |= GetFloat("_FurDebugContact") == 1;

			if (debugging) targetMat.EnableKeyword("FUR_DEBUGGING");
			else targetMat.DisableKeyword("FUR_DEBUGGING");

			if (normalMapTI != null && GetFloat("_PBSSkin") > 0) targetMat.EnableKeyword("_NORMALMAP");
			else targetMat.DisableKeyword("_NORMALMAP");

			if (metallicMapTI != null) targetMat.EnableKeyword("_METALLICGLOSSMAP");
			else targetMat.DisableKeyword("_METALLICGLOSSMAP");

			if (specularMapTI != null) targetMat.EnableKeyword("_SPECGLOSSMAP");
			else targetMat.DisableKeyword("_SPECGLOSSMAP");

			if (specularMapTI != null) targetMat.EnableKeyword("_SPECGLOSSMAP");
			else targetMat.DisableKeyword("_SPECGLOSSMAP");

			if (GetFloat("_SmoothnessTextureChannel") == (float)SmoothnessMapChannel.AlbedoAlpha) targetMat.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
			else targetMat.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");

			if (variant != Variant.Soft && variant != Variant.SoftLite && variant != Variant.UltraLite)
			{
				if (GetFloat("_TwoSided") > 0)
				{
					targetMat.EnableKeyword("FXAA");
					SetProperty("_Cull", 0);
				}
				else
				{
					targetMat.DisableKeyword("FXAA");
					SetProperty("_Cull", 2);
				}

				int maxLayers = (int)GetFloat("_MaximumLayers");
				if (pipeline == Pipeline.Pipeline3 || maxLayers == 2)
				{
					targetMat.DisableKeyword("FXAA_LOW");
					targetMat.DisableKeyword("FXAA_KEEP_ALPHA");
				}
				else if (maxLayers == 0)
				{
					targetMat.EnableKeyword("FXAA_LOW");
					targetMat.DisableKeyword("FXAA_KEEP_ALPHA");
				}
				else
				{
					targetMat.DisableKeyword("FXAA_LOW");
					targetMat.EnableKeyword("FXAA_KEEP_ALPHA");
				}
			}

			// Only Opaque and Cutout modes are supported
			if (GetFloat("_Mode") > 1) SetProperty("_Mode", 0);
			if (GetFloat("_Mode") == 0)
			{
				targetMat.DisableKeyword("_ALPHATEST_ON");
				targetMat.DisableKeyword("_ALPHABLEND_ON");
				targetMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			}
			else
			{
				targetMat.EnableKeyword("_ALPHATEST_ON");
				targetMat.DisableKeyword("_ALPHABLEND_ON");
				targetMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			}


			// This is for backwards-compatibility with the older "_MinBrightness" setting
			float minBrightness = GetFloat("_MinBrightness");
			if (minBrightness > 0)
			{
				SetProperty("_MinBrightness", 0f);
				SetProperty("_ExtraLightingEnable", 1f);
				SetProperty("_ExtraLighting", minBrightness);
				SetProperty("_ExtraLightingRim", 0f);
				SetProperty("_ExtraLightingMode", 1f);
			}
		}
		#endregion

		#region "Check Import Settings"
		//*************************************************************************************************************************************************
		// Check that the various texture imports are configured correctly (ex. some need to be linear, some need to be no compression)
		//*************************************************************************************************************************************************
		private void CheckImports()
		{
			furDataMapTI = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(GetProperty("_FurShapeMap").textureValue));
			hairDataMapTI = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(GetProperty("_HairMap").textureValue));
			if (variant != Variant.UltraLite)
			{
				furDataMask1TI = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(GetProperty("_FurShapeMask1").textureValue));
				furDataMask2TI = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(GetProperty("_FurShapeMask2").textureValue));
				furDataMask3TI = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(GetProperty("_FurShapeMask3").textureValue));
				furDataMask4TI = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(GetProperty("_FurShapeMask4").textureValue));
				hairDataMapCoarseTI = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(GetProperty("_HairMapCoarse").textureValue));
				markingsMapTI = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(GetProperty("_MarkingsMap").textureValue));
				normalMapTI = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(GetProperty("_BumpMap").textureValue));
				metallicMapTI = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(GetProperty("_MetallicGlossMap").textureValue));
				specularMapTI = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(GetProperty("_SpecGlossMap").textureValue));
				occlusionMapTI = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(GetProperty("_OcclusionMap").textureValue));
			}

			// This should always be a TextureImporter, but we check, just in case somebody makes a weird texture type. If they do, then it's up to them to set this stuff correctly.
			if (furDataMapTI is TextureImporter && furDataMapTI != null)
			{
				if (((TextureImporter)furDataMapTI).sRGBTexture || ((TextureImporter)furDataMapTI).crunchedCompression
				|| !((TextureImporter)furDataMapTI).textureCompression.Equals(TextureImporterCompression.CompressedHQ) || !((TextureImporter)furDataMapTI).streamingMipmaps)
				{
					((TextureImporter)furDataMapTI).sRGBTexture = false;
					((TextureImporter)furDataMapTI).textureCompression = TextureImporterCompression.CompressedHQ;
					((TextureImporter)furDataMapTI).crunchedCompression = false;
					((TextureImporter)furDataMapTI).streamingMipmaps = true;
					furDataMapTI.SaveAndReimport();
				}
			}

			if (variant != Variant.UltraLite)
			{
				// This should always be a TextureImporter, but we check, just in case somebody makes a weird texture type. If they do, then it's up to them to set this stuff correctly.
				if (furDataMask1TI is TextureImporter && furDataMask1TI != null)
				{
					if (((TextureImporter)furDataMask1TI).sRGBTexture || ((TextureImporter)furDataMask1TI).crunchedCompression)
					{
						((TextureImporter)furDataMask1TI).sRGBTexture = false;
						((TextureImporter)furDataMask1TI).crunchedCompression = false;
						((TextureImporter)furDataMask1TI).streamingMipmaps = true;
						furDataMask1TI.SaveAndReimport();
					}
				}

				// This should always be a TextureImporter, but we check, just in case somebody makes a weird texture type. If they do, then it's up to them to set this stuff correctly.
				if (furDataMask2TI is TextureImporter && furDataMask2TI != null)
				{
					if (((TextureImporter)furDataMask2TI).sRGBTexture || ((TextureImporter)furDataMask2TI).crunchedCompression
					|| !((TextureImporter)furDataMask2TI).streamingMipmaps)
					{
						((TextureImporter)furDataMask2TI).sRGBTexture = false;
						((TextureImporter)furDataMask2TI).crunchedCompression = false;
						((TextureImporter)furDataMask2TI).streamingMipmaps = true;
						furDataMask2TI.SaveAndReimport();
					}
				}

				// This should always be a TextureImporter, but we check, just in case somebody makes a weird texture type. If they do, then it's up to them to set this stuff correctly.
				if (furDataMask3TI is TextureImporter && furDataMask3TI != null)
				{
					if (((TextureImporter)furDataMask3TI).sRGBTexture || ((TextureImporter)furDataMask3TI).crunchedCompression
					|| !((TextureImporter)furDataMask3TI).streamingMipmaps)
					{
						((TextureImporter)furDataMask3TI).sRGBTexture = false;
						((TextureImporter)furDataMask3TI).crunchedCompression = false;
						((TextureImporter)furDataMask3TI).streamingMipmaps = true;
						furDataMask3TI.SaveAndReimport();
					}
				}

				// This should always be a TextureImporter, but we check, just in case somebody makes a weird texture type. If they do, then it's up to them to set this stuff correctly.
				if (furDataMask4TI is TextureImporter && furDataMask4TI != null)
				{
					if (((TextureImporter)furDataMask4TI).sRGBTexture || ((TextureImporter)furDataMask4TI).crunchedCompression
					|| !((TextureImporter)furDataMask4TI).streamingMipmaps)
					{
						((TextureImporter)furDataMask4TI).sRGBTexture = false;
						((TextureImporter)furDataMask4TI).crunchedCompression = false;
						((TextureImporter)furDataMask4TI).streamingMipmaps = true;
						furDataMask4TI.SaveAndReimport();
					}
				}
			}


			int mipType = (int)GetFloat("_HairMipType"); // 0 = Box, 1 = Kaiser

			// This should always be a TextureImporter, but we check, just in case somebody makes a weird texture type. If they do, then it's up to them to set this stuff correctly.
			if (hairDataMapTI is TextureImporter && hairDataMapTI != null)
			{
				if (((TextureImporter)hairDataMapTI).sRGBTexture || ((TextureImporter)hairDataMapTI).crunchedCompression || ((TextureImporter)hairDataMapTI).textureCompression != TextureImporterCompression.CompressedHQ
				|| !((TextureImporter)hairDataMapTI).streamingMipmaps || ((TextureImporter)hairDataMapTI).mipmapFilter != (TextureImporterMipFilter)mipType || !((TextureImporter)hairDataMapTI).isReadable)
				{
					((TextureImporter)hairDataMapTI).sRGBTexture = false;
					((TextureImporter)hairDataMapTI).textureCompression = TextureImporterCompression.CompressedHQ;
					((TextureImporter)hairDataMapTI).crunchedCompression = false;
					((TextureImporter)hairDataMapTI).streamingMipmaps = true;
					((TextureImporter)hairDataMapTI).mipmapFilter = (TextureImporterMipFilter)mipType;
					((TextureImporter)hairDataMapTI).isReadable = true;
					hairDataMapTI.SaveAndReimport();
				}
			}

			// This should always be a TextureImporter, but we check, just in case somebody makes a weird texture type. If they do, then it's up to them to set this stuff correctly.
			if (variant != Variant.UltraLite)
			{
				if (hairDataMapCoarseTI is TextureImporter && hairDataMapCoarseTI != null)
				{
					if (((TextureImporter)hairDataMapCoarseTI).sRGBTexture || ((TextureImporter)hairDataMapCoarseTI).crunchedCompression || ((TextureImporter)hairDataMapCoarseTI).textureCompression != TextureImporterCompression.CompressedHQ
					|| !((TextureImporter)hairDataMapCoarseTI).streamingMipmaps || ((TextureImporter)hairDataMapCoarseTI).mipmapFilter != (TextureImporterMipFilter)mipType)
					{
						((TextureImporter)hairDataMapCoarseTI).sRGBTexture = false;
						((TextureImporter)hairDataMapCoarseTI).textureCompression = TextureImporterCompression.CompressedHQ;
						((TextureImporter)hairDataMapCoarseTI).crunchedCompression = false;
						((TextureImporter)hairDataMapCoarseTI).streamingMipmaps = true;
						((TextureImporter)hairDataMapCoarseTI).mipmapFilter = (TextureImporterMipFilter)mipType;
						hairDataMapCoarseTI.SaveAndReimport();
					}
				}
			}
		}
		#endregion

		#region "Check Property Defaults"
		//*************************************************************************************************************************************************
		// Check or reset default property values
		//*************************************************************************************************************************************************
		bool CheckDefaults(String[] propertyNames)
		{
			bool isDefault = true;

			foreach (String propertyName in propertyNames)
			{
				int index = FindPropertyIndex(propertyName);
				if (index >= 0)
				{
					if (targetMat.shader.GetPropertyType(index) == UnityEngine.Rendering.ShaderPropertyType.Float || targetMat.shader.GetPropertyType(index) == UnityEngine.Rendering.ShaderPropertyType.Range)
					{
						if (GetFloat(propertyName) != targetMat.shader.GetPropertyDefaultFloatValue(index)) isDefault = false;
					}
					if (targetMat.shader.GetPropertyType(index) == UnityEngine.Rendering.ShaderPropertyType.Vector)
					{
						if (GetProperty(propertyName).vectorValue != targetMat.shader.GetPropertyDefaultVectorValue(index)) isDefault = false;
					}
					if (targetMat.shader.GetPropertyType(index) == UnityEngine.Rendering.ShaderPropertyType.Color)
					{
						Vector4 colour = targetMat.shader.GetPropertyDefaultVectorValue(index);
						if (!GetProperty(propertyName).colorValue.Equals(new Color(colour.x, colour.y, colour.z, colour.w))) isDefault = false;
					}
				}
			}

			return isDefault;
		}

		void SetDefaults(String[] propertyNames)
		{
			foreach (String propertyName in propertyNames)
			{
				int index = FindPropertyIndex(propertyName);
				if (index >= 0)
				{
					if (targetMat.shader.GetPropertyType(index) == UnityEngine.Rendering.ShaderPropertyType.Float || targetMat.shader.GetPropertyType(index) == UnityEngine.Rendering.ShaderPropertyType.Range)
					{
						SetProperty(propertyName, targetMat.shader.GetPropertyDefaultFloatValue(index));
					}
					if (targetMat.shader.GetPropertyType(index) == UnityEngine.Rendering.ShaderPropertyType.Vector)
					{
						SetProperty(propertyName, targetMat.shader.GetPropertyDefaultVectorValue(index));
					}
					if (targetMat.shader.GetPropertyType(index) == UnityEngine.Rendering.ShaderPropertyType.Color)
					{
						Vector4 colour = targetMat.shader.GetPropertyDefaultVectorValue(index);
						SetProperty(propertyName, new Color(colour.x, colour.y, colour.z, colour.w));
					}
				}
			}
		}
		#endregion

		#region "Calculate Quick Stuff"
		//*************************************************************************************************************************************************
		// Calculate various properties automatically
		//*************************************************************************************************************************************************
		private void CalculateQuickStuff()
		{
			if (variant != Variant.UltraLite)
			{
				// Ensure that the hair fade sliders remain in the correct order
				float[] hairFade = new float[4];
				hairFade[0] = GetFloat("_HairRootPoint");
				hairFade[1] = GetFloat("_HairMidLowPoint");
				hairFade[2] = GetFloat("_HairMidHighPoint");
				hairFade[3] = GetFloat("_HairTipPoint");
				bool doCheck = true;
				while (doCheck)
				{
					doCheck = false;
					for (int x = 2; x >= 0; x--)
					{
						if (hairFade[x] >= hairFade[x + 1])
						{
							if (hairFade[x] == recentFadePoints[x]) hairFade[x] = hairFade[x + 1] - 0.001f;
							else hairFade[x + 1] = hairFade[x] + 0.001f;
							doCheck = true;
						}
					}

					for (int x = 0; x <= 2; x++)
					{
						if (hairFade[x + 1] <= hairFade[x])
						{
							if (hairFade[x] == recentFadePoints[x]) hairFade[x] = hairFade[x + 1] - 0.001f;
							else hairFade[x + 1] = hairFade[x] + 0.001f;
							doCheck = true;
						}
					}
					if (doCheck)
					{
						SetProperty("_HairRootPoint", Mathf.Max(0.000f, Mathf.Min(0.997f, hairFade[0])));
						SetProperty("_HairMidLowPoint", Mathf.Max(0.001f, Mathf.Min(0.998f, hairFade[1])));
						SetProperty("_HairMidHighPoint", Mathf.Max(0.002f, Mathf.Min(0.999f, hairFade[2])));
						SetProperty("_HairTipPoint", Mathf.Max(0.003f, Mathf.Min(1.000f, hairFade[3])));
					}
				}
				recentFadePoints = hairFade;


				// Calibrate the Hue sliders so that the total brightness remains the same
				float[] toonHues = new float[6];
				toonHues[0] = GetFloat("_ToonHueRGB");
				toonHues[1] = GetFloat("_ToonHueGBR");
				toonHues[2] = GetFloat("_ToonHueBRG");
				toonHues[3] = GetFloat("_ToonHueRBG");
				toonHues[4] = GetFloat("_ToonHueBGR");
				toonHues[5] = GetFloat("_ToonHueGRB");
				float total = 0;
				foreach (float toonHue in toonHues) total += toonHue;
				if (Mathf.Abs(total - 1) > 0.001)
				{
					float weightAdjusted = total - 1;

					int z = 0;
					for (z = 0; z < 10; z++)
					{
						float selectedWeight = 0;
						float recentSelectedWeight = 0;
						total = 0;

						for (int x = 0; x < 6; x++)
						{
							if (toonHues[x] != recentToonHues[x])
							{
								selectedWeight = toonHues[x];
								recentSelectedWeight = recentToonHues[x];
							}
							total += toonHues[x];
						}
						if (Mathf.Abs(total - 1) > 0.001)
						{
							if (Mathf.Abs(recentSelectedWeight - 1) < 0.001)
							{
								if (toonHues[0] == 0)
								{
									toonHues[0] = 1 - selectedWeight;
									recentToonHues[0] = 1 - selectedWeight;
								}
								else
								{
									toonHues[1] = 1 - selectedWeight;
									recentToonHues[1] = 1 - selectedWeight;
								}
							}
							else
							{
								float otherWeights = total - selectedWeight;
								for (int x = 0; x < 6; x++)
								{
									if (toonHues[x] == recentToonHues[x]) toonHues[x] -= (weightAdjusted * toonHues[x]) / otherWeights;
								}
							}
						}
						else break;
					}

					if (z == 10)
					{
						toonHues[0] = 1.0f;
						for (int x = 1; x < 6; x++) toonHues[x] = 0f;
					}

					SetProperty("_ToonHueRGB", toonHues[0]);
					SetProperty("_ToonHueGBR", toonHues[1]);
					SetProperty("_ToonHueBRG", toonHues[2]);
					SetProperty("_ToonHueRBG", toonHues[3]);
					SetProperty("_ToonHueBGR", toonHues[4]);
					SetProperty("_ToonHueGRB", toonHues[5]);
				}
				recentToonHues = toonHues;
			}

			// Calculate statistics about the fur thickness
			if (variant == Variant.UltraLite) furThickness = normalMagnitidue * GetFloat("_ScaleCalibration") * GetFloat("_FurShellSpacing") * 0.95f;
			else furThickness = normalMagnitidue * GetFloat("_ScaleCalibration") * GetFloat("_OverrideScale") * GetFloat("_FurShellSpacing") * 0.95f;
			layerDensity = Mathf.Max((Mathf.Min(furThickness, 0.025f) + 0.024f) / 2f, (Mathf.Min(0.05f, furThickness) + Mathf.Min(0.035f, furThickness)) / 2f) / furThickness;
		}
		#endregion

		#region "Calculate Slow Stuff"
		private void CalculateSlowStuff()
		{
			if (Application.isPlaying) return;

			TextureFunctions textureFunctions = new TextureFunctions();

			if (variant != Variant.UltraLite)
			{
				// Which pipeline is this?
				string shaderPath = AssetDatabase.GetAssetPath(Shader.Find("Warren's Fast Fur/Fast Fur - Lite")).Replace("/FastFur-Lite.shader", "");
				string pipelineFilePath = shaderPath + "/FastFur-Pipeline.cginc";
				string fileContents = System.IO.File.ReadAllText(pipelineFilePath);
				Pipeline filePipeline = (Pipeline)(-1);
				if (fileContents.Contains("#define PIPELINE1")) filePipeline = Pipeline.Pipeline1;
				if (fileContents.Contains("#define PIPELINE2")) filePipeline = Pipeline.Pipeline2;
				if (fileContents.Contains("#define PIPELINE3")) filePipeline = Pipeline.Pipeline3;
				bool doWrite = false;
				// If the file copy is invalid, we default to the V2 Pipeline
				if (filePipeline.Equals((Pipeline)(-1)))
				{
					pipeline = Pipeline.Pipeline2;
					doWrite = true;
				}
				// If our properties have been changed by the user, then write those changes
				if (pipeline != prevPipeline && prevPipeline >= Pipeline.Pipeline1)
				{
					doWrite = true;
				}
				// Write the changes. Unfortunately, the older version of Unity that VR Chat uses does not
				// support conditional #pragma statements, so we need to write changes directly to the
				// shader files themselves.
				if (doWrite)
				{
					if (pipeline != Pipeline.Pipeline3 || GetFloat("_ConfirmPipeline") > 0)
					{
						SetProperty("_RenderPipeline", (int)pipeline);
						fileContents = "#define PIPELINE" + (int)(pipeline + 1) + "\n";
						System.IO.File.WriteAllText(pipelineFilePath, fileContents);
						if ((pipeline.Equals(Pipeline.Pipeline1) || filePipeline.Equals(Pipeline.Pipeline1)) && !pipeline.Equals(filePipeline))
						{
							string[] shaderPaths = new string[3];
							shaderPaths[0] = shaderPath + "/FastFur.shader";
							shaderPaths[1] = shaderPath + "/FastFur-Lite.shader";
							shaderPaths[2] = shaderPath + "/FastFur-Semi-Transparent.shader";

							foreach (string path in shaderPaths)
							{
								if (System.IO.File.Exists(path))
								{
									string contents = System.IO.File.ReadAllText(path);
									if (pipeline.Equals(Pipeline.Pipeline1))
									{
										contents = contents.Replace("#pragma hull hull", "//#pragma hull hull");
										contents = contents.Replace("#pragma domain doma", "//#pragma domain doma");
										contents = contents.Replace("////#pragma hull hull", "//#pragma hull hull");
										contents = contents.Replace("////#pragma domain doma", "//#pragma domain doma");
									}
									else
									{
										contents = contents.Replace("//#pragma hull hull", "#pragma hull hull");
										contents = contents.Replace("//#pragma domain doma", "#pragma domain doma");
									}
									System.IO.File.WriteAllText(path, contents);
								}
							}
						}
						AssetDatabase.Refresh();
						prevPipeline = pipeline;
					}
				}
				else
				{
					// Update our properties to the match the file
					if (pipeline != filePipeline)
					{
						pipeline = filePipeline;
						SetProperty("_RenderPipeline", (int)filePipeline);
						SetProperty("_ConfirmPipeline", pipeline.Equals(Pipeline.Pipeline3) ? 1 : 0);
					}
					prevPipeline = pipeline;
				}


				// Fix non-standard property names
				Color oldColour = GetProperty("_Colour").colorValue;
				Color defaultColour = new Color(-1, -1, -1, -1);
				if (!oldColour.Equals(defaultColour))
				{
					SetProperty("_Color", oldColour);
					SetProperty("_Colour", defaultColour);
				}
				oldColour = GetProperty("_EmissionColour").colorValue;
				if (!oldColour.Equals(defaultColour))
				{
					SetProperty("_EmissionColor", oldColour);
					SetProperty("_EmissionColour", defaultColour);
				}


				// Create histograms of the hair length modifying textures and use them to calibrate the maximum hair length
				Texture2D hairMap = (Texture2D)GetProperty("_HairMap").textureValue;

				if (variant != Variant.UltraLite)
				{
					float hairMapHash = GetFloat("_HairMapHash");

					Texture2D MarkingsMap = (Texture2D)GetProperty("_MarkingsMap").textureValue;
					float MarkingsMapHash = GetFloat("_MarkingsMapHash");

					if (markingsMapTI != null)
					{
						if (MarkingsMapHash != MarkingsMap.imageContentsHash.GetHashCode())
						{
							SetProperty("_MarkingsMapPositiveCutoff", textureFunctions.ValuePosHistogram(MarkingsMap, 0.9999f));
							SetProperty("_MarkingsMapNegativeCutoff", textureFunctions.ValueNegHistogram(MarkingsMap, 0.9999f));
							SetProperty("_MarkingsMapHash", MarkingsMap.imageContentsHash.GetHashCode());
						}
					}
				}
			}

			try
			{
				// Calibrate the fur thickness to the mesh size. This allows the avatar to be scaled and have the fur scale as well.
				//
				bool success = false;
				Renderer targetRenderer = null;

				Transform[] allTransforms = UnityEngine.Object.FindObjectsOfType<Transform>();
				foreach (Transform transform in allTransforms)
				{
					Renderer myRenderer = transform.gameObject.GetComponent<Renderer>() as Renderer;
					if (myRenderer == null) continue;
					if (!myRenderer.enabled) continue;
					Material[] materials = myRenderer.sharedMaterials;
					for (int x = 0; x < materials.Length; x++)
					{
						if (materials[x].GetInstanceID() == targetMat.GetInstanceID())
						{
							DebugMessage("Calibration found matching renderer:  " + myRenderer.name + " (" + x + ")");
							targetRenderer = myRenderer;
							success = true;
							break;
						}
						if (success) break;
					}
					if (success) break;
				}

				if (success)
				{
					float magnitude = 1;
					float magnitudeCheck = 0;

					if (targetRenderer is SkinnedMeshRenderer)
					{
						Mesh bakedMesh = new Mesh();
						((SkinnedMeshRenderer)targetRenderer).BakeMesh(bakedMesh);
						magnitude = bakedMesh.normals[0].magnitude;
						magnitudeCheck = bakedMesh.normals[1].magnitude;
					}

					if (targetRenderer is MeshRenderer)
					{
						MeshFilter meshFilter = targetRenderer.GetComponent<MeshFilter>();
						magnitude = meshFilter.sharedMesh.normals[0].magnitude;
						magnitudeCheck = meshFilter.sharedMesh.normals[1].magnitude;
					}

					normalMagnitidue = magnitude;

					DebugMessage("Calibration normal magnitude equals " + magnitude + " (check = " + magnitudeCheck + ")");

					if (GetFloat("_ScaleCalibration") < 0) SetProperty("_ScaleCalibration", 0.1f / magnitude);
				}
				else
				{
					DebugMessage("Calibration couldn't find renderer using material:  " + targetMat.name);
					if (GetFloat("_ScaleCalibration") < 0) SetProperty("_ScaleCalibration", 0.25f);
				}
			}
			catch (Exception e)
			{
				DebugMessage("Calibration encountered an error: " + e);
				if (GetFloat("_ScaleCalibration") < 0) SetProperty("_ScaleCalibration", 0.25f);
			}
		}
		#endregion

		#region "Set Decal Position"
		private void SetDecal(int decalIndex)
		{
			if (colliderGameObject != null) SceneView.DestroyImmediate(colliderGameObject);
			colliderGameObject = new GameObject("Fur Grooming Decal Mesh Collider");

			MeshCollider myMeshCollider = colliderGameObject.AddComponent<MeshCollider>();
			CheckDecal myCheckDecal = colliderGameObject.AddComponent<CheckDecal>();

			myCheckDecal.material = targetMat;
			myCheckDecal.settingDecal = decalIndex;
			myCheckDecal.runInEditMode = true;

			Renderer[] allRenderers = SceneView.FindObjectsOfType<Renderer>();

			foreach (Renderer myRenderer in allRenderers)
			{
				Material[] allMaterials = myRenderer.sharedMaterials;
				int index = 0;
				foreach (Material mat in allMaterials)
				{
					if (mat == targetMat)
					{
						if (myRenderer is SkinnedMeshRenderer)
						{
							Mesh colliderMesh = new Mesh();
							((SkinnedMeshRenderer)myRenderer).BakeMesh(colliderMesh);
							myMeshCollider.sharedMesh = colliderMesh;
							myCheckDecal.targetTriangleStartIndex = myMeshCollider.sharedMesh.GetSubMesh(index).indexStart;
							myCheckDecal.targetTriangleEndIndex = myCheckDecal.targetTriangleStartIndex + myMeshCollider.sharedMesh.GetSubMesh(index).indexCount;
							colliderGameObject.transform.SetPositionAndRotation(myRenderer.transform.position, myRenderer.transform.rotation);
						}
						if (myRenderer is MeshRenderer)
						{
							MeshFilter colliderMesh = myRenderer.GetComponent<MeshFilter>();
							myMeshCollider.sharedMesh = colliderMesh.sharedMesh;
							myCheckDecal.targetTriangleStartIndex = myMeshCollider.sharedMesh.GetSubMesh(index).indexStart;
							myCheckDecal.targetTriangleEndIndex = myCheckDecal.targetTriangleStartIndex + myMeshCollider.sharedMesh.GetSubMesh(index).indexCount;
							colliderGameObject.transform.SetPositionAndRotation(myRenderer.transform.position, myRenderer.transform.rotation);
						}
					}
					index++;
				}
			}
		}





		public class CheckDecal : MonoBehaviour
		{
			public Material material = null;
			public int settingDecal = -1;
			public int settingDecalTries = 0;
			public int targetTriangleStartIndex = 0;
			public int targetTriangleEndIndex = 0;


			void OnRenderObject()
			{
				Ray myRay = new Ray(SceneView.lastActiveSceneView.camera.transform.position, SceneView.lastActiveSceneView.camera.transform.forward);
				RaycastHit hit;

				int tries = 0;
				while (Physics.Raycast(myRay, out hit))
				{
					int hitIndex = hit.triangleIndex * 3;
					if (hitIndex >= targetTriangleStartIndex && hitIndex <= targetTriangleEndIndex)
					{
						if (settingDecal == 0) material.SetVector("_DecalPosition", new Vector4(hit.textureCoord.x, hit.textureCoord.y, 0, 0));
						if (settingDecal == 1) material.SetVector("_DecalPosition1", new Vector4(hit.textureCoord.x, hit.textureCoord.y, 0, 0));
						if (settingDecal == 2) material.SetVector("_DecalPosition2", new Vector4(hit.textureCoord.x, hit.textureCoord.y, 0, 0));
						if (settingDecal == 3) material.SetVector("_DecalPosition3", new Vector4(hit.textureCoord.x, hit.textureCoord.y, 0, 0));

						SceneView.DestroyImmediate(this.gameObject);
						return;
					}
					// Not the right material, so move a tiny bit further ahead and do another raycast in the same direction
					myRay = new Ray(hit.point + myRay.direction.normalized * 0.00001f, myRay.direction);
					if (tries++ > 20) break;
				}
				if (settingDecalTries++ > 10)
				{
					SceneView.DestroyImmediate(this.gameObject);
				}
			}
		}
		#endregion

		#region "GUI Helpers"
		private void WFFGUIStartMainGroup()
		{
			EditorGUI.indentLevel++;
			EditorGUILayout.Space();
			backgroundRect = EditorGUILayout.BeginVertical();
			EditorGUILayout.Space();

			Rect outline1 = backgroundRect;
			Rect outline2 = backgroundRect;
			Rect outline3 = backgroundRect;
			backgroundRect.xMin -= 1 / EditorGUIUtility.pixelsPerPoint;
			backgroundRect.xMax += 1 / EditorGUIUtility.pixelsPerPoint;
			backgroundRect.yMin -= 1 / EditorGUIUtility.pixelsPerPoint;
			backgroundRect.yMax += 1 / EditorGUIUtility.pixelsPerPoint;
			outline1.xMin -= 3 / EditorGUIUtility.pixelsPerPoint;
			outline1.xMax += 3 / EditorGUIUtility.pixelsPerPoint;
			outline2.yMin -= 3 / EditorGUIUtility.pixelsPerPoint;
			outline2.yMax += 3 / EditorGUIUtility.pixelsPerPoint;
			outline3.xMin -= 2 / EditorGUIUtility.pixelsPerPoint;
			outline3.xMax += 2 / EditorGUIUtility.pixelsPerPoint;
			outline3.yMin -= 2 / EditorGUIUtility.pixelsPerPoint;
			outline3.yMax += 2 / EditorGUIUtility.pixelsPerPoint;
			if (backgroundRect.yMax - backgroundRect.yMin > 4)
			{
				EditorGUI.DrawRect(outline1, EditorGUIUtility.isProSkin ? Color.black : Color.white);
				EditorGUI.DrawRect(outline2, EditorGUIUtility.isProSkin ? Color.black : Color.white);
				EditorGUI.DrawRect(outline3, EditorGUIUtility.isProSkin ? Color.black : Color.white);
				EditorGUI.DrawRect(backgroundRect, EditorGUIUtility.isProSkin ? new Color(0.2f, 0.2f, 0.2f) : new Color(0.85f, 0.85f, 0.85f));
			}
		}

		private void WFFGUIEndMainGroup()
		{
			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
			EditorGUI.indentLevel--;
			EditorGUILayout.Space();
			EditorGUILayout.Space();
		}

		private void WFFGUIStartSubGroup()
		{
			EditorGUI.indentLevel++;
			backgroundRect = EditorGUILayout.BeginVertical();

			float colourValue = EditorGUIUtility.isProSkin ? 0.2f + 0.03f * (EditorGUI.indentLevel - 1) : 0.85f - 0.04f * (EditorGUI.indentLevel - 1);
			Color myColour = new Color(colourValue, colourValue, colourValue);

			backgroundRect.xMin -= 1 / EditorGUIUtility.pixelsPerPoint;
			backgroundRect.xMax += 1 / EditorGUIUtility.pixelsPerPoint;
			backgroundRect.yMin -= 1 / EditorGUIUtility.pixelsPerPoint;
			backgroundRect.yMax += 1 / EditorGUIUtility.pixelsPerPoint;

			if (backgroundRect.yMax - backgroundRect.yMin > 4)
			{
				EditorGUI.DrawRect(backgroundRect, myColour);
			}
		}

		private void WFFGUIEndSubGroup()
		{
			EditorGUILayout.EndVertical();
			EditorGUI.indentLevel--;
		}

		private bool WFFFoldout(string property)
		{

			if (GetProperty(property) == null) return false;
			SetProperty(property, EditorGUILayout.Foldout(GetFloat(property) == 1, GetProperty(property).displayName, true, EditorGUI.indentLevel > 0 ? EditorStyles.foldout : headingStyle) ? 1 : 0);
			bool opened = GetFloat(property) > 0;
			if (!opened && EditorGUI.indentLevel == 0)
			{
				EditorGUILayout.Space();
				EditorGUILayout.Space();
			}
			return (opened);
		}

		#endregion
	}
}
#endif