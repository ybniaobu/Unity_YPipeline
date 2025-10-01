using UnityEditor;
using UnityEditor.Rendering;

namespace YPipeline.Editor
{
    [CustomEditor(typeof(AmbientOcclusion))]
    public class AmbientOcclusionEditor : VolumeComponentEditor
    {
        private SerializedDataParameter m_AmbientOcclusionMode;
        private SerializedDataParameter m_HalfResolution;
        
        // SSAO
        private SerializedDataParameter m_SSAOIntensity;
        private SerializedDataParameter m_SampleCount;
        private SerializedDataParameter m_SSAORadius;
        
        // HBAO
        private SerializedDataParameter m_HBAOIntensity;
        private SerializedDataParameter m_HBAORadius;
        private SerializedDataParameter m_HBAODirectionCount;
        private SerializedDataParameter m_HBAOStepCount;
        
        // GTAO
        private SerializedDataParameter m_GTAOIntensity;
        private SerializedDataParameter m_GTAORadius;
        private SerializedDataParameter m_GTAODirectionCount;
        private SerializedDataParameter m_GTAOStepCount;
        
        // Spatial Filter
        private SerializedDataParameter m_EnableSpatialFilter;
        private SerializedDataParameter m_KernelRadius;
        private SerializedDataParameter m_SpatialSigma;
        private SerializedDataParameter m_DepthSigma;
        
        // Temporal Filter
        private SerializedDataParameter m_EnableTemporalFilter;
        private SerializedDataParameter m_CriticalValue;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<AmbientOcclusion>(serializedObject);
            
            m_AmbientOcclusionMode = Unpack(o.Find(x => x.ambientOcclusionMode));
            m_HalfResolution = Unpack(o.Find(x => x.halfResolution));
            
            // SSAO
            m_SSAOIntensity = Unpack(o.Find(x => x.ssaoIntensity));
            m_SampleCount = Unpack(o.Find(x => x.sampleCount));
            m_SSAORadius = Unpack(o.Find(x => x.ssaoRadius));
            
            // HBAO
            m_HBAOIntensity = Unpack(o.Find(x => x.hbaoIntensity));
            m_HBAORadius = Unpack(o.Find(x => x.hbaoRadius));
            m_HBAODirectionCount = Unpack(o.Find(x => x.hbaoDirectionCount));
            m_HBAOStepCount = Unpack(o.Find(x => x.hbaoStepCount));
            
            // GTAO
            m_GTAOIntensity = Unpack(o.Find(x => x.gtaoIntensity));
            m_GTAORadius = Unpack(o.Find(x => x.gtaoRadius));
            m_GTAODirectionCount = Unpack(o.Find(x => x.gtaoDirectionCount));
            m_GTAOStepCount = Unpack(o.Find(x => x.gtaoStepCount));
            
            // Spatial Filter
            m_EnableSpatialFilter = Unpack(o.Find(x => x.enableSpatialFilter));
            m_KernelRadius = Unpack(o.Find(x => x.kernelRadius));
            m_SpatialSigma = Unpack(o.Find(x => x.spatialSigma));
            m_DepthSigma = Unpack(o.Find(x => x.depthSigma));
            
            // Temporal Filter
            m_EnableTemporalFilter = Unpack(o.Find(x => x.enableTemporalFilter));
            m_CriticalValue = Unpack(o.Find(x => x.criticalValue));
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Screen Space Ambient Occlusion", EditorStyles.boldLabel);
            
            PropertyField(m_AmbientOcclusionMode);
            PropertyField(m_HalfResolution);

            switch (m_AmbientOcclusionMode.value.enumValueIndex)
            {
                case (int) AmbientOcclusionMode.None:
                    break;
                case (int) AmbientOcclusionMode.SSAO:
                    PropertyField(m_SSAOIntensity, EditorGUIUtility.TrTextContent("Intensity"));
                    PropertyField(m_SampleCount);
                    PropertyField(m_SSAORadius, EditorGUIUtility.TrTextContent("Radius"));
                    break;
                case (int) AmbientOcclusionMode.HBAO:
                    PropertyField(m_HBAOIntensity, EditorGUIUtility.TrTextContent("Intensity"));
                    PropertyField(m_HBAORadius, EditorGUIUtility.TrTextContent("Radius"));
                    PropertyField(m_HBAODirectionCount, EditorGUIUtility.TrTextContent("Direction Count"));
                    PropertyField(m_HBAOStepCount, EditorGUIUtility.TrTextContent("Step Count"));
                    break;
                case (int) AmbientOcclusionMode.GTAO:
                    PropertyField(m_GTAOIntensity, EditorGUIUtility.TrTextContent("Intensity"));
                    PropertyField(m_GTAORadius, EditorGUIUtility.TrTextContent("Radius"));
                    PropertyField(m_GTAODirectionCount, EditorGUIUtility.TrTextContent("Direction Count"));
                    PropertyField(m_GTAOStepCount, EditorGUIUtility.TrTextContent("Step Count"));
                    break;
                default:
                    break;
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Spatial Filter - Bilateral Blur", EditorStyles.boldLabel);
            
            PropertyField(m_EnableSpatialFilter, EditorGUIUtility.TrTextContent("Enable"));
            PropertyField(m_KernelRadius);
            PropertyField(m_SpatialSigma);
            PropertyField(m_DepthSigma);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Temporal Filter", EditorStyles.boldLabel);
            
            PropertyField(m_EnableTemporalFilter, EditorGUIUtility.TrTextContent("Enable"));
            PropertyField(m_CriticalValue);
        }
    }
}