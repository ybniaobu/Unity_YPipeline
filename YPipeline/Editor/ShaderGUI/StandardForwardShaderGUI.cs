using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline.Editor
{
    public class StandardForwardShaderGUI : YPipelineShaderGUI
    {
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            base.OnGUI(materialEditor, properties);
            // UnityEmissionProperty();
        }

        // private void UnityEmissionProperty()
        // {
        //     EditorGUI.BeginChangeCheck();
        //     m_MaterialEditor.LightmapEmissionProperty();
        //     //m_MaterialEditor.LightmapEmissionFlagsProperty(0, true);
        //     if (EditorGUI.EndChangeCheck())
        //     {
        //         foreach (Material m in m_Materials) 
        //         {
        //             m.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        //         }
        //     }
        // }
    }
}