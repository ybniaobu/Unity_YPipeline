using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class ErrorMaterialPass : PipelinePass
    {
#if UNITY_EDITOR
        private class ErrorMaterialPassData
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
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<ErrorMaterialPassData>("Draw Error Material", out var passData))
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

                passData.rendererList = data.renderGraph.CreateRendererList(rendererListDesc);
                builder.UseRendererList(passData.rendererList);
                
                passData.colorAttachment = builder.UseColorBuffer(data.CameraColorAttachment, 0);
                passData.depthAttachment = builder.UseDepthBuffer(data.CameraDepthAttachment, DepthAccess.Read);
                
                builder.SetRenderFunc((ErrorMaterialPassData data, RenderGraphContext context) =>
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