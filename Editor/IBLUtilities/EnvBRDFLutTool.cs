using UnityEngine;
using UnityEditor;
using System.IO;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

public class EnvBRDFLutTool : OdinEditorWindow
{
    [MenuItem("Tools/IBL Tools/Generate Environment BRDF Lut")]
    private static void ShowWindow()
    {
        EnvBRDFLutTool window = OdinEditorWindow.GetWindow<EnvBRDFLutTool>();
        window.Show();
    }

    [Title("Compute Shader Settings")] 
    [ReadOnly] public string csPath = "Assets/Editor/IBLUtilities/EnvBRDFLut.compute";
    [ReadOnly] public ComputeShader envBRDFLutCs;
    [ReadOnly] public string csKernelName = "GenerateEnvBRDFLut";
    
    [Title("Save Settings")]
    public int sampleNumber = 2048;
    public int envBRDFLutSize = 1024;
    [FolderPath] public string savePath = "Assets";
    public string saveName = "EnvBRDFLut";
    
    [Title("Result Preview")]
    [InlineEditor(InlineEditorModes.LargePreview)] public Texture2D envBRDFLut;
    private RenderTexture m_RWTexture;

    [InfoBox("Just refresh the Project window, if you don't see the generated lut image.")]
    [Button(ButtonSizes.Large), GUIColor(1, 1, 1)]
    private void GenerateEnvBRDFLut()
    {
        m_RWTexture = new RenderTexture(envBRDFLutSize, envBRDFLutSize, 32, RenderTextureFormat.ARGBHalf);
        m_RWTexture.enableRandomWrite = true;
        m_RWTexture.Create();

        envBRDFLutCs = AssetDatabase.LoadAssetAtPath<ComputeShader>(csPath);
        int kernelIndex = envBRDFLutCs.FindKernel(csKernelName);
        envBRDFLutCs.SetTexture(kernelIndex, "_RWTexture", m_RWTexture);
        envBRDFLutCs.SetInt("_SampleNumber", sampleNumber);
        envBRDFLutCs.SetInt("_LutSize", envBRDFLutSize);
        envBRDFLutCs.Dispatch(kernelIndex, envBRDFLutSize / 8, envBRDFLutSize / 8, 1);
        
        envBRDFLut = new Texture2D(envBRDFLutSize, envBRDFLutSize, TextureFormat.RGBAHalf, false);
        
        Graphics.CopyTexture(m_RWTexture, envBRDFLut);
        RenderTexture.active = m_RWTexture;
        envBRDFLut.ReadPixels(new Rect(0, 0, m_RWTexture.width, m_RWTexture.height), 0, 0);
        RenderTexture.active = null;
        
        var bytes = ImageConversion.EncodeToEXR(envBRDFLut, Texture2D.EXRFlags.CompressZIP);
        File.WriteAllBytes(Application.dataPath + savePath.Substring(6)+ "/" + saveName + ".exr", bytes);
    }
}
