using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline.Editor
{
    public class UnlitShaderGUI : YPipelineShaderGUI
    {
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            base.OnGUI(materialEditor, properties);
            UnityEmissionProperty();
        }
        
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

        public override void ValidateMaterial(Material material)
        {
            base.ValidateMaterial(material);
            DisableMotionVectorsPass(material);
        }


        private const string k_MotionVectorPassName = "MotionVectors";
        private void DisableMotionVectorsPass(Material material)
        {
            if (material.GetShaderPassEnabled(k_MotionVectorPassName))
            {
                material.SetShaderPassEnabled(k_MotionVectorPassName, false);
            }
        }
    }
}