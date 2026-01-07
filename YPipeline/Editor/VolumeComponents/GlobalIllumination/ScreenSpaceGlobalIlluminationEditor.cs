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
        private SerializedDataParameter m_HBILRadius;
        private SerializedDataParameter m_HBILDirectionCount;
        private SerializedDataParameter m_HBILStepCount;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<ScreenSpaceGlobalIllumination>(serializedObject);
            
            m_Mode = Unpack(o.Find(x => x.mode));
            
            // HBIL
            m_HBILIntensity = Unpack(o.Find(x => x.hbilIntensity));
            m_HBILRadius = Unpack(o.Find(x => x.hbilRadius));
            m_HBILDirectionCount = Unpack(o.Find(x => x.hbilDirectionCount));
            m_HBILStepCount = Unpack(o.Find(x => x.hbilStepCount));
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
                    PropertyField(m_HBILIntensity, EditorGUIUtility.TrTextContent("Intensity"));
                    PropertyField(m_HBILRadius, EditorGUIUtility.TrTextContent("Radius"));
                    PropertyField(m_HBILDirectionCount, EditorGUIUtility.TrTextContent("Direction Count"));
                    PropertyField(m_HBILStepCount, EditorGUIUtility.TrTextContent("Step Count"));
                    break;
                case (int) SSGIMode.SSGI:
                    EditorGUILayout.HelpBox("SSGI 暂未实现，使用的仍然是 HBIL。", MessageType.Warning);
                    break;
            }
        }
    }
}