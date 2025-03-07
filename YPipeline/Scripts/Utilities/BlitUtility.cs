using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public static class BlitUtility
    {
        private static readonly int k_BlitTextureId = Shader.PropertyToID("_BlitTexture");
        
        public static void BlitTexture(CommandBuffer cmd, int sourceID, int destinationID, Material material, int pass)
        {
            cmd.SetGlobalTexture(k_BlitTextureId, new RenderTargetIdentifier(sourceID));
            cmd.SetRenderTarget(new RenderTargetIdentifier(destinationID));
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
        }
        
        public static void BlitTexture(CommandBuffer cmd, int sourceID, BuiltinRenderTextureType destination, Material material, int pass)
        {
            cmd.SetGlobalTexture(k_BlitTextureId, new RenderTargetIdentifier(sourceID));
            cmd.SetRenderTarget(new RenderTargetIdentifier(destination));
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
        }
    }
}