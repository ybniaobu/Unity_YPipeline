using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    [Serializable]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public class YPipelineEditorResources : IRenderPipelineResources
    {
        [SerializeField][HideInInspector] private int m_Version = 1;
        public int version => m_Version;
        
        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => false;
        
        // ----------------------------------------------------------------------------------------------------
        // Materials
        // ----------------------------------------------------------------------------------------------------
        
        #region Materials
        
        [SerializeField] [ResourcePath("YPipeline/PipelineResources/DefaultMaterials/StandardPBR.mat")]
        private Material m_DefaultPBRMaterial;
        public Material DefaultPBRMaterial
        {
            get => m_DefaultPBRMaterial;
            set => this.SetValueAndNotify(ref m_DefaultPBRMaterial, value, nameof(m_DefaultPBRMaterial));
        }

        #endregion
    }
}