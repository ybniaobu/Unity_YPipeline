using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

namespace YPipeline
{
    public class ForwardResourcesPass : PipelinePass
    {
        private class ForwardResourcesPassData
        {
            // Global Constant Buffer Variables
            public Vector2Int bufferSize;
            public Vector4 jitter;
            public Vector4 timeParams;
            public Vector4 cascadeSettings;
            public Vector4 shadowMapSizes;
        }
        
        private RTHandle m_CameraColorTarget;
        private RTHandle m_CameraDepthTarget;
        
        private RTHandle m_EnvBRDFLut;
        private RTHandle m_BlueNoise64;
            
        protected override void Initialize() { }

        protected override void OnDispose()
        {
            RTHandles.Release(m_CameraColorTarget);
            RTHandles.Release(m_CameraDepthTarget);
            m_CameraColorTarget = null;
            m_CameraDepthTarget = null;
            
            RTHandles.Release(m_EnvBRDFLut);
            RTHandles.Release(m_BlueNoise64);
            m_EnvBRDFLut = null;
            m_BlueNoise64 = null;
        }

        protected override void OnRecord(ref YPipelineData data)
        {
            // TODO：m_EnvBRDFLut、m_BlueNoise64 初始化一次，并使用 SetGlobalTextureAfterPass
            using (var builder = data.renderGraph.AddUnsafePass<ForwardResourcesPassData>("Set Global Resources", out var passData))
            {
                ImportBackBuffers(ref data);
                
                // ----------------------------------------------------------------------------------------------------
                // Imported texture resources
                // ----------------------------------------------------------------------------------------------------
                
                if (m_EnvBRDFLut == null || m_EnvBRDFLut.externalTexture != data.asset.pipelineResources.textures.environmentBRDFLut)
                {
                    m_EnvBRDFLut?.Release();
                    m_EnvBRDFLut = RTHandles.Alloc(data.asset.pipelineResources.textures.environmentBRDFLut);
                }
                TextureHandle envBRDFLut = data.renderGraph.ImportTexture(m_EnvBRDFLut);
                builder.UseTexture(envBRDFLut, AccessFlags.Read);
                builder.SetGlobalTextureAfterPass(envBRDFLut, YPipelineShaderIDs.k_EnvBRDFLutID);

                if (m_BlueNoise64 == null || m_BlueNoise64.externalTexture != data.asset.pipelineResources.textures.blueNoise64)
                {
                    m_BlueNoise64?.Release();
                    m_BlueNoise64 = RTHandles.Alloc(data.asset.pipelineResources.textures.blueNoise64);
                }
                TextureHandle blueNoise64 = data.renderGraph.ImportTexture(m_BlueNoise64);
                builder.UseTexture(blueNoise64, AccessFlags.Read);
                builder.SetGlobalTextureAfterPass(blueNoise64, YPipelineShaderIDs.k_BlueNoise64ID);
                
                // ----------------------------------------------------------------------------------------------------
                // Buffers
                // ----------------------------------------------------------------------------------------------------
                
                Vector2Int bufferSize = data.BufferSize;
                passData.bufferSize = bufferSize;
                
                TextureDesc colorAttachmentDesc = new TextureDesc(bufferSize.x,bufferSize.y)
                {
                    colorFormat = SystemInfo.GetGraphicsFormat(data.asset.enableHDRColorBuffer ? DefaultFormat.HDR : DefaultFormat.LDR),
                    filterMode = FilterMode.Bilinear,
                    clearBuffer = true,
                    clearColor = Color.clear,
                    name = "Color Attachment"
                };
                
                TextureDesc colorTextureDesc = new TextureDesc(bufferSize.x,bufferSize.y)
                {
                    colorFormat = SystemInfo.GetGraphicsFormat(data.asset.enableHDRColorBuffer ? DefaultFormat.HDR : DefaultFormat.LDR),
                    filterMode = FilterMode.Bilinear,
                    clearBuffer = true,
                    clearColor = Color.clear,
                    name = "Color Texture"
                };
                
                TextureDesc depthAttachmentDesc = new TextureDesc(bufferSize.x,bufferSize.y)
                {
                    depthBufferBits = DepthBits.Depth32,
                    filterMode = FilterMode.Point,
                    clearBuffer = true,
                    name = "Depth Attachment"
                };
                
                TextureDesc depthTextureDesc = new TextureDesc(bufferSize.x,bufferSize.y)
                {
                    depthBufferBits = DepthBits.Depth32,
                    filterMode = FilterMode.Point,
                    clearBuffer = true,
                    name = "Depth Texture"
                };
                
                TextureDesc thinGBufferDesc = new TextureDesc(bufferSize.x, bufferSize.y)
                {
                    format = GraphicsFormat.R8G8B8A8_UNorm,
                    filterMode = FilterMode.Point,
                    clearBuffer = true,
                    name = "Thin GBuffer"
                };
                
                data.CameraColorAttachment = data.renderGraph.CreateTexture(colorAttachmentDesc);
                data.CameraDepthAttachment = data.renderGraph.CreateTexture(depthAttachmentDesc);
                data.CameraColorTexture = data.renderGraph.CreateTexture(colorTextureDesc);
                data.CameraDepthTexture = data.renderGraph.CreateTexture(depthTextureDesc);
                data.ThinGBuffer = data.renderGraph.CreateTexture(thinGBufferDesc);
                
                // ----------------------------------------------------------------------------------------------------
                // Global Constant Buffer Variables
                // ----------------------------------------------------------------------------------------------------
                int frameCount = Time.frameCount;
                
                Vector2 jitter = RandomUtility.k_Halton[frameCount % 64 + 1] - new Vector2(0.5f, 0.5f);
                passData.jitter = new Vector4(1.0f / jitter.x, 1.0f / jitter.y, jitter.x, jitter.y);
                passData.timeParams = new Vector4(frameCount, 1.0f / frameCount);
                passData.cascadeSettings = new Vector4(data.asset.maxShadowDistance, data.asset.distanceFade, data.asset.cascadeCount, data.asset.cascadeEdgeFade);
                passData.shadowMapSizes = new Vector4((int) data.asset.sunLightShadowMapSize, (int) data.asset.spotLightShadowMapSize, (int) data.asset.pointLightShadowMapSize);
                
                builder.AllowPassCulling(false);
                
                builder.SetRenderFunc((ForwardResourcesPassData data, UnsafeGraphContext context) =>
                {
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_BufferSizeID, new Vector4(1f / data.bufferSize.x, 1f / data.bufferSize.y, data.bufferSize.x, data.bufferSize.y));
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_JitterID, data.jitter);
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_TimeParams,data.timeParams);
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_CascadeSettingsID, data.cascadeSettings);
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_ShadowMapSizesID, data.shadowMapSizes);
                });
            }
        }

        private void ImportBackBuffers(ref YPipelineData data)
        {
            RenderTargetIdentifier targetColorId = data.camera.targetTexture != null ? new RenderTargetIdentifier(data.camera.targetTexture) : BuiltinRenderTextureType.CameraTarget;
            RenderTargetIdentifier targetDepthId = data.camera.targetTexture != null ? new RenderTargetIdentifier(data.camera.targetTexture) : BuiltinRenderTextureType.Depth;
            
            if (m_CameraColorTarget == null || m_CameraColorTarget.nameID != targetColorId)
            {
                m_CameraColorTarget?.Release();
                m_CameraColorTarget = RTHandles.Alloc(targetColorId, "Backbuffer Color");
            }

            if (m_CameraDepthTarget == null || m_CameraDepthTarget.nameID != targetDepthId)
            {
                m_CameraDepthTarget?.Release();
                m_CameraDepthTarget = RTHandles.Alloc(targetDepthId, "Backbuffer Depth");
            }
            
            RenderTargetInfo importInfoColor = new RenderTargetInfo();
            RenderTargetInfo importInfoDepth = new RenderTargetInfo();
            
            if (data.camera.targetTexture == null)
            {
                importInfoColor.width = Screen.width;
                importInfoColor.height = Screen.height;
                importInfoColor.volumeDepth = 1;
                importInfoColor.msaaSamples = 1;

                importInfoColor.format = SystemInfo.GetGraphicsFormat(data.camera.allowHDR ? DefaultFormat.HDR : DefaultFormat.LDR);

                importInfoDepth = importInfoColor;
                importInfoDepth.format = SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil);
            }
            else
            {
                importInfoColor.width = data.camera.targetTexture.width;
                importInfoColor.height = data.camera.targetTexture.height;
                importInfoColor.volumeDepth = data.camera.targetTexture.volumeDepth;
                importInfoColor.msaaSamples = data.camera.targetTexture.antiAliasing;
                importInfoColor.format = data.camera.targetTexture.graphicsFormat;

                importInfoDepth = importInfoColor;
                importInfoDepth.format = data.camera.targetTexture.depthStencilFormat;
            }
            
            if (importInfoDepth.format == GraphicsFormat.None)
            {
                throw new System.Exception("In the render graph API, the output Render Texture must have a depth buffer.");
            }
            
            ImportResourceParams importBackbufferParams = new ImportResourceParams()
            {
                clearOnFirstUse = true,
                clearColor = Color.clear,
                discardOnLastUse = false
            };
            
            data.CameraColorTarget = data.renderGraph.ImportTexture(m_CameraColorTarget, importInfoColor, importBackbufferParams);
            data.CameraDepthTarget = data.renderGraph.ImportTexture(m_CameraDepthTarget, importInfoDepth, importBackbufferParams);
        }
    }
}