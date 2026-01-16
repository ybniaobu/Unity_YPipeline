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
            public bool enableHalfResolution;
            public bool enableTemporalDenoise;
            public bool enableBilateralDenoise;
            
            public ComputeShader hbilCS;
            public ComputeShader denoiseCS;
            
            public Vector2Int threadGroupSizesFull8;
            public Vector2Int threadGroupSizes1;
            public Vector2Int threadGroupSizes8;
            public Vector2Int threadGroupSizes64;
            
            public Vector4 textureSize;
            public Vector4 ssgiParams;
            public Vector4 fallbackParams;
            public Vector4 denoiseParams;
            
            public TextureHandle sceneHistory; // TAAHistory or SceneHistory
            public TextureHandle reprojectedSceneHistory;
            public TextureHandle irradianceTexture;
            public TextureHandle irradianceHistory;
            public TextureHandle transition;
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
                passData.denoiseCS = data.runtimeResources.SSGIDenoiseCS;
                passData.enableHalfResolution = m_SSGI.halfResolution.value;
                passData.enableTemporalDenoise = m_SSGI.enableTemporalDenoise.value;
                passData.enableBilateralDenoise = m_SSGI.enableBilateralDenoise.value;
                
                Vector2Int bufferSize = data.BufferSize;
                passData.threadGroupSizesFull8 = new Vector2Int(Mathf.CeilToInt(bufferSize.x / 8.0f),  Mathf.CeilToInt(bufferSize.y / 8.0f));
                Vector2Int textureSize = passData.enableHalfResolution ? bufferSize / 2 : bufferSize;
                passData.textureSize = new Vector4(1f / textureSize.x, 1f / textureSize.y, textureSize.x, textureSize.y);
                passData.threadGroupSizes1 = textureSize;
                int threadGroupSizeX = Mathf.CeilToInt(textureSize.x / 8.0f);
                int threadGroupSizeY = Mathf.CeilToInt(textureSize.y / 8.0f);
                passData.threadGroupSizes8 = new Vector2Int(threadGroupSizeX, threadGroupSizeY);
                threadGroupSizeX = Mathf.CeilToInt(textureSize.x / 64.0f);
                threadGroupSizeY = Mathf.CeilToInt(textureSize.y / 64.0f);
                passData.threadGroupSizes64 = new Vector2Int(threadGroupSizeX, threadGroupSizeY);
                
                // Pass Data
                passData.ssgiParams = new Vector4(m_SSGI.hbilIntensity.value, m_SSGI.convergeDegree.value, m_SSGI.directionCount.value, m_SSGI.stepCount.value);
                passData.fallbackParams = new Vector4((int)m_SSGI.fallbackMode.value, m_SSGI.fallbackIntensity.value, m_SSGI.farFieldAO.value, m_SSGI.enableTemporalDenoise.value ? 1 : 0);
                passData.denoiseParams = new Vector4(m_SSGI.kernelRadius.value, m_SSGI.sigma.value, m_SSGI.depthThreshold.value, m_SSGI.criticalValue.value);
                
                // Irradiance Texture
                TextureDesc irradianceTextureDesc = new TextureDesc(bufferSize.x, bufferSize.y)
                {
                    format = GraphicsFormat.R16G16B16A16_SFloat,
                    filterMode = FilterMode.Bilinear,
                    clearBuffer = false,
                    enableRandomWrite = true,
                    name = "Irradiance Texture"
                };
                
                data.IrradianceTexture = data.renderGraph.CreateTexture(irradianceTextureDesc);
                passData.irradianceTexture = data.IrradianceTexture;
                builder.UseTexture(data.IrradianceTexture, AccessFlags.Write);
                builder.SetGlobalTextureAfterPass(data.IrradianceTexture, YPipelineShaderIDs.k_IrradianceTextureID);
                
                // Irradiance Transition Texture
                TextureDesc transitionDesc = new TextureDesc(textureSize.x, textureSize.y)
                {
                    format = GraphicsFormat.R16G16B16A16_SFloat,
                    filterMode = FilterMode.Bilinear,
                    clearBuffer = false,
                    enableRandomWrite = true,
                    name = "Irradiance Transition"
                };
                
                passData.transition = builder.CreateTransientTexture(transitionDesc);
                
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
                
                // Scene History
                passData.sceneHistory = data.TAAHistory;
                builder.UseTexture(data.TAAHistory, AccessFlags.Read);
                
                if (!passData.enableHalfResolution)
                {
                    TextureDesc reprojectedSceneHistoryDesc = new TextureDesc(bufferSize.x, bufferSize.y)
                    {
                        format = GraphicsFormat.R16G16B16A16_SFloat,
                        filterMode = FilterMode.Bilinear,
                        clearBuffer = true,
                        enableRandomWrite = true,
                        name = "Reprojected Scene History"
                    };
                    
                    passData.reprojectedSceneHistory = builder.CreateTransientTexture(reprojectedSceneHistoryDesc);
                }
                
                // Other Render Textures
                if (data.IsDeferredRenderingEnabled) builder.UseTexture(data.GBuffer1, AccessFlags.Read);
                else builder.UseTexture(data.ThinGBuffer, AccessFlags.Read);
                builder.UseTexture(data.CameraDepthTexture, AccessFlags.Read);
                builder.UseTexture(data.MotionVectorTexture, AccessFlags.Read);
                
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((SSGIPassData data, UnsafeGraphContext context) =>
                {
                    bool enableBlur = data.enableTemporalDenoise || data.enableBilateralDenoise;
                    LocalKeyword halfResKeyword = new LocalKeyword(data.hbilCS, "_HALF_RESOLUTION");
                    context.cmd.SetKeyword(data.hbilCS, halfResKeyword, data.enableHalfResolution);
                    context.cmd.SetComputeVectorParam(data.hbilCS, "_TextureSize", data.textureSize);
                    context.cmd.SetComputeVectorParam(data.hbilCS, YPipelineShaderIDs.k_SSGIParamsID, data.ssgiParams);
                    context.cmd.SetComputeVectorParam(data.hbilCS, YPipelineShaderIDs.k_SSGIFallbackParamsID, data.fallbackParams);
                    
                    // Reprojection
                    // 若开启了 Half Resolution, 在 Downsample Pass 中会将 Scene History Reprojection
                    if (!data.enableHalfResolution)
                    {
                        context.cmd.BeginSample("Scene History Reprojection");
                        int reprojectionKernel = data.hbilCS.FindKernel("HBILReprojectionKernel");
                        context.cmd.SetComputeTextureParam(data.hbilCS, reprojectionKernel, "_InputTexture", data.sceneHistory);
                        context.cmd.SetComputeTextureParam(data.hbilCS, reprojectionKernel, "_OutputTexture", data.reprojectedSceneHistory);
                        context.cmd.DispatchCompute(data.hbilCS, reprojectionKernel, data.threadGroupSizesFull8.x, data.threadGroupSizesFull8.y, 1);
                        context.cmd.EndSample("Scene History Reprojection");
                    }
                    
                    // HBIL
                    context.cmd.BeginSample("HBIL");
                    // int hbgiKernel = data.cs.FindKernel("HBILAlternateKernel");
                    int hbgiKernel = data.hbilCS.FindKernel("HBILKernel");
                    TextureHandle occlusionOutput = data.enableTemporalDenoise ? data.transition : data.irradianceTexture;
                    context.cmd.SetComputeTextureParam(data.hbilCS, hbgiKernel, "_InputTexture", data.reprojectedSceneHistory);
                    context.cmd.SetComputeTextureParam(data.hbilCS, hbgiKernel, "_OutputTexture", occlusionOutput);
                    context.cmd.DispatchCompute(data.hbilCS, hbgiKernel, data.threadGroupSizes8.x, data.threadGroupSizes8.y, 1);
                    context.cmd.EndSample("HBIL");
                    
                    // Denoise
                    if (enableBlur)
                    {
                        context.cmd.SetComputeVectorParam(data.denoiseCS, "_TextureSize", data.textureSize);
                        context.cmd.SetComputeVectorParam(data.denoiseCS, YPipelineShaderIDs.k_SSGIDenoiseParamsID, data.denoiseParams);
                    }
                    
                    // Temporal Denoise
                    if (data.enableTemporalDenoise)
                    {
                        context.cmd.BeginSample("SSGI Temporal Denoise");
                        int temporalKernel = data.denoiseCS.FindKernel("TemporalDenoiseKernel");
                        context.cmd.SetComputeTextureParam(data.denoiseCS, temporalKernel, YPipelineShaderIDs.k_IrradianceHistoryID, data.irradianceHistory);
                        context.cmd.SetComputeTextureParam(data.denoiseCS, temporalKernel, "_InputTexture", data.transition);
                        context.cmd.SetComputeTextureParam(data.denoiseCS, temporalKernel, "_OutputTexture", data.irradianceTexture);
                        context.cmd.DispatchCompute(data.denoiseCS, temporalKernel, data.threadGroupSizes8.x, data.threadGroupSizes8.y, 1);
                        
                        // TODO: 是否改为使用 CS 复制
                        context.cmd.CopyTexture(data.irradianceTexture, data.irradianceHistory);
                        context.cmd.EndSample("SSGI Temporal Denoise");
                    }
                    
                    // Bilateral Denoise
                    if (data.enableBilateralDenoise)
                    {
                        context.cmd.BeginSample("SSGI Bilateral Denoise");
                        int horizontalKernel = data.denoiseCS.FindKernel("BilateralDenoiseHorizontalKernel");
                        context.cmd.SetComputeTextureParam(data.denoiseCS, horizontalKernel, "_InputTexture", data.irradianceTexture);
                        context.cmd.SetComputeTextureParam(data.denoiseCS, horizontalKernel, "_OutputTexture", data.transition);
                        context.cmd.DispatchCompute(data.denoiseCS, horizontalKernel, data.threadGroupSizes64.x, data.threadGroupSizes1.y, 1);

                        int verticalKernel = data.denoiseCS.FindKernel("BilateralDenoiseVerticalKernel");
                        context.cmd.SetComputeTextureParam(data.denoiseCS, verticalKernel, "_InputTexture", data.transition);
                        context.cmd.SetComputeTextureParam(data.denoiseCS, verticalKernel, "_OutputTexture", data.irradianceTexture);
                        context.cmd.DispatchCompute(data.denoiseCS, verticalKernel, data.threadGroupSizes1.x, data.threadGroupSizes64.y, 1);

                        context.cmd.EndSample("SSGI Bilateral Denoise");
                    }
                    
                    // 考虑在 Bilateral Denoise 后 Copy
                    // if (data.enableTemporalDenoise)
                    // {
                    //     context.cmd.CopyTexture(data.irradianceTexture, data.irradianceHistory);
                    // }
                });
            }
        }
    }
}