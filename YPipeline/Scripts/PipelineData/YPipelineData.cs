using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public struct YPipelineData
    {
        // ----------------------------------------------------------------------------------------------------
        // References
        // ----------------------------------------------------------------------------------------------------
        
        public YRenderPipelineAsset asset;
        public RenderGraph renderGraph;
        public ScriptableRenderContext context;
        public Camera camera;
        public CommandBuffer cmd;
        public CullingResults cullingResults;
        public YPipelineLightsData lightsData;
        
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public DebugSettings debugSettings;
#endif
        
        // ----------------------------------------------------------------------------------------------------
        // Properties
        // ----------------------------------------------------------------------------------------------------
        
        public Vector2Int BufferSize => new Vector2Int((int) (camera.pixelWidth * asset.renderScale), (int) (camera.pixelHeight * asset.renderScale));
        
        // ----------------------------------------------------------------------------------------------------
        // Buffer and Texture Handles
        // ----------------------------------------------------------------------------------------------------
        
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
        
        public TextureHandle MotionVectorTexture { set; get; }
        
        public TextureHandle TAATarget { set; get; }
        
        public TextureHandle BloomTexture { set; get; }
        public TextureHandle ColorGradingLutTexture { set; get; }
        public TextureHandle CameraFinalTexture { set; get; }
        
        // Imported texture resources
        public TextureHandle TAAHistory { set; get; }
        
        public TextureHandle EnvBRDFLut { set; get; }
        public TextureHandle BlueNoise256 { set; get; }
        
        // ----------------------------------------------------------------------------------------------------
        // Structured Buffers
        // ----------------------------------------------------------------------------------------------------
        public BufferHandle PunctualLightBufferHandle { set; get; }
        public BufferHandle PointLightShadowBufferHandle { set; get; }
        public BufferHandle PointLightShadowMatricesBufferHandle { set; get; }
        public BufferHandle SpotLightShadowBufferHandle { set; get; }
        public BufferHandle SpotLightShadowMatricesBufferHandle { set; get; }
        
        public BufferHandle TilesLightIndicesBufferHandle { set; get; }
    }
}