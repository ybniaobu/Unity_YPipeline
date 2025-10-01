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
            public AmbientOcclusionMode aoMode;
            
            public Vector2Int threadGroupSizes8;
            public Vector2Int threadGroupSizes16;
            public Vector2Int textureSize;
            public bool enableHalfResolution;
            public bool enableSpatialBlur;
            public bool enableTemporalBlur;

            public TextureHandle halfDepth;
            public TextureHandle aoTexture;
            public TextureHandle transition0;
            public TextureHandle transition1;
            public TextureHandle aoHistory;
            
            public Vector4 ambientOcclusionParams;
            public Vector4 aoSpatialBlurParams;
            public Vector4 aoTemporalBlurParams;
        }

        private AmbientOcclusion m_AO;
        
        protected override void Initialize() { }

        public override void OnRecord(ref YPipelineData data)
        {
            var stack = VolumeManager.instance.stack;
            m_AO = stack.GetComponent<AmbientOcclusion>();
            YPipelineCamera yCamera = data.camera.GetYPipelineCamera();
            
            data.isAmbientOcclusionTextureCreated = m_AO.IsActive();
            CoreUtils.SetKeyword(data.cmd, YPipelineKeywords.k_ScreenSpaceAmbientOcclusion, data.isAmbientOcclusionTextureCreated);
            
            if (data.isAmbientOcclusionTextureCreated)
            {
                using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<AmbientOcclusionPassData>("Ambient Occlusion", out var passData))
                {
                    int halfResolution = m_AO.halfResolution.value ? 2 : 1;
                    Vector2Int bufferSize = data.BufferSize;
                    Vector2Int textureSize = bufferSize / halfResolution;
                    passData.textureSize = textureSize;
                    int threadGroupSizeX = Mathf.CeilToInt(textureSize.x / 8.0f);
                    int threadGroupSizeY = Mathf.CeilToInt(textureSize.y / 8.0f);
                    passData.threadGroupSizes8 = new Vector2Int(threadGroupSizeX, threadGroupSizeY);
                    threadGroupSizeX = Mathf.CeilToInt(textureSize.x / 16.0f);
                    threadGroupSizeY = Mathf.CeilToInt(textureSize.y / 16.0f);
                    passData.threadGroupSizes16 = new Vector2Int(threadGroupSizeX, threadGroupSizeY);

                    passData.cs = data.asset.pipelineResources.computeShaders.ambientOcclusionCs;
                    passData.aoMode = m_AO.ambientOcclusionMode.value;
                    builder.ReadTexture(data.ThinGBuffer);
                    builder.ReadTexture(data.CameraDepthTexture);

                    passData.enableHalfResolution = m_AO.halfResolution.value;
                    passData.enableSpatialBlur = m_AO.enableSpatialFilter.value;
                    passData.enableTemporalBlur = m_AO.enableTemporalFilter.value;

                    switch (passData.aoMode)
                    {
                        case AmbientOcclusionMode.SSAO:
                            passData.ambientOcclusionParams = new Vector4(m_AO.ssaoIntensity.value, m_AO.sampleCount.value, m_AO.ssaoRadius.value);
                            break;
                        case AmbientOcclusionMode.HBAO:
                            passData.ambientOcclusionParams = new Vector4(m_AO.hbaoIntensity.value, m_AO.hbaoRadius.value, m_AO.hbaoDirectionCount.value, m_AO.hbaoStepCount.value);
                            break;
                        case AmbientOcclusionMode.GTAO:
                            passData.ambientOcclusionParams = new Vector4(m_AO.gtaoIntensity.value, m_AO.gtaoRadius.value, m_AO.gtaoDirectionCount.value, m_AO.gtaoStepCount.value);
                            break;
                        default:
                            passData.ambientOcclusionParams = new Vector4(m_AO.gtaoIntensity.value, m_AO.gtaoRadius.value, m_AO.gtaoDirectionCount.value, m_AO.gtaoStepCount.value);
                            break;
                    }
                    passData.aoSpatialBlurParams = new Vector4(m_AO.kernelRadius.value, m_AO.spatialSigma.value, m_AO.depthSigma.value);
                    passData.aoTemporalBlurParams = new Vector4(passData.enableTemporalBlur ? 1 : 0, m_AO.criticalValue.value);
                    
                    // Half Depth Texture
                    if (passData.enableHalfResolution)
                    {
                        TextureDesc halfDepthDesc = new TextureDesc(textureSize.x, textureSize.y)
                        {
                            format = GraphicsFormat.R32_SFloat,
                            filterMode = FilterMode.Point,
                            clearBuffer = false,
                            enableRandomWrite = true,
                            name = "Half Depth Texture"
                        };
                        
                        passData.halfDepth = builder.CreateTransientTexture(halfDepthDesc);
                    }

                    // Create Ambient Occlusion Texture
                    TextureDesc aoTextureDesc = new TextureDesc(bufferSize.x, bufferSize.y)
                    {
                        format = GraphicsFormat.R16G16_SFloat,
                        filterMode = FilterMode.Bilinear,
                        clearBuffer = true,
                        clearColor = Color.white,
                        enableRandomWrite = true,
                        name = "Ambient Occlusion Texture"
                    };

                    data.AmbientOcclusionTexture = data.renderGraph.CreateTexture(aoTextureDesc);
                    passData.aoTexture = builder.WriteTexture(data.AmbientOcclusionTexture);
                    
                    // Ambient Occlusion Transition Texture
                    TextureDesc transitionDesc0 = new TextureDesc(textureSize.x, textureSize.y)
                    {
                        format = GraphicsFormat.R16G16_SFloat,
                        filterMode = FilterMode.Bilinear,
                        clearBuffer = false,
                        enableRandomWrite = true,
                        name = "Ambient Occlusion transition0"
                    };
                    
                    TextureDesc transitionDesc1 = new TextureDesc(textureSize.x, textureSize.y)
                    {
                        format = GraphicsFormat.R16G16_SFloat,
                        filterMode = FilterMode.Bilinear,
                        clearBuffer = false,
                        enableRandomWrite = true,
                        name = "Ambient Occlusion transition1"
                    };

                    if (passData.enableSpatialBlur || passData.enableTemporalBlur || passData.enableHalfResolution)
                    {
                        passData.transition0 = builder.CreateTransientTexture(transitionDesc0);
                    }

                    if (passData.enableSpatialBlur || passData.enableTemporalBlur)
                    {
                        passData.transition1 = builder.CreateTransientTexture(transitionDesc1);
                    }
                    
                    // Ambient Occlusion History
                    RenderTextureDescriptor aoHistoryDesc = new RenderTextureDescriptor(textureSize.x, textureSize.y)
                    {
                        graphicsFormat = GraphicsFormat.R16G16_SFloat,
                        msaaSamples = 1,
                        mipCount = 0,
                        autoGenerateMips = false,
                    };

                    if (passData.enableTemporalBlur)
                    {
                        RTHandle aoHistory = yCamera.perCameraData.GetAOHistory(ref aoHistoryDesc);
                        yCamera.perCameraData.IsAOHistoryReset = false;
                        passData.aoHistory = data.renderGraph.ImportTexture(aoHistory);
                        builder.ReadWriteTexture(passData.aoHistory);

                        builder.ReadTexture(data.MotionVectorTexture);
                    }
                    else
                    {
                        yCamera.perCameraData.ReleaseAOHistory();
                    }
                    
                    builder.AllowPassCulling(false);
                    builder.AllowRendererListCulling(false);

                    builder.SetRenderFunc((AmbientOcclusionPassData data, RenderGraphContext context) =>
                    {
                        bool enableBlur = data.enableSpatialBlur || data.enableTemporalBlur;
                        CoreUtils.SetKeyword(context.cmd, data.cs, "_HALF_RESOLUTION", data.enableHalfResolution);
                        context.cmd.SetComputeVectorParam(data.cs, "_TextureSize", new Vector4(1f / data.textureSize.x, 1f / data.textureSize.y, data.textureSize.x, data.textureSize.y));
                        context.cmd.SetComputeVectorParam(data.cs, YPipelineShaderIDs.k_AmbientOcclusionParamsID, data.ambientOcclusionParams);
                        context.cmd.SetComputeVectorParam(data.cs, YPipelineShaderIDs.k_AOSpatialBlurParamsID, data.aoSpatialBlurParams);
                        context.cmd.SetComputeVectorParam(data.cs, YPipelineShaderIDs.k_AOTemporalBlurParamsID, data.aoTemporalBlurParams);

                        if (data.enableHalfResolution)
                        {
                            context.cmd.BeginSample("SSAO Depth Downsample");
                            int depthDownsampleKernel = data.cs.FindKernel("DepthDownsampleKernel");
                            context.cmd.SetComputeTextureParam(data.cs, depthDownsampleKernel, "_OutputTexture", data.halfDepth);
                            context.cmd.DispatchCompute(data.cs, depthDownsampleKernel, data.threadGroupSizes8.x, data.threadGroupSizes8.y, 1);
                            context.cmd.EndSample("SSAO Depth Downsample");
                        }
                        
                        context.cmd.BeginSample("SSAO Compute Occlusion");

                        int ssaoKernel;
                        switch (data.aoMode)
                        {
                            case AmbientOcclusionMode.SSAO:
                                ssaoKernel = data.cs.FindKernel("SSAOKernel");
                                break;
                            case AmbientOcclusionMode.HBAO:
                                ssaoKernel = data.cs.FindKernel("HBAOKernel");
                                break;
                            case AmbientOcclusionMode.GTAO:
                                ssaoKernel = data.cs.FindKernel("GTAOKernel");
                                break;
                            default:
                                ssaoKernel = data.cs.FindKernel("GTAOKernel");
                                break;
                        }
                        
                        TextureHandle occlusionOutput = enableBlur ? data.transition0 : data.aoTexture;
                        occlusionOutput = data.enableHalfResolution ? data.transition0 : occlusionOutput;
                        context.cmd.SetComputeTextureParam(data.cs, ssaoKernel, "_OutputTexture", occlusionOutput);
                        if (data.enableHalfResolution) context.cmd.SetComputeTextureParam(data.cs, ssaoKernel,"_HalfDepthTexture", data.halfDepth);
                        context.cmd.DispatchCompute(data.cs, ssaoKernel, data.threadGroupSizes8.x, data.threadGroupSizes8.y, 1);
                        context.cmd.EndSample("SSAO Compute Occlusion");
                        
                        if (data.enableSpatialBlur)
                        {
                            context.cmd.BeginSample("SSAO Spatial Blur Horizontal");
                            int blurHorizontalKernel = data.cs.FindKernel("SpatialBlurHorizontalKernel");
                            context.cmd.SetComputeTextureParam(data.cs, blurHorizontalKernel, "_InputTexture", data.transition0);
                            context.cmd.SetComputeTextureParam(data.cs, blurHorizontalKernel, "_OutputTexture", data.transition1);
                            context.cmd.DispatchCompute(data.cs, blurHorizontalKernel, data.threadGroupSizes16.x, data.textureSize.y, 1);
                            context.cmd.EndSample("SSAO Spatial Blur Horizontal");

                            context.cmd.BeginSample("SSAO Spatial Blur Vertical");
                            int blurVerticalKernel = data.cs.FindKernel("SpatialBlurVerticalKernel");
                            context.cmd.SetComputeTextureParam(data.cs, blurVerticalKernel, "_InputTexture", data.transition1);
                            TextureHandle spatialBlurOutput = data.enableTemporalBlur ? data.transition0 : data.aoTexture;
                            spatialBlurOutput = data.enableHalfResolution ? data.transition0 : spatialBlurOutput;
                            context.cmd.SetComputeTextureParam(data.cs, blurVerticalKernel, "_OutputTexture", spatialBlurOutput);
                            context.cmd.DispatchCompute(data.cs, blurVerticalKernel, data.textureSize.x, data.threadGroupSizes16.y, 1);
                            context.cmd.EndSample("SSAO Spatial Blur Vertical");
                        }

                        if (data.enableTemporalBlur)
                        {
                            context.cmd.BeginSample("SSAO Temporal Blur");
                            int temporalBlurKernel = data.cs.FindKernel("TemporalBlurKernel");
                            context.cmd.SetComputeTextureParam(data.cs, temporalBlurKernel, "_InputTexture", data.transition0);
                            context.cmd.SetComputeTextureParam(data.cs, temporalBlurKernel, YPipelineShaderIDs.k_AmbientOcclusionHistoryID, data.aoHistory);
                            TextureHandle temporalBlurOutput = data.enableHalfResolution ? data.transition1 : data.aoTexture;
                            context.cmd.SetComputeTextureParam(data.cs, temporalBlurKernel, "_OutputTexture", temporalBlurOutput);
                            context.cmd.DispatchCompute(data.cs, temporalBlurKernel, data.threadGroupSizes8.x, data.threadGroupSizes8.y, 1);
                            
                            bool copyTextureSupported = SystemInfo.copyTextureSupport > CopyTextureSupport.None;
                            if (copyTextureSupported) context.cmd.CopyTexture(temporalBlurOutput, data.aoHistory);
                            else BlitUtility.BlitTexture(context.cmd, temporalBlurOutput, data.aoHistory);
                            
                            context.cmd.EndSample("SSAO Temporal Blur");
                        }

                        if (data.enableHalfResolution)
                        {
                            context.cmd.BeginSample("SSAO Upsample");
                            int upsampleKernel = data.cs.FindKernel("UpsampleKernel");
                            TextureHandle upsampleInput = data.enableTemporalBlur ? data.transition1 : data.transition0;
                            context.cmd.SetComputeTextureParam(data.cs, upsampleKernel, "_InputTexture", upsampleInput);
                            context.cmd.SetComputeTextureParam(data.cs, upsampleKernel, "_OutputTexture", data.aoTexture);
                            // context.cmd.SetComputeTextureParam(data.cs, upsampleKernel,"_HalfDepthTexture", data.halfDepth);
                            int threadGroupSize = Mathf.CeilToInt(data.textureSize.x * 2.0f / 8.0f);
                            context.cmd.DispatchCompute(data.cs, upsampleKernel, threadGroupSize, threadGroupSize, 1);
                            context.cmd.EndSample("SSAO Upsample");
                        }
                        
                        context.cmd.SetGlobalTexture(YPipelineShaderIDs.k_AmbientOcclusionTextureID, data.aoTexture);
                        context.renderContext.ExecuteCommandBuffer(context.cmd);
                        context.cmd.Clear();
                    });
                }
            }
        }
    }
}