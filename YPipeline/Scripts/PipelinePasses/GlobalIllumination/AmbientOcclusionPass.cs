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
            public TextureHandle transition;
            
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
            
            if (data.isAmbientOcclusionTextureCreated)
            {
                using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<AmbientOcclusionPassData>("Ambient Occlusion", out var passData))
                {
                    Vector2Int bufferSize = data.BufferSize / 2;
                    int threadGroupSizeX = Mathf.CeilToInt(bufferSize.x / 8.0f);
                    int threadGroupSizeY = Mathf.CeilToInt(bufferSize.y / 8.0f);
                    passData.threadGroupSize = new Vector2Int(threadGroupSizeX, threadGroupSizeY);
                    passData.textureSize = new Vector4(1f / bufferSize.x, 1f / bufferSize.y, bufferSize.x, bufferSize.y);

                    passData.cs = data.asset.pipelineResources.computeShaders.ambientOcclusionCs;
                    builder.ReadTexture(data.ThinGBuffer);
                    builder.ReadTexture(data.CameraDepthTexture);
                    
                    passData.ambientOcclusionParams = new Vector4(m_AO.intensity.value, m_AO.sampleCount.value, m_AO.radius.value, m_AO.centerIntensity.value);

                    // Create Ambient Occlusion Texture
                    TextureDesc ambientOcclusionTextureDesc = new TextureDesc(bufferSize.x, bufferSize.y)
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
                    TextureDesc transitionDesc = new TextureDesc(bufferSize.x, bufferSize.y)
                    {
                        format = GraphicsFormat.R8_UNorm,
                        filterMode = FilterMode.Bilinear,
                        clearBuffer = false,
                        enableRandomWrite = true,
                        name = "Ambient Occlusion transition"
                    };
                    
                    passData.transition = builder.CreateTransientTexture(transitionDesc);
                    
                    
                    builder.AllowPassCulling(false);
                    builder.AllowRendererListCulling(false);

                    builder.SetRenderFunc((AmbientOcclusionPassData data, RenderGraphContext context) =>
                    {
                        context.cmd.SetComputeVectorParam(data.cs, "_TextureSize", data.textureSize);
                        context.cmd.SetComputeVectorParam(data.cs, YPipelineShaderIDs.k_AmbientOcclusionParamsID, data.ambientOcclusionParams);
                        
                        context.cmd.BeginSample("SSAO Compute Occlusion");
                        int ssaoKernel = data.cs.FindKernel("SSAOKernel");
                        context.cmd.SetComputeTextureParam(data.cs, ssaoKernel, "_OutputTexture", data.ambientOcclusionTexture);
                        context.cmd.DispatchCompute(data.cs, ssaoKernel, data.threadGroupSize.x, data.threadGroupSize.y, 1);
                        context.cmd.EndSample("SSAO Compute Occlusion");
                        
                        context.cmd.BeginSample("SSAO Gaussian Blur Horizontal");
                        int blurHorizontalKernel = data.cs.FindKernel("GaussianBlurHorizontalKernel");
                        context.cmd.SetComputeTextureParam(data.cs, blurHorizontalKernel, "_InputTexture", data.ambientOcclusionTexture);
                        context.cmd.SetComputeTextureParam(data.cs, blurHorizontalKernel, "_OutputTexture", data.transition);
                        context.cmd.DispatchCompute(data.cs, blurHorizontalKernel, data.threadGroupSize.x, data.threadGroupSize.y, 1);
                        context.cmd.EndSample("SSAO Gaussian Blur Horizontal");
                        
                        context.cmd.BeginSample("SSAO Gaussian Blur Vertical");
                        int blurVerticalKernel = data.cs.FindKernel("GaussianBlurVerticalKernel");
                        context.cmd.SetComputeTextureParam(data.cs, blurVerticalKernel, "_InputTexture", data.transition);
                        context.cmd.SetComputeTextureParam(data.cs, blurVerticalKernel, "_OutputTexture", data.ambientOcclusionTexture);
                        context.cmd.DispatchCompute(data.cs, blurVerticalKernel, data.threadGroupSize.x, data.threadGroupSize.y, 1);
                        context.cmd.EndSample("SSAO Gaussian Blur Vertical");
                        
                        context.cmd.SetGlobalTexture(YPipelineShaderIDs.k_AmbientOcclusionTextureID, data.ambientOcclusionTexture);

                        context.renderContext.ExecuteCommandBuffer(context.cmd);
                        context.cmd.Clear();
                    });
                }
            }
        }
    }
}