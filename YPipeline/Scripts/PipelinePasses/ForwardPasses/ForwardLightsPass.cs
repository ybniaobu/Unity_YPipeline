using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using Unity.Collections;
using UnityEngine.Experimental.Rendering;

namespace YPipeline
{
    /// <summary>
    /// 记录灯光数据至 YPipelineData 的 YPipelineLightsData 并且将 per light data 传递至 GPU
    /// </summary>
    public class ForwardLightsPass : PipelinePass
    {
        private class ForwardLightsPassData
        {
            
        }
        
        protected override void Initialize() { }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<ForwardLightsPassData>("Set Global Light Data", out var passData))
            {
                
            }
        }
    }
}