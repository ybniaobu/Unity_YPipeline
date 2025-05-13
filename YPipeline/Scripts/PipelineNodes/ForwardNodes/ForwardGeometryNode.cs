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
                    sortingCriteria = SortingCriteria.OptimizeStateChanges
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

                nodeData.colorAttachment = builder.UseColorBuffer(data.CameraColorAttachment, 0);
                nodeData.depthAttachment = builder.UseDepthBuffer(data.CameraDepthAttachment, DepthAccess.Read);
               
                builder.AllowPassCulling(false);
                builder.AllowRendererListCulling(false);

                builder.SetRenderFunc((ForwardGeometryNodeData data, RenderGraphContext context) =>
                {
                    context.cmd.SetRenderTarget(data.colorAttachment, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                        data.depthAttachment, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
                    
                    context.cmd.BeginSample("Draw Opaque");
                    context.cmd.DrawRendererList(data.opaqueRendererList);
                    context.cmd.EndSample("Draw Opaque");
                    
                    context.cmd.BeginSample("Draw AlphaTest");
                    context.cmd.DrawRendererList(data.alphaTestRendererList);
                    context.cmd.EndSample("Draw AlphaTest");
                    
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
                });
            }
        }
    }
}