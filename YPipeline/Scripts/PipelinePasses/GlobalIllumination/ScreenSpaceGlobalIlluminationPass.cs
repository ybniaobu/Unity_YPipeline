using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class ScreenSpaceGlobalIlluminationPass : PipelinePass
    {
        private class SSGIPassData
        {
            public ComputeShader hbilCS;
            public ComputeShader denoiseCS;
            public bool enableHalfResolution;
            public bool enableTemporalDenoise;
            public Vector2Int threadGroupSizes;
            
            public Vector4 textureSize;
            public Vector4 ssgiParams;
            public Vector4 fallbackParams;
            
            public TextureHandle sceneHistory; // TAAHistory or SceneHistory
            public TextureHandle irradianceTexture;
            public TextureHandle irradianceHistory;
            public TextureHandle transition0;
        }

        private ScreenSpaceGlobalIllumination m_SSGI;

        protected override void Initialize(ref YPipelineData data)
        {
            var stack = VolumeManager.instance.stack;
            m_SSGI = stack.GetComponent<ScreenSpaceGlobalIllumination>();
        }

        protected override void OnDispose()
        {
            m_SSGI = null;
        }

        protected override void OnRecord(ref YPipelineData data)
        {
            data.isSSGIEnabled = m_SSGI.IsActive();
            CoreUtils.SetKeyword(data.cmd, YPipelineKeywords.k_ScreenSpaceIrradiance, data.isSSGIEnabled);
            if (!data.isSSGIEnabled || Time.frameCount == 0) return;

            using (var builder = data.renderGraph.AddUnsafePass<SSGIPassData>("Diffuse Global Illumination", out var passData))
            {
                YPipelineCamera yCamera = data.camera.GetYPipelineCamera();
                
                passData.hbilCS = data.runtimeResources.HBILCS;
                passData.denoiseCS = data.runtimeResources.GIDenoiseCS;
                passData.enableHalfResolution = m_SSGI.halfResolution.value;
                passData.enableTemporalDenoise = m_SSGI.enableTemporalDenoise.value;
                
                Vector2Int bufferSize = data.BufferSize;
                Vector2Int textureSize = bufferSize;
                passData.textureSize = new Vector4(1f / textureSize.x, 1f / textureSize.y, textureSize.x, textureSize.y);
                int threadGroupSizeX = Mathf.CeilToInt(textureSize.x / 8.0f);
                int threadGroupSizeY = Mathf.CeilToInt(textureSize.y / 8.0f);
                passData.threadGroupSizes = new Vector2Int(threadGroupSizeX, threadGroupSizeY);
                
                // Pass Data
                passData.ssgiParams = new Vector4(m_SSGI.hbilIntensity.value, m_SSGI.convergeDegree.value, m_SSGI.directionCount.value, m_SSGI.stepCount.value);
                passData.fallbackParams = new Vector4((int)m_SSGI.fallbackMode.value, m_SSGI.fallbackIntensity.value, m_SSGI.farFieldAO.value, 0);
                
                // Irradiance Texture
                TextureDesc irradianceTextureDesc = new TextureDesc(bufferSize.x, bufferSize.y)
                {
                    format = GraphicsFormat.R16G16B16A16_SFloat,
                    filterMode = FilterMode.Bilinear,
                    clearBuffer = true,
                    clearColor = Color.black,
                    enableRandomWrite = true,
                    name = "Irradiance Texture"
                };
                
                data.IrradianceTexture = data.renderGraph.CreateTexture(irradianceTextureDesc);
                passData.irradianceTexture = data.IrradianceTexture;
                builder.UseTexture(data.IrradianceTexture, AccessFlags.Write);
                builder.SetGlobalTextureAfterPass(data.IrradianceTexture, YPipelineShaderIDs.k_IrradianceTextureID);
                
                // Irradiance Transition Texture
                TextureDesc transitionDesc0 = new TextureDesc(textureSize.x, textureSize.y)
                {
                    format = GraphicsFormat.R16G16B16A16_SFloat,
                    filterMode = FilterMode.Bilinear,
                    clearBuffer = false,
                    enableRandomWrite = true,
                    name = "Irradiance Transition0"
                };
                
                if (passData.enableTemporalDenoise)
                {
                    passData.transition0 = builder.CreateTransientTexture(transitionDesc0);
                }
                
                // Irradiance History
                RenderTextureDescriptor irradianceHistoryDesc = new RenderTextureDescriptor(textureSize.x, textureSize.y)
                {
                    graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat,
                    msaaSamples = 1,
                    mipCount = 0,
                    autoGenerateMips = false,
                };

                if (passData.enableTemporalDenoise)
                {
                    RTHandle irradianceHistory = yCamera.perCameraData.GetIrradianceHistory(ref irradianceHistoryDesc);
                    yCamera.perCameraData.IsIrradianceHistoryReset = false;
                    passData.irradianceHistory = data.renderGraph.ImportTexture(irradianceHistory);
                    builder.UseTexture(passData.irradianceHistory, AccessFlags.ReadWrite);

                    builder.UseTexture(data.MotionVectorTexture, AccessFlags.Read);
                }
                else
                {
                    yCamera.perCameraData.ReleaseIrradianceHistory();
                }
                
                // Other Render Textures
                passData.sceneHistory = data.TAAHistory;
                builder.UseTexture(data.TAAHistory, AccessFlags.Read);
                
                if (data.IsDeferredRenderingEnabled) builder.UseTexture(data.GBuffer1, AccessFlags.Read);
                else builder.UseTexture(data.ThinGBuffer, AccessFlags.Read);
                builder.UseTexture(data.CameraDepthTexture, AccessFlags.Read);
                builder.UseTexture(data.MotionVectorTexture, AccessFlags.Read);
                
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((SSGIPassData data, UnsafeGraphContext context) =>
                {
                    // HBIL
                    context.cmd.SetComputeVectorParam(data.hbilCS, "_TextureSize", data.textureSize);
                    context.cmd.SetComputeVectorParam(data.hbilCS, YPipelineShaderIDs.k_SSGIParamsID, data.ssgiParams);
                    context.cmd.SetComputeVectorParam(data.hbilCS, YPipelineShaderIDs.k_SSGIFallbackParamsID, data.fallbackParams);
                    
                    // int hbgiKernel = data.cs.FindKernel("HBILAlternateKernel");
                    int hbgiKernel = data.hbilCS.FindKernel("HBILKernel");
                    TextureHandle occlusionOutput = data.enableTemporalDenoise ? data.transition0 : data.irradianceTexture;
                    context.cmd.SetComputeTextureParam(data.hbilCS, hbgiKernel, "_InputTexture", data.sceneHistory);
                    context.cmd.SetComputeTextureParam(data.hbilCS, hbgiKernel, "_OutputTexture", occlusionOutput);
                    context.cmd.DispatchCompute(data.hbilCS, hbgiKernel, data.threadGroupSizes.x, data.threadGroupSizes.y, 1);
                    
                    // Denoise
                    if (data.enableTemporalDenoise)
                    {
                        context.cmd.BeginSample("SSGI Temporal Denoise");
                        context.cmd.SetComputeVectorParam(data.denoiseCS, "_TextureSize", data.textureSize);
                    
                        int denoiseKernel = data.denoiseCS.FindKernel("TemporalDenoiseKernel");
                        context.cmd.SetComputeTextureParam(data.denoiseCS, hbgiKernel, YPipelineShaderIDs.k_IrradianceHistoryID, data.irradianceHistory);
                        context.cmd.SetComputeTextureParam(data.denoiseCS, hbgiKernel, "_InputTexture", data.transition0);
                        context.cmd.SetComputeTextureParam(data.denoiseCS, hbgiKernel, "_OutputTexture", data.irradianceTexture);
                        context.cmd.DispatchCompute(data.denoiseCS, hbgiKernel, data.threadGroupSizes.x, data.threadGroupSizes.y, 1);
                    
                        context.cmd.CopyTexture(data.irradianceTexture, data.irradianceHistory);
                        context.cmd.EndSample("SSGI Temporal Denoise");
                    }
                });
            }
            
        }
    }
}