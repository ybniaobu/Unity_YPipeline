using System.Diagnostics;
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
                    nodeData.preGizmosRendererList = data.renderGraph.CreateGizmoRendererList(data.camera, GizmoSubset.PreImageEffects);
                    builder.UseRendererList(nodeData.preGizmosRendererList);
                    nodeData.postGizmosRendererList = data.renderGraph.CreateGizmoRendererList(data.camera, GizmoSubset.PostImageEffects);
                    builder.UseRendererList(nodeData.postGizmosRendererList);
                    
                    builder.SetRenderFunc((GizmosNodeData data, RenderGraphContext context) =>
                    {
                        BlitUtility.CopyDepth(context.cmd, YPipelineShaderIDs.k_DepthBufferID, BuiltinRenderTextureType.CameraTarget);
                        context.cmd.DrawRendererList(data.preGizmosRendererList);
                        context.cmd.DrawRendererList(data.postGizmosRendererList);
                    });
                }
            }
#endif
        }
    }
}