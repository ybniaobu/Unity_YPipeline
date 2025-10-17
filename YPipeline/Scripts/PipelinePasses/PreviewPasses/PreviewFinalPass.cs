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
        private class PostProcessingPassData
        {
            public TextureHandle depthAttachment;
            public TextureHandle cameraDepthTarget;
            public TextureHandle colorAttachment;
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
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<PostProcessingPassData>("Disable Post Processing (Editor Preview)", out var passData))
            {
                passData.colorAttachment = builder.ReadTexture(data.CameraColorAttachment);
                passData.cameraColorTarget = builder.WriteTexture(data.CameraColorTarget);
                passData.depthAttachment = builder.ReadTexture(data.CameraDepthAttachment);
                passData.cameraDepthTarget = builder.WriteTexture(data.CameraDepthTarget);
                
                passData.preGizmosRendererList = data.renderGraph.CreateGizmoRendererList(data.camera, GizmoSubset.PreImageEffects);
                builder.UseRendererList(passData.preGizmosRendererList);
                passData.postGizmosRendererList = data.renderGraph.CreateGizmoRendererList(data.camera, GizmoSubset.PostImageEffects);
                builder.UseRendererList(passData.postGizmosRendererList);
                
                builder.SetRenderFunc((PostProcessingPassData data, RenderGraphContext context) =>
                {
                    if (Handles.ShouldRenderGizmos()) context.cmd.DrawRendererList(data.preGizmosRendererList);
                    BlitUtility.CopyDepth(context.cmd, data.depthAttachment, data.cameraDepthTarget);
                    BlitUtility.BlitTexture(context.cmd, data.colorAttachment, data.cameraColorTarget);
                    if (Handles.ShouldRenderGizmos()) context.cmd.DrawRendererList(data.postGizmosRendererList);
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
                });
            }
        }
    }
}