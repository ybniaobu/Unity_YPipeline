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
    }
}