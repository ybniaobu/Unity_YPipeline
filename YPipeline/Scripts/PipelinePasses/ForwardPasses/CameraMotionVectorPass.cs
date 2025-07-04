using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

namespace YPipeline
{
    public class CameraMotionVectorPass : PipelinePass
    {
        private class CameraMotionVectorPassData
        {
            public Material material;
            
            public TextureHandle cameraMotionVectorTexture;
        }
        
        private const string k_CameraMotionVector = "Hidden/YPipeline/CameraMotionVector";
        private Material m_CameraMotionVectorMaterial;
        private Material CameraMotionVectorMaterial
        {
            get
            {
                if (m_CameraMotionVectorMaterial == null)
                {
                    m_CameraMotionVectorMaterial = new Material(Shader.Find(k_CameraMotionVector));
                    m_CameraMotionVectorMaterial.hideFlags = HideFlags.HideAndDontSave;
                }
                return m_CameraMotionVectorMaterial;
            }
        }
        
        protected override void Initialize() { }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<CameraMotionVectorPassData>("Camera Motion Vector", out var passData))
            {
                passData.material = CameraMotionVectorMaterial;
                builder.ReadTexture(data.CameraDepthTexture);
                
                TextureDesc cameraMotionVectorDesc = new TextureDesc(data.BufferSize.x, data.BufferSize.y)
                {
                    colorFormat = GraphicsFormat.R16G16_SFloat,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    clearBuffer = true,
                    clearColor = Color.clear,
                    name = "Camera Motion Vector Texture"
                };

                data.CameraMotionVectorTexture = data.renderGraph.CreateTexture(cameraMotionVectorDesc);
                passData.cameraMotionVectorTexture = builder.WriteTexture(data.CameraMotionVectorTexture);
                
                builder.AllowPassCulling(false);
                
                builder.SetRenderFunc((CameraMotionVectorPassData data, RenderGraphContext context) =>
                {
                    BlitUtility.DrawTexture(context.cmd, data.cameraMotionVectorTexture, data.material, 0);
                    
                    context.cmd.SetGlobalTexture(YPipelineShaderIDs.k_CameraMotionVectorTextureID, data.cameraMotionVectorTexture);
                    
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
                });
            }
        }
    }
}