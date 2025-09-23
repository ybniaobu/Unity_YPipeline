using UnityEditor;
using UnityEditor.Rendering;

namespace YPipeline.Editor
{
    [CustomEditor(typeof(AmbientOcclusion))]
    public class AmbientOcclusionEditor : VolumeComponentEditor
    {
        private SerializedDataParameter m_AmbientOcclusionMode;
        private SerializedDataParameter m_HalfResolution;
        private SerializedDataParameter m_Intensity;
        
        // SSAO
        private SerializedDataParameter m_SampleCount;
        private SerializedDataParameter m_SSAORadius;
        
        // GTAO
        private SerializedDataParameter m_GTAORadius;
        private SerializedDataParameter m_DirectionCount;
        private SerializedDataParameter m_StepCount;
        
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
            m_Intensity = Unpack(o.Find(x => x.intensity));
            
            // SSAO
            m_SampleCount = Unpack(o.Find(x => x.sampleCount));
            m_SSAORadius = Unpack(o.Find(x => x.ssaoRadius));
            
            // GTAO
            m_GTAORadius = Unpack(o.Find(x => x.gtaoRadius));
            m_DirectionCount = Unpack(o.Find(x => x.directionCount));
            m_StepCount = Unpack(o.Find(x => x.stepCount));
            
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
            EditorGUILayout.HelpBox("如果开启了 SSGI，AO 会在 GI 中计算，下面参数都会被 GI 中的参数覆盖", MessageType.Info);
            EditorGUILayout.LabelField("Screen Space Ambient Occlusion", EditorStyles.boldLabel);
            
            PropertyField(m_AmbientOcclusionMode);
            PropertyField(m_HalfResolution);
            PropertyField(m_Intensity);

            switch (m_AmbientOcclusionMode.value.enumValueIndex)
            {
                case (int) AmbientOcclusionMode.None:
                    break;
                case (int) AmbientOcclusionMode.SSAO:
                    PropertyField(m_SampleCount);
                    PropertyField(m_SSAORadius, EditorGUIUtility.TrTextContent("Radius"));
                    break;
                case (int) AmbientOcclusionMode.GTAO:
                    PropertyField(m_GTAORadius, EditorGUIUtility.TrTextContent("Radius"));
                    PropertyField(m_DirectionCount);
                    PropertyField(m_StepCount);
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