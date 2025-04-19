using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public class SkyboxNode : PipelineNode
    {
        protected override void Initialize()
        {
            
        }
        
        protected override void OnDispose()
        {
            //DestroyImmediate(this);
        }

        protected override void OnRender(ref YPipelineData data)
        {
            base.OnRender(ref data);
            SkyboxRenderer(ref data);
            data.context.ExecuteCommandBuffer(data.buffer);
            data.buffer.Clear();
            data.context.Submit();
        }

        private void SkyboxRenderer(ref YPipelineData data)
        {
            RendererList skyboxRendererList = data.context.CreateSkyboxRendererList(data.camera);
            data.buffer.DrawRendererList(skyboxRendererList);
        }
    }
}