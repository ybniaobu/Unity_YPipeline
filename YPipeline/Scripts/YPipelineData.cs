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
    }
}