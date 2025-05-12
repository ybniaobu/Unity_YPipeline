using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class ErrorMaterialNode : PipelineNode
    {
#if UNITY_EDITOR
        private class ErrorMaterialNodeData
        {
            public TextureHandle colorAttachment;
            public TextureHandle depthAttachment;
            public RendererListHandle rendererList;
        }
        
        private Material m_ErrorMaterial;
#endif
        protected override void Initialize() { }

        public override void OnRecord(ref YPipelineData data)
        {
#if UNITY_EDITOR
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<ErrorMaterialNodeData>("Draw Error Material", out var nodeData))
            {
                if (m_ErrorMaterial == null)
                {
                    m_ErrorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
                    m_ErrorMaterial.hideFlags = HideFlags.HideAndDontSave;
                }
                
                RendererListDesc rendererListDesc = new RendererListDesc(YPipelineShaderTagIDs.k_LegacyShaderTagIds, data.cullingResults, data.camera)
                {
                    overrideMaterial = m_ErrorMaterial,
                    renderQueueRange = RenderQueueRange.all,
                };

                nodeData.rendererList = data.renderGraph.CreateRendererList(rendererListDesc);
                builder.UseRendererList(nodeData.rendererList);
                
                nodeData.colorAttachment = builder.UseColorBuffer(data.CameraColorAttachment, 0);
                nodeData.depthAttachment = builder.UseDepthBuffer(data.CameraDepthAttachment, DepthAccess.Read);
                
                builder.SetRenderFunc((ErrorMaterialNodeData data, RenderGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.rendererList);
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
                });
            }
#endif
        }
    }
}