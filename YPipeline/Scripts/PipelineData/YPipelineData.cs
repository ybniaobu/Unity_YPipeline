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
        public YPipelineLightsData lightsData;
        
        public Vector2Int BufferSize => new Vector2Int((int) (camera.pixelWidth * asset.renderScale), (int) (camera.pixelHeight * asset.renderScale));
        
        // Buffers and Textures
        public TextureHandle CameraColorTarget { set; get; }
        public TextureHandle CameraDepthTarget { set; get; }
        public TextureHandle CameraColorAttachment { set; get; }
        public TextureHandle CameraDepthAttachment { set; get; }
        public TextureHandle CameraColorTexture { set; get; }
        public TextureHandle CameraDepthTexture { set; get; }
        
        public TextureHandle SunLightShadowMap { set; get; }
        public bool isSunLightShadowMapCreated;
        
        public TextureHandle SpotLightShadowMap { set; get; }
        public bool isSpotLightShadowMapCreated;
        
        public TextureHandle PointLightShadowMap { set; get; }
        public bool isPointLightShadowMapCreated;
        
        public TextureHandle BloomTexture { set; get; }
        public TextureHandle ColorGradingLutTexture { set; get; }
        public TextureHandle CameraFinalTexture { set; get; }
        
        // Imported texture resources
        public TextureHandle EnvBRDFLut { set; get; }
        public TextureHandle BlueNoise256 { set; get; }
        
        // Structured Buffers
        //public BufferHandle SunLightConstantBufferHandle { set; get; }
        
        public BufferHandle TilesBuffer { set; get; }
    }
}