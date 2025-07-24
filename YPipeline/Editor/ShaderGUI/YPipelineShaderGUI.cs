using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline.Editor
{
    public abstract class YPipelineShaderGUI : ShaderGUI
    {
        protected MaterialEditor m_MaterialEditor;
        protected Object[] m_Materials;
        protected MaterialProperty[] m_Properties;
            
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            base.OnGUI(materialEditor, properties);
            m_MaterialEditor = materialEditor;
            m_Materials = materialEditor.targets;
            m_Properties = properties;
        }

        public override void ValidateMaterial(Material material)
        {
            base.ValidateMaterial(material);
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            base.AssignNewShaderToMaterial(material, oldShader, newShader);
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Motion Vector Related
        // ----------------------------------------------------------------------------------------------------
        
        private const string k_MotionVectorPassName = "MotionVectors";
        
        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        protected static void DisableMotionVectorsPass(Material material)
        {
            if (material.GetShaderPassEnabled(k_MotionVectorPassName))
            {
                material.SetShaderPassEnabled(k_MotionVectorPassName, false);
            }
        }
    }
}