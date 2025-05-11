using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

namespace YPipeline
{
    public class ForwardBuffersNode : PipelineNode
    {
        private class ForwardBuffersNodeData
        {
            public Vector2Int bufferSize;
        }
            
        protected override void Initialize() { }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<ForwardBuffersNodeData>("Forward Buffers Preparation", out var nodeData))
            {
                Vector2Int bufferSize = data.BufferSize;
                nodeData.bufferSize = bufferSize;
                
                TextureDesc colorAttachmentDesc = new TextureDesc(bufferSize.x,bufferSize.y)
                {
                    colorFormat = SystemInfo.GetGraphicsFormat(data.asset.enableHDRFrameBufferFormat ? DefaultFormat.HDR : DefaultFormat.LDR),
                    filterMode = FilterMode.Bilinear,
                    name = "Color Attachment"
                };
                
                TextureDesc colorTextureDesc = new TextureDesc(bufferSize.x,bufferSize.y)
                {
                    colorFormat = SystemInfo.GetGraphicsFormat(data.asset.enableHDRFrameBufferFormat ? DefaultFormat.HDR : DefaultFormat.LDR),
                    filterMode = FilterMode.Bilinear,
                    name = "Color Texture"
                };
                
                TextureDesc depthAttachmentDesc = new TextureDesc(bufferSize.x,bufferSize.y)
                {
                    depthBufferBits = DepthBits.Depth32,
                    filterMode = FilterMode.Point,
                    name = "Depth Attachment"
                };
                
                TextureDesc depthTextureDesc = new TextureDesc(bufferSize.x,bufferSize.y)
                {
                    depthBufferBits = DepthBits.Depth32,
                    filterMode = FilterMode.Point,
                    name = "Depth Texture"
                };
                
                data.CameraTarget = data.renderGraph.ImportBackbuffer(BuiltinRenderTextureType.CameraTarget);
                data.CameraColorAttachment = data.renderGraph.CreateTexture(colorAttachmentDesc);
                data.CameraDepthAttachment = data.renderGraph.CreateTexture(depthAttachmentDesc);
                data.CameraColorTexture = data.renderGraph.CreateTexture(colorTextureDesc);
                data.CameraDepthTexture = data.renderGraph.CreateTexture(depthTextureDesc);
                builder.AllowPassCulling(false);
                
                // // 待删除!!!!!!!!!!!!!!!!!!
                // data.cmd.GetTemporaryRT(YPipelineShaderIDs.k_ColorBufferID, bufferSize.x, bufferSize.y, 0, FilterMode.Bilinear, 
                //     data.asset.enableHDRFrameBufferFormat ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
                // data.cmd.GetTemporaryRT(YPipelineShaderIDs.k_DepthBufferID, bufferSize.x, bufferSize.y, 32, FilterMode.Point, 
                //     RenderTextureFormat.Depth);
                // data.cmd.GetTemporaryRT(YPipelineShaderIDs.k_ColorTextureID, bufferSize.x, bufferSize.y, 0, FilterMode.Bilinear, 
                //     data.asset.enableHDRFrameBufferFormat ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
                // data.cmd.GetTemporaryRT(YPipelineShaderIDs.k_DepthTextureID, bufferSize.x, bufferSize.y, 32, FilterMode.Point, 
                //     RenderTextureFormat.Depth);
                //
                //
                // data.cmd.GetTemporaryRT(YPipelineShaderIDs.k_FinalTextureID, bufferSize.x, bufferSize.y, 0, FilterMode.Bilinear,
                //     data.asset.enableHDRFrameBufferFormat ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
                
                    
                    
                builder.SetRenderFunc((ForwardBuffersNodeData data, RenderGraphContext context) =>
                {
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_BufferSizeID, new Vector4(1f / data.bufferSize.x, 1f / data.bufferSize.y, data.bufferSize.x, data.bufferSize.y));
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
                });
            }
        }
    }
}