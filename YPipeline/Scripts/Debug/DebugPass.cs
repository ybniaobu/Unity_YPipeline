using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class DebugPass : PipelinePass
    {
        private class DebugPassData
        {
            // Light Culling Debug
            public Material lightCullingDebugMaterial;
            public float tileOpacity;
        }

        protected override void Initialize() { }
        
        protected override void OnDispose() { }

        protected override void OnRecord(ref YPipelineData data)
        {
            if (data.debugSettings.lightingDebugSettings.showLightTiles)
            {
                using (var builder = data.renderGraph.AddRasterRenderPass<DebugPassData>("Debug (Editor)", out var passData))
                {
                    // Light Culling Debug
                    passData.lightCullingDebugMaterial =
                        data.debugSettings.lightingDebugSettings.lightCullingDebugMaterial;
                    passData.tileOpacity = data.debugSettings.lightingDebugSettings.tileOpacity;

                    builder.SetRenderAttachment(data.CameraColorTarget, 0, AccessFlags.Write);

                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc((DebugPassData data, RasterGraphContext context) =>
                    {
                        // Light Culling Debug
                        data.lightCullingDebugMaterial.SetFloat(LightingDebugSettings.k_TilesDebugOpacityID, data.tileOpacity);
                        context.cmd.DrawProcedural(Matrix4x4.identity, data.lightCullingDebugMaterial, 0, MeshTopology.Triangles, 3);
                    });
                }
            }
        }
    }
}