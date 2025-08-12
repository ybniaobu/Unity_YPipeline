using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class AmbientOcclusionPass : PipelinePass
    {
        private class AmbientOcclusionPassData
        {
            public ComputeShader cs;
            
            public Vector2Int threadGroupSize;
            public Vector4 textureSize;
            
            public TextureHandle ambientOcclusionTexture;
            public TextureHandle transition0;
            public TextureHandle transition1;
            
            // TODO: 给 shading model 的全局 AO intensity
            // public Vector4 ambientOcclusionParams;
            public Vector4 ambientOcclusionParams;
        }

        private AmbientOcclusion m_AO;
        
        protected override void Initialize() { }

        public override void OnRecord(ref YPipelineData data)
        {
            var stack = VolumeManager.instance.stack;
            m_AO = stack.GetComponent<AmbientOcclusion>();
            data.isAmbientOcclusionTextureCreated = m_AO.IsActive();
            CoreUtils.SetKeyword(data.cmd, YPipelineKeywords.k_ScreenSpaceAmbientOcclusion, data.isAmbientOcclusionTextureCreated);
            
            if (data.isAmbientOcclusionTextureCreated)
            {
                using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<AmbientOcclusionPassData>("Ambient Occlusion", out var passData))
                {
                    int halfResolution = m_AO.halfResolution.value ? 2 : 1;
                    Vector2Int textureSize = data.BufferSize / halfResolution;
                    int threadGroupSizeX = Mathf.CeilToInt(textureSize.x / 8.0f);
                    int threadGroupSizeY = Mathf.CeilToInt(textureSize.y / 8.0f);
                    passData.threadGroupSize = new Vector2Int(threadGroupSizeX, threadGroupSizeY);
                    passData.textureSize = new Vector4(1f / textureSize.x, 1f / textureSize.y, textureSize.x, textureSize.y);

                    passData.cs = data.asset.pipelineResources.computeShaders.ambientOcclusionCs;
                    builder.ReadTexture(data.ThinGBuffer);
                    builder.ReadTexture(data.CameraDepthTexture);
                    
                    passData.ambientOcclusionParams = new Vector4(m_AO.intensity.value, m_AO.sampleCount.value, m_AO.radius.value, m_AO.reflectionRate.value);

                    // Create Ambient Occlusion Texture
                    TextureDesc ambientOcclusionTextureDesc = new TextureDesc(textureSize.x, textureSize.y)
                    {
                        format = GraphicsFormat.R8_UNorm,
                        filterMode = FilterMode.Bilinear,
                        clearBuffer = true,
                        clearColor = Color.white,
                        enableRandomWrite = true,
                        name = "Ambient Occlusion Texture"
                    };

                    data.AmbientOcclusionTexture = data.renderGraph.CreateTexture(ambientOcclusionTextureDesc);
                    passData.ambientOcclusionTexture = builder.WriteTexture(data.AmbientOcclusionTexture);
                    
                    // Ambient Occlusion Transition Texture
                    TextureDesc transitionDesc0 = new TextureDesc(textureSize.x, textureSize.y)
                    {
                        format = GraphicsFormat.R8G8B8A8_UNorm,
                        filterMode = FilterMode.Bilinear,
                        clearBuffer = false,
                        enableRandomWrite = true,
                        name = "Ambient Occlusion transition0"
                    };
                    
                    TextureDesc transitionDesc1 = new TextureDesc(textureSize.x, textureSize.y)
                    {
                        format = GraphicsFormat.R8G8B8A8_UNorm,
                        filterMode = FilterMode.Bilinear,
                        clearBuffer = false,
                        enableRandomWrite = true,
                        name = "Ambient Occlusion transition1"
                    };
                    
                    passData.transition0 = builder.CreateTransientTexture(transitionDesc0);
                    passData.transition1 = builder.CreateTransientTexture(transitionDesc1);
                    
                    builder.AllowPassCulling(false);
                    builder.AllowRendererListCulling(false);

                    builder.SetRenderFunc((AmbientOcclusionPassData data, RenderGraphContext context) =>
                    {
                        context.cmd.SetComputeVectorParam(data.cs, "_TextureSize", data.textureSize);
                        context.cmd.SetComputeVectorParam(data.cs, YPipelineShaderIDs.k_AmbientOcclusionParamsID, data.ambientOcclusionParams);
                        
                        context.cmd.BeginSample("SSAO Compute Occlusion");
                        int ssaoKernel = data.cs.FindKernel("SSAOKernel");
                        context.cmd.SetComputeTextureParam(data.cs, ssaoKernel, "_OutputTexture", data.transition0);
                        context.cmd.DispatchCompute(data.cs, ssaoKernel, data.threadGroupSize.x, data.threadGroupSize.y, 1);
                        context.cmd.EndSample("SSAO Compute Occlusion");
                        
                        context.cmd.BeginSample("SSAO Spatial Blur Horizontal");
                        int blurHorizontalKernel = data.cs.FindKernel("SpatialBlurHorizontalKernel");
                        context.cmd.SetComputeTextureParam(data.cs, blurHorizontalKernel, "_InputTexture", data.transition0);
                        context.cmd.SetComputeTextureParam(data.cs, blurHorizontalKernel, "_OutputTexture", data.transition1);
                        context.cmd.DispatchCompute(data.cs, blurHorizontalKernel, data.threadGroupSize.x, data.threadGroupSize.y, 1);
                        context.cmd.EndSample("SSAO Spatial Blur Horizontal");
                        
                        context.cmd.BeginSample("SSAO Spatial Blur Vertical");
                        int blurVerticalKernel = data.cs.FindKernel("SpatialBlurVerticalKernel");
                        context.cmd.SetComputeTextureParam(data.cs, blurVerticalKernel, "_InputTexture", data.transition1);
                        context.cmd.SetComputeTextureParam(data.cs, blurVerticalKernel, "_OutputTexture", data.ambientOcclusionTexture);
                        context.cmd.DispatchCompute(data.cs, blurVerticalKernel, data.threadGroupSize.x, data.threadGroupSize.y, 1);
                        context.cmd.EndSample("SSAO Spatial Blur Vertical");
                        
                        context.cmd.SetGlobalTexture(YPipelineShaderIDs.k_AmbientOcclusionTextureID, data.ambientOcclusionTexture);

                        context.renderContext.ExecuteCommandBuffer(context.cmd);
                        context.cmd.Clear();
                    });
                }
            }
        }
    }
}