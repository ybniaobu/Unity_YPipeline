using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public struct YPipelineData
    {
        public YRenderPipelineAsset asset;
        public RenderGraph renderGraph;
        public ScriptableRenderContext context;
        public Camera camera;
        public CommandBuffer cmd;
        public CullingResults cullingResults;
        
        public Vector2Int BufferSize => new Vector2Int((int) (camera.pixelWidth * asset.renderScale), (int) (camera.pixelHeight * asset.renderScale));
        
        public TextureHandle CameraColorTarget { set; get; }
        public TextureHandle CameraDepthTarget { set; get; }
        public TextureHandle CameraColorAttachment { set; get; }
        public TextureHandle CameraDepthAttachment { set; get; }
        public TextureHandle CameraColorTexture { set; get; }
        public TextureHandle CameraDepthTexture { set; get; }
        
        public TextureHandle BloomTexture { set; get; }
        public TextureHandle ColorGradingLutTexture { set; get; }
        public TextureHandle CameraFinalTexture { set; get; }
    }
}