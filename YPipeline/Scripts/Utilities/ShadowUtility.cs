using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public static class ShadowUtility
    {
        /// <summary>
        /// 获取从世界空间坐标到阴影空间坐标的矩阵
        /// </summary>
        /// <param name="vp"></param>
        /// <returns></returns>
        public static Matrix4x4 GetDirLightWorldToShadowMatrix(Matrix4x4 vp)
        {
            if (SystemInfo.usesReversedZBuffer) 
            {
                vp.m20 = -vp.m20;
                vp.m21 = -vp.m21;
                vp.m22 = -vp.m22;
                vp.m23 = -vp.m23;
            }
            
            //缩放 0.5 加上平移 0.5 
            vp.m00 = 0.5f * (vp.m00 + vp.m30);
            vp.m01 = 0.5f * (vp.m01 + vp.m31);
            vp.m02 = 0.5f * (vp.m02 + vp.m32);
            vp.m03 = 0.5f * (vp.m03 + vp.m33);
            vp.m10 = 0.5f * (vp.m10 + vp.m30);
            vp.m11 = 0.5f * (vp.m11 + vp.m31);
            vp.m12 = 0.5f * (vp.m12 + vp.m32);
            vp.m13 = 0.5f * (vp.m13 + vp.m33);
            vp.m20 = 0.5f * (vp.m20 + vp.m30);
            vp.m21 = 0.5f * (vp.m21 + vp.m31);
            vp.m22 = 0.5f * (vp.m22 + vp.m32);
            vp.m23 = 0.5f * (vp.m23 + vp.m33);
            
            return vp;
        }

        public static Matrix4x4 GetTiledDirLightWorldToShadowMatrix(Matrix4x4 vp, Vector2 offset, int split)
        {
            Matrix4x4 vps = GetDirLightWorldToShadowMatrix(vp);
            
            //还是缩放再平移
            float scale = 1f / split;
            vps.m00 = (vps.m00 + offset.x * vps.m30) * scale;
            vps.m01 = (vps.m01 + offset.x * vps.m31) * scale;
            vps.m02 = (vps.m02 + offset.x * vps.m32) * scale;
            vps.m03 = (vps.m03 + offset.x * vps.m33) * scale;
            vps.m10 = (vps.m10 + offset.y * vps.m30) * scale;
            vps.m11 = (vps.m11 + offset.y * vps.m31) * scale;
            vps.m12 = (vps.m12 + offset.y * vps.m32) * scale;
            vps.m13 = (vps.m13 + offset.y * vps.m33) * scale;
            
            return vps;
        }
    }
}