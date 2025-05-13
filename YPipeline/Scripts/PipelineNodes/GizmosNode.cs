using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace YPipeline
{
    public class GizmosNode : PipelineNode
    {
#if UNITY_EDITOR
        private class GizmosNodeData
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
                using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<GizmosNodeData>("Gizmos (Editor)", out var nodeData))
                {
                    nodeData.depthAttachment = data.CameraDepthAttachment;
                    nodeData.cameraDepthTarget = data.CameraDepthTarget;
                    builder.ReadTexture(nodeData.depthAttachment);
                    builder.WriteTexture(nodeData.cameraDepthTarget);
                    
                    nodeData.preGizmosRendererList = data.renderGraph.CreateGizmoRendererList(data.camera, GizmoSubset.PreImageEffects);
                    builder.UseRendererList(nodeData.preGizmosRendererList);
                    nodeData.postGizmosRendererList = data.renderGraph.CreateGizmoRendererList(data.camera, GizmoSubset.PostImageEffects);
                    builder.UseRendererList(nodeData.postGizmosRendererList);
                    
                    builder.SetRenderFunc((GizmosNodeData data, RenderGraphContext context) =>
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