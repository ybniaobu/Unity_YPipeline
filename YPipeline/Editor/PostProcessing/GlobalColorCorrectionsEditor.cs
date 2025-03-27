using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEditor;
using UnityEditor.Rendering;
using YPipeline;

namespace YPipeline.Editor
{
    [CustomEditor(typeof(GlobalColorCorrections))]
    public class GlobalColorCorrectionsEditor : VolumeComponentEditor
    {
        private SerializedDataParameter m_Temperature;
        private SerializedDataParameter m_Tint;
        private SerializedDataParameter m_ColorFilter;
        private SerializedDataParameter m_Hue;
        private SerializedDataParameter m_Exposure;
        private SerializedDataParameter m_Contrast;
        private SerializedDataParameter m_Saturation;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<GlobalColorCorrections>(serializedObject);

            m_Temperature = Unpack(o.Find(x => x.temperature));
            m_Tint = Unpack(o.Find(x => x.tint));
            m_ColorFilter = Unpack(o.Find(x => x.colorFilter));
            m_Hue = Unpack(o.Find(x => x.hue));
            m_Exposure = Unpack(o.Find(x => x.exposure));
            m_Contrast = Unpack(o.Find(x => x.contrast));
            m_Saturation = Unpack(o.Find(x => x.saturation));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Temperature);
            PropertyField(m_Tint);
            PropertyField(m_ColorFilter);
            PropertyField(m_Hue);
            PropertyField(m_Exposure);
            PropertyField(m_Contrast);
            PropertyField(m_Saturation);
        }
    }
}