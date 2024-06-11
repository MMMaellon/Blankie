#if UNITY_EDITOR
using UnityEngine;

namespace WarrensFastFur
{

	// The reason I've created a separate class for all these funtions is because I am getting reports of the GUI not loading.
	// This is an experiment to see if splitting things up helps.

	public class TextureFunctions
	{
		// Determine the highest and the average value of all non-zero pixels
		public Vector2 Histogram(Texture2D tex, int channel, int calibrationChannel, int mipLevel)
		{
			if (tex == null) return new Vector2(0f, 0f);

			float[] pixelCount = new float[256];
			for (int x = 0; x < 256; x++) pixelCount[x] = 0;

			Color[] pixels = tex.GetPixels(mipLevel);

			for (int x = 0; x < pixels.Length; x++)
			{
				float pixel = pixels[x].r;
				if (channel == 1) pixel = pixels[x].g;
				if (channel == 2) pixel = pixels[x].b;
				if (channel == 3) pixel = pixels[x].a;

				if (calibrationChannel == 0) pixel = (pixel + 0.1f) / (pixels[x].r + 0.1f);
				if (calibrationChannel == 1) pixel = (pixel + 0.1f) / (pixels[x].g + 0.1f);
				if (calibrationChannel == 2) pixel = (pixel + 0.1f) / (pixels[x].b + 0.1f);
				if (calibrationChannel == 3) pixel = (pixel + 0.1f) / (pixels[x].a + 0.1f);

				if (pixel > 1f) pixel = 1f;

				pixelCount[(int)(pixel * 255)]++;
			}

			float highest = 0;
			float total = 0;
			for (int x = 1; x <= 255; x++)
			{
				if (pixelCount[x] > 0) highest = x;
				total += pixelCount[x] * x;
			}

			return new Vector2(highest, total / (pixels.Length - pixelCount[0]));
		}

		// Find the highest positive brightness
		public float ValuePosHistogram(Texture2D tex, float cutoff)
		{
			if (tex == null) return 0.5f;

			float[] pixelCount = new float[256];
			for (int x = 0; x < 256; x++) pixelCount[x] = 0;

			byte[] temp = tex.GetRawTextureData();
			Texture2D workingTexture = new Texture2D(tex.width, tex.height, tex.format, false, true);
			workingTexture.LoadRawTextureData(temp);
			Color[] pixels = workingTexture.GetPixels();

			for (int x = 0; x < tex.width * tex.height; x++)
			{
				int height = (int)((pixels[x].r * 0.30f + pixels[x].g * 0.59f + pixels[x].b * 0.11f) * 255);
				pixelCount[height]++;
			}

			float finalResult = 1;
			float totalPixels = tex.width * tex.height;

			for (int x = 255; x >= 0; x--)
			{
				if (totalPixels >= pixels.Length * cutoff) finalResult = (float)x / 255;
				totalPixels -= pixelCount[x];
			}
			return finalResult;
		}

		// Find the lowest negative brightness
		public float ValueNegHistogram(Texture2D tex, float cutoff)
		{
			if (tex == null) return 0.5f;

			float[] pixelCount = new float[256];
			for (int x = 0; x < 256; x++) pixelCount[x] = 0;

			byte[] temp = tex.GetRawTextureData();
			Texture2D workingTexture = new Texture2D(tex.width, tex.height, tex.format, false, true);
			workingTexture.LoadRawTextureData(temp);
			Color[] pixels = workingTexture.GetPixels();

			for (int x = 0; x < tex.width * tex.height; x++)
			{
				int height = (int)((pixels[x].r * 0.30f + pixels[x].g * 0.59f + pixels[x].b * 0.11f) * 255);
				pixelCount[height]++;
			}

			float finalResult = 1;
			float totalPixels = tex.width * tex.height;

			for (int x = 0; x < 256; x++)
			{
				if (totalPixels >= pixels.Length * cutoff) finalResult = (float)x / 255;
				totalPixels -= pixelCount[x];
			}
			return finalResult;
		}
	}
}
#endif
