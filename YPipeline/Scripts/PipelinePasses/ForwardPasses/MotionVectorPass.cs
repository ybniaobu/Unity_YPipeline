using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

namespace YPipeline
{
    public class MotionVectorPass : PipelinePass
    {
        private class MotionVectorPassData
        {
            public Material material;
            
            public TextureHandle motionVectorTexture;
        }
        
        private const string k_MotionVector = "Hidden/YPipeline/MotionVector";
        private Material m_MotionVectorMaterial;
        private Material MotionVectorMaterial
        {
            get
            {
                if (m_MotionVectorMaterial == null)
                {
                    m_MotionVectorMaterial = new Material(Shader.Find(k_MotionVector));
                    m_MotionVectorMaterial.hideFlags = HideFlags.HideAndDontSave;
                }
                return m_MotionVectorMaterial;
            }
        }
        
        protected override void Initialize() { }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<MotionVectorPassData>("Motion Vector Pass", out var passData))
            {
                passData.material = MotionVectorMaterial;
                builder.ReadTexture(data.CameraDepthTexture);
                
                TextureDesc motionVectorDesc = new TextureDesc(data.BufferSize.x, data.BufferSize.y)
                {
                    colorFormat = GraphicsFormat.R16G16_SFloat,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    clearBuffer = true,
                    clearColor = Color.clear,
                    name = "Motion Vector Texture"
                };

                data.MotionVectorTexture = data.renderGraph.CreateTexture(motionVectorDesc);
                passData.motionVectorTexture = builder.WriteTexture(data.MotionVectorTexture);
                
                builder.AllowPassCulling(false);
                
                builder.SetRenderFunc((MotionVectorPassData data, RenderGraphContext context) =>
                {
                    BlitUtility.DrawTexture(context.cmd, data.motionVectorTexture, data.material, 0);
                    
                    context.cmd.SetGlobalTexture(YPipelineShaderIDs.k_MotionVectorTextureID, data.motionVectorTexture);
                    
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
                });
            }
        }
    }
}