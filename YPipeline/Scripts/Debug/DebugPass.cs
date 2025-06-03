using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public class DebugPass : PipelinePass
    {
        private class DebugPassData
        {
            // Light Culling Debug
            public Material lightCullingDebugMaterial;
            public bool showLightTiles;
            public float tileOpacity;
        }

        protected override void Initialize()
        {
            
        }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<DebugPassData>("Debug (Editor)", out var passData))
            {
                // Light Culling Debug
                passData.lightCullingDebugMaterial = data.debugSettings.lightingDebugSettings.lightCullingDebugMaterial;
                passData.showLightTiles = data.debugSettings.lightingDebugSettings.showLightTiles;
                passData.tileOpacity = data.debugSettings.lightingDebugSettings.tileOpacity;
                
                builder.AllowPassCulling(true);
                
                builder.SetRenderFunc((DebugPassData data, RenderGraphContext context) =>
                {
                    // Light Culling Debug
                    if (data.showLightTiles)
                    {
                        data.lightCullingDebugMaterial.SetFloat(LightingDebugSettings.k_TilesDebugOpacityID, data.tileOpacity);
                        context.cmd.DrawProcedural(Matrix4x4.identity, data.lightCullingDebugMaterial, 0, MeshTopology.Triangles, 3);
                    }
                    
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
                });
            }
        }
    }
#endif
}