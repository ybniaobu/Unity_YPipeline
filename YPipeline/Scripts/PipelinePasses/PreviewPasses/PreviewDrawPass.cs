﻿using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class PreviewDrawPass : PipelinePass
    {
        private class DrawPassData
        {
            public TextureHandle colorTarget;
            public TextureHandle depthTarget;
            
            public RendererListHandle opaqueRendererList;
            public RendererListHandle alphaTestRendererList;
            public RendererListHandle errorRendererList;
            public RendererListHandle skyboxRendererList;
            public RendererListHandle transparencyRendererList;
        }
        
        private Material m_ErrorMaterial;

        protected override void Initialize() { }

        protected override void OnDispose()
        {
            //DestroyImmediate(this);
        }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<DrawPassData>("Preview Draw", out var passData))
            {
                var stateBlock = new RenderStateBlock(RenderStateMask.Depth);
                stateBlock.depthState = new DepthState(true, CompareFunction.LessEqual);
                
                // opaque & alphaTest
                RendererListDesc opaqueRendererListDesc = new RendererListDesc(YPipelineShaderTagIDs.k_ForwardOpaqueShaderTagIds, data.cullingResults, data.camera)
                {
                    rendererConfiguration = PerObjectData.ReflectionProbes | PerObjectData.Lightmaps | PerObjectData.LightProbe,
                    renderQueueRange = new RenderQueueRange(2000, 2449),
                    sortingCriteria = SortingCriteria.OptimizeStateChanges,
                    stateBlock = stateBlock
                };
                
                RendererListDesc alphaTestRendererListDesc = new RendererListDesc(YPipelineShaderTagIDs.k_ForwardOpaqueShaderTagIds, data.cullingResults, data.camera)
                {
                    rendererConfiguration = PerObjectData.ReflectionProbes | PerObjectData.Lightmaps | PerObjectData.LightProbe,
                    renderQueueRange = new RenderQueueRange(2450, 2499),
                    sortingCriteria = SortingCriteria.OptimizeStateChanges,
                    stateBlock = stateBlock
                };
                
                passData.opaqueRendererList = data.renderGraph.CreateRendererList(opaqueRendererListDesc);
                passData.alphaTestRendererList = data.renderGraph.CreateRendererList(alphaTestRendererListDesc);
                builder.UseRendererList(passData.opaqueRendererList);
                builder.UseRendererList(passData.alphaTestRendererList);
                
                // Error Material
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

                passData.errorRendererList = data.renderGraph.CreateRendererList(rendererListDesc);
                builder.UseRendererList(passData.errorRendererList);
                
                // Skybox
                passData.skyboxRendererList = data.renderGraph.CreateSkyboxRendererList(data.camera);
                builder.UseRendererList(passData.skyboxRendererList);
                
                // Transparency
                RendererListDesc transparencyRendererListDesc = new RendererListDesc(YPipelineShaderTagIDs.k_ForwardTransparencyShaderTagIds, data.cullingResults, data.camera)
                {
                    rendererConfiguration = PerObjectData.ReflectionProbes | PerObjectData.Lightmaps | PerObjectData.LightProbe,
                    renderQueueRange = RenderQueueRange.transparent,
                    sortingCriteria = SortingCriteria.CommonTransparent,
                };
                
                passData.transparencyRendererList = data.renderGraph.CreateRendererList(transparencyRendererListDesc);
                builder.UseRendererList(passData.transparencyRendererList);
                
                // Use Color & Depth Buffer
                passData.colorTarget = builder.UseColorBuffer(data.CameraColorTarget, 0);
                passData.depthTarget = builder.UseDepthBuffer(data.CameraDepthTarget, DepthAccess.Write);
                
                builder.ReadBuffer(data.PunctualLightBufferHandle);
                
                builder.AllowPassCulling(false);
                builder.AllowRendererListCulling(false);

                builder.SetRenderFunc((DrawPassData data, RenderGraphContext context) =>
                {
                    // context.cmd.SetRenderTarget(data.colorAttachment, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    //     data.depthAttachment, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
                    context.cmd.ClearRenderTarget(true, true, Color.clear);
                    
                    context.cmd.DrawRendererList(data.opaqueRendererList);
                    context.cmd.DrawRendererList(data.alphaTestRendererList);
                    context.cmd.DrawRendererList(data.errorRendererList);
                    context.cmd.DrawRendererList(data.skyboxRendererList);
                    context.cmd.DrawRendererList(data.transparencyRendererList);
                    
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
                });
            }
        }
    }
}