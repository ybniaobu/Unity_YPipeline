using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Collections.Generic;

public class EnvMapPrefilterTool : OdinEditorWindow
{
    [MenuItem("Tools/IBL Tools/Environment Map Prefilter")]
    private static void ShowWindow()
    {
        EnvMapPrefilterTool window = OdinEditorWindow.GetWindow<EnvMapPrefilterTool>();
        window.Show();
    }
    
    [Title("Skybox material that uses EnvMapPrefilter Shader")]
    public Material EnvMapPrefilterSkybox;

    [Title("Save Settings")] 
    public int sizePerFace = 2048;
    public int mipCount = 9;
    public float exposureLevel = 1.5f;
    [FolderPath] public string savePath = "Assets";
    public string saveName = "PrefilteredCubemap";
    
    [Title("Result Preview")]
    [ReadOnly] [InlineEditor(InlineEditorModes.LargePreview)] public Cubemap prefilteredEnvMap;

    private List<Cubemap> m_CubemapMipmaps;
    private GameObject m_CubemapCamera;
    
    [InfoBox("This tool prefilters the environment map with GGX distribution.")]
    [Button(ButtonSizes.Large), GUIColor(1, 1, 1)]
    private void EnvMapPrefilter()
    {
        m_CubemapMipmaps = new List<Cubemap>();
        
        prefilteredEnvMap = new Cubemap(sizePerFace, TextureFormat.RGBA64, mipCount);
        
        m_CubemapCamera = new GameObject("CubemapCamera");
        m_CubemapCamera.AddComponent<Camera>();
        m_CubemapCamera.transform.position = Vector3.zero;
        m_CubemapCamera.transform.rotation = Quaternion.identity;
        
        for (int i = 0; i < mipCount; i++)
        {
            EnvMapPrefilterSkybox.SetFloat("_Roughness", 1.0f / (mipCount - 1) * i);
            EnvMapPrefilterSkybox.SetFloat("_SampleNumber", Mathf.Pow(2.0f, i + 4));
            //EnvMapPrefilterSkybox.SetFloat("_Exposure", Mathf.Lerp(1, exposureLevel, (float) i / (mipCount - 1.0f)));
            
            m_CubemapMipmaps.Add(new Cubemap((int) (sizePerFace / Mathf.Pow(2.0f, i)), TextureFormat.RGBA64, false));
            m_CubemapCamera.GetComponent<Camera>().RenderToCubemap(m_CubemapMipmaps[i]);
            
            var face00 = m_CubemapMipmaps[i].GetPixelData<Color>(0, CubemapFace.NegativeX);
            var face01 = m_CubemapMipmaps[i].GetPixelData<Color>(0, CubemapFace.PositiveX);
            var face02 = m_CubemapMipmaps[i].GetPixelData<Color>(0, CubemapFace.NegativeY);
            var face03 = m_CubemapMipmaps[i].GetPixelData<Color>(0, CubemapFace.PositiveY);
            var face04 = m_CubemapMipmaps[i].GetPixelData<Color>(0, CubemapFace.NegativeZ);
            var face05 = m_CubemapMipmaps[i].GetPixelData<Color>(0, CubemapFace.PositiveZ);
            
            prefilteredEnvMap.SetPixelData(face00, i, CubemapFace.NegativeX);
            prefilteredEnvMap.SetPixelData(face01, i, CubemapFace.PositiveX);
            prefilteredEnvMap.SetPixelData(face02, i, CubemapFace.NegativeY);
            prefilteredEnvMap.SetPixelData(face03, i, CubemapFace.PositiveY);
            prefilteredEnvMap.SetPixelData(face04, i, CubemapFace.NegativeZ);
            prefilteredEnvMap.SetPixelData(face05, i, CubemapFace.PositiveZ);
        }
        
        DestroyImmediate(m_CubemapCamera);
        
        AssetDatabase.CreateAsset(prefilteredEnvMap, savePath + "/" + saveName + ".cubemap");
    }
}
