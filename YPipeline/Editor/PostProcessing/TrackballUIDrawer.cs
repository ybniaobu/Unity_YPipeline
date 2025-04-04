﻿using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEditor;
using UnityEditor.Rendering;

namespace YPipeline.Editor
{
    public sealed class TrackballUIDrawer
    {
        private static readonly int s_ThumbHash = "colorWheelThumb".GetHashCode();
        private static GUIStyle s_WheelThumb;
        private static Vector2 s_WheelThumbSize;
        private static Material s_Material;
        private static bool s_MaterialConfigured;
        
        private Func<Vector4, Vector3> m_ComputeFunc;
        private bool m_ResetState;
        private Vector2 m_CursorPos;

        public void OnGUI(SerializedProperty property, [CanBeNull] SerializedProperty overrideState, GUIContent title, Func<Vector4, Vector3> computeFunc)
        {
            if (property.propertyType != SerializedPropertyType.Vector4)
            {
                Debug.LogWarning("TrackballUIDrawer requires a Vector4 property");
                return;
            }

            if (!s_MaterialConfigured)
            {
                // Initialization of materials with Shader.Find from static constructors is not allowed.
                s_Material = new Material(Shader.Find("Hidden/Editor/Trackball")) { hideFlags = HideFlags.HideAndDontSave };
                s_MaterialConfigured = true;
            }

            m_ComputeFunc = computeFunc;
            var value = property.vector4Value;

            using (new EditorGUILayout.VerticalScope())
            {
                bool isOverridden = overrideState?.boolValue ?? true;
                using (new EditorGUI.DisabledScope(!isOverridden))
                    DrawWheel(ref value, isOverridden);

                DrawLabelAndOverride(title, overrideState);
            }

            if (m_ResetState)
            {
                value = new Vector4(1f, 1f, 1f, 0f);
                m_ResetState = false;
            }

            property.vector4Value = value;
        }

        void DrawWheel(ref Vector4 value, bool overrideState)
        {
            var wheelRect = GUILayoutUtility.GetAspectRect(1f);
            float size = wheelRect.width;
            float hsize = size / 2f;
            float radius = 0.38f * size;

            Vector3 hsv;
            Color.RGBToHSV(value, out hsv.x, out hsv.y, out hsv.z);
            float offset = value.w;

            // Thumb
            var thumbPos = Vector2.zero;
            float theta = hsv.x * (Mathf.PI * 2f);
            thumbPos.x = Mathf.Cos(theta + (Mathf.PI / 2f));
            thumbPos.y = Mathf.Sin(theta - (Mathf.PI / 2f));
            thumbPos *= hsv.y * radius;

            // Draw the wheel
            if (Event.current.type == EventType.Repaint)
            {
                // Style init
                if (s_WheelThumb == null)
                {
                    s_WheelThumb = new GUIStyle("ColorPicker2DThumb");
                    s_WheelThumbSize = new Vector2(
                        !Mathf.Approximately(s_WheelThumb.fixedWidth, 0f) ? s_WheelThumb.fixedWidth : s_WheelThumb.padding.horizontal,
                        !Mathf.Approximately(s_WheelThumb.fixedHeight, 0f) ? s_WheelThumb.fixedHeight : s_WheelThumb.padding.vertical
                    );
                }

                // Retina support
                float scale = EditorGUIUtility.pixelsPerPoint;

                // Wheel texture
                var oldRT = RenderTexture.active;
                var rt = RenderTexture.GetTemporary((int)(size * scale), (int)(size * scale), 0, GraphicsFormat.R8G8B8A8_SRGB);
                s_Material.SetFloat("_Offset", offset);
                s_Material.SetFloat("_DisabledState", overrideState && GUI.enabled ? 1f : 0.5f);
                s_Material.SetVector("_Resolution", new Vector2(size * scale, size * scale / 2f));
                Graphics.Blit(null, rt, s_Material, EditorGUIUtility.isProSkin ? 0 : 1);
                RenderTexture.active = oldRT;

                GUI.DrawTexture(wheelRect, rt);
                RenderTexture.ReleaseTemporary(rt);

                var thumbSize = s_WheelThumbSize;
                var thumbSizeH = thumbSize / 2f;
                s_WheelThumb.Draw(new Rect(wheelRect.x + hsize + thumbPos.x - thumbSizeH.x, wheelRect.y + hsize + thumbPos.y - thumbSizeH.y, thumbSize.x, thumbSize.y), false, false, false, false);
            }

            // Input
            var bounds = wheelRect;
            bounds.x += hsize - radius;
            bounds.y += hsize - radius;
            bounds.width = bounds.height = radius * 2f;
            hsv = GetInput(bounds, hsv, thumbPos, radius);


            Vector3Int displayHSV = new Vector3Int(Mathf.RoundToInt(hsv.x * 360), Mathf.RoundToInt(hsv.y * 100), 100);
            bool displayInputFields = EditorGUIUtility.currentViewWidth > 600;
            if (displayInputFields)
            {
                var valuesRect = GUILayoutUtility.GetRect(1f, 17f);
                valuesRect.width /= 5f;
                float textOff = valuesRect.width * 0.2f;
                EditorGUI.LabelField(valuesRect, "Y");
                valuesRect.x += textOff;
                offset = EditorGUI.DelayedFloatField(valuesRect, offset);
                offset = Mathf.Clamp(offset, -1.0f, 1.0f);
                valuesRect.x += valuesRect.width + valuesRect.width * 0.05f;
                EditorGUI.LabelField(valuesRect, "H");
                valuesRect.x += textOff;
                displayHSV.x = EditorGUI.DelayedIntField(valuesRect, displayHSV.x);
                hsv.x = displayHSV.x / 360.0f;
                valuesRect.x += valuesRect.width + valuesRect.width * 0.05f;
                EditorGUI.LabelField(valuesRect, "S");
                valuesRect.x += textOff;
                displayHSV.y = EditorGUI.DelayedIntField(valuesRect, displayHSV.y);
                displayHSV.y = Mathf.Clamp(displayHSV.y, 0, 100);
                hsv.y = displayHSV.y / 100.0f;
                valuesRect.x += valuesRect.width + valuesRect.width * 0.05f;
                EditorGUI.LabelField(valuesRect, "V");
                valuesRect.x += textOff;
                GUI.enabled = false;
                EditorGUI.IntField(valuesRect, 100);
                GUI.enabled = true;
            }


            value = Color.HSVToRGB(hsv.x, hsv.y, 1f);
            value.w = offset;

            // Offset
            var sliderRect = GUILayoutUtility.GetRect(1f, 17f);
            float padding = sliderRect.width * 0.05f; // 5% padding
            sliderRect.xMin += padding;
            sliderRect.xMax -= padding;
            value.w = GUI.HorizontalSlider(sliderRect, value.w, -1f, 1f);

            if (m_ComputeFunc == null)
                return;

            // Values
            var displayValue = m_ComputeFunc(value);
            using (new EditorGUI.DisabledGroupScope(true))
            {
                var valuesRect = GUILayoutUtility.GetRect(1f, 17f);
                valuesRect.width /= (displayInputFields ? 4f : 3.0f);
                if (displayInputFields)
                {
                    GUI.Label(valuesRect, "RGB Value:", EditorStyles.centeredGreyMiniLabel);
                    valuesRect.x += valuesRect.width;
                }
                GUI.Label(valuesRect, displayValue.x.ToString("F2"), EditorStyles.centeredGreyMiniLabel);
                valuesRect.x += valuesRect.width;
                GUI.Label(valuesRect, displayValue.y.ToString("F2"), EditorStyles.centeredGreyMiniLabel);
                valuesRect.x += valuesRect.width;
                GUI.Label(valuesRect, displayValue.z.ToString("F2"), EditorStyles.centeredGreyMiniLabel);
                valuesRect.x += valuesRect.width;
            }
        }

        void DrawLabelAndOverride(GUIContent title, SerializedProperty overrideState)
        {
            // Title
            var areaRect = GUILayoutUtility.GetRect(1f, 17f);
            var labelSize = EditorStyles.miniLabel.CalcSize(title);
            var labelRect = new Rect(areaRect.x + areaRect.width / 2 - labelSize.x / 2, areaRect.y, labelSize.x, labelSize.y);
            GUI.Label(labelRect, title, EditorStyles.miniLabel);

            // Override checkbox
            if (overrideState != null)
            {
                var overrideRect = new Rect(labelRect.x - 17, labelRect.y + 3, 17f, 17f);
                overrideState.boolValue = GUI.Toggle(overrideRect, overrideState.boolValue,
                    EditorGUIUtility.TrTextContent("", "Override this setting for this volume."),
                    CoreEditorStyles.smallTickbox);
            }
        }

        Vector3 GetInput(Rect bounds, Vector3 hsv, Vector2 thumbPos, float radius)
        {
            var e = Event.current;
            var id = GUIUtility.GetControlID(s_ThumbHash, FocusType.Passive, bounds);
            var mousePos = e.mousePosition;

            if (e.type == EventType.MouseDown && GUIUtility.hotControl == 0 && bounds.Contains(mousePos))
            {
                if (e.button == 0)
                {
                    var center = new Vector2(bounds.x + radius, bounds.y + radius);
                    float dist = Vector2.Distance(center, mousePos);

                    if (dist <= radius)
                    {
                        e.Use();
                        m_CursorPos = new Vector2(thumbPos.x + radius, thumbPos.y + radius);
                        GUIUtility.hotControl = id;
                        GUI.changed = true;
                    }
                }
                else if (e.button == 1)
                {
                    e.Use();
                    GUI.changed = true;
                    m_ResetState = true;
                }
            }
            else if (e.type == EventType.MouseDrag && e.button == 0 && GUIUtility.hotControl == id)
            {
                e.Use();
                GUI.changed = true;
                m_CursorPos += e.delta * 0.4f; // Sensitivity
                GetWheelHueSaturation(m_CursorPos.x, m_CursorPos.y, radius, out hsv.x, out hsv.y);
            }
            else if (e.rawType == EventType.MouseUp && e.button == 0 && GUIUtility.hotControl == id)
            {
                e.Use();
                GUIUtility.hotControl = 0;
            }

            return hsv;
        }

        void GetWheelHueSaturation(float x, float y, float radius, out float hue, out float saturation)
        {
            float dx = (x - radius) / radius;
            float dy = (y - radius) / radius;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            hue = Mathf.Atan2(dx, -dy);
            hue = 1f - ((hue > 0) ? hue : (Mathf.PI * 2f) + hue) / (Mathf.PI * 2f);
            saturation = Mathf.Clamp01(d);
        }
    }
}