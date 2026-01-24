using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.SceneManagement;

namespace YPipeline.Editor
{
    public partial class YPipelineReflectionProbeEditor
    {
        private static Mesh m_Sphere;
        private static Material m_PreviewMaterial;
        
        public void OnSceneGUI()
        {
            
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Tool Bar
        // ----------------------------------------------------------------------------------------------------
        
        [DrawGizmo(GizmoType.NonSelected | GizmoType.Active)]
        private static void DrawReflectionProbeGizmoUnSelected(ReflectionProbe probe, GizmoType gizmoType)
        {
            // 绘制 Reflection Probe 影响范围
            Color oldColor = Gizmos.color;
            Gizmos.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            Matrix4x4 probeMatrix = Matrix4x4.TRS(probe.transform.position, Quaternion.identity, Vector3.one);
            Gizmos.matrix = probeMatrix;
            Gizmos.DrawWireCube(probe.center, probe.size);
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = oldColor;
        }
        
        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        private static void DrawReflectionProbeGizmoSelected(ReflectionProbe probe, GizmoType gizmoType)
        {
            // 绘制 Reflection Probe 影响范围
            Color oldColor = Gizmos.color;
            Gizmos.color = new Color(0.75f, 0.5f, 0.25f, 1.0f);
            Matrix4x4 probeMatrix = Matrix4x4.TRS(probe.transform.position, Quaternion.identity, Vector3.one);
            Gizmos.matrix = probeMatrix;
            Gizmos.DrawWireCube(probe.center, probe.size);
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = oldColor;
            
            // 绘制 Reflection Probe 在 Gizmo 的预览
            if (m_Sphere == null) m_Sphere = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            if (m_PreviewMaterial == null) m_PreviewMaterial = new Material(Shader.Find("Hidden/Preview/ReflectionProbePreview"));
            
            m_PreviewMaterial.SetTexture("_Cubemap", probe.texture);
            m_PreviewMaterial.SetVector("_Cubemap_HDR", probe.textureHDRDecodeValues);
            m_PreviewMaterial.SetPass(0);
            Graphics.DrawMeshNow(m_Sphere, probeMatrix);
        }
    }
}