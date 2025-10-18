using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace YPipeline
{
    public class PreviewFinalPass : PipelinePass
    {
        private class FinalPassData
        {
            public TextureHandle cameraDepthTarget;
            public TextureHandle cameraColorTarget;
            
            public RendererListHandle preGizmosRendererList;
            public RendererListHandle postGizmosRendererList;
        }
        
        protected override void Initialize()
        {
            
        }
        
        protected override void OnDispose()
        {
            //DestroyImmediate(this);
        }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<FinalPassData>("Preview Final", out var passData))
            {
                passData.cameraColorTarget = builder.ReadTexture(data.CameraColorTarget);
                passData.cameraDepthTarget = builder.ReadTexture(data.CameraDepthTarget);
                
                passData.preGizmosRendererList = data.renderGraph.CreateGizmoRendererList(data.camera, GizmoSubset.PreImageEffects);
                builder.UseRendererList(passData.preGizmosRendererList);
                passData.postGizmosRendererList = data.renderGraph.CreateGizmoRendererList(data.camera, GizmoSubset.PostImageEffects);
                builder.UseRendererList(passData.postGizmosRendererList);
                
                builder.AllowPassCulling(false);
                
                builder.SetRenderFunc((FinalPassData data, RenderGraphContext context) =>
                {
                    if (Handles.ShouldRenderGizmos()) context.cmd.DrawRendererList(data.preGizmosRendererList);
                    // BlitUtility.CopyDepth(context.cmd, data.depthAttachment, data.cameraDepthTarget);
                    // BlitUtility.BlitTexture(context.cmd, data.colorAttachment, data.cameraColorTarget);
                    if (Handles.ShouldRenderGizmos()) context.cmd.DrawRendererList(data.postGizmosRendererList);
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
                });
            }
        }
    }
}