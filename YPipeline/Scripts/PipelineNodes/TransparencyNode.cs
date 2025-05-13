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
                
                builder.ReadTexture(data.CameraColorTexture);
                builder.ReadTexture(data.CameraDepthTexture);
                nodeData.colorAttachment = builder.UseColorBuffer(data.CameraColorAttachment, 0);
                nodeData.depthAttachment = builder.UseDepthBuffer(data.CameraDepthAttachment, DepthAccess.Read);
                builder.AllowPassCulling(false);
                builder.AllowRendererListCulling(false);

                builder.SetRenderFunc((TransparencyNodeData data, RenderGraphContext context) =>
                {
                    // context.cmd.SetRenderTarget(data.colorAttachment, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,
                    //     data.depthAttachment, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
                    
                    context.cmd.DrawRendererList(data.transparencyRendererList);
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
                });
            }
        }
    }
}