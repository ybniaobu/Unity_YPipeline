using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace YPipeline
{
    public class PostProcessingPass : PipelinePass
    {
        private class PostProcessingPassData
        {
            // TODO: 更改到 SceneCameraRenderer 内后删除
            public CameraType cameraType;
            
            public TextureHandle colorAttachment;
            public TextureHandle cameraColorTarget;
        }
        
        private BloomSubPass m_BloomSubPass;
        private ColorGradingLutSubPass m_ColorGradingLutSubPass;
        private UberPostProcessingSubPass m_UberPostProcessingSubPass;
        private FinalPostProcessingSubPass m_FinalPostProcessingSubPass;
        
        protected override void Initialize()
        {
            m_BloomSubPass = PostProcessingSubPass.Create<BloomSubPass>();
            m_ColorGradingLutSubPass = PostProcessingSubPass.Create<ColorGradingLutSubPass>();
            m_UberPostProcessingSubPass = PostProcessingSubPass.Create<UberPostProcessingSubPass>();
            m_FinalPostProcessingSubPass = PostProcessingSubPass.Create<FinalPostProcessingSubPass>();
        }
        
        protected override void OnDispose()
        {
            //DestroyImmediate(this);
        }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<PostProcessingPassData>("Post Processing", out var passData))
            {
                passData.cameraType = data.camera.cameraType;
                passData.colorAttachment = builder.ReadTexture(data.CameraColorAttachment);
                passData.cameraColorTarget = builder.WriteTexture(data.CameraColorTarget);
                
                builder.SetRenderFunc((PostProcessingPassData data, RenderGraphContext context) =>
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
                
            m_BloomSubPass.OnRecord(ref data);
            m_ColorGradingLutSubPass.OnRecord(ref data);
            m_UberPostProcessingSubPass.OnRecord(ref data);
            m_FinalPostProcessingSubPass.OnRecord(ref data);
        }
    }
}