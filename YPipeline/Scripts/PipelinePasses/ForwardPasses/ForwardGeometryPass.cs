using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class ForwardGeometryPass : PipelinePass
    {
        private class ForwardGeometryPassData
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
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<ForwardGeometryPassData>("Draw Opaque & AlphaTest", out var passData))
            {
                RendererListDesc opaqueRendererListDesc = new RendererListDesc(YPipelineShaderTagIDs.k_OpaqueShaderTagIds, data.cullingResults, data.camera)
                {
                    rendererConfiguration = PerObjectData.ReflectionProbes | PerObjectData.Lightmaps | PerObjectData.LightProbe,
                    renderQueueRange = new RenderQueueRange(2000, 2449),
                    sortingCriteria = SortingCriteria.OptimizeStateChanges
                };
                
                RendererListDesc alphaTestRendererListDesc = new RendererListDesc(YPipelineShaderTagIDs.k_OpaqueShaderTagIds, data.cullingResults, data.camera)
                {
                    rendererConfiguration = PerObjectData.ReflectionProbes | PerObjectData.Lightmaps | PerObjectData.LightProbe,
                    renderQueueRange = new RenderQueueRange(2450, 2499),
                    sortingCriteria = SortingCriteria.OptimizeStateChanges
                };
                
                passData.opaqueRendererList = data.renderGraph.CreateRendererList(opaqueRendererListDesc);
                passData.alphaTestRendererList = data.renderGraph.CreateRendererList(alphaTestRendererListDesc);
                builder.UseRendererList(passData.opaqueRendererList);
                builder.UseRendererList(passData.alphaTestRendererList);

                passData.colorAttachment = builder.UseColorBuffer(data.CameraColorAttachment, 0);
                passData.depthAttachment = builder.UseDepthBuffer(data.CameraDepthAttachment, DepthAccess.Read);
                if (data.isSunLightShadowMapCreated) builder.ReadTexture(data.SunLightShadowMap);
                if (data.isPointLightShadowMapCreated) builder.ReadTexture(data.PointLightShadowMap);
                if (data.isSpotLightShadowMapCreated) builder.ReadTexture(data.SpotLightShadowMap);

                builder.ReadBuffer(data.PunctualLightBufferHandle);
                builder.ReadBuffer(data.PointLightShadowBufferHandle);
                builder.ReadBuffer(data.PointLightShadowMatricesBufferHandle);
                builder.ReadBuffer(data.SpotLightShadowBufferHandle);
                builder.ReadBuffer(data.SpotLightShadowMatricesBufferHandle);
                //if (data.tilesBuffer.IsValid()) builder.ReadBuffer(data.tilesBuffer);
               
                builder.AllowPassCulling(false);
                builder.AllowRendererListCulling(false);

                builder.SetRenderFunc((ForwardGeometryPassData data, RenderGraphContext context) =>
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