using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public struct YPipelineData
    {
        public YRenderPipelineAsset asset;
        public ScriptableRenderContext context;
        public Camera camera;
        public CommandBuffer cmd;
        public CullingResults cullingResults;
        public RenderGraph renderGraph;
        
        public Vector2Int bufferSize => new Vector2Int((int) (camera.pixelWidth * asset.renderScale), (int) (camera.pixelHeight * asset.renderScale));
    }
}