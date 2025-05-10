using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class ErrorMaterialNode : PipelineNode
    {
#if UNITY_EDITOR
        private class ErrorMaterialNodeData
        {
            public RendererListHandle rendererList;
        }
        
        private Material m_ErrorMaterial;
#endif
        protected override void Initialize() { }

        public override void OnRecord(ref YPipelineData data)
        {
#if UNITY_EDITOR
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<ErrorMaterialNodeData>("Draw Error Material", out var nodeData))
            {
                if (m_ErrorMaterial == null)
                {
                    m_ErrorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
                }
                
                DrawingSettings drawingSettings = new DrawingSettings(YPipelineShaderTagIDs.k_LegacyShaderTagIds[0], new SortingSettings(data.camera))
                {
                    overrideMaterial = m_ErrorMaterial
                };
                for (int i = 1; i < YPipelineShaderTagIDs.k_LegacyShaderTagIds.Count; i++) 
                {
                    drawingSettings.SetShaderPassName(i, YPipelineShaderTagIDs.k_LegacyShaderTagIds[i]);
                }
                RendererListParams rendererListParams = new RendererListParams(data.cullingResults, drawingSettings, FilteringSettings.defaultValue);

                nodeData.rendererList = data.renderGraph.CreateRendererList(rendererListParams);
                builder.UseRendererList(nodeData.rendererList);
                
                builder.SetRenderFunc((ErrorMaterialNodeData data, RenderGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.rendererList);
                });
            }
#endif
        }
    }
}