using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public class PostProcessingNode : PipelineNode
    {
        protected override void Initialize()
        {
            
        }
        
        protected override void Dispose()
        {
            DestroyImmediate(this);
        }

        protected override void OnRelease(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            base.OnRelease(asset, ref data);
            data.context.ExecuteCommandBuffer(data.buffer);
            data.buffer.Clear();
            data.context.Submit();
        }

        protected override void OnRender(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            base.OnRender(asset, ref data);
            data.buffer.Blit(new RenderTargetIdentifier(ForwardRenderTarget.frameBufferId), new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget));
            data.context.ExecuteCommandBuffer(data.buffer);
            data.buffer.Clear();
            data.context.Submit();
        }
    }
}