// ----------------------------------------------------------------------------------------------------
// Filter Utility Functions
// ----------------------------------------------------------------------------------------------------

inline float2 LoadAOandDepth(int2 pixelCoord)
{
    return LOAD_TEXTURE2D_LOD(_InputTexture, pixelCoord, 0).rg;
}

// ----------------------------------------------------------------------------------------------------
// Depth Downsample Kernel
// ----------------------------------------------------------------------------------------------------

[numthreads(8, 8, 1)]
void DepthDownsampleKernel(uint3 id : SV_DispatchThreadID)
{
    int2 pixelCoord = clamp(id.xy, 0, _TextureSize.zw - 1);
    _OutputTexture[id.xy] = LOAD_TEXTURE2D_LOD(_CameraDepthTexture, pixelCoord * 2, 0).r;
}

// ----------------------------------------------------------------------------------------------------
// Upsample Kernel
// ----------------------------------------------------------------------------------------------------

[numthreads(8, 8, 1)]
void UpsampleKernel(uint3 id : SV_DispatchThreadID)
{
    float2 screenUV = (float2(id.xy) + float2(0.5, 0.5)) * _CameraBufferSize.xy;
    uint2 pixelCoord = clamp(id.xy, 0, _CameraBufferSize.zw - 1);

    // ------------------------- Fetch Full Resolution Depth & 4 Half Resolution Depths -------------------------
    
    float fullDepth = LOAD_TEXTURE2D_LOD(_CameraDepthTexture, pixelCoord, 0).r;
    fullDepth = GetViewDepthFromDepthTexture(fullDepth);
    // float4 halfDepths = GATHER_RED_TEXTURE2D(_HalfDepthTexture, sampler_PointClamp, screenUV);
    float4 halfDepths = GATHER_GREEN_TEXTURE2D(_InputTexture, sampler_PointClamp, screenUV);
    
    halfDepths.x = GetViewDepthFromDepthTexture(halfDepths.x);
    halfDepths.y = GetViewDepthFromDepthTexture(halfDepths.y);
    halfDepths.z = GetViewDepthFromDepthTexture(halfDepths.z);
    halfDepths.w = GetViewDepthFromDepthTexture(halfDepths.w);
    float4 aos = GATHER_RED_TEXTURE2D(_InputTexture, sampler_PointClamp, screenUV);

    // ------------------------- Nearest-Depth Upsample -------------------------
    
    float ao;
    float4 depthRatios = abs(1 - halfDepths / fullDepth);
    if (all(depthRatios < 0.1)) // Edge Test
    {
        ao = SAMPLE_TEXTURE2D_LOD(_InputTexture, sampler_LinearClamp, screenUV, 0).r;
    }
    else
    {
        ao = aos.x;
        float4 deltas = abs(halfDepths - fullDepth);
        ao = lerp(ao, aos.y, deltas.y < deltas.x);
        ao = lerp(ao, aos.z, deltas.z < deltas.y);
        ao = lerp(ao, aos.w, deltas.w < deltas.z);
    }
    
    _OutputTexture[id.xy] = float2(ao, 0);
}

// ----------------------------------------------------------------------------------------------------
// Spatial Filter Kernels
// ----------------------------------------------------------------------------------------------------

inline float BilateralWeight(float radiusDelta, float depthDelta, float2 sigma)
{
    return exp2(-radiusDelta * radiusDelta * rcp(2.0 * sigma.x * sigma.x) - depthDelta * depthDelta * rcp(2.0 * sigma.y * sigma.y));
}

[numthreads(64, 1, 1)]
void SpatialBlurHorizontalKernel(uint3 id : SV_DispatchThreadID, uint groupIndex : SV_GroupIndex)
{
    // ------------------------- Fetch AO & Depth -------------------------

    int2 pixelCoord = clamp(id.xy, 0, _TextureSize.zw - 1);
    float2 aoAndDepth = LoadAOandDepth(pixelCoord);
    float outDepth = aoAndDepth.g;
    aoAndDepth.g = GetViewDepthFromDepthTexture(aoAndDepth.g);
    _AOAndDepth[groupIndex + MAX_FILTER_RADIUS] = aoAndDepth;

    if (groupIndex < MAX_FILTER_RADIUS)
    {
        int2 extraCoord = pixelCoord - int2(MAX_FILTER_RADIUS, 0);
        extraCoord = clamp(extraCoord, 0, _TextureSize.zw - 1);
        float2 extraAOAndDepth = LoadAOandDepth(extraCoord);
        extraAOAndDepth.g = GetViewDepthFromDepthTexture(extraAOAndDepth.g);
        _AOAndDepth[groupIndex] = extraAOAndDepth;
    }

    if (groupIndex >= 64 - MAX_FILTER_RADIUS)
    {
        int2 extraCoord = pixelCoord + int2(MAX_FILTER_RADIUS, 0);
        extraCoord = clamp(extraCoord, 0, _TextureSize.zw - 1);
        float2 extraAOAndDepth = LoadAOandDepth(extraCoord);
        extraAOAndDepth.g = GetViewDepthFromDepthTexture(extraAOAndDepth.g);
        _AOAndDepth[groupIndex + 2 * MAX_FILTER_RADIUS] = extraAOAndDepth;
    }
    
    GroupMemoryBarrierWithGroupSync();

    // ------------------------- Bilateral Blur -------------------------

    float2 middle = _AOAndDepth[groupIndex + MAX_FILTER_RADIUS];
    float weightSum = 0.0;
    float aoFactor = 0.0;
        
    int radius = int(GetSpatialBlurKernelRadius());
    for (int i = -radius; i <= radius; i++)
    {
        float2 sample = _AOAndDepth[groupIndex + MAX_FILTER_RADIUS + i];
        float depthDelta = abs(1 - sample.g / middle.g);
        // float depthDelta = abs(sample.g - middle.g);
        float weight = BilateralWeight(i, depthDelta, GetSpatialBlurSigma());
        aoFactor += sample.r * weight;
        weightSum += weight;
    }
    aoFactor /= weightSum;
    _OutputTexture[id.xy] = float2(aoFactor, outDepth);
}

[numthreads(1, 64, 1)]
void SpatialBlurVerticalKernel(uint3 id : SV_DispatchThreadID, uint groupIndex : SV_GroupIndex)
{
    // ------------------------- Fetch AO & Depth -------------------------
    
    int2 pixelCoord = clamp(id.xy, 0, _TextureSize.zw - 1);
    float2 aoAndDepth = LoadAOandDepth(pixelCoord);
    float outDepth = aoAndDepth.g;
    aoAndDepth.g = GetViewDepthFromDepthTexture(aoAndDepth.g);
    _AOAndDepth[groupIndex + MAX_FILTER_RADIUS] = aoAndDepth;

    if (groupIndex < MAX_FILTER_RADIUS)
    {
        int2 extraCoord = pixelCoord - int2(0, MAX_FILTER_RADIUS);
        extraCoord = clamp(extraCoord, 0, _TextureSize.zw - 1);
        float2 extraAOAndDepth = LoadAOandDepth(extraCoord);
        extraAOAndDepth.g = GetViewDepthFromDepthTexture(extraAOAndDepth.g);
        _AOAndDepth[groupIndex] = extraAOAndDepth;
    }

    if (groupIndex >= 64 - MAX_FILTER_RADIUS)
    {
        int2 extraCoord = pixelCoord + int2(0, MAX_FILTER_RADIUS);
        extraCoord = clamp(extraCoord, 0, _TextureSize.zw - 1);
        float2 extraAOAndDepth = LoadAOandDepth(extraCoord);
        extraAOAndDepth.g = GetViewDepthFromDepthTexture(extraAOAndDepth.g);
        _AOAndDepth[groupIndex + 2 * MAX_FILTER_RADIUS] = extraAOAndDepth;
    }
    
    GroupMemoryBarrierWithGroupSync();

    // ------------------------- Bilateral Blur -------------------------

    float2 middle = _AOAndDepth[groupIndex + MAX_FILTER_RADIUS];
    float weightSum = 0.0;
    float aoFactor = 0.0;
        
    int radius = int(GetSpatialBlurKernelRadius());
    for (int i = -radius; i <= radius; i++)
    {
        float2 sample = _AOAndDepth[groupIndex + MAX_FILTER_RADIUS + i];
        float depthDelta = abs(1 - sample.g / middle.g);
        // float depthDelta = abs(sample.g - middle.g);
        float weight = BilateralWeight(i, depthDelta, GetSpatialBlurSigma());
        aoFactor += sample.r * weight;
        weightSum += weight;
    }
    aoFactor /= weightSum;
    _OutputTexture[id.xy] = float2(aoFactor, outDepth);
}

// ----------------------------------------------------------------------------------------------------
// Temporal Filter Functions & Kernel
// ----------------------------------------------------------------------------------------------------

inline void GetNeighbourhoodTileIDs(uint2 middleTileID, inout uint tileIDs[9])
{
    tileIDs[0] = (middleTileID.y     ) * TILE_SIZE + (middleTileID.x     );
    tileIDs[1] = (middleTileID.y +  0) * TILE_SIZE + (middleTileID.x +  1);
    tileIDs[2] = (middleTileID.y +  1) * TILE_SIZE + (middleTileID.x +  0);
    tileIDs[3] = (middleTileID.y +  0) * TILE_SIZE + (middleTileID.x + -1);
    tileIDs[4] = (middleTileID.y + -1) * TILE_SIZE + (middleTileID.x +  0);
    tileIDs[5] = (middleTileID.y +  1) * TILE_SIZE + (middleTileID.x + -1);
    tileIDs[6] = (middleTileID.y +  1) * TILE_SIZE + (middleTileID.x +  1);
    tileIDs[7] = (middleTileID.y + -1) * TILE_SIZE + (middleTileID.x + -1);
    tileIDs[8] = (middleTileID.y + -1) * TILE_SIZE + (middleTileID.x +  1);
}

inline void GetNeighbourhoodSamples(in uint tileIDs[9], inout float2 samples[9])
{
    samples[0] = _AOZ[tileIDs[0]];
    samples[1] = _AOZ[tileIDs[1]];
    samples[2] = _AOZ[tileIDs[2]];
    samples[3] = _AOZ[tileIDs[3]];
    samples[4] = _AOZ[tileIDs[4]];
    samples[5] = _AOZ[tileIDs[5]];
    samples[6] = _AOZ[tileIDs[6]];
    samples[7] = _AOZ[tileIDs[7]];
    samples[8] = _AOZ[tileIDs[8]];
}

float FilterMiddleColor(in float2 samples[9], float middleDepth)
{
    const float weights[9] = { 4.0, 2.0, 2.0, 2.0, 2.0, 1.0, 1.0, 1.0, 1.0 };
    
    float weightSum = 4.0;
    float filtered = weightSum * samples[0].r;

    UNITY_UNROLL
    for (int i = 0; i < 8; i++)
    {
        float sampleDepth = GetViewDepthFromDepthTexture(samples[i + 1].g);
        bool occlusionTest = abs(1 - sampleDepth / middleDepth) < 0.1;
        float weight = weights[i + 1] * occlusionTest;
        weightSum += weight;
        filtered += weight * samples[i + 1].r;
    }
    
    filtered *= rcp(weightSum);
    return filtered;
}

float2 VarianceMinMax(in float2 samples[9], float gamma)
{
    float m1 = 0;
    float m2 = 0;

    UNITY_UNROLL
    for (int i = 0; i < 9; i++)
    {
        float sampleColor = samples[i].r;
        m1 += sampleColor;
        m2 += sampleColor * sampleColor;
    }

    const int sampleCount = 9;
    m1 *= rcp(sampleCount);
    m2 *= rcp(sampleCount);

    float sigma = sqrt(abs(m2 - m1 * m1)); // standard deviation
    float neighborMin = m1 - gamma * sigma;
    float neighborMax = m1 + gamma * sigma;

    return float2(neighborMin, neighborMax);
}

[numthreads(THREAD_NUM, THREAD_NUM, 1)]
void TemporalBlurKernel(uint3 id : SV_DispatchThreadID, uint3 groupThreadId : SV_GroupThreadID, uint3 groupId : SV_GroupID, uint groupIndex : SV_GroupIndex)
{
    // ------------------------- Fetch AO & Depth -------------------------

    int2 tileTopLeftCoord = groupId.xy * THREAD_NUM - TILE_BORDER;

    UNITY_UNROLL
    for (uint i = groupIndex; i < TILE_SIZE * TILE_SIZE; i += THREAD_NUM * THREAD_NUM)
    {
        int2 coord = tileTopLeftCoord + int2(i % TILE_SIZE, i / TILE_SIZE);
        coord = clamp(coord, 0, _TextureSize.zw - 1);
        _AOZ[i] = LoadAOandDepth(coord);
    }

    GroupMemoryBarrierWithGroupSync();

    // ------------------------- Temporal Blur -------------------------

    float2 screenUV = (float2(id.xy) + float2(0.5, 0.5)) * _TextureSize.xy;
    float2 velocity = SAMPLE_TEXTURE2D_LOD(_MotionVectorTexture, sampler_PointClamp, screenUV, 0).rg;
    float2 historyUV = screenUV - velocity;
    float2 history = SAMPLE_TEXTURE2D_LOD(_AmbientOcclusionHistory, sampler_LinearClamp, historyUV, 0);

    uint2 middleTileID = groupThreadId.xy + TILE_BORDER;
    uint tileIDs[9];
    GetNeighbourhoodTileIDs(middleTileID, tileIDs);

    float2 neighbours[9];
    GetNeighbourhoodSamples(tileIDs, neighbours);
    
    float2 minMax = VarianceMinMax(neighbours, GetTemporalVarianceCriticalValue());
    history.r = clamp(history.r, minMax.x, minMax.y);

    float middleDepth = GetViewDepthFromDepthTexture(neighbours[0].g);
    float prefiltered = FilterMiddleColor(neighbours, middleDepth);
    
    float historyDepth = GetViewDepthFromDepthTexture(history.g);
    bool depthTest = abs(1 - middleDepth / historyDepth) < 0.1;
    float blendFactor = lerp(0, 0.9, depthTest);
    
    blendFactor = lerp(blendFactor, 0, any(abs(historyUV - 0.5) > 0.5));
    
    _OutputTexture[id.xy] = float2(lerp(prefiltered, history.r, blendFactor), neighbours[0].g);
}