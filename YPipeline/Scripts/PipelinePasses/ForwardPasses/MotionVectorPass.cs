using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

namespace YPipeline
{
    public class MotionVectorPass : PipelinePass
    {
        private class MotionVectorPassData
        {
            public Material cameraMotionVectorMaterial;
            
            public RendererListHandle opaqueRendererList;
            public RendererListHandle alphaTestRendererList;
            
            public TextureHandle motionVectorTexture;
            public TextureHandle motionVectorStencil;
            
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
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<MotionVectorPassData>("Camera & Object Motion Vector", out var passData))
            {
                // These flags are still required in SRP or the engine won't compute previous model matrices...
                // If the flag hasn't been set yet on this camera, motion vectors will skip a frame.
                data.camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;
                
                // Motion Vector Texture
                Vector2Int bufferSize = data.BufferSize;
                TextureDesc motionVectorDesc = new TextureDesc(bufferSize.x, bufferSize.y)
                {
                    colorFormat = GraphicsFormat.R16G16_SFloat,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    clearBuffer = true,
                    clearColor = Color.clear,
                    name = "Motion Vector Texture"
                };

                data.MotionVectorTexture = data.renderGraph.CreateTexture(motionVectorDesc);
                passData.motionVectorTexture = builder.WriteTexture(data.MotionVectorTexture);
                
                // Object Motion Vector
                TextureDesc motionVectorStencilDesc = new TextureDesc(bufferSize.x,bufferSize.y)
                {
                    format = GraphicsFormat.S8_UInt,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    clearBuffer = true,
                    name = "Motion Vector Stencil"
                };
                
                passData.motionVectorStencil = builder.CreateTransientTexture(motionVectorStencilDesc);
                
                RendererListDesc opaqueRendererListDesc = new RendererListDesc(YPipelineShaderTagIDs.k_MotionVectorsShaderTagId, data.cullingResults, data.camera)
                {
                    rendererConfiguration = PerObjectData.MotionVectors,
                    renderQueueRange = new RenderQueueRange(2000, 2449),
                    sortingCriteria = SortingCriteria.CommonOpaque
                };
                
                RendererListDesc alphaTestRendererListDesc = new RendererListDesc(YPipelineShaderTagIDs.k_MotionVectorsShaderTagId, data.cullingResults, data.camera)
                {
                    rendererConfiguration = PerObjectData.MotionVectors,
                    renderQueueRange = new RenderQueueRange(2450, 2499),
                    sortingCriteria = SortingCriteria.OptimizeStateChanges
                };
                
                passData.opaqueRendererList = data.renderGraph.CreateRendererList(opaqueRendererListDesc);
                passData.alphaTestRendererList = data.renderGraph.CreateRendererList(alphaTestRendererListDesc);
                builder.UseRendererList(passData.opaqueRendererList);
                builder.UseRendererList(passData.alphaTestRendererList);
                
                // Camera Motion Vector
                passData.cameraMotionVectorMaterial = CameraMotionVectorMaterial;
                builder.ReadTexture(data.CameraDepthTexture);
                
                // Render Graph Setting
                builder.AllowPassCulling(false);
                builder.AllowRendererListCulling(false);
                
                builder.SetRenderFunc((MotionVectorPassData data, RenderGraphContext context) =>
                {
                    context.cmd.SetRenderTarget(data.motionVectorTexture, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,
                        data.motionVectorStencil, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
                    
                    // Object Motion Vector
                    context.cmd.BeginSample("Object Motion Vector");
                    context.cmd.DrawRendererList(data.opaqueRendererList);
                    context.cmd.DrawRendererList(data.alphaTestRendererList);
                    context.cmd.EndSample("Object Motion Vector");
                    
                    // Camera Motion Vector
                    context.cmd.BeginSample("Camera Motion Vector");
                    context.cmd.DrawProcedural(Matrix4x4.identity, data.cameraMotionVectorMaterial, 0, MeshTopology.Triangles, 3);
                    context.cmd.EndSample("Camera Motion Vector");
                    
                    context.cmd.SetGlobalTexture(YPipelineShaderIDs.k_MotionVectorTextureID, data.motionVectorTexture);
                    
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
                });
            }
        }
    }
}