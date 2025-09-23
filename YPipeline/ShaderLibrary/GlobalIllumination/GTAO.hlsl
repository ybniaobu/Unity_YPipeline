#ifndef YPIPELINE_GTAO_INCLUDED
#define YPIPELINE_GTAO_INCLUDED

#define GTAO_INTENSITY 
#define GTAO_NUM_DIRECTIONS GetGTAODirectionCount() 
#define GTAO_NUM_STEPS GetGTAOStepCount()
#define TEXTURE_SIZE _TextureSize

// ----------------------------------------------------------------------------------------------------
// GTAO Utility Functions
// ----------------------------------------------------------------------------------------------------

inline float GTAOOffset(int2 pixelCoord)
{
    return 0.25 * ((pixelCoord.y - pixelCoord.x) & 3);
}

inline float GTAONoise(int2 pixelCoord)
{
    return frac(52.9829189 * frac(dot(pixelCoord, half2( 0.06711056, 0.00583715))));
}

float IntegrateArc_UniformWeight(float2 h)
{
    float2 arc = 1 - cos(h);
    return arc.x + arc.y;
}

float IntegrateArc_CosWeight(float2 h, half n)
{
    float2 arc = -cos(2 * h - n) + cos(n) + 2 * h * sin(n);
    return 0.25 * (arc.x + arc.y);
}

float2 GTAO(uint2 pixelCoord, int numDirs, int numSteps)
{
    float2 screenUV = (float2(pixelCoord) + float2(0.5, 0.5)) * _TextureSize.xy;

    // ------------------------- Fetch Position & Normal -------------------------
    
    float rawDepth = LoadDepth(pixelCoord);
    float3 P = FetchViewPosition(screenUV, rawDepth);
    float3 viewDir = normalize(-P);

    float3 normalWS = LoadAndDecodeNormal(pixelCoord);
    float3 normalVS = FetchViewNormal(normalWS);
}

#endif