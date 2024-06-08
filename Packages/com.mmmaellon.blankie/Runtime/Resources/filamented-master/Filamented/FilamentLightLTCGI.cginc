#ifndef FILAMENT_LIGHT_LTCGI
#define FILAMENT_LIGHT_LTCGI

#if defined(_LTCGI)
    #if defined(_SPECULARHIGHLIGHTS_OFF)
        #define LTCGI_SPECULAR_OFF
    #endif
#include "Packages/at.pimaker.ltcgi/Shaders/LTCGI_structs.cginc"

struct accumulator_struct {
    float3 diffuse;
    float3 specular;
    float3 specularIntensity;
};
void callback_diffuse(inout accumulator_struct acc, in ltcgi_output output);
void callback_specular(inout accumulator_struct acc, in ltcgi_output output);

#define LTCGI_V2_CUSTOM_INPUT accumulator_struct
#define LTCGI_V2_DIFFUSE_CALLBACK callback_diffuse
#define LTCGI_V2_SPECULAR_CALLBACK callback_specular

#include "Packages/at.pimaker.ltcgi/Shaders/LTCGI.cginc"

void callback_diffuse(inout accumulator_struct acc, in ltcgi_output output) {
    acc.diffuse += output.intensity * output.color;
}
void callback_specular(inout accumulator_struct acc, in ltcgi_output output) {
    acc.specular += output.intensity * output.color;
    acc.specularIntensity += output.intensity;
}
#endif

//------------------------------------------------------------------------------
// LTCGI evaluation
//------------------------------------------------------------------------------

// This is a small function to abstract the calls to the LTCGI functions.

void evaluateLTCGI(const ShadingParams shading, const PixelParams pixel, inout float3 color) {
#if defined(_LTCGI)
    accumulator_struct acc = (accumulator_struct)0;

    LTCGI_Contribution(
        acc,
        shading.position,
        shading.normal,
        shading.view,
        pixel.perceptualRoughness,
        shading.lightmapUV.xy
    );
    color.rgb += acc.specular + acc.diffuse;
#endif
}

#endif // FILAMENT_LIGHT_LTCGI