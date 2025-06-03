using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public class LightingDebugSettings
    {
        private const string k_PanelName = "Lighting";
        public static readonly int k_TilesDebugOpacityID = Shader.PropertyToID("_TilesDebugOpacity");
        
        public bool showLightTiles;
        public float tileOpacity = 0.5f;
        
        private const string k_LightCullingDebug = "Hidden/YPipeline/Debug/LightCullingDebug";
        public Material lightCullingDebugMaterial;
        
        
        public void Initialize()
        {
            lightCullingDebugMaterial = CoreUtils.CreateEngineMaterial(k_LightCullingDebug);
            
            DebugManager.instance.GetPanel(k_PanelName, true, 0).children.Add(
                new DebugUI.RuntimeDebugShadersMessageBox(),
                new DebugUI.Foldout
                {
                    displayName = "Light Culling",
                    opened = false,
                    children =
                    {
                        new DebugUI.BoolField
                        {
                            displayName = "Show Light Tiles",
                            tooltip = "Whether the light tiles overlay is shown.",
                            getter = () => showLightTiles,
                            setter = value => showLightTiles = value
                        },
                        new DebugUI.FloatField
                        {
                            displayName = "Tile Opacity",
                            tooltip = "Opacity of the overlay.",
                            min = () => 0f,
                            max = () => 1f,
                            getter = () => tileOpacity,
                            setter = value => tileOpacity = value
                        },
                    }
                }
            );
            
            
        }
        
        public void OnDispose()
        {
            CoreUtils.Destroy(lightCullingDebugMaterial);
            DebugManager.instance.RemovePanel(k_PanelName);
        }
    }
#endif
}