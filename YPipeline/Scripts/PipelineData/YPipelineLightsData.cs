using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class YPipelineLightsData
    {
        private const int k_MaxDirectionalLightCount = 1;  // Only Support One Directional Light - Sunlight
        private const int k_MaxCascadeCount = 4;
        private const int k_MaxPunctualLightCount = 256;
        private const int k_MaxShadowingSpotLightCount = 64;
        private const int k_MaxShadowingPointLightCount = 12;
        
        public int sunLightCount;
        public int cascadeCount;
        public int shadowingSunLightCount;
        public int sunLightIndex; // store shadowing sun light visible light index
        public float sunLightNearPlaneOffset;
        public Vector4 sunLightColor;
        public Vector4 sunLightDirection;
        public Vector4 sunLightShadowBias;
        public Vector4 sunLightPCFParams;
        public Vector4 sunLightShadowParams;
        public Vector4[] cascadeCullingSpheres = new Vector4[k_MaxCascadeCount];
        public Matrix4x4[] sunLightViewMatrices = new Matrix4x4[k_MaxCascadeCount];
        public Matrix4x4[] sunLightProjectionMatrices = new Matrix4x4[k_MaxCascadeCount];
        public Matrix4x4[] sunLightShadowMatrices = new Matrix4x4[k_MaxCascadeCount];
        public Vector4[] sunLightDepthParams = new Vector4[k_MaxCascadeCount];
        
        
        public int spotLightCount;
        public int pointLightCount;
        public Vector4[] punctualLightColors = new Vector4[k_MaxPunctualLightCount]; // xyz: light color * intensity, w: light type (point 1, spot 2)
        public Vector4[] punctualLightPositions = new Vector4[k_MaxPunctualLightCount]; // xyz: light position, w: shadowing spot/point light index (non-shadowing is -1)
        public Vector4[] punctualLightDirections = new Vector4[k_MaxPunctualLightCount]; // xyz: spot light direction
        public Vector4[] punctualLightParams = new Vector4[k_MaxPunctualLightCount]; // x: 1.0 / light range square, y: 控制距离衰减的参数, z: invAngleRange, w: cosOuterAngle
        
        public int shadowingSpotLightCount;
        public Matrix4x4[] spotLightViewMatrices = new Matrix4x4[k_MaxShadowingSpotLightCount];
        public Matrix4x4[] spotLightProjectionMatrices = new Matrix4x4[k_MaxShadowingSpotLightCount];
        public Matrix4x4[] spotLightShadowMatrices = new Matrix4x4[k_MaxShadowingSpotLightCount];
        public int[] shadowingSpotLightIndices = new int[k_MaxShadowingSpotLightCount]; // store shadowing spot light visible light index
        public Vector4[] spotLightShadowColors = new Vector4[k_MaxShadowingSpotLightCount]; // xyz: shadow color, w: shadow strength
        public Vector4[] spotLightPenumbraColors = new Vector4[k_MaxShadowingSpotLightCount]; // xyz: penumbra color
        public Vector4[] spotLightShadowBias = new Vector4[k_MaxShadowingSpotLightCount]; // x: depth bias, y: slope scaled depth bias, z: normal bias, w: slope scaled normal bias
        public Vector4[] spotLightShadowParams = new Vector4[k_MaxShadowingSpotLightCount]; // x: penumbra(filter) width or scale, y: filter sample number
        public Vector4[] spotLightShadowParams2 = new Vector4[k_MaxShadowingSpotLightCount]; // x: light diameter, y: blocker search scale z: blocker search sample number, w: min penumbra(filter) width
        public Vector4[] spotLightDepthParams = new Vector4[k_MaxShadowingSpotLightCount]; // x: (f + n) / (f - n), y: -2 * f * n / (f - n); [if UNITY_REVERSED_Z] x: (f + n) / (n - f), y: -2 * f * n / (n - f)
        
        public int shadowingPointLightCount;
        public Matrix4x4[] pointLightViewMatrices = new Matrix4x4[k_MaxShadowingPointLightCount * 6];
        public Matrix4x4[] pointLightProjectionMatrices = new Matrix4x4[k_MaxShadowingPointLightCount * 6];
        public Matrix4x4[] pointLightShadowMatrices = new Matrix4x4[k_MaxShadowingPointLightCount * 6];
        public int[] shadowingPointLightIndices = new int[k_MaxShadowingPointLightCount]; // store shadowing point light visible light index
        public Vector4[] pointLightShadowColors = new Vector4[k_MaxShadowingPointLightCount]; // xyz: shadow color, w: shadow strengths
        public Vector4[] pointLightPenumbraColors = new Vector4[k_MaxShadowingPointLightCount]; // xyz: penumbra color
        public Vector4[] pointLightShadowBias = new Vector4[k_MaxShadowingPointLightCount]; // x: depth bias, y: slope scaled depth bias, z: normal bias, w: slope scaled normal bias
        public Vector4[] pointLightShadowParams = new Vector4[k_MaxShadowingPointLightCount]; // x: penumbra(filter) width or scale, y: filter sample number
        public Vector4[] pointLightShadowParams2 = new Vector4[k_MaxShadowingPointLightCount]; // x: light diameter, y: blocker search scale z: blocker search sample number, w: min penumbra(filter) width
        public Vector4[] pointLightDepthParams = new Vector4[k_MaxShadowingPointLightCount]; // x: (f + n) / (f - n), y: -2 * f * n / (f - n); [if UNITY_REVERSED_Z] x: (f + n) / (n - f), y: -2 * f * n / (n - f)
    }
}