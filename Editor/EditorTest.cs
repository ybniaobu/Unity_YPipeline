using UnityEngine;
using UnityEditor;
using UnityEditor.AssetImporters;
using System.IO;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Unity.Collections;

public class EditorTest : OdinEditorWindow
{
    [MenuItem("Tools/Test")]
    private static void ShowWindow()
    {
        EditorTest window = OdinEditorWindow.GetWindow<EditorTest>();
        window.Show();
    }
    
    
    private string m_AssetPath;
    private bool m_QualifyForSpritePacking;
    private bool m_EnablePostProcessor;
    private TextureImporterSettings m_Settings;
    private TextureImporterPlatformSettings m_PlatformSettings;
    private SourceTextureInformation m_SourceTextureInformation;
    private SpriteImportData[] m_SpriteImportData;
    private string m_SpritePackingTag;
    private SecondarySpriteTexture[] m_SecondarySpriteTextures;
    

    [Button(ButtonSizes.Large), GUIColor(1, 1, 1)]
    private void Test()
    {
        
    }
}
