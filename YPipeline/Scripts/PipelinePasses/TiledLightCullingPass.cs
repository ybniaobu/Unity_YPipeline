using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using System.Runtime.InteropServices;
using Unity.Collections;

namespace YPipeline
{
    public class TiledLightCullingPass : PipelinePass
    {
        private class TiledLightCullingPassData
        {
            // Input Buffer
            public BufferHandle lightsInputDataBuffer;
            public Vector4[] lightsBound = new Vector4[k_MaxPunctualLightCount];
            public BufferHandle lightTypeBuffer;
            
            // Output Buffer
            public BufferHandle tilesLightsIndicesBuffer; // 包括每个 tile 里每个 light 的 index，根据 index 判断灯光类型
            public BufferHandle tilesLightsCountBuffer; // 包括每个 tile 的 light 数量
            
            // 
            public Vector2Int tileCountXY;
            public int tileCount;
            public Vector2 tileUVSize;
        }

        struct LightsInputData
        {
            public Vector4 bounds;
            public int lightType;
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Constants
        // ----------------------------------------------------------------------------------------------------
        
        public static readonly int k_TilesBufferID = Shader.PropertyToID("_tilesBuffer");
        public static readonly int k_TilesSettingsID = Shader.PropertyToID("_tilesSettings");

        private const int k_MaxPunctualLightCount = 16 * 16;
        // private const int k_MaxLightsPerTile = 32;
        private const int k_TileSize = 16;
        
        protected override void Initialize() { }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<TiledLightCullingPassData>("Tiled Based Light Culling", out var passData))
            {
                NativeArray<VisibleLight> visibleLights = data.cullingResults.visibleLights;
                
                // int otherLightCount = 0;
                // for (int i = 0; i < visibleLights.Length; i++)
                // {
                //     VisibleLight visibleLight = visibleLights[i];
                //     if (visibleLight.lightType != LightType.Directional)
                //     {
                //         Rect r = visibleLight.screenRect;
                //         passData.lightsBound[i] = new Vector4(r.xMin, r.yMin, r.xMax, r.yMax);
                //         passData.lightType[i] = (int) visibleLight.lightType;
                //     }
                // }
                //
                // passData.lightaBoundBuffer = builder.CreateTransientBuffer(new BufferDesc()
                // {
                //
                // });
                
                
                builder.SetRenderFunc((TiledLightCullingPassData data, RenderGraphContext context) =>
                {
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
                    
                });
            }
        }
    }
}