#ifndef SERVICE_PARALLAX_INCLUDED
#define SERVICE_PARALLAX_INCLUDED

// Please define a PerPixelHeightDisplacementParam parameter
// and ComputePerPixelHeightDisplacement function to sample the heightmap.

float2 ParallaxRaymarching(float2 viewDir, PerPixelHeightDisplacementParam ppdParam, 
    float strength, out float outHeight)
{
    const float raymarch_steps = 10;

    float2 uvOffset = 0;
    const float stepSize = 1.0 / raymarch_steps;
    float2 uvDelta = viewDir * (stepSize * strength);
    
    float stepHeight = 1;
    float surfaceHeight = ComputePerPixelHeightDisplacement(0, 0, ppdParam);
    
    float2 prevUVOffset = uvOffset;
    float prevStepHeight = stepHeight;
    float prevSurfaceHeight = surfaceHeight;
    
    for (int i = 1; i < raymarch_steps && stepHeight > surfaceHeight; i++)
    {
        prevUVOffset = uvOffset;
        prevStepHeight = stepHeight;
        prevSurfaceHeight = surfaceHeight;
        
        uvOffset -= uvDelta;
        stepHeight -= stepSize;
        surfaceHeight = ComputePerPixelHeightDisplacement(uvOffset, 0, ppdParam);
    }
    
    float prevDifference = prevStepHeight - prevSurfaceHeight;
    float difference = surfaceHeight - stepHeight;
    float t = prevDifference / (prevDifference + difference);
    uvOffset = prevUVOffset -uvDelta * t;
    
    outHeight = surfaceHeight;
    return uvOffset;
}

float2 ParallaxRaymarchingDynamic(float3 viewDir, PerPixelHeightDisplacementParam ppdParam, 
    float strength, float lod)
{
    viewDir = normalize(viewDir);
    const float minLayers = 8.0;
    const float maxLayers = 48.0;
    // lod should be dot(normalWS, viewDirWS)
    float numLayers = lerp(maxLayers, minLayers, lod);
    float heightScale = _Parallax; // 0.05
    float layerDepth = 1.0 / numLayers;
    float currLayerDepth = 0.0;
    float2 deltaUV = viewDir.xy * heightScale / (viewDir.z * numLayers);
    float2 uvOffset = 0;
    float height = 1.0 - ComputePerPixelHeightDisplacement(0, 0, ppdParam);

    for (int i = 0; i < numLayers; i++) {
        currLayerDepth += layerDepth;
        uvOffset -= deltaUV;
        height = 1.0 - ComputePerPixelHeightDisplacement(uvOffset, 0, ppdParam);
        if (height < currLayerDepth) {
            break;
        }
    }
    float2 prevOffset = uvOffset + deltaUV;
    float nextDepth = height - currLayerDepth;
    float prevDepth = 1.0 - ComputePerPixelHeightDisplacement(prevOffset, 0, ppdParam) -
            currLayerDepth + layerDepth;
    float2 parallaxUVs = lerp(uvOffset, prevOffset, nextDepth / (nextDepth - prevDepth));
    return parallaxUVs;
    
}

#endif // SERVICE_PARALLAX_INCLUDED