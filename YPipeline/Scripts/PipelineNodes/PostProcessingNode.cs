using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace YPipeline
{
    public class PostProcessingNode : PipelineNode
    {
        private class PostProcessingNodeData
        {
            // TODO: 更改到 SceneCameraRenderer 内后删除
            public CameraType cameraType;
            
            public TextureHandle colorAttachment;
            public TextureHandle cameraColorTarget;
        }
        
        private BloomRenderer m_BloomRenderer;
        private ColorGradingLutRenderer m_ColorGradingLutRenderer;
        private UberPostProcessingRenderer m_UberPostProcessingRenderer;
        private FinalPostProcessingRenderer m_FinalPostProcessingRenderer;
        
        protected override void Initialize()
        {
            m_BloomRenderer = PostProcessingRenderer.Create<BloomRenderer>();
            m_ColorGradingLutRenderer = PostProcessingRenderer.Create<ColorGradingLutRenderer>();
            m_UberPostProcessingRenderer = PostProcessingRenderer.Create<UberPostProcessingRenderer>();
            m_FinalPostProcessingRenderer = PostProcessingRenderer.Create<FinalPostProcessingRenderer>();
        }
        
        protected override void OnDispose()
        {
            //DestroyImmediate(this);
        }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<PostProcessingNodeData>("Post Processing", out var nodeData))
            {
                nodeData.cameraType = data.camera.cameraType;
                nodeData.colorAttachment = builder.ReadTexture(data.CameraColorAttachment);
                nodeData.cameraColorTarget = builder.WriteTexture(data.CameraColorTarget);
                
                builder.SetRenderFunc((PostProcessingNodeData data, RenderGraphContext context) =>
                {
#if UNITY_EDITOR
                    // disable post-processing in material preview and reflection probe preview
                    if (data.cameraType > CameraType.SceneView)
                    {
                        // TODO: 改变逻辑
                        BlitUtility.BlitTexture(context.cmd, data.colorAttachment, data.cameraColorTarget);
                    }
            
                    // enable or disable post-processing in the scene window via its effects dropdown menu in its toolbar
                    if (data.cameraType == CameraType.SceneView && !SceneView.currentDrawingSceneView.sceneViewState.showImageEffects)
                    {
                        BlitUtility.BlitTexture(context.cmd, data.colorAttachment, data.cameraColorTarget);
                    }
#endif
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
                });
            }
            
            // TODO: 更改到 SceneCameraRenderer 内
#if UNITY_EDITOR
            if (data.camera.cameraType > CameraType.SceneView || data.camera.cameraType == CameraType.SceneView && !SceneView.currentDrawingSceneView.sceneViewState.showImageEffects)
            {
                return;
            }
#endif
                
            m_BloomRenderer.OnRecord(ref data);
            m_ColorGradingLutRenderer.OnRecord(ref data);
            m_UberPostProcessingRenderer.OnRecord(ref data);
            m_FinalPostProcessingRenderer.OnRecord(ref data);
        }
    }
}