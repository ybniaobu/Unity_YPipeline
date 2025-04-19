using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public struct YPipelineData
    {
        public YRenderPipelineAsset asset;
        public ScriptableRenderContext context;
        public Camera camera;
        public CommandBuffer buffer;
        public CullingResults cullingResults;
        
        public Vector2Int bufferSize => new Vector2Int((int) (camera.pixelWidth * asset.renderScale), (int) (camera.pixelHeight * asset.renderScale));
    }
}