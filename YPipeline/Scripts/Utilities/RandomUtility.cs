using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public static class RandomUtility
    {
        // ----------------------------------------------------------------------------------------------------
        // Low-discrepancy sequence
        // From "Physically Based Rendering: From Theory To Implementation"
        // ----------------------------------------------------------------------------------------------------

        // Radical Inverse Functions(Van der Corput sequence)
        
        /// <summary>
        /// The base-2 radical inverse computed by a series of logical bit operations.
        /// </summary>
        /// <param name="a"> 0,1,2,3,4... </param>
        /// <returns>the base-2 radical inverse</returns>
        public static float RadicalInverseVdC_Bits(uint a)
        {
            a = (a << 16) | (a >> 16);
            a = ((a & 0x00ff00ff) << 8) | ((a & 0xff00ff00) >> 8);
            a = ((a & 0x0f0f0f0f) << 4) | ((a & 0xf0f0f0f0) >> 4);
            a = ((a & 0x33333333) << 2) | ((a & 0xcccccccc) >> 2);
            a = ((a & 0x55555555) << 1) | ((a & 0xaaaaaaaa) >> 1);
            return a * (float) 2.3283064365386963e-10;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseNum">use prime number as the base</param>
        /// <param name="a"> 0,1,2,3,4... </param>
        /// <returns></returns>
        public static float RadicalInverseVdC_Float(int baseNum, ulong a)
        {
            float invBase = 1.0f / baseNum;
            ulong reversedDigits = 0;
            float invBaseN = 1.0f;
            
            while (a != 0)
            {
                ulong next = a / (ulong) baseNum;
                ulong digit = a - next * (ulong) baseNum;
                reversedDigits = reversedDigits * (ulong) baseNum + digit;
                invBaseN *= invBase;
                a = next;
            }
            return reversedDigits * invBaseN;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="aDigits"></param>
        /// <returns></returns>
        public static float RadicalInverseVdC_Inverse(int baseNum, ulong a)
        {
            if (a == 0) return 0.0f;
            
            int aDigits = (int) Mathf.Floor(Mathf.Log(a, 2) + 1);
            float basePower = Mathf.Pow(baseNum, aDigits);
            
            ulong index = 0;
            for (int i = 0; i < aDigits; ++i)
            {
                ulong digit = a % (ulong) baseNum;
                a /= (ulong) baseNum;
                index = index * (ulong) baseNum + digit;
            }
            
            return index / basePower;
        }
        
        
        
        public static Vector2 Hammersley_Bits(uint index, uint sampleNum)
        {
            return new Vector2((float) index / sampleNum, RadicalInverseVdC_Bits(index));
        }

        public static Vector2 Hammersley_Float(uint index, uint sampleNum)
        {
            return new Vector2((float) index / sampleNum, RadicalInverseVdC_Float(2, index));
        }
        
        public static Vector2 Hammersley_Inverse(uint index, uint sampleNum)
        {
            return new Vector2((float) index / sampleNum, RadicalInverseVdC_Inverse(2, index));
        }
    }
}