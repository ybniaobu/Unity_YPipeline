using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public static class BlitUtility
    {
        private static readonly int k_BlitTextureId = Shader.PropertyToID("_BlitTexture");
        
        // ----------------------------------------------------------------------------------------------------
        // Materials
        // ----------------------------------------------------------------------------------------------------
        
        private const string k_Copy = "Hidden/YPipeline/Copy";
        private static Material m_CopyMaterial;
        public static Material CopyMaterial
        {
            get
            {
                if (m_CopyMaterial == null)
                {
                    m_CopyMaterial = new Material(Shader.Find(k_Copy))
                    {
                        name = "Copy",
                        hideFlags = HideFlags.HideAndDontSave
                    };
                }
                return m_CopyMaterial;
            }
        }
        
        private const string k_CopyDepth = "Hidden/YPipeline/CopyDepth";
        private static Material m_CopyDepthMaterial;
        public static Material CopyDepthMaterial
        {
            get
            {
                if (m_CopyDepthMaterial == null)
                {
                    m_CopyDepthMaterial = new Material(Shader.Find(k_CopyDepth))
                    {
                        name = "CopyDepth",
                        hideFlags = HideFlags.HideAndDontSave
                    };
                }
                return m_CopyDepthMaterial;
            }
        }

        // ----------------------------------------------------------------------------------------------------
        // Functions
        // ----------------------------------------------------------------------------------------------------
        
        public static void BlitTexture(CommandBuffer cmd, int sourceID, int destinationID)
        {
            cmd.SetGlobalTexture(k_BlitTextureId, new RenderTargetIdentifier(sourceID));
            cmd.SetRenderTarget(new RenderTargetIdentifier(destinationID), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, CopyMaterial, 0, MeshTopology.Triangles, 3);
        }
        
        public static void BlitTexture(CommandBuffer cmd, int sourceID, BuiltinRenderTextureType destination)
        {
            cmd.SetGlobalTexture(k_BlitTextureId, new RenderTargetIdentifier(sourceID));
            cmd.SetRenderTarget(new RenderTargetIdentifier(destination), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, CopyMaterial, 0, MeshTopology.Triangles, 3);
        }
        
        public static void BlitTexture(CommandBuffer cmd, int sourceID, int destinationID, Material material, int pass)
        {
            cmd.SetGlobalTexture(k_BlitTextureId, new RenderTargetIdentifier(sourceID));
            cmd.SetRenderTarget(new RenderTargetIdentifier(destinationID), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
        }
        
        public static void BlitTexture(CommandBuffer cmd, int sourceID, BuiltinRenderTextureType destination, Material material, int pass)
        {
            cmd.SetGlobalTexture(k_BlitTextureId, new RenderTargetIdentifier(sourceID));
            cmd.SetRenderTarget(new RenderTargetIdentifier(destination), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
        }
        
        public static void BlitCameraTarget(CommandBuffer cmd, int sourceID, Rect cameraRect)
        {
            cmd.SetGlobalTexture(k_BlitTextureId, new RenderTargetIdentifier(sourceID));
            cmd.SetRenderTarget(new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.SetViewport(cameraRect);
            cmd.DrawProcedural(Matrix4x4.identity, CopyMaterial, 0, MeshTopology.Triangles, 3);
        }
        
        public static void BlitCameraTarget(CommandBuffer cmd, int sourceID, Rect cameraRect, Material material, int pass)
        {
            cmd.SetGlobalTexture(k_BlitTextureId, new RenderTargetIdentifier(sourceID));
            cmd.SetRenderTarget(new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.SetViewport(cameraRect);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
        }
        
        public static void DrawTexture(CommandBuffer cmd, int destinationID, Material material, int pass)
        {
            cmd.SetRenderTarget(new RenderTargetIdentifier(destinationID), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
        }
        
        public static void DrawTexture(CommandBuffer cmd, BuiltinRenderTextureType destination, Material material, int pass)
        {
            cmd.SetRenderTarget(new RenderTargetIdentifier(destination), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
        }
        
        public static void CopyDepth(CommandBuffer cmd, int sourceID, int destinationID)
        {
            cmd.SetGlobalTexture(k_BlitTextureId, new RenderTargetIdentifier(sourceID));
            cmd.SetRenderTarget(new RenderTargetIdentifier(destinationID), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, CopyDepthMaterial, 0, MeshTopology.Triangles, 3);
        }
        
        public static void CopyDepth(CommandBuffer cmd, int sourceID, BuiltinRenderTextureType destination)
        {
            cmd.SetGlobalTexture(k_BlitTextureId, new RenderTargetIdentifier(sourceID));
            cmd.SetRenderTarget(new RenderTargetIdentifier(destination), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, CopyDepthMaterial, 0, MeshTopology.Triangles, 3);
        }
    }
}