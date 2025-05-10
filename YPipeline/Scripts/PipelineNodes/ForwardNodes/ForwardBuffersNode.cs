using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

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
                
                data.cmd.SetGlobalVector(YPipelineShaderIDs.k_BufferSizeID, new Vector4(1f / bufferSize.x, 1f / bufferSize.y, bufferSize.x, bufferSize.y));
                data.cmd.GetTemporaryRT(YPipelineShaderIDs.k_ColorBufferID, bufferSize.x, bufferSize.y, 0, FilterMode.Bilinear, 
                    data.asset.enableHDRFrameBufferFormat ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
                data.cmd.GetTemporaryRT(YPipelineShaderIDs.k_DepthBufferID, bufferSize.x, bufferSize.y, 32, FilterMode.Point, 
                    RenderTextureFormat.Depth);
                data.cmd.GetTemporaryRT(YPipelineShaderIDs.k_ColorTextureID, bufferSize.x, bufferSize.y, 0, FilterMode.Bilinear, 
                    data.asset.enableHDRFrameBufferFormat ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
                data.cmd.GetTemporaryRT(YPipelineShaderIDs.k_DepthTextureID, bufferSize.x, bufferSize.y, 32, FilterMode.Point, 
                    RenderTextureFormat.Depth);
                data.cmd.GetTemporaryRT(YPipelineShaderIDs.k_FinalTextureID, bufferSize.x, bufferSize.y, 0, FilterMode.Bilinear,
                    data.asset.enableHDRFrameBufferFormat ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
                    
                    
                builder.SetRenderFunc((ForwardBuffersNodeData data, RenderGraphContext context) =>
                {
                    
                });
            }
        }
    }
}