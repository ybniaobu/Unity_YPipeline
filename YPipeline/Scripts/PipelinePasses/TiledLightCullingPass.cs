using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class TiledLightCullingPass : PipelinePass
    {
        private class TiledLightCullingPassData
        {
            public ComputeShader cs;
            // Input Buffer
            public BufferHandle lightsCullingInputBuffer;
            public Vector4[] lightsBound;
            
            // Output Buffer
            public BufferHandle tilesLightIndicesBuffer; // 每个 tile 都包含一个 header（light 的数量）和每个 light 的 index
            
            // Params
            public Vector2Int tileCountXY;
            public Vector2 tileUVSize;
        }

        struct LightsInputData
        {
            public Vector4 bounds;
            public int lightType;
        }
        
        
        protected override void Initialize() { }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<TiledLightCullingPassData>("Tiled Based Light Culling", out var passData))
            {
                passData.cs = data.asset.pipelineResources.computeShaders.tiledLightCullingCs;
                
                // Input
                passData.lightsBound = data.lightsData.lightsBound;

                passData.lightsCullingInputBuffer = builder.CreateTransientBuffer(new BufferDesc()
                {
                    count = YPipelineLightsData.k_MaxPunctualLightCount,
                    stride = 4 * sizeof(float),
                    target = GraphicsBuffer.Target.Structured,
                    name = "Lights Culling Input Buffer"
                });
                
                float pixelToTileX = data.BufferSize.x / (float) YPipelineLightsData.k_TileSize;
                float pixelToTileY = data.BufferSize.y / (float) YPipelineLightsData.k_TileSize;
                passData.tileCountXY = new Vector2Int(Mathf.CeilToInt(pixelToTileX), Mathf.CeilToInt(pixelToTileY));
                int tileCount = passData.tileCountXY.x * passData.tileCountXY.y;
                passData.tileUVSize = new Vector2(1.0f / pixelToTileX, 1.0f / pixelToTileY);
                
                // Output
                data.TilesLightIndicesBufferHandle = data.renderGraph.CreateBuffer(new BufferDesc()
                {
                    count = tileCount * YPipelineLightsData.k_PerTileDataSize,
                    stride = 4,
                    target = GraphicsBuffer.Target.Structured,
                    name = "Tiled Light Indices Buffer"
                });
                
                passData.tilesLightIndicesBuffer = builder.WriteBuffer(data.TilesLightIndicesBufferHandle);
                
                builder.AllowPassCulling(false);
                
                builder.SetRenderFunc((TiledLightCullingPassData data, RenderGraphContext context) =>
                {
                    int kernel = data.cs.FindKernel("TiledLightCulling");
                    context.cmd.SetBufferData(data.lightsCullingInputBuffer, data.lightsBound);
                    context.cmd.SetComputeBufferParam(data.cs, kernel, YPipelineShaderIDs.k_LightsCullingInputBufferID, data.lightsCullingInputBuffer);
                    
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_TileParamsID, new Vector4(data.tileCountXY.x, data.tileCountXY.y, data.tileUVSize.x, data.tileUVSize.y));
                    context.cmd.SetGlobalBuffer(YPipelineShaderIDs.k_TilesLightIndicesBufferID, data.tilesLightIndicesBuffer);
                    context.cmd.DispatchCompute(data.cs, kernel, data.tileCountXY.x, data.tileCountXY.y, 1);
                    
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
                });
            }
        }
    }
}