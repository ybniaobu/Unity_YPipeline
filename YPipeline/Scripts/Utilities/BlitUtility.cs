﻿using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

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

        public static void BlitGlobalTexture(CommandBuffer cmd, TextureHandle source, TextureHandle destination)
        {
            cmd.SetGlobalTexture(k_BlitTextureId, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, CopyMaterial, 0, MeshTopology.Triangles, 3);
        }
        
        public static void BlitTexture(CommandBuffer cmd, TextureHandle source, TextureHandle destination)
        {
            CopyMaterial.SetTexture(k_BlitTextureId, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, CopyMaterial, 0, MeshTopology.Triangles, 3);
        }
        
        public static void BlitGlobalTexture(CommandBuffer cmd, TextureHandle source, TextureHandle destination, Material material, int pass)
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
            CopyDepthMaterial.SetTexture(k_BlitTextureId, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, CopyDepthMaterial, 0, MeshTopology.Triangles, 3);
        }
    }
}