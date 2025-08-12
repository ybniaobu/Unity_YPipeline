using UnityEditor;
using UnityEditor.Rendering;

namespace YPipeline.Editor
{
    [CustomEditor(typeof(AmbientOcclusion))]
    public class AmbientOcclusionEditor : VolumeComponentEditor
    {
        // Screen Space Ambient Occlusion
        private SerializedDataParameter m_AmbientOcclusionMode;
        private SerializedDataParameter m_HalfResolution;
        private SerializedDataParameter m_Intensity;
        private SerializedDataParameter m_SampleCount;
        private SerializedDataParameter m_Radius;
        private SerializedDataParameter m_ReflectionRate;
        
        // Spatial Filter
        private SerializedDataParameter m_EnableSpatialFilter;
        private SerializedDataParameter m_KernelRadius;
        private SerializedDataParameter m_Sigma;
        
        // Temporal Filter

        public override void OnEnable()
        {
            var o = new PropertyFetcher<AmbientOcclusion>(serializedObject);
            
            // Screen Space Ambient Occlusion
            m_AmbientOcclusionMode = Unpack(o.Find(x => x.ambientOcclusionMode));
            m_HalfResolution = Unpack(o.Find(x => x.halfResolution));
            m_Intensity = Unpack(o.Find(x => x.intensity));
            m_SampleCount = Unpack(o.Find(x => x.sampleCount));
            m_Radius = Unpack(o.Find(x => x.radius));
            m_ReflectionRate = Unpack(o.Find(x => x.reflectionRate));
            
            // Spatial Filter
            m_EnableSpatialFilter = Unpack(o.Find(x => x.enableSpatialFilter));
            m_KernelRadius = Unpack(o.Find(x => x.kernelRadius));
            m_Sigma = Unpack(o.Find(x => x.sigma));
            
            // Temporal Filter
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Screen Space Ambient Occlusion", EditorStyles.boldLabel);
            
            PropertyField(m_AmbientOcclusionMode);
            PropertyField(m_HalfResolution);
            PropertyField(m_Intensity);
            PropertyField(m_SampleCount);
            PropertyField(m_Radius);
            PropertyField(m_ReflectionRate);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Spatial Filter - Bilateral Blur", EditorStyles.boldLabel);
            
            PropertyField(m_EnableSpatialFilter, EditorGUIUtility.TrTextContent("Enable"));
            PropertyField(m_KernelRadius);
            PropertyField(m_Sigma);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Temporal Filter", EditorStyles.boldLabel);
        }
    }
}