using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class ForwardGeometryNode : PipelineNode
    {
        private class ForwardGeometryNodeData
        {
            public TextureHandle colorAttachment;
            public TextureHandle depthAttachment;
            
            public RendererListHandle opaqueRendererList;
            public RendererListHandle alphaTestRendererList;
        }

        protected override void Initialize() { }

        protected override void OnDispose()
        {
            //DestroyImmediate(this);
        }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<ForwardGeometryNodeData>("Draw Opaque & AlphaTest", out var nodeData))
            {
                RendererListDesc opaqueRendererListDesc = new RendererListDesc(YPipelineShaderTagIDs.k_OpaqueShaderTagIds, data.cullingResults, data.camera)
                {
                    rendererConfiguration = PerObjectData.ReflectionProbes | PerObjectData.Lightmaps | PerObjectData.ShadowMask | PerObjectData.LightProbe | PerObjectData.OcclusionProbe,
                    renderQueueRange = new RenderQueueRange(2000, 2449),
                    sortingCriteria = SortingCriteria.CommonOpaque
                };
                
                RendererListDesc alphaTestRendererListDesc = new RendererListDesc(YPipelineShaderTagIDs.k_OpaqueShaderTagIds, data.cullingResults, data.camera)
                {
                    rendererConfiguration = PerObjectData.ReflectionProbes | PerObjectData.Lightmaps | PerObjectData.ShadowMask | PerObjectData.LightProbe | PerObjectData.OcclusionProbe,
                    renderQueueRange = new RenderQueueRange(2450, 2499),
                    sortingCriteria = SortingCriteria.OptimizeStateChanges
                };
                
                nodeData.opaqueRendererList = data.renderGraph.CreateRendererList(opaqueRendererListDesc);
                nodeData.alphaTestRendererList = data.renderGraph.CreateRendererList(alphaTestRendererListDesc);
                builder.UseRendererList(nodeData.opaqueRendererList);
                builder.UseRendererList(nodeData.alphaTestRendererList);

                nodeData.colorAttachment = data.CameraColorAttachment;
                nodeData.depthAttachment = data.CameraDepthAttachment;
                builder.WriteTexture(nodeData.colorAttachment);
                builder.ReadTexture(nodeData.depthAttachment);
               
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((ForwardGeometryNodeData data, RenderGraphContext context) =>
                {
                    context.cmd.SetRenderTarget(nodeData.colorAttachment, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                        nodeData.depthAttachment, RenderBufferLoadAction.Load, RenderBufferStoreAction.DontCare);
                    context.cmd.ClearRenderTarget(false, true, Color.clear);
                    
                    context.cmd.DrawRendererList(data.opaqueRendererList);
                    context.cmd.DrawRendererList(data.alphaTestRendererList);
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
                });
            }
        }
    }
}