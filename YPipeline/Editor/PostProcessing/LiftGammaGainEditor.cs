﻿using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using YPipeline;

namespace YPipeline.Editor
{
    [CustomEditor(typeof(LiftGammaGain))]
    public class LiftGammaGainEditor : VolumeComponentEditor
    {
        static class Styles
        {
            public static readonly GUIContent liftLabel = EditorGUIUtility.TrTextContent("Lift", "Use this to control and apply a hue to the dark tones. This has a more exaggerated effect on shadows.");
            public static readonly GUIContent gammaLabel = EditorGUIUtility.TrTextContent("Gamma", "Use this to control and apply a hue to the mid-range tones with a power function.");
            public static readonly GUIContent gainLabel = EditorGUIUtility.TrTextContent("Gain", "Use this to increase and apply a hue to the signal and make highlights brighter.");
        }

        private SerializedDataParameter m_Lift;
        private SerializedDataParameter m_Gamma;
        private SerializedDataParameter m_Gain;
        
        private readonly TrackballUIDrawer m_TrackballUIDrawer = new TrackballUIDrawer();
        
        public override void OnEnable()
        {
            var o = new PropertyFetcher<LiftGammaGain>(serializedObject);

            m_Lift = Unpack(o.Find(x => x.lift));
            m_Gamma = Unpack(o.Find(x => x.gamma));
            m_Gain = Unpack(o.Find(x => x.gain));
        }
        
        public override void OnInspectorGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                m_TrackballUIDrawer.OnGUI(m_Lift.value, enableOverrides ? m_Lift.overrideState : null, Styles.liftLabel, GetLiftValue);
                GUILayout.Space(4f);
                m_TrackballUIDrawer.OnGUI(m_Gamma.value, enableOverrides ? m_Gamma.overrideState : null, Styles.gammaLabel, GetLiftValue);
                GUILayout.Space(4f);
                m_TrackballUIDrawer.OnGUI(m_Gain.value, enableOverrides ? m_Gain.overrideState : null, Styles.gainLabel, GetLiftValue);
            }
        }
        
        private Vector3 GetLiftValue(Vector4 x) => new Vector3(x.x + x.w, x.y + x.w, x.z + x.w);
    }
}