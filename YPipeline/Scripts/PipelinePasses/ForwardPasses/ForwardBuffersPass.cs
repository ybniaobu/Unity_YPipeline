using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

namespace YPipeline
{
    public class ForwardBuffersPass : PipelinePass
    {
        private class ForwardBuffersPassData
        {
            public TextureHandle envBRDFLut;
            public TextureHandle blueNoise64;
            
            public Vector2Int bufferSize;
        }
        
        private RTHandle m_CameraColorTarget;
        private RTHandle m_CameraDepthTarget;
        
        private RTHandle m_EnvBRDFLut;
        private RTHandle m_BlueNoise64;
            
        protected override void Initialize() { }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<ForwardBuffersPassData>("Forward Buffers Preparation", out var passData))
            {
                ImportBackBuffers(ref data);
                
                // Imported texture resources
                if (m_EnvBRDFLut == null || m_EnvBRDFLut.externalTexture != data.asset.pipelineResources.textures.environmentBRDFLut)
                {
                    m_EnvBRDFLut = RTHandles.Alloc(data.asset.pipelineResources.textures.environmentBRDFLut);
                }
                passData.envBRDFLut = data.renderGraph.ImportTexture(m_EnvBRDFLut);
                builder.ReadTexture(passData.envBRDFLut);

                if (m_BlueNoise64 == null || m_BlueNoise64.externalTexture != data.asset.pipelineResources.textures.blueNoise64)
                {
                    m_BlueNoise64 = RTHandles.Alloc(data.asset.pipelineResources.textures.blueNoise64);
                }
                passData.blueNoise64 = data.renderGraph.ImportTexture(m_BlueNoise64);
                builder.ReadTexture(passData.blueNoise64);
                
                // Buffers
                Vector2Int bufferSize = data.BufferSize;
                passData.bufferSize = bufferSize;
                
                TextureDesc colorAttachmentDesc = new TextureDesc(bufferSize.x,bufferSize.y)
                {
                    colorFormat = SystemInfo.GetGraphicsFormat(data.asset.enableHDRFrameBufferFormat ? DefaultFormat.HDR : DefaultFormat.LDR),
                    filterMode = FilterMode.Bilinear,
                    clearBuffer = true,
                    clearColor = Color.clear,
                    name = "Color Attachment"
                };
                
                TextureDesc colorTextureDesc = new TextureDesc(bufferSize.x,bufferSize.y)
                {
                    colorFormat = SystemInfo.GetGraphicsFormat(data.asset.enableHDRFrameBufferFormat ? DefaultFormat.HDR : DefaultFormat.LDR),
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
                
                data.CameraColorAttachment = data.renderGraph.CreateTexture(colorAttachmentDesc);
                data.CameraDepthAttachment = data.renderGraph.CreateTexture(depthAttachmentDesc);
                data.CameraColorTexture = data.renderGraph.CreateTexture(colorTextureDesc);
                data.CameraDepthTexture = data.renderGraph.CreateTexture(depthTextureDesc);
                
                builder.AllowPassCulling(false);
                
                builder.SetRenderFunc((ForwardBuffersPassData data, RenderGraphContext context) =>
                {
                    context.cmd.SetGlobalTexture(YPipelineShaderIDs.k_EnvBRDFLutID, data.envBRDFLut);
                    context.cmd.SetGlobalTexture(YPipelineShaderIDs.k_BlueNoise64ID, data.blueNoise64);
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_BufferSizeID, new Vector4(1f / data.bufferSize.x, 1f / data.bufferSize.y, data.bufferSize.x, data.bufferSize.y));
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
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