using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public static class RandomUtility
    {
        // ----------------------------------------------------------------------------------------------------
        // Low-discrepancy sequence - Radical Inverse Functions(Van der Corput sequence)
        // From "Physically Based Rendering: From Theory To Implementation"
        // ----------------------------------------------------------------------------------------------------
        
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
        /// The radical inverse computed by an integer arithmetic loop (support any base)
        /// </summary>
        /// <param name="baseNum">use prime number as the base</param>
        /// <param name="a"> 0,1,2,3,4... </param>
        /// <returns></returns>
        public static float RadicalInverseVdC_Specialized(int baseNum, ulong a)
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
        /// The radical inverse computed by the reversed integer digits (support any base)
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
        
        // ----------------------------------------------------------------------------------------------------
        // Low-discrepancy sequence - Hammersley sequence
        // ----------------------------------------------------------------------------------------------------
        
        public static Vector2 Hammersley_Bits(uint index, uint sampleNum)
        {
            return new Vector2((float) index / sampleNum, RadicalInverseVdC_Bits(index));
        }

        public static Vector2 Hammersley_Specialized(uint index, uint sampleNum)
        {
            return new Vector2((float) index / sampleNum, RadicalInverseVdC_Specialized(2, index));
        }
        
        public static Vector2 Hammersley_Inverse(uint index, uint sampleNum)
        {
            return new Vector2((float) index / sampleNum, RadicalInverseVdC_Inverse(2, index));
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Low-discrepancy sequence - Halton sequence
        // ----------------------------------------------------------------------------------------------------
        
        public static readonly Vector2[] k_Halton = new Vector2[65]
        {
            new Vector2(0.00000000f, 0.00000000f), 
            new Vector2(0.50000000f, 0.33333333f), new Vector2(0.25000000f, 0.66666667f), new Vector2(0.75000000f, 0.11111111f), new Vector2(0.12500000f, 0.44444444f),
            new Vector2(0.62500000f, 0.77777778f), new Vector2(0.37500000f, 0.22222222f), new Vector2(0.87500000f, 0.55555556f), new Vector2(0.06250000f, 0.88888889f),
            new Vector2(0.56250000f, 0.03703704f), new Vector2(0.31250000f, 0.37037037f), new Vector2(0.81250000f, 0.70370370f), new Vector2(0.18750000f, 0.14814815f),
            new Vector2(0.68750000f, 0.48148148f), new Vector2(0.43750000f, 0.81481481f), new Vector2(0.93750000f, 0.25925926f), new Vector2(0.03125000f, 0.59259259f),
            new Vector2(0.53125000f, 0.92592593f), new Vector2(0.28125000f, 0.07407407f), new Vector2(0.78125000f, 0.40740741f), new Vector2(0.15625000f, 0.74074074f),
            new Vector2(0.65625000f, 0.18518519f), new Vector2(0.40625000f, 0.51851852f), new Vector2(0.90625000f, 0.85185185f), new Vector2(0.09375000f, 0.29629630f),
            new Vector2(0.59375000f, 0.62962963f), new Vector2(0.34375000f, 0.96296296f), new Vector2(0.84375000f, 0.01234568f), new Vector2(0.21875000f, 0.34567901f),
            new Vector2(0.71875000f, 0.67901235f), new Vector2(0.46875000f, 0.12345679f), new Vector2(0.96875000f, 0.45679012f), new Vector2(0.01562500f, 0.79012346f),
            new Vector2(0.51562500f, 0.23456790f), new Vector2(0.26562500f, 0.56790123f), new Vector2(0.76562500f, 0.90123457f), new Vector2(0.14062500f, 0.04938272f),
            new Vector2(0.64062500f, 0.38271605f), new Vector2(0.39062500f, 0.71604938f), new Vector2(0.89062500f, 0.16049383f), new Vector2(0.07812500f, 0.49382716f),
            new Vector2(0.57812500f, 0.82716049f), new Vector2(0.32812500f, 0.27160494f), new Vector2(0.82812500f, 0.60493827f), new Vector2(0.20312500f, 0.93827160f),
            new Vector2(0.70312500f, 0.08641975f), new Vector2(0.45312500f, 0.41975309f), new Vector2(0.95312500f, 0.75308642f), new Vector2(0.04687500f, 0.19753086f),
            new Vector2(0.54687500f, 0.53086420f), new Vector2(0.29687500f, 0.86419753f), new Vector2(0.79687500f, 0.30864198f), new Vector2(0.17187500f, 0.64197531f),
            new Vector2(0.67187500f, 0.97530864f), new Vector2(0.42187500f, 0.02469136f), new Vector2(0.92187500f, 0.35802469f), new Vector2(0.10937500f, 0.69135802f),
            new Vector2(0.60937500f, 0.13580247f), new Vector2(0.35937500f, 0.46913580f), new Vector2(0.85937500f, 0.80246914f), new Vector2(0.23437500f, 0.24691358f),
            new Vector2(0.73437500f, 0.58024691f), new Vector2(0.48437500f, 0.91358025f), new Vector2(0.98437500f, 0.06172840f), new Vector2(0.00781250f, 0.39506173f)
        };

        public static Vector2 Halton_Specialized(uint index)
        {
            return new Vector2(RadicalInverseVdC_Bits(index), RadicalInverseVdC_Specialized(3, index));
        }
        
        public static Vector2 Halton_Inverse(uint index)
        {
            return new Vector2(RadicalInverseVdC_Bits(index), RadicalInverseVdC_Inverse(3, index));
        }
    }
}