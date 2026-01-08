using UnityEditor;
using UnityEditor.Rendering;

namespace YPipeline.Editor
{
    [CustomEditor(typeof(ScreenSpaceGlobalIllumination))]
    public class ScreenSpaceGlobalIlluminationEditor : VolumeComponentEditor
    {
        private SerializedDataParameter m_Mode;
        
        // HBIL
        private SerializedDataParameter m_HBILIntensity;
        private SerializedDataParameter m_ConvergeDegree;
        private SerializedDataParameter m_DirectionCount;
        private SerializedDataParameter m_StepCount;
        
        // Fallback
        private SerializedDataParameter m_FallbackMode;
        private SerializedDataParameter m_FallbackIntensity;
        private SerializedDataParameter m_UseBentNormal;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<ScreenSpaceGlobalIllumination>(serializedObject);
            
            m_Mode = Unpack(o.Find(x => x.mode));
            
            // HBIL
            m_HBILIntensity = Unpack(o.Find(x => x.hbilIntensity));
            m_ConvergeDegree = Unpack(o.Find(x => x.convergeDegree));
            m_DirectionCount = Unpack(o.Find(x => x.directionCount));
            m_StepCount = Unpack(o.Find(x => x.stepCount));
            
            // Fallback
            m_FallbackMode = Unpack(o.Find(x => x.fallbackMode));
            m_FallbackIntensity = Unpack(o.Find(x => x.fallbackIntensity));
            m_UseBentNormal = Unpack(o.Find(x => x.useBentNormal));
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Screen Space Diffuse Indirect Lighting", EditorStyles.boldLabel);
            
            PropertyField(m_Mode);

            switch (m_Mode.value.enumValueIndex)
            {
                case (int) SSGIMode.None:
                    break;
                case (int) SSGIMode.HBIL:
                    PropertyField(m_HBILIntensity, EditorGUIUtility.TrTextContent("Near Field Intensity"));
                    PropertyField(m_ConvergeDegree);
                    PropertyField(m_DirectionCount);
                    PropertyField(m_StepCount);
                    break;
                case (int) SSGIMode.SSGI:
                    EditorGUILayout.HelpBox("暂未实现 SSGI", MessageType.Warning);
                    break;
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Fallback", EditorStyles.boldLabel);
            
            PropertyField(m_FallbackMode);
            PropertyField(m_FallbackIntensity);
            PropertyField(m_UseBentNormal);
        }
    }
}