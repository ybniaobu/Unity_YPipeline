using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Rendering;

namespace YPipeline.Editor
{
    public static partial class YPipelineReflectionProbeUI
    {
        /// <summary>
        /// 生成包含 7 个 mipmap 的 Octahedral Atlas：低质量的高度为 cubemapSize * 0.5，宽度为 cubemapSize * 0.75；
        /// 中质量高度为 cubemapSize，宽度为 cubemapSize * 1.5；高质量的高度为 cubemapSize * 2，宽度为 cubemapSize * 3；
        /// </summary>
        private static void GenerateOctahedralAtlas(SerializedYPipelineReflectionProbe serialized, ReflectionProbe probe, string directoryPath, Quality3Tier quality)
        {
            if (!GraphicsSettings.TryGetRenderPipelineSettings(out YPipelineEditorResources editorResources))
            {
                Debug.LogError("未找到烘焙所需资源 (YPipelineEditorResources)，无法烘焙！");
                return;
            }
            
            // Render Texture
            int qualityLevel = (int) Mathf.Pow(2, (int) quality);
            int width = (int) (qualityLevel * 0.75 * probe.resolution);
            int height = (int) (qualityLevel * 0.5 * probe.resolution);
            RenderTexture renderTexture = new RenderTexture(width, height, 0)
            {
                format =  RenderTextureFormat.ARGBHalf,
                enableRandomWrite = true,
            };
            
            // Octahedral Mapping
            ComputeShader cs = editorResources.OctahedralMappingCS;
            int kernel = cs.FindKernel("OctahedralMappingKernel");
            cs.SetTexture(kernel, "_OutputTexture", renderTexture);
            cs.SetTexture(kernel, "_Cubemap", probe.texture);
            cs.SetVector("_TextureSize", new Vector4(width, height, 1.0f / width, 1.0f / height));

            for (int i = 0; i < 7; i++)
            {
                cs.SetInt("_MipMap", i);
                int threadGroups = Mathf.CeilToInt(height / 8.0f / Mathf.Pow(2, i));
                cs.Dispatch(kernel, threadGroups, threadGroups, 1);
            }
            
            // GPU to CPU
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBAHalf, false);
            RenderTexture.active = renderTexture;
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            
            // Save to EXR
            string filePath = Path.Combine(directoryPath, probe.name);
            switch (quality)
            {
                case Quality3Tier.Low:
                    filePath = filePath + "_OctahedralAtlas_Low" + ".exr";
                    break;
                case Quality3Tier.Medium:
                    filePath = filePath + "_OctahedralAtlas_Medium" + ".exr";
                    break;
                case Quality3Tier.High:
                    filePath = filePath + "_OctahedralAtlas_High" + ".exr";
                    break;
            }
            var bytes = ImageConversion.EncodeToEXR(tex, Texture2D.EXRFlags.CompressZIP);
            File.WriteAllBytes(filePath, bytes);
            AssetDatabase.Refresh();
            
            // Clear Resources
            RenderTexture.active = null;
            renderTexture.Release();
            Object.DestroyImmediate(renderTexture);
            Object.DestroyImmediate(tex);
            
            // Texture Import Settings
            TextureImporter importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.textureShape = TextureImporterShape.Texture2D;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 8192;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.SaveAndReimport();
            }
            
            // Serialize
            Texture atlas = AssetDatabase.LoadAssetAtPath(filePath, typeof(Texture)) as Texture;
            switch (quality)
            {
                case Quality3Tier.Low:
                    serialized.octahedralAtlasLow.objectReferenceValue = atlas;
                    break;
                case Quality3Tier.Medium:
                    serialized.octahedralAtlasMedium.objectReferenceValue = atlas;
                    break;
                case Quality3Tier.High:
                    serialized.octahedralAtlasHigh.objectReferenceValue = atlas;
                    break;
            }
        }
    }
}