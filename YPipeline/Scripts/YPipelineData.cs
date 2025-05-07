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
        
        public Vector2Int bufferSize => new Vector2Int((int) (camera.pixelWidth * asset.renderScale), (int) (camera.pixelHeight * asset.renderScale));
        
        public static TextureHandle CameraColorBuffer { set; get; }
        public static TextureHandle CameraDepthBuffer { set; get; }
        public static TextureHandle CameraColorTexture { set; get; }
        public static TextureHandle CameraDepthTexture { set; get; }
        public static TextureHandle CameraFinalTexture { set; get; }
        public static TextureHandle BloomTexture { set; get; }
    }
}