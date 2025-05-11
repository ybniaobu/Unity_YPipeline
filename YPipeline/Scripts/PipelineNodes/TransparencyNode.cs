using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class TransparencyNode : PipelineNode
    {
        private class TransparencyNodeData
        {
            public TextureHandle colorAttachment;
            public TextureHandle depthAttachment;
            
            public RendererListHandle transparencyRendererList;
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
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<TransparencyNodeData>("Draw Transparency", out var nodeData))
            {
                RendererListDesc transparencyRendererListDesc = new RendererListDesc(YPipelineShaderTagIDs.k_TransparencyShaderTagIds, data.cullingResults, data.camera)
                {
                    rendererConfiguration = PerObjectData.ReflectionProbes | PerObjectData.Lightmaps | PerObjectData.ShadowMask | PerObjectData.LightProbe | PerObjectData.OcclusionProbe,
                    renderQueueRange = RenderQueueRange.transparent,
                    sortingCriteria = SortingCriteria.CommonTransparent
                };
                
                nodeData.transparencyRendererList = data.renderGraph.CreateRendererList(transparencyRendererListDesc);
                builder.UseRendererList(nodeData.transparencyRendererList);
                
                nodeData.colorAttachment = data.CameraColorAttachment;
                nodeData.depthAttachment = data.CameraDepthAttachment;
                builder.WriteTexture(nodeData.colorAttachment);
                builder.WriteTexture(nodeData.depthAttachment);
                
                builder.ReadTexture(data.CameraColorTexture);
                builder.ReadTexture(data.CameraDepthTexture);

                builder.SetRenderFunc((TransparencyNodeData data, RenderGraphContext context) =>
                {
                    context.cmd.SetRenderTarget(data.colorAttachment, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,
                        data.depthAttachment, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
                    context.cmd.DrawRendererList(data.transparencyRendererList);
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
                });
            }
        }
    }
}