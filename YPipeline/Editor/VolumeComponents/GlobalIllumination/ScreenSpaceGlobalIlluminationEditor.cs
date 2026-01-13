using UnityEditor;
using UnityEditor.Rendering;

namespace YPipeline.Editor
{
    [CustomEditor(typeof(ScreenSpaceGlobalIllumination))]
    public class ScreenSpaceGlobalIlluminationEditor : VolumeComponentEditor
    {
        private SerializedDataParameter m_Mode;
        private SerializedDataParameter m_HalfResolution;
        
        // HBIL
        private SerializedDataParameter m_HBILIntensity;
        private SerializedDataParameter m_ConvergeDegree;
        private SerializedDataParameter m_DirectionCount;
        private SerializedDataParameter m_StepCount;
        
        // Fallback
        private SerializedDataParameter m_FallbackMode;
        private SerializedDataParameter m_FallbackIntensity;
        private SerializedDataParameter m_FarFieldAO;
        
        // Bilateral Denoise
        private SerializedDataParameter m_EnableBilateralDenoise;
        private SerializedDataParameter m_KernelRadius;
        private SerializedDataParameter m_SpatialSigma;
        private SerializedDataParameter m_DepthSigma;
        
        // Temporal Denoise
        private SerializedDataParameter m_EnableTemporalDenoise;
        private SerializedDataParameter m_CriticalValue;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<ScreenSpaceGlobalIllumination>(serializedObject);
            
            m_Mode = Unpack(o.Find(x => x.mode));
            m_HalfResolution = Unpack(o.Find(x => x.halfResolution));
            
            // HBIL
            m_HBILIntensity = Unpack(o.Find(x => x.hbilIntensity));
            m_ConvergeDegree = Unpack(o.Find(x => x.convergeDegree));
            m_DirectionCount = Unpack(o.Find(x => x.directionCount));
            m_StepCount = Unpack(o.Find(x => x.stepCount));
            
            // Fallback
            m_FallbackMode = Unpack(o.Find(x => x.fallbackMode));
            m_FallbackIntensity = Unpack(o.Find(x => x.fallbackIntensity));
            m_FarFieldAO = Unpack(o.Find(x => x.farFieldAO));
            
            // Bilateral Denoise
            m_EnableBilateralDenoise = Unpack(o.Find(x => x.enableBilateralDenoise));
            m_KernelRadius = Unpack(o.Find(x => x.kernelRadius));
            m_SpatialSigma = Unpack(o.Find(x => x.spatialSigma));
            m_DepthSigma = Unpack(o.Find(x => x.depthSigma));
            
            // Temporal Denoise
            m_EnableTemporalDenoise = Unpack(o.Find(x => x.enableTemporalDenoise));
            m_CriticalValue = Unpack(o.Find(x => x.criticalValue));
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Indirect Diffuse Lighting", EditorStyles.boldLabel);
            
            PropertyField(m_Mode);

            switch (m_Mode.value.enumValueIndex)
            {
                case (int) SSGIMode.None:
                    break;
                case (int) SSGIMode.HBIL:
                    PropertyField(m_HalfResolution);
                    PropertyField(m_HBILIntensity, EditorGUIUtility.TrTextContent("Near Field Intensity"));
                    PropertyField(m_ConvergeDegree);
                    PropertyField(m_DirectionCount);
                    PropertyField(m_StepCount);
                    break;
                case (int) SSGIMode.SSGI:
                    EditorGUILayout.HelpBox("暂未实现 SSGI", MessageType.Warning);
                    break;
            }
            
            if (m_Mode.value.enumValueIndex == (int) SSGIMode.None) return;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Fallback", EditorStyles.boldLabel);
            
            PropertyField(m_FallbackMode);
            PropertyField(m_FallbackIntensity);
            PropertyField(m_FarFieldAO);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Denoise", EditorStyles.boldLabel);
            
            PropertyField(m_EnableBilateralDenoise);

            if (m_EnableBilateralDenoise.value.boolValue)
            {
                PropertyField(m_KernelRadius);
                PropertyField(m_SpatialSigma);
                PropertyField(m_DepthSigma);
            }
            
            PropertyField(m_EnableTemporalDenoise);

            if (m_EnableTemporalDenoise.value.boolValue)
            {
                PropertyField(m_CriticalValue);
            }
        }
    }
}