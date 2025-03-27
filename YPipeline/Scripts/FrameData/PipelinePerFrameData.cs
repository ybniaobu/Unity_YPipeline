using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    /// <summary>
    /// 记录每帧都可能变化的数据的结构体。渲染所需数据大致可以分为两类，一类是每一帧都可能会发生变化的数据，比如灯光强度、摄像机焦距等等，
    /// 还有一类是全局设定，即 PipelineAsset 里的设定，一旦设定好，在渲染时就不会改变的数据。
    /// </summary>
    public struct PipelinePerFrameData
    {
        public ScriptableRenderContext context;
        public CommandBuffer buffer;
        public CullingResults cullingResults;
        public Camera camera;
        
        // ××××××××××××××××××××××××××××××××××××××××××××××××××××××××××××××××××××××××××××××××
        // Data 相关方法
    }

    public static class RenderTargetIDs
    {
        public static readonly int k_FrameBufferId = Shader.PropertyToID("_CameraFrameBuffer");
        public static readonly int k_FinalBlitTextureId = Shader.PropertyToID("_FinalBlitTexture");
        
        public static readonly int k_BloomTextureId = Shader.PropertyToID("_BloomTexture");
        public static readonly int k_ColorGradingLutTextureId = Shader.PropertyToID("_ColorGradingLutTexture");
    }
    
    // public class PipelineData
    
}