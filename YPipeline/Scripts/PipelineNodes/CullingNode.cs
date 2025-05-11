using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class CullingNode : PipelineNode
    {
        private class CullingNodeData
        {
            
        }
        
        protected override void Initialize() { }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<CullingNodeData>("Culling", out var nodeData))
            {
                data.camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters);
                cullingParameters.shadowDistance = Mathf.Min(data.asset.maxShadowDistance, data.camera.farClipPlane);
                data.cullingResults = data.context.Cull(ref cullingParameters);
                
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((CullingNodeData data, RenderGraphContext context) => { });
            }
        }
    }
}