using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace YPipeline
{
    public class GizmosPass : PipelinePass
    {
#if UNITY_EDITOR
        private class GizmosPassData
        {
            public TextureHandle depthAttachment;
            public TextureHandle cameraDepthTarget;
            
            public RendererListHandle preGizmosRendererList;
            public RendererListHandle postGizmosRendererList;
        }
#endif
        
        protected override void Initialize()
        {
            
        }

        public override void OnRecord(ref YPipelineData data)
        {
#if UNITY_EDITOR
            if (Handles.ShouldRenderGizmos())
            {
                using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<GizmosPassData>("Gizmos (Editor)", out var passData))
                {
                    passData.depthAttachment = data.CameraDepthAttachment;
                    passData.cameraDepthTarget = data.CameraDepthTarget;
                    builder.ReadTexture(passData.depthAttachment);
                    builder.WriteTexture(passData.cameraDepthTarget);
                    
                    passData.preGizmosRendererList = data.renderGraph.CreateGizmoRendererList(data.camera, GizmoSubset.PreImageEffects);
                    builder.UseRendererList(passData.preGizmosRendererList);
                    passData.postGizmosRendererList = data.renderGraph.CreateGizmoRendererList(data.camera, GizmoSubset.PostImageEffects);
                    builder.UseRendererList(passData.postGizmosRendererList);
                    
                    builder.SetRenderFunc((GizmosPassData data, RenderGraphContext context) =>
                    {
                        BlitUtility.CopyDepth(context.cmd, data.depthAttachment, data.cameraDepthTarget);
                        context.cmd.DrawRendererList(data.preGizmosRendererList);
                        context.cmd.DrawRendererList(data.postGizmosRendererList);
                        context.renderContext.ExecuteCommandBuffer(context.cmd);
                        context.cmd.Clear();
                    });
                }
            }
#endif
        }
    }
}