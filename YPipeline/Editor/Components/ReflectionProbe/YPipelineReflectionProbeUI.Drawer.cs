using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Rendering;

namespace YPipeline.Editor
{
    using CED = CoreEditorDrawer<SerializedYPipelineReflectionProbe>;
    
    public static partial class YPipelineReflectionProbeUI
    {
        public enum Expandable
        {
            Runtime = 1 << 0,
            Capture = 1 << 1,
        }
        
        private static readonly ExpandedState<Expandable, ReflectionProbe> k_ExpandedState = new ExpandedState<Expandable, ReflectionProbe>(~-1, "YPipeline");
        
        public static void Draw(SerializedYPipelineReflectionProbe serialized, UnityEditor.Editor owner)
        {
            DrawModeSettings(serialized, owner);
            
            CED.Group( 
                CED.FoldoutGroup(k_RuntimeSettingsHeader, Expandable.Runtime, k_ExpandedState, DrawRuntimeSettings),
                CED.FoldoutGroup(k_CaptureSettingsHeader, Expandable.Capture, k_ExpandedState, DrawCaptureSettings),
                CED.Group(DrawBakeButton)
                
                
            
            ).Draw(serialized, owner);
        }
        

        private static void DrawModeSettings(SerializedYPipelineReflectionProbe serialized, UnityEditor.Editor owner)
        {
            EditorGUILayout.IntPopup(serialized.mode, k_ModeContents, k_ModeValues, k_ModeText);
            
            switch ((ReflectionProbeMode) serialized.mode.intValue)
            {
                case ReflectionProbeMode.Baked:
                    break;
                case ReflectionProbeMode.Custom:
                    Rect lineRect = EditorGUILayout.GetControlRect(true, 64);
                    EditorGUI.BeginChangeCheck();
                    var customTexture = EditorGUI.ObjectField(lineRect, k_CustomCubemapText, serialized.customBakedTexture.objectReferenceValue, typeof(Cubemap), false);
                    if (EditorGUI.EndChangeCheck()) serialized.customBakedTexture.objectReferenceValue = customTexture;
                    break;
                case ReflectionProbeMode.Realtime:
                    EditorGUILayout.PropertyField(serialized.refreshMode, k_RefreshModeText);
                    EditorGUILayout.PropertyField(serialized.timeSlicingMode, k_TimeSlicingText);
                    break;
            }
            
            EditorGUILayout.Space();
        }

        private static void DrawRuntimeSettings(SerializedYPipelineReflectionProbe serialized, UnityEditor.Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.importance, k_ImportanceText);
            EditorGUILayout.PropertyField(serialized.intensityMultiplier, k_IntensityText);
            EditorGUILayout.PropertyField(serialized.boxProjection, k_BoxProjectionText);
            EditorGUILayout.PropertyField(serialized.blendDistance, k_BlendDistanceText);
            EditorGUILayout.PropertyField(serialized.boxSize, k_BoxSizeText);
        }

        private static void DrawCaptureSettings(SerializedYPipelineReflectionProbe serialized, UnityEditor.Editor owner)
        {
            EditorGUILayout.IntPopup(serialized.resolution, k_ResolutionContents, k_ResolutionValues, k_ResolutionText, GUILayout.MinWidth(40));
            EditorGUILayout.PropertyField(serialized.renderDynamicObjects, k_RenderDynamicObjects);
            EditorGUILayout.PropertyField(serialized.HDR, k_HDRText);
            EditorGUILayout.PropertyField(serialized.shadowDistance, k_ShadowDistanceText);
            EditorGUILayout.IntPopup(serialized.clearFlags, k_ClearFlagsContents, k_ClearFlagsValues, k_ClearFlagsText);

            if (serialized.clearFlags.intValue == 2)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serialized.backgroundColor, k_BackgroundColorText);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.PropertyField(serialized.cullingMask, k_CullingMaskText);
            EditorGUILayout.PropertyField(serialized.useOcclusionCulling, k_UseOcclusionCulling);
            
            CoreEditorUtils.DrawMultipleFields(k_ClippingPlanesLabel, new[] { serialized.nearClip, serialized.farClip }, k_NearAndFarLabels);
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Reflection Probe Bake Related
        // ----------------------------------------------------------------------------------------------------

        private static void DrawBakeButton(SerializedYPipelineReflectionProbe serialized, UnityEditor.Editor owner)
        {
            ReflectionProbe probe = owner.target as ReflectionProbe;
            if (probe == null) return;
            
            GUILayout.BeginHorizontal();

            switch (serialized.mode.intValue)
            {
                case (int) ReflectionProbeMode.Baked:
                    using (new EditorGUI.DisabledScope(!probe.enabled))
                    {
                        if (GUILayout.Button(k_BakeButtonLabel, GUILayout.Height(32)))
                        {
                            // EditorGUI.BeginChangeCheck();
                            var texture = BakeReflectionProbe(probe);
                            // Debug.Log(serialized.bakedTexture.objectReferenceValue == null);
                            // serialized.bakedTexture.objectReferenceValue = texture;
                            // EditorGUI.EndChangeCheck();
                            // serialized.ApplyModifiedProperties();
                            GUIUtility.ExitGUI();
                        }
                    }
                    break;
                case (int) ReflectionProbeMode.Custom:
                    using (new EditorGUI.DisabledScope(!probe.enabled))
                    {
                        if (GUILayout.Button(k_BakeButtonLabel, GUILayout.Height(32)))
                        {
                            BakeCustomReflectionProbe(probe);
                            GUIUtility.ExitGUI();
                        }
                    }
                    break;
                case (int) ReflectionProbeMode.Realtime:
                    break;
            }
            GUILayout.EndHorizontal();
        }

        private static Object BakeReflectionProbe(ReflectionProbe probe)
        {
            string path = AssetDatabase.GetAssetPath(probe.bakedTexture);
            string extension = probe.hdr ? "exr" : "png";

            if (string.IsNullOrEmpty(path) || Path.GetExtension(path) != "." + extension)
            {
                // 若 scene 文件夹不存在，则创建文件夹
                string scenePath = Path.ChangeExtension(SceneManager.GetActiveScene().path, null);
                if (string.IsNullOrEmpty(scenePath)) scenePath = "Assets";
                else if (Directory.Exists(scenePath) == false) Directory.CreateDirectory(scenePath);
                
                // 查找 ReflectionProbe-X
                HashSet<int> existingNumbers = new HashSet<int>();
                foreach (string filePath in Directory.GetFiles(scenePath, "ReflectionProbe-*"))
                {
                    if (filePath.EndsWith(".meta")) continue;
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    
                    string numberPart = fileName.Substring("ReflectionProbe-".Length);
                    if (int.TryParse(numberPart, out int number))
                    {
                        existingNumbers.Add(number);
                    }
                }
                
                int firstAvailableNumber = 0;
                while (existingNumbers.Contains(firstAvailableNumber))
                {
                    firstAvailableNumber++;
                }
                
                path = Path.Combine(scenePath, $"ReflectionProbe-{firstAvailableNumber}" + "." + extension);
            }
            
            EditorUtility.DisplayProgressBar("Reflection Probes", "Baking " + path, 0.5f);
            if (!Lightmapping.BakeReflectionProbe(probe, path)) Debug.LogError("Failed to bake reflection probe to " + path);
            EditorUtility.ClearProgressBar();

            return AssetDatabase.LoadAssetAtPath(path, typeof(Object));
        }
        
        private static void BakeCustomReflectionProbe(ReflectionProbe probe)
        {
            string path = AssetDatabase.GetAssetPath(probe.customBakedTexture);
            string extension = probe.hdr ? "exr" : "png";

            if (string.IsNullOrEmpty(path) || Path.GetExtension(path) != "." + extension)
            {
                // 若 scene 文件夹不存在，则创建文件夹
                string scenePath = Path.ChangeExtension(SceneManager.GetActiveScene().path, null);
                if (string.IsNullOrEmpty(scenePath)) scenePath = "Assets";
                else if (Directory.Exists(scenePath) == false) Directory.CreateDirectory(scenePath);
                
                // 文件名
                string fileName = probe.name + "." + extension;
                fileName = Path.GetFileNameWithoutExtension(AssetDatabase.GenerateUniqueAssetPath(Path.Combine(scenePath, fileName)));
                path = EditorUtility.SaveFilePanelInProject("Save Reflection Probe's Cubemap.", fileName, extension, string.Empty, scenePath);
                if (string.IsNullOrEmpty(path)) return;
                
                // 检查文件路径是否和其他 ReflectionProbe 的 cubemap 冲突
                ReflectionProbe collidingProbe;
                if (IsCustomReflectionProbeCollidingWithOtherProbes(path, probe, out collidingProbe))
                {
                    string message = $"'{path}' path is used by the game object '{collidingProbe.name}', do you really want to overwrite it?";
                    if (!EditorUtility.DisplayDialog("Cubemap is used by other reflection probe", message, "Yes", "No")) return;
                }
            }
            
            EditorUtility.DisplayProgressBar("Reflection Probes", "Baking " + path, 0.5f);
            if (!Lightmapping.BakeReflectionProbe(probe, path)) Debug.LogError("Failed to bake reflection probe to " + path);
            EditorUtility.ClearProgressBar();
        }
        
        private static bool IsCustomReflectionProbeCollidingWithOtherProbes(string targetPath, ReflectionProbe targetProbe, out ReflectionProbe collidingProbe)
        {
            ReflectionProbe[] probes = Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.InstanceID).ToArray();
            collidingProbe = null;
            foreach (var probe in probes)
            {
                if (probe == targetProbe || probe.customBakedTexture == null) continue;
                string path = AssetDatabase.GetAssetPath(probe.customBakedTexture);
                if (path == targetPath)
                {
                    collidingProbe = probe;
                    return true;
                }
            }
            return false;
        }
    }
}