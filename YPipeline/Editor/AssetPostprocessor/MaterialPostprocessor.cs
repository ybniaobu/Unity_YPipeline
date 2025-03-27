using UnityEngine;
using UnityEditor;

public class MaterialPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string assetPath in importedAssets)
        {
            if (assetPath.EndsWith(".mat"))
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                if (material != null && material.shader.name == "Standard")
                {
                    Shader defaultShader = Shader.Find("YPipeline/PBR/Standard Forward");
                    
                    if (defaultShader != null)
                    {
                        material.shader = defaultShader;
                        EditorUtility.SetDirty(material);
                    }
                }
            }
        }
    }
}
