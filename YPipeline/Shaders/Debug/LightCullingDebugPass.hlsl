#ifndef YPIPELINE_LIGHT_CULLING_DEBUG_PASS_INCLUDED
#define YPIPELINE_LIGHT_CULLING_DEBUG_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Debug.hlsl"
#include "../../ShaderLibrary/Core/UnityInput.hlsl"
#include "../../ShaderLibrary/Core/YPipelineMacros.hlsl"

float4 _CameraBufferSize; // x: 1.0 / bufferSize.x, y: 1.0 / bufferSize.y, z: bufferSize.x, w: bufferSize.y
float4 _TileParams; // xy: tileCountXY, zw: tileUVSizeXY
StructuredBuffer<uint> _TilesLightIndicesBuffer;

float _TilesDebugOpacity;

struct Varyings
{
    float4 positionHCS  : SV_POSITION;
    float2 uv           : TEXCOORD0;
};

Varyings Vert(uint vertexID : SV_VertexID)
{
    Varyings OUT;
    
    //OUT.positionHCS = float4(vertexID <= 1 ? -1.0 : 3.0, vertexID == 1 ? 3.0 : -1.0, 0.0, 1.0);
    //OUT.uv = float2(vertexID <= 1 ? 0.0 : 2.0, vertexID == 1 ? 2.0 : 0.0);
    OUT.uv = float2((vertexID << 1) & 2, vertexID & 2);
    OUT.positionHCS = float4(OUT.uv * 2.0 - 1.0, UNITY_NEAR_CLIP_VALUE, 1.0);
    
    if (_ProjectionParams.x < 0.0) OUT.uv.y = 1.0 - OUT.uv.y;
    
    // #if UNITY_UV_STARTS_AT_TOP
    //     OUT.uv.y = 1.0 - OUT.uv.y;
    // #endif
    
    return OUT;
}

float4 Frag(Varyings IN) : SV_TARGET
{
    uint2 tileCoord = floor(IN.uv / _TileParams.zw);
    float2 startUV = tileCoord * _TileParams.zw;
    bool IsMinimumEdgePixel = any(IN.uv - startUV < _CameraBufferSize.xy);

    int tileIndex = tileCoord.y * (int) _TileParams.x + tileCoord.x;
    int headerIndex = tileIndex * (MAX_LIGHT_COUNT_PER_TILE + 1);
    int lightCount = _TilesLightIndicesBuffer[headerIndex];
    
    float3 color;
    if (IsMinimumEdgePixel) color = 1.0;
    else color = OverlayHeatMap(IN.uv * _CameraBufferSize.zw, _CameraBufferSize.zw * _TileParams.zw, lightCount, MAX_LIGHT_COUNT_PER_TILE, 1.0).rgb;
    
    return float4(color, _TilesDebugOpacity);
}

#endif