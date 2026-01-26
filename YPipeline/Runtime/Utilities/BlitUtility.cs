using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public static class BlitUtility
    {
        public static readonly int k_BlitTextureId = Shader.PropertyToID("_BlitTexture");
        
        // ----------------------------------------------------------------------------------------------------
        // Materials
        // ----------------------------------------------------------------------------------------------------
        
        private static Material m_CopyMaterial;
        private static Material m_CopyDepthMaterial;

        public static void Initialize()
        {
            var runtimeResources = GraphicsSettings.GetRenderPipelineSettings<YPipelineRuntimeResources>();
            m_CopyMaterial = CoreUtils.CreateEngineMaterial(runtimeResources.CopyShader);
            m_CopyDepthMaterial = CoreUtils.CreateEngineMaterial(runtimeResources.CopyDepthShader);
        }

        public static void Dispose()
        {
            CoreUtils.Destroy(m_CopyMaterial);
            m_CopyMaterial = null;
            CoreUtils.Destroy(m_CopyDepthMaterial);
            m_CopyDepthMaterial = null;
        }

        // ----------------------------------------------------------------------------------------------------
        // Functions
        // ----------------------------------------------------------------------------------------------------

        public static void BlitGlobalTexture(CommandBuffer cmd, TextureHandle source, TextureHandle destination)
        {
            cmd.SetGlobalTexture(k_BlitTextureId, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, m_CopyMaterial, 0, MeshTopology.Triangles, 3);
        }
        
        public static void BlitTexture(CommandBuffer cmd, TextureHandle source, TextureHandle destination)
        {
            m_CopyMaterial.SetTexture(k_BlitTextureId, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, m_CopyMaterial, 0, MeshTopology.Triangles, 3);
        }
        
        public static void BlitTexture(UnsafeCommandBuffer cmd, TextureHandle source, TextureHandle destination)
        {
            m_CopyMaterial.SetTexture(k_BlitTextureId, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, m_CopyMaterial, 0, MeshTopology.Triangles, 3);
        }
        
        public static void BlitGlobalTexture(CommandBuffer cmd, TextureHandle source, TextureHandle destination, Material material, int pass)
        {
            cmd.SetGlobalTexture(k_BlitTextureId, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
        }
        
        public static void BlitGlobalTexture(UnsafeCommandBuffer cmd, TextureHandle source, TextureHandle destination, Material material, int pass)
        {
            cmd.SetGlobalTexture(k_BlitTextureId, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
        }
        
        public static void BlitTexture(CommandBuffer cmd, TextureHandle source, TextureHandle destination, Material material, int pass)
        {
            material.SetTexture(k_BlitTextureId, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
        }
        
        public static void BlitTexture(UnsafeCommandBuffer cmd, TextureHandle source, TextureHandle destination, Material material, int pass)
        {
            material.SetTexture(k_BlitTextureId, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
        }
        
        public static void BlitGlobalTexture(CommandBuffer cmd, TextureHandle source, TextureHandle destination, Rect cameraRect, Material material, int pass)
        {
            cmd.SetGlobalTexture(k_BlitTextureId, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.SetViewport(cameraRect);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
        }
        
        public static void BlitTexture(CommandBuffer cmd, TextureHandle source, TextureHandle destination, Rect cameraRect, Material material, int pass)
        {
            material.SetTexture(k_BlitTextureId, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.SetViewport(cameraRect);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
        }
        
        public static void DrawTexture(CommandBuffer cmd, TextureHandle destination, Material material, int pass)
        {
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
        }

        public static void CopyDepth(CommandBuffer cmd, TextureHandle source, TextureHandle destination)
        {
            //cmd.SetGlobalTexture(k_BlitTextureId, source);
            m_CopyDepthMaterial.SetTexture(k_BlitTextureId, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, m_CopyDepthMaterial, 0, MeshTopology.Triangles, 3);
        }
        
        public static void CopyDepth(UnsafeCommandBuffer cmd, TextureHandle source, TextureHandle destination)
        {
            //cmd.SetGlobalTexture(k_BlitTextureId, source);
            m_CopyDepthMaterial.SetTexture(k_BlitTextureId, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, m_CopyDepthMaterial, 0, MeshTopology.Triangles, 3);
        }
    }
}