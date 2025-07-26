using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline.Editor
{
    public abstract class YPipelineShaderGUI : ShaderGUI
    {
        protected bool m_FirstTimeApply = true;
        
        // ----------------------------------------------------------------------------------------------------
        // ShaderGUI Event Functions
        // ----------------------------------------------------------------------------------------------------
        
        protected MaterialEditor m_MaterialEditor;
        protected Object[] m_Materials;
        protected Material m_Material;
        protected MaterialProperty[] m_Properties;
        protected string[] m_Keywords;
            
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            base.OnGUI(materialEditor, properties);
            m_MaterialEditor = materialEditor;
            m_Materials = materialEditor.targets;
            m_Material = materialEditor.target as Material;
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
        // GUI Content 
        // ----------------------------------------------------------------------------------------------------

        protected static class Styles
        {
            public static readonly GUIContent k_BaseTex = new GUIContent("Albedo Texture",
                "Specifies the base color of the surface. If using Alpha Blending or Alpha Clipping, material uses the base texture’s & base color's alpha channel.");
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Find Properties
        // ----------------------------------------------------------------------------------------------------

        // Common Material Properties
        protected MaterialProperty m_BaseTexProperty;
        protected MaterialProperty m_BaseColorProperty;
        protected MaterialProperty m_EmissionTexProperty;
        protected MaterialProperty m_EmissionColorProperty;
        protected MaterialProperty m_AlphaClippingProperty;
        protected MaterialProperty m_AlphaCutoffProperty;
        
        public virtual void FindProperties(MaterialProperty[] properties)
        {
            var material = m_MaterialEditor?.target as Material;
            if (material == null) return;
            
            m_BaseTexProperty = FindProperty(YPipelineMaterialProperties.k_BaseTex, properties, false);
            m_BaseColorProperty = FindProperty(YPipelineMaterialProperties.k_BaseColor, properties, false);
            m_EmissionTexProperty = FindProperty(YPipelineMaterialProperties.k_EmissionTex, properties, false);
            m_EmissionColorProperty = FindProperty(YPipelineMaterialProperties.k_EmissionColor, properties, false);
            m_AlphaClippingProperty = FindProperty(YPipelineMaterialProperties.k_AlphaClipping, properties, false);
            m_AlphaCutoffProperty = FindProperty(YPipelineMaterialProperties.k_AlphaCutoff, properties, false);
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Draw Properties
        // ----------------------------------------------------------------------------------------------------
        
        public virtual void DrawBaseTexAndColor(Material material)
        {
            if (m_BaseTexProperty != null && m_BaseColorProperty != null)
            {
                m_MaterialEditor.TexturePropertySingleLine(Styles.k_BaseTex, m_BaseTexProperty, m_BaseColorProperty);
            }
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