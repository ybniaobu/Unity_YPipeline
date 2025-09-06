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
// Spatial Filter Kernels
// ----------------------------------------------------------------------------------------------------

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
        float depthDelta = abs(sample.g - middle.g);
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
        float depthDelta = abs(sample.g - middle.g);
        float weight = BilateralWeight(i, depthDelta, GetSpatialBlurSigma());
        aoFactor += sample.r * weight;
        weightSum += weight;
    }
    aoFactor /= weightSum;
    _OutputTexture[id.xy] = float2(aoFactor, outDepth);
}

// ----------------------------------------------------------------------------------------------------
// Temporal Filter Kernel
// ----------------------------------------------------------------------------------------------------

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

    uint2 tileID = groupThreadId.xy + TILE_BORDER;
    uint tileIDs[9];
    tileIDs[0] = (tileID.y) * TILE_SIZE + (tileID.x);
    tileIDs[1] = (tileID.y +  0) * TILE_SIZE + (tileID.x +  1);
    tileIDs[2] = (tileID.y +  1) * TILE_SIZE + (tileID.x +  0);
    tileIDs[3] = (tileID.y +  0) * TILE_SIZE + (tileID.x + -1);
    tileIDs[4] = (tileID.y + -1) * TILE_SIZE + (tileID.x +  0);
    tileIDs[5] = (tileID.y +  1) * TILE_SIZE + (tileID.x + -1);
    tileIDs[6] = (tileID.y +  1) * TILE_SIZE + (tileID.x +  1);
    tileIDs[7] = (tileID.y + -1) * TILE_SIZE + (tileID.x + -1);
    tileIDs[8] = (tileID.y + -1) * TILE_SIZE + (tileID.x +  1);

    float2 neighbours[9];
    GetNeighbourhoodSamples(tileIDs, neighbours);

    float prefiltered = FilterMiddleColor(neighbours);

    float2 screenUV = (float2(id.xy) + float2(0.5, 0.5)) * _TextureSize.xy;
    float2 velocity = SAMPLE_TEXTURE2D_LOD(_MotionVectorTexture, sampler_PointClamp, screenUV, 0).rg;
    float2 historyUV = screenUV - velocity;
    float2 history = SAMPLE_TEXTURE2D_LOD(_AmbientOcclusionHistory, sampler_PointClamp, historyUV, 0);

    float2 minMax;
    VarianceMinMax(neighbours, prefiltered, 0.75, minMax);
    
    history.r = ClipToFiltered(minMax, prefiltered, history.r);
    float currentDepth = GetViewDepthFromDepthTexture(neighbours[0].g);
    float historyDepth = GetViewDepthFromDepthTexture(history.g);
    
    bool depthTest = abs(1 - currentDepth / historyDepth) < 0.1;
    float blendFactor = lerp(0, 0.9, depthTest);
    
    _OutputTexture[id.xy] = float2(lerp(prefiltered, history.r, blendFactor), neighbours[0].g);
}