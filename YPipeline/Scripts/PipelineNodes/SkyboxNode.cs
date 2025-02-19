using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public class SkyboxNode : PipelineNode
    {
        protected override void Initialize()
        {
            
        }
        
        protected override void Dispose()
        {
            DestroyImmediate(this);
        }
        
        protected override void OnRender(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            base.OnRender(asset, ref data);
            SkyboxRenderer(asset, ref data);
        }

        private void SkyboxRenderer(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            RendererList skyboxRendererList = data.context.CreateSkyboxRendererList(data.cameraData.camera);
            data.buffer.DrawRendererList(skyboxRendererList);
            data.context.ExecuteCommandBuffer(data.buffer);
            data.buffer.Clear();
        }
    }
}