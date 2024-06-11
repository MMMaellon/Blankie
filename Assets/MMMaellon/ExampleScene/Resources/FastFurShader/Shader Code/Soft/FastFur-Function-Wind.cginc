// This wind function has gone through a whole bunch of revisions. The current version uses a 2-part random seed. 75% is based on
// the uv position, the other 25% world space. Because the uv position is unique and constant, it can be used to generate high
// frequency noise. The world space position is used to make low-frequency waves of turbulence moving through the fur (it can't
// be used to seed high frequency noise, because the turbulence will animate at hyper-speed when you move).

// The calculations aren't intuitive to look at (I created them using a graphing calculator), so I've added all these defines to keep
// things clearer when I'm tweaking the settings.
#define WIND_WORLD_SCALE 1.0
#define WIND_FORCE 1.0
// 3 starting numbers * 6 waves = 18 turbulence frequencies
#define WIND_TURBULENCE_STARTING_SEED float3(0.843,1,1.27)
#define WIND_TURBULENCE_FREQUENCY_BIAS 0.5
#define WIND_TURBULENCE_FREQUENCY 6.0
#define WIND_TURBULENCE_SCALING_BIAS 0.5
#define WIND_TURBULENCE_SCALING 0.1
#define WIND_GUSTS_FREQUENCY_BIAS 1.0
#define WIND_GUSTS_FREQUENCY 0.25
#define WIND_GUSTS_SCALING 0.25


float3 calculateWind(float3 worldPos, float2 uv, out float3 windTurbulence)
{
	// Start by calculating the wind vector
	float windYCos = cos(radians(_WindAngle));
	float3 windVector = normalize(float3(sin(radians(_WindDirection)) * windYCos, -sin(radians(_WindAngle)), cos(radians(_WindDirection)) * windYCos)) * _WindSpeed;

 #if !defined(FUR_SKIN_LAYER)
	// Where are we on the linear path of the wind? This gives us our base position in the 1-dimensional wind path.
	float windPosition = dot(worldPos, windVector);

	// Offset the time by world postion, so that turbulence speeds up when moving into the wind.
	float windTimeOffset = windPosition * _WindSpeed;

	// Calculate a starting seed, starting with the wind gusts packed into the w channel.
	float4 windSeed;
	windSeed.w = ((windPosition + ((WIND_GUSTS_FREQUENCY_BIAS + _WindSpeed) * ((WIND_WORLD_SCALE * (_Time.y % 10000)) + windTimeOffset)) * _WindGustsStrength * WIND_GUSTS_FREQUENCY));

	// Next, generate high frequence seeds using the uv channels. This noise is just random, but the frequence is affected by the windTimeOffset so that it speeds up a bit when moving into the wind.
	// The *1000 multiplier ensures each vertex seed is not related to any other. If set low, like *25, then nearby vertexes will move in groups, but the patterns don't look wind-like, so we don't want that.
	// We also need to use % 10000 on the time (which causes a single frame stutter every few hours), or else we will exceed the floating point precision after a few days and the turbulence will break.
	windSeed.xyz = (float3(uv.x, uv.y, uv.x + uv.y) * 1000) + (WIND_TURBULENCE_STARTING_SEED * (((WIND_WORLD_SCALE * (_Time.y % 10000)) + windTimeOffset) * (WIND_TURBULENCE_FREQUENCY_BIAS + _WindSpeed) * _WindTurbulenceStrength * WIND_TURBULENCE_FREQUENCY));
	//float debug = (WIND_WORLD_SCALE * _Time.y);

	// Mix everything through the chaos function. The xyz turbulence channels get mixed together, but the w gusts channel stays separate.
	float4 wave1 = sin(windSeed.xyzw * 1.0000);
	float4 wave2 = sin(windSeed.yzxw * 1.2092);
	float4 wave3 = sin(windSeed.xzyw * 1.6831);
	float4 wave4 = sin(windSeed.zxyw * 1.9094);
	float4 wave5 = sin(windSeed.yxzw * 2.3072);
	float4 wave6 = sin(windSeed.zyxw * 2.7513);
	float4 waveTotal = (wave1 - wave2 + wave3 - wave4 + wave5 - wave6) * 0.166667;// Results will be between -1 and +1

	// Apply the wind gusts
	float gustWave = waveTotal.w + (abs(waveTotal.w) * 0.5);
	windVector = (windVector + (gustWave * windVector * _WindGustsStrength * WIND_GUSTS_SCALING)) * WIND_FORCE;

	// Calculate the turbulence
	windTurbulence = (waveTotal.xyz * (WIND_TURBULENCE_SCALING_BIAS + (_WindTurbulenceStrength * _WindSpeed)) * WIND_TURBULENCE_SCALING);

	// For debugging, to see the wind gust speed and direction
	//windVector += (abs(debug % 3) < 0.1 ? float3(0,1e10,0) : 0);
	// For debugging, to check for precision overflows
	//windVector += float3(0, debug > 1e6 ? 10.0 : 0.0, 0);
#else
	windTurbulence = (float3) 0;
#endif

	return(windVector);
}