using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace YPipeline.Editor
{
    [CustomEditor(typeof(Debugger))]
    public class DebuggerEditor : UnityEditor.Editor
    {
        private Debugger Debugger => target as Debugger;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Debug Log", GUILayout.Height(20)))
            {
                Debugger.DebugEntry();
                GUIUtility.ExitGUI();
            }
            GUILayout.EndHorizontal();
        }
    }
}