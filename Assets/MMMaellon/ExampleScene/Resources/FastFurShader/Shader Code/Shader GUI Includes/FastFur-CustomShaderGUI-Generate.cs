#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace WarrensFastFur
{

	// The reason I've created a separate class for all these funtions is because I am getting reports of the GUI not loading.
	// This is an experiment to see if splitting things up helps.

	public class GenerateFunctions
	{

		//*************************************************************************************************************************************************
		// Generate a Hair Map
		//*************************************************************************************************************************************************
		public void GenerateHairMap(CustomShaderGUI gui, string assetPath, string coarseAssetPath)
		{
			Texture2D newTextureA = new Texture2D(512, 512, TextureFormat.ARGB32, false, true);
			Texture2D newTextureB = new Texture2D(512, 512, TextureFormat.ARGB32, false, true);

			var newPixelsA = newTextureA.GetPixels();
			var newPixelsB = newTextureB.GetPixels();
			var newColourA = new Color(0.498f, 0.498f, 0f, 0f);
			var newColourB = new Color(0f, 0f, 0f, 0f);
			for (int x = 0; x < newPixelsA.Length; x++)
			{
				newPixelsA[x] = newColourA;
				newPixelsB[x] = newColourB;
			}

			drawHairs(newPixelsA, newPixelsB, newTextureA.width, newTextureA.height, 6, 8, gui.GetProperty("_GenGuardHairs").floatValue, gui.GetProperty("_GenGuardHairsTaper").floatValue, gui.GetProperty("_GenGuardHairMinHeight").floatValue, gui.GetProperty("_GenGuardHairMaxHeight").floatValue, gui.GetProperty("_GenGuardHairMinColourShift").floatValue, gui.GetProperty("_GenGuardHairMaxColourShift").floatValue, gui.GetProperty("_GenGuardHairMinHighlight").floatValue, gui.GetProperty("_GenGuardHairMaxHighlight").floatValue, (int)gui.GetProperty("_GenGuardHairMaxOverlap").floatValue);
			drawHairs(newPixelsA, newPixelsB, newTextureA.width, newTextureA.height, 4, 5, gui.GetProperty("_GenMediumHairs").floatValue, gui.GetProperty("_GenMediumHairsTaper").floatValue, gui.GetProperty("_GenMediumHairMinHeight").floatValue, gui.GetProperty("_GenMediumHairMaxHeight").floatValue, gui.GetProperty("_GenMediumHairMinColourShift").floatValue, gui.GetProperty("_GenMediumHairMaxColourShift").floatValue, gui.GetProperty("_GenMediumHairMinHighlight").floatValue, gui.GetProperty("_GenMediumHairMaxHighlight").floatValue, (int)gui.GetProperty("_GenMediumHairMaxOverlap").floatValue);
			drawHairs(newPixelsA, newPixelsB, newTextureA.width, newTextureA.height, 2, 3, gui.GetProperty("_GenFineHairs").floatValue, gui.GetProperty("_GenFineHairsTaper").floatValue, gui.GetProperty("_GenFineHairMinHeight").floatValue, gui.GetProperty("_GenFineHairMaxHeight").floatValue, gui.GetProperty("_GenFineHairMinColourShift").floatValue, gui.GetProperty("_GenFineHairMaxColourShift").floatValue, gui.GetProperty("_GenFineHairMinHighlight").floatValue, gui.GetProperty("_GenFineHairMaxHighlight").floatValue, (int)gui.GetProperty("_GenFineHairMaxOverlap").floatValue);

			newTextureA.SetPixels(newPixelsA);
			newTextureB.SetPixels(newPixelsB);
			newTextureA.Apply();
			newTextureB.Apply();

			byte[] bytesA = newTextureA.EncodeToPNG();
			System.IO.File.WriteAllBytes(assetPath, bytesA);

			byte[] bytesB = newTextureB.EncodeToPNG();
			System.IO.File.WriteAllBytes(coarseAssetPath, bytesB);

			AssetDatabase.Refresh();
		}

		private void drawHairs(Color[] pixelsA, Color[] pixelsB, int texWidth, int texHeight, float minSize, float maxSize, float count, float taper, float minHeight, float maxHeight, float minShift, float maxShift, float minHighlight, float maxHighlight, int overlaps)
		{
			int actual = 0;
			for (int i = 0; i < count; i++)
			{
				for (int j = 0; j < 250; j++)
				{
					float x = UnityEngine.Random.Range(0f, texWidth);
					float y = UnityEngine.Random.Range(0f, texHeight);
					float size = (UnityEngine.Random.Range(minSize, maxSize) + UnityEngine.Random.Range(minSize, maxSize)) * 0.5f;

					if (checkHair(pixelsA, texWidth, texHeight, size, x, y, overlaps))
					{
						float height = (UnityEngine.Random.Range(minHeight, maxHeight) + UnityEngine.Random.Range(minHeight, maxHeight)) * 0.5f;
						float shift = ((UnityEngine.Random.Range(minShift, maxShift) + UnityEngine.Random.Range(minShift, maxShift)) * 0.25f) + 0.498f;
						float highlight = ((UnityEngine.Random.Range(minHighlight, maxHighlight) + UnityEngine.Random.Range(minHighlight, maxHighlight)) * 0.25f) + 0.498f;

						drawHair(pixelsA, texWidth, texHeight, size, taper, x, y, height, shift, highlight);
						if (size > 3) drawHair(pixelsB, texWidth, texHeight, size * 3, 1, x, y, height, shift, highlight);

						actual++;
						break;
					}
				}
			}

			Debug.Log("Total " + (minSize + maxSize * 0.25) + " size hairs successfully placed = " + actual);
		}

		private bool checkHair(Color[] pixels, int texWidth, int texHeight, float size, float x, float y, int overlaps)
		{
			size = (size + 1) * 0.5f;
			int intSize = Mathf.CeilToInt(size) + 1;
			int overlapCount = 0;

			for (int x2 = -intSize; x2 <= intSize; x2++)
			{
				for (int y2 = -intSize; y2 <= intSize; y2++)
				{
					float x3 = x + x2;
					float y3 = y + y2;
					float distx = x - x3;
					float disty = y - y3;
					float distance = Mathf.Sqrt(distx * distx + disty * disty);
					if (distance >= size + 2) continue;
					if (x3 >= texWidth) x3 -= texWidth;
					if (y3 >= texHeight) y3 -= texHeight;
					if (x3 < 0) x3 += texWidth;
					if (y3 < 0) y3 += texHeight;
					if (pixels[(int)x3 + (int)y3 * texWidth].b > 0)
					{
						overlapCount++;
						if(overlapCount > overlaps) return false;
					}
				}
			}
			return true;
		}

		private void drawHair(Color[] pixels, int texWidth, int texHeight, float size, float taper, float x, float y, float height, float shift, float highlight)
		{
			size *= 0.5f;
			int intSize = Mathf.CeilToInt(size) + 2;

			for (int x2 = -intSize; x2 <= intSize; x2++)
			{
				for (int y2 = -intSize; y2 <= intSize; y2++)
				{
					float x3 = (int)x + (int)x2 + 0.5f;
					float y3 = (int)y + (int)y2 + 0.5f;
					float distx = x - x3;
					float disty = y - y3;
					float distance = Mathf.Sqrt(distx * distx + disty * disty);
					if (distance >= size + 2) continue;
					if (x3 >= texWidth) x3 -= texWidth;
					if (y3 >= texHeight) y3 -= texHeight;
					if (x3 < 0) x3 += texWidth;
					if (y3 < 0) y3 += texHeight;
					float pixelHeight = height;
					if (taper == 1) pixelHeight = Mathf.Lerp(height, Mathf.Clamp01(height - 0.75f), Mathf.Clamp01((distance - 1.5f) / (size + 1)));// Pointy tips
					if (taper == 2) pixelHeight = Mathf.Lerp(height, Mathf.Clamp01(height - 0.75f), Mathf.Clamp01((distance - 0.5f) / (size + 1)));// Pointy tips
					pixelHeight *= Mathf.InverseLerp(size + 1, size, distance);// Trim edges

					pixels[(int)x3 + (int)y3 * texWidth] = new Color(highlight, shift, pixelHeight, pixelHeight > 0 ? 1 : 0.5f);
				}
			}
		}




















		//*************************************************************************************************************************************************
		// Generate random fur markings (using Alan Turing's method)
		//*************************************************************************************************************************************************
		public void GenerateFurMarkings(CustomShaderGUI gui, string assetPath)
		{
			Texture2D newTexture = new Texture2D(256, 256, TextureFormat.ARGB32, false, true);
			var newPixels = newTexture.GetPixels();
			float initialDensity = gui.GetProperty("_InitialDensity").floatValue;
			for (int x = 0; x < newPixels.Length; x++)
			{
				newPixels[x] = new Color(0f, 0f, 0f, UnityEngine.Random.Range(0f, 1f) > initialDensity ? 1f : 0f);
			}

			newTexture.SetPixels(newPixels);
			newTexture.Apply();

			Material turingMaterial = new Material(Shader.Find("Warren's Fast Fur/Internal Utilities/Turing Fur Markings Algorithm"));
			turingMaterial.SetColor("_PigmentColour", gui.GetProperty("_PigmentColour").colorValue);
			turingMaterial.SetColor("_BaseColour", gui.GetProperty("_BaseColour").colorValue);
			turingMaterial.SetColor("_TransitionalColour", gui.GetProperty("_TransitionalColour").colorValue);
			turingMaterial.SetFloat("_Contrast", gui.GetProperty("_MarkingsContrast").floatValue);
			turingMaterial.SetFloat("_ActivatorHormoneRadius", gui.GetProperty("_ActivatorHormoneRadius").floatValue);
			turingMaterial.SetFloat("_InhibitorHormoneAdditionalRadius", gui.GetProperty("_InhibitorHormoneAdditionalRadius").floatValue);
			turingMaterial.SetFloat("_ActivatorCycles", gui.GetProperty("_ActivatorCycles").floatValue);

			float _ActivatorHormoneRadius = gui.GetProperty("_ActivatorHormoneRadius").floatValue;
			float _InhibitorHormoneAdditionalRadius = gui.GetProperty("_InhibitorHormoneAdditionalRadius").floatValue;
			float _CellStretch = gui.GetProperty("_CellStretch").floatValue;
			float _InhibitorStrength = gui.GetProperty("_InhibitorStrength").floatValue;

			float totalRange = _ActivatorHormoneRadius + _InhibitorHormoneAdditionalRadius;

			float xMultiplier = 1 + _CellStretch;
			float yMultiplier = 1;
			float scanRadiusX = Mathf.Round(totalRange * xMultiplier);
			float scanRadiusY = Mathf.Round(totalRange * yMultiplier);
			turingMaterial.SetFloat("_XMultiplier", xMultiplier);
			turingMaterial.SetFloat("_YMultiplier", yMultiplier);
			turingMaterial.SetFloat("_ScanRadiusX", scanRadiusX);
			turingMaterial.SetFloat("_ScanRadiusY", scanRadiusY);

			float activatorSize = _ActivatorHormoneRadius * xMultiplier * yMultiplier;
			float inhibitorSize = ((_ActivatorHormoneRadius + _InhibitorHormoneAdditionalRadius) * xMultiplier * yMultiplier) - activatorSize;
			_InhibitorStrength = (_InhibitorStrength * activatorSize) / inhibitorSize;
			turingMaterial.SetFloat("_InhibitorStrength", _InhibitorStrength);

			Material mutateMaterial = new Material(Shader.Find("Warren's Fast Fur/Internal Utilities/Turing Fur Markings Mutations"));
			mutateMaterial.SetFloat("_MutationRate", gui.GetProperty("_MutationRate").floatValue);

			RenderTexture renderTexture = new RenderTexture(newTexture.width, newTexture.height, 0, RenderTextureFormat.ARGB32);
			RenderTexture renderTextureBuffer = new RenderTexture(newTexture.width, newTexture.height, 0, RenderTextureFormat.ARGB32);
			Graphics.Blit(newTexture, renderTexture);

			float cycles = gui.GetProperty("_ActivatorCycles").floatValue;
			for (int x = 0; x < (int)cycles; x++)
			{
				mutateMaterial.SetVector("_Seed", new Vector4(UnityEngine.Random.Range(0f, 1000f), UnityEngine.Random.Range(0f, 1000f), UnityEngine.Random.Range(0f, 1000f), UnityEngine.Random.Range(0f, 1000f)));
				Graphics.Blit(renderTexture, renderTextureBuffer, mutateMaterial);
				Graphics.Blit(renderTextureBuffer, renderTexture, turingMaterial);
			}

			/*        cycles = gui.GetProperty("_GrowthCycles").floatValue;
					for (int x = 0; x < (int)cycles; x++)
					{
						mutateMaterial.SetVector("_Seed", new Vector4(Random.Range(0f, 1000f), Random.Range(0f, 1000f), Random.Range(0f, 1000f), Random.Range(0f, 1000f)));
						renderTextureBuffer = new RenderTexture(newTexture.width + x, newTexture.height + x, 0, RenderTextureFormat.ARGB32);
						Graphics.Blit(renderTexture, renderTextureBuffer, mutateMaterial);
						renderTexture.Release();
						renderTexture = new RenderTexture(newTexture.width + x, newTexture.height + x, 0, RenderTextureFormat.ARGB32);
						Graphics.Blit(renderTextureBuffer, renderTexture, turingMaterial);
					}
			*/
			RenderTexture finalTexture = new RenderTexture(newTexture.width, newTexture.height, 0, RenderTextureFormat.ARGB32);
			Graphics.Blit(renderTexture, finalTexture);
			RenderTexture.active = finalTexture;
			newTexture.ReadPixels(new Rect(0, 0, newTexture.width, newTexture.height), 0, 0);
			newTexture.Apply();
			RenderTexture.active = null;

			byte[] bytes = newTexture.EncodeToPNG();
			System.IO.File.WriteAllBytes(assetPath, bytes);

			AssetDatabase.Refresh();
		}










		/*
		//*************************************************************************************************************************************************
		// Generate animations
		//*************************************************************************************************************************************************
		private void CreateAnimations(MaterialProperty[] properties)
		{
			SkinnedMeshRenderer[] targetSkinnedMeshRenderers;
			targetSkinnedMeshRenderers = GameObject.FindObjectsOfType<SkinnedMeshRenderer>();

			int meshId = -1;
			for(int x = 0 ; x < targetSkinnedMeshRenderers.Length ; x++)
			{
				foreach(Material mat in targetSkinnedMeshRenderers[x].sharedMaterials)
				{
					if(mat.ComputeCRC() == ((Material) materials[0]).ComputeCRC())
					{
						meshId = x;
					}
				}
			}

			VRCAvatarDescriptor avatarDescriptor = targetSkinnedMeshRenderers[meshId].GetComponentInParent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
			if (avatarDescriptor == null)
			{
				EditorUtility.DisplayDialog("Error", "Unable to locate the VR Chat Avatar Descriptor.", "OK");
				return;
			}

			AnimatorController animatorController = (AnimatorController) avatarDescriptor.baseAnimationLayers[4].animatorController;
			if (animatorController == null )
			{
				EditorUtility.DisplayDialog("Error", "Unable to locate the FX layer Animator Controller.", "OK");
				return;
			}

			if(avatarDescriptor.baseAnimationLayers[4].type != VRCAvatarDescriptor.AnimLayerType.FX)
			{
				EditorUtility.DisplayDialog("Error", "Unable to locate a valid FX layer Animator Controller.", "OK");
				return;
			}

			VRCExpressionParameters expressionParameters = avatarDescriptor.expressionParameters;
			if(expressionParameters == null )
			{
				EditorUtility.DisplayDialog("Error", "Unable to locate VR Chat Expression Parameters.", "OK");
				return;
			}

			if(!CreateVRCParameters(expressionParameters)) return;
			if(!CreateAnimatorParameters(animatorController)) return;
			if(!CreateAnimationLayers(animatorController)) return;
			if(!CreateBlendTrees(animatorController)) return;

			EditorUtility.DisplayDialog("Success", "Animations have been successfully updated.", "OK");
		}

		private bool CreateBlendTrees(AnimatorController animatorController, MaterialProperty[] properties)
		{
			BlendTree[] blendTrees = (BlendTree) animatorController.



			int velocityXLayerIndex = -1;
			int velocityYLayerIndex = -1;
			int velocityZLayerIndex = -1;
			int angularYLayerIndex = -1;
		
			AnimatorControllerLayer[] layers = animatorController.layers;
			for(int x = layers.Length - 1 ; x >= 0 ; x--)
			{
				if(layers[x].name.Equals("Fast Fur - VelocityX")) velocityXLayerIndex = x;
				if(layers[x].name.Equals("Fast Fur - VelocityY")) velocityYLayerIndex = x;
				if(layers[x].name.Equals("Fast Fur - VelocityZ")) velocityZLayerIndex = x;
				if(layers[x].name.Equals("Fast Fur - AngularY")) angularYLayerIndex = x;
			}

			if(velocityXLayerIndex < 0 || velocityYLayerIndex < 0 || velocityZLayerIndex < 0 || angularYLayerIndex < 0)
			{
				EditorUtility.DisplayDialog("Error", "Unable to find the newly created Animation Controller Layers. Something unexpected has gone wrong, because this shouldn't happen. Aborting.", "OK");
				return(false);
			}

			AnimatorControllerParameter velocityXParam = null;
			AnimatorControllerParameter velocityYParam = null;
			AnimatorControllerParameter velocityZParam = null;
			AnimatorControllerParameter angularYParam = null;

			foreach(var param in animatorController.parameters)
			{
				if(param.name == "VelocityX") velocityXParam = param;
				if(param.name == "VelocityY") velocityYParam = param;
				if(param.name == "VelocityZ") velocityZParam = param;
				if(param.name == "AngularY") angularYParam = param;
			}

			if(velocityXParam == null || velocityYParam == null || velocityZParam == null || angularYParam == null)
			{
				EditorUtility.DisplayDialog("Error", "Unable to find the newly created Animation Controller Parameters. Something unexpected has gone wrong, because this shouldn't happen. Aborting.", "OK");
				return(false);
			}

		
			AnimatorStateMachine stateMachine = layers[velocityXLayerIndex].stateMachine;

	
			stateMachine.

			BlendTree velocityXTree;

			animatorController.CreateBlendTreeInController("Fast Fur - Blend Tree - VelocityX", out velocityXTree, velocityXLayerIndex);
			velocityXTree.blendParameter = "VelocityX";
			velocityXTree.useAutomaticThresholds = false;

			AnimationEvent animation = new AnimationEvent();
			animation.floatParameter = -1;
			animation.objectReferenceParameter = gui.GetProperty("_VelocityX",properties).targets[0];

			velocityXTree.AddChild(
			//AnimationClip velocityXminus1 = new AnimationClip();
			//AnimationUtility.
			//velocityXminus1.AddEvent(animation);
			//velocityXTree.AddChild(velocityXminus1);
			return(true);
		}




		private bool CreateAnimationLayers(AnimatorController animatorController)
		{
			AnimatorControllerLayer[] layers = animatorController.layers;
			if(layers.Length > 0)
			{
				for(int x = layers.Length - 1 ; x >= 0 ; x--)
				{
					if(layers[x].name.Equals("Fast Fur - VelocityX")) {animatorController.RemoveLayer(x);continue;}
					if(layers[x].name.Equals("Fast Fur - VelocityY")) {animatorController.RemoveLayer(x);continue;}
					if(layers[x].name.Equals("Fast Fur - VelocityZ")) {animatorController.RemoveLayer(x);continue;}
					if(layers[x].name.Equals("Fast Fur - AngularY")) {animatorController.RemoveLayer(x);continue;}
				}
			}

			animatorController.AddLayer("Fast Fur - VelocityX");
			animatorController.AddLayer("Fast Fur - VelocityY");
			animatorController.AddLayer("Fast Fur - VelocityZ");
			animatorController.AddLayer("Fast Fur - AngularY");

			return(true);
		}


		private bool CreateAnimatorParameters(AnimatorController animatorController)
		{
			AnimatorControllerParameter velocityXParam = null;
			AnimatorControllerParameter velocityYParam = null;
			AnimatorControllerParameter velocityZParam = null;
			AnimatorControllerParameter angularYParam = null;

			foreach(var param in animatorController.parameters)
			{
				if(param.name == "VelocityX") velocityXParam = param;
				if(param.name == "VelocityY") velocityYParam = param;
				if(param.name == "VelocityZ") velocityZParam = param;
				if(param.name == "AngularY") angularYParam = param;
			}

			if(velocityXParam != null && !velocityXParam.type.ToString().Equals("Float"))
			{
				EditorUtility.DisplayDialog("Error", "There is a conflict with the existing Animator Controller Parameters. The parameter 'VelocityX' already exists, but it is not a Float.", "OK");
				return(false);
			}
			if(velocityYParam != null && !velocityYParam.type.ToString().Equals("Float"))
			{
				EditorUtility.DisplayDialog("Error", "There is a conflict with the existing Animator Controller Parameters. The parameter 'VelocityY' already exists, but it is not a Float.", "OK");
				return(false);
			}
			if(velocityZParam != null && !velocityZParam.type.ToString().Equals("Float"))
			{
				EditorUtility.DisplayDialog("Error", "There is a conflict with the existing Animator Controller Parameters. The parameter 'VelocityZ' already exists, but it is not a Float.", "OK");
				return(false);
			}
			if(angularYParam != null && !angularYParam.type.ToString().Equals("Float"))
			{
				EditorUtility.DisplayDialog("Error", "There is a conflict with the existing Animator Controller Parameters. The parameter 'AngularY' already exists, but it is not a Float.", "OK");
				return(false);
			}

			int missing = 0;
			if(velocityXParam == null) missing++;
			if(velocityYParam == null) missing++;
			if(velocityZParam == null) missing++;
			if(angularYParam == null) missing++;

			// Do we need to create new parameters?
			if(missing > 0)
			{
				string toBeAdded = (velocityXParam == null ? "'VelocityX' " : "") + (velocityYParam == null ? "'VelocityY' " : "") + (velocityZParam == null ? "'VelocityZ' " : "") + (angularYParam == null ? "'AngularY' " : "");
				if(EditorUtility.DisplayDialog("Warning!", "Are you sure you want to create " + missing + " new Animator Controller Parameter" + (missing > 1 ? "s" : "") + "? The new parameter"  + (missing > 1 ? "s" : "") + " will be added to your existing parameters:\n\n" + toBeAdded, "OK", "Cancel"))
				{
					if(velocityXParam == null) animatorController.AddParameter("VelocityX", 0f);
					if(velocityYParam == null) animatorController.AddParameter("VelocityY", 0f);
					if(velocityZParam == null) animatorController.AddParameter("VelocityZ", 0f);
					if(angularYParam == null) animatorController.AddParameter("AngularY", 0f);

					EditorUtility.DisplayDialog("Note", missing + " parameter" + (missing > 1 ? "s" : "") + " added successfully:\n\n" + toBeAdded, "OK");
					return(true);
				}
				return(false);
			}
			return(true);
		}
  

		private bool CreateVRCParameters(VRCExpressionParameters expressionParameters)
		{
			// Start by checking the existing parameters
			var velocityX = expressionParameters.FindParameter("VelocityX");
			var velocityY = expressionParameters.FindParameter("VelocityY");
			var velocityZ = expressionParameters.FindParameter("VelocityZ");
			var angularY = expressionParameters.FindParameter("AngularY");
			if(velocityX != null && !velocityX.valueType.ToString().Equals("Float"))
			{
				EditorUtility.DisplayDialog("Error", "There is a conflict with the existing VR Chat Expression Parameters. The parameter 'VelocityX' already exists, but it is not a Float.", "OK");
				return(false);
			}
			if(velocityY != null && !velocityY.valueType.ToString().Equals("Float"))
			{
				EditorUtility.DisplayDialog("Error", "There is a conflict with the existing VR Chat Expression Parameters. The parameter 'VelocityY' already exists, but it is not a Float.", "OK");
				return(false);
			}
			if(velocityZ != null && !velocityZ.valueType.ToString().Equals("Float"))
			{
				EditorUtility.DisplayDialog("Error", "There is a conflict with the existing VR Chat Expression Parameters. The parameter 'VelocityZ' already exists, but it is not a Float.", "OK");
				return(false);
			}
			if(angularY != null && !angularY.valueType.ToString().Equals("Float"))
			{
				EditorUtility.DisplayDialog("Error", "There is a conflict with the existing VR Chat Expression Parameters. The parameter 'AngularY' already exists, but it is not a Float.", "OK");
				return(false);
			}

			int missing = 0;
			if(velocityX == null) missing++;
			if(velocityY == null) missing++;
			if(velocityZ == null) missing++;
			if(angularY == null) missing++;

			// Do we need to create new parameters?
			if(missing > 0)
			{
				if((expressionParameters.CalcTotalCost() + missing * 8) > VRCExpressionParameters.MAX_PARAMETER_COST)
				{
					EditorUtility.DisplayDialog("Error", "There is a conflict with the existing VR Chat Expression Parameters. There is not enough memory to add " + missing + " parameter" + (missing > 1 ? "s" : "") + " without exceeding " + VRCExpressionParameters.MAX_PARAMETER_COST + " bits.", "OK");
					return(false);
				}

				string toBeAdded = (velocityX == null ? "'VelocityX' " : "") + (velocityY == null ? "'VelocityY' " : "") + (velocityZ == null ? "'VelocityZ' " : "") + (angularY == null ? "'AngularY' " : "");
				if(EditorUtility.DisplayDialog("Warning!", "Are you sure you want to create " + missing + " new VR Chat Expression Parameter" + (missing > 1 ? "s" : "") + "? The new parameter"  + (missing > 1 ? "s" : "") + " will be added to your existing parameters:\n\n" + toBeAdded, "OK", "Cancel"))
				{
					var newParameters = new VRCExpressionParameters.Parameter[expressionParameters.parameters.Length + missing];

					int index;
					for(index = 0 ; index < expressionParameters.parameters.Length ; index++)
					{
						newParameters[index] = expressionParameters.parameters[index];
					}
					if(velocityX == null){
						newParameters[index] = new VRCExpressionParameters.Parameter();
						newParameters[index].name = "VelocityX";
						newParameters[index].valueType = VRCExpressionParameters.ValueType.Float;
						index++;
					}
					if(velocityY == null){
						newParameters[index] = new VRCExpressionParameters.Parameter();
						newParameters[index].name = "VelocityY";
						newParameters[index].valueType = VRCExpressionParameters.ValueType.Float;
						index++;
					}
					if(velocityZ == null){
						newParameters[index] = new VRCExpressionParameters.Parameter();
						newParameters[index].name = "VelocityZ";
						newParameters[index].valueType = VRCExpressionParameters.ValueType.Float;
						index++;
					}
					if(angularY == null){
						newParameters[index] = new VRCExpressionParameters.Parameter();
						newParameters[index].name = "AngularY";
						newParameters[index].valueType = VRCExpressionParameters.ValueType.Float;
						index++;
					}
					expressionParameters.parameters = newParameters;

					EditorUtility.DisplayDialog("Note", missing + " parameter" + (missing > 1 ? "s" : "") + " added successfully:\n\n" + toBeAdded, "OK");
					return(true);
				}
				return(false);
			}
			return(true);
		}
		*/
	}
}

#endif
