using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class CullingPass : PipelinePass
    {
        // private class CullingPassData
        // {
        //     
        // }
        
        protected override void Initialize() { }

        public override void OnRecord(ref YPipelineData data)
        {
            data.camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters);
            cullingParameters.shadowDistance = Mathf.Min(data.asset.maxShadowDistance, data.camera.farClipPlane);
            data.cullingResults = data.context.Cull(ref cullingParameters);
            
            // TODO：Culling 是否需要放在 RenderGraph 记录前，以便更早进行 Culling？？观察一下 URP 和 HDRP。
            // using (var builder = data.renderGraph.AddUnsafePass<CullingPassData>("Culling", out var passData))
            // {
            //     data.camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters);
            //     cullingParameters.shadowDistance = Mathf.Min(data.asset.maxShadowDistance, data.camera.farClipPlane);
            //     data.cullingResults = data.context.Cull(ref cullingParameters);
            //     
            //     builder.AllowPassCulling(false);
            //     builder.SetRenderFunc((CullingPassData data, UnsafeGraphContext context) => { });
            // }
        }
    }
}