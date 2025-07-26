using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline.Editor
{
    public class UnlitShaderGUI : YPipelineShaderGUI
    {
        // ----------------------------------------------------------------------------------------------------
        // OnGUI Related
        // ----------------------------------------------------------------------------------------------------
        
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            // FindProperties(properties);
            // GUILayout.Label("Hello Word");
            // DrawBaseTexAndColor(m_Material);
            
            
            base.OnGUI(materialEditor, properties);

            
            UnityEmissionProperty();
            
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Find Properties
        // ----------------------------------------------------------------------------------------------------

        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Draw Properties
        // ----------------------------------------------------------------------------------------------------

        private void UnityEmissionProperty()
        {
            EditorGUI.BeginChangeCheck();
            m_MaterialEditor.LightmapEmissionProperty();
            //m_MaterialEditor.LightmapEmissionFlagsProperty(0, true);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (Material m in m_Materials) 
                {
                    m.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                }
            }
        }
        
        // ----------------------------------------------------------------------------------------------------
        // ValidateMaterial Related
        // ----------------------------------------------------------------------------------------------------

        public override void ValidateMaterial(Material material)
        {
            DisableMotionVectorsPass(material);
        }
    }
}