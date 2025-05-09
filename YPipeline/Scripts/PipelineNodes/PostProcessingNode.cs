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

        protected override void OnRelease(ref YPipelineData data)
        {
            base.OnRelease(ref data);
            data.context.ExecuteCommandBuffer(data.cmd);
            data.cmd.Clear();
            data.context.Submit();
        }

        protected override void OnRender(ref YPipelineData data)
        {
            base.OnRender(ref data);
            
// #if UNITY_EDITOR
//             // disable post-processing in material preview and reflection probe preview
//             if (data.camera.cameraType > CameraType.SceneView)
//             {
//                 BlitUtility.BlitTexture(data.cmd, YPipelineShaderIDs.k_ColorBufferID, BuiltinRenderTextureType.CameraTarget);
//                 return;
//             }
//             
//             // enable or disable post-processing in the scene window via its effects dropdown menu in its toolbar
//             if (data.camera.cameraType == CameraType.SceneView && !SceneView.currentDrawingSceneView.sceneViewState.showImageEffects)
//             {
//                 BlitUtility.BlitTexture(data.cmd, YPipelineShaderIDs.k_ColorBufferID, BuiltinRenderTextureType.CameraTarget);
//                 return;
//             }
// #endif
//             data.cmd.BeginSample("Post Processing");
//             
//             PostProcessingRender(ref data);
//             
//             data.cmd.EndSample("Post Processing");
            
            data.context.ExecuteCommandBuffer(data.cmd);
            data.cmd.Clear();
            data.context.Submit();
        }

        private void PostProcessingRender(ref YPipelineData data)
        {
            // Bloom
            // m_BloomRenderer.Render(ref data);
            
            // // Color Grading Lut
            // m_ColorGradingLutRenderer.Render(ref data);
            
            // // Post Color Grading
            // m_UberPostProcessingRenderer.Render(ref data);
            //
            // // Final Post Processing
            // m_FinalPostProcessingRenderer.Render(ref data);
            
            // Clear RT
            // data.cmd.ReleaseTemporaryRT(YPipelineShaderIDs.k_BloomTextureID);
            // data.cmd.ReleaseTemporaryRT(YPipelineShaderIDs.k_ColorGradingLutTextureID);
        }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<PostProcessingNodeData>("Post Processing", out var nodeData))
            {
                nodeData.cameraType = data.camera.cameraType;
                
                builder.SetRenderFunc((PostProcessingNodeData data, RenderGraphContext context) =>
                {
#if UNITY_EDITOR
                    // disable post-processing in material preview and reflection probe preview
                    if (data.cameraType > CameraType.SceneView)
                    {
                        // TODO: 改变逻辑
                        BlitUtility.BlitTexture(context.cmd, YPipelineShaderIDs.k_ColorBufferID, BuiltinRenderTextureType.CameraTarget);
                    }
            
                    // enable or disable post-processing in the scene window via its effects dropdown menu in its toolbar
                    if (data.cameraType == CameraType.SceneView && !SceneView.currentDrawingSceneView.sceneViewState.showImageEffects)
                    {
                        BlitUtility.BlitTexture(context.cmd, YPipelineShaderIDs.k_ColorBufferID, BuiltinRenderTextureType.CameraTarget);
                    }
#endif
                });
                
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
}