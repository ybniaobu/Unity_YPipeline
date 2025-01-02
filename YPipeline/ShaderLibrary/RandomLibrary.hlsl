#ifndef YPIPELINE_RANDOM_LIBRARY_INCLUDED
#define YPIPELINE_RANDOM_LIBRARY_INCLUDED

// ----------------------------------------------------------------------------------------------------
// Low-discrepancy sequence - Radical Inverse Functions(Van der Corput sequence)
// From https://www.pbr-book.org/3ed-2018/Sampling_and_Reconstruction/The_Halton_Sampler or https://pbr-book.org/4ed/Sampling_and_Reconstruction/Halton_Sampler
uint RadicalInverseVdC_Bits(uint bits) // Base-2 radical inverse
{
    bits = (bits << 16u) | (bits >> 16u);
    bits = ((bits & 0x55555555u) << 1u) | ((bits & 0xAAAAAAAAu) >> 1u);
    bits = ((bits & 0x33333333u) << 2u) | ((bits & 0xCCCCCCCCu) >> 2u);
    bits = ((bits & 0x0F0F0F0Fu) << 4u) | ((bits & 0xF0F0F0F0u) >> 4u);
    bits = ((bits & 0x00FF00FFu) << 8u) | ((bits & 0xFF00FF00u) >> 8u);
    return bits;
}

float RadicalInverseVdc_Float(uint base, uint a) // Use prime number as base; a = 0, 1, ...
{
    float invBase = 1.0 / (float) base;
    float invBaseM = 1.0;
    uint reversedDigits = 0;

    while (a)
    {
        uint next = a / base;
        uint digit = a - next * base;
        reversedDigits = reversedDigits * base + digit;
        invBaseM *= invBase;
        a = next;
    }
    return reversedDigits * invBaseM;
}

// ----------------------------------------------------------------------------------------------------
// Low-discrepancy sequence - Hammersley sequence
float2 Hammersley_Bits(uint index, uint sampleNumber)
{
    return float2(float(index) / float(sampleNumber), float(RadicalInverseVdC_Bits(index)) * 2.3283064365386963e-10); // /0x100000000
}

float2 Hammersley_Float(uint index, uint sampleNumber, uint primeBase = 2)
{
    return float2(float(index) / float(sampleNumber), RadicalInverseVdc_Float(primeBase,index));
}

// ----------------------------------------------------------------------------------------------------
// Low-discrepancy sequence - Halton sequence
float2 Halton_Float(uint index, uint primeBase1 = 2, uint primeBase2 = 3)
{
    return float2(RadicalInverseVdc_Float(primeBase1,index), RadicalInverseVdc_Float(primeBase2,index));
}

// ----------------------------------------------------------------------------------------------------
// Low-discrepancy sequence - Sobol sequence
// From https://www.shadertoy.com/view/sd2Xzm and https://www.jcgt.org/published/0009/04/01/
uint2 SobolGenerator(uint index)
{
    uint2 p = uint2(0u, 0u);
    uint2 d = uint2(0x80000000u, 0x80000000u);

    for(; index != 0u; index >>= 1u)
    {
        if((index & 1u) != 0u) p ^= d;
        d.x >>= 1u; // 1st dimension Sobol matrix, is same as base 2 Van der Corput
        d.y ^= d.y >> 1u; // 2nd dimension Sobol matrix
    }
    return p;
}

float2 Sobol_Bits(uint index)
{
    return float2(SobolGenerator(index)) * 2.3283064365386963e-10;
}

// From https://psychopath.io/post/2021_01_30_building_a_better_lk_hash
uint Hash_Owen(uint x, uint seed) // works best with random seeds
{
    x ^= x * 0x3d20adeau;
    x += seed;
    x *= (seed >> 16) | 1u;
    x ^= x * 0x05526c56u;
    x ^= x * 0x53a22864u;
    return x;
}

uint OwenScramble(uint x, uint seed)
{
    x = RadicalInverseVdC_Bits(x);
    x = Hash_Owen(x, seed);
    return RadicalInverseVdC_Bits(x);
}

float2 Sobol_Scrambled(uint index, uint seed1, uint seed2) // works best with random seeds
{
    uint2 sobol = SobolGenerator(index);
    sobol.x = OwenScramble(sobol.x, seed1);
    sobol.y = OwenScramble(sobol.y, seed2);
    return float2(sobol) * 2.3283064365386963e-10;
}

// ----------------------------------------------------------------------------------------------------
// Bob Jenkins' One-At-A-Time hashing algorithm: https://en.wikipedia.org/wiki/Jenkins_hash_function
uint Hash_Jenkins(uint x)
{
    x += ( x << 10u );
    x ^= ( x >>  6u );
    x += ( x <<  3u );
    x ^= ( x >> 11u );
    x += ( x << 15u );
    return x;
}

uint Hash_Jenkins(uint2 v) { return Hash_Jenkins(v.x ^ Hash_Jenkins(v.y)); }
uint Hash_Jenkins(uint3 v) { return Hash_Jenkins(v.x ^ Hash_Jenkins(v.y) ^ Hash_Jenkins(v.z)); }
uint Hash_Jenkins(uint4 v) { return Hash_Jenkins(v.x ^ Hash_Jenkins(v.y) ^ Hash_Jenkins(v.z) ^ Hash_Jenkins(v.w)); }

// ----------------------------------------------------------------------------------------------------
// Generate Pseudo-random value in [0, 1) using Bob Jenkins' One-At-A-Time hashing algorithm
// From https://stackoverflow.com/a/17479300

// Construct a float with half-open range [0, 1) using low 23 bits.
// All zeroes yields 0.0, all ones yields the next smallest representable value below 1.0.
float floatConstruct(uint m)
{
    const uint ieeeMantissa = 0x007FFFFFu; // binary32 mantissa bitmask
    const uint ieeeOne      = 0x3F800000u; // 1.0 in IEEE binary32

    m &= ieeeMantissa;                     // Keep only mantissa bits (fractional part)
    m |= ieeeOne;                          // Add fractional part to 1.0

    float f = asfloat(m);                 // Range [1, 2)
    return f - 1.0;                        // Range [0, 1)
}

float Random_Mantissa(float x) { return floatConstruct(Hash_Jenkins(asuint(x))); }
float Random_Mantissa(float2 v) { return floatConstruct(Hash_Jenkins(asuint(v))); }
float Random_Mantissa(float3 v) { return floatConstruct(Hash_Jenkins(asuint(v))); }
float Random_Mantissa(float4 v) { return floatConstruct(Hash_Jenkins(asuint(v))); }

// ----------------------------------------------------------------------------------------------------
// Generate Pseudo-random value in [0, 1), this method produces pattern when using large values, recommend to use a small range in [0, 1]
// From "The Book of Shaders" by Patricio Gonzalez Vivo and Jen Lowe
// https://thebookofshaders.com/10/ or https://github.com/patriciogonzalezvivo/lygia
float Random_Sine(float x) { return frac(sin(x) * 43758.5453123); }
float Random_Sine(float2 uv) { return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453123); }
float Random_Sine(float3 pos) { return frac(sin(dot(pos, float3(70.9898, 78.233, 32.4355))) * 43758.5453123); }
float Random_Sine(float4 pos) { return frac(sin(dot(pos, float4(12.9898, 78.233, 45.164, 94.673))) * 43758.5453123); }

// ----------------------------------------------------------------------------------------------------
// Generate Pseudo-random value in [0, 1) without sine
// From https://www.shadertoy.com/view/4djSRW or https://github.com/patriciogonzalezvivo/lygia
// smallRange = true: input with a small range in [0, 1] ; smallRange = false: input with a big range over 0 and 1
float Random_NoSine(float x, bool smallRange = true)
{
    x *= lerp(1, 43758.5453123, smallRange);
    x = frac(x * .1031);
    x *= x + 33.33;
    x *= x + x;
    return frac(x);
}

float Random_NoSine(float2 uv, bool smallRange = true)
{
    uv *= lerp(1, 43758.5453123, smallRange);
    float3 p = frac(uv.xyx * float3(.1031, .1030, .0973));
    p += dot(p, p.yzx + 33.33);
    return frac((p.x + p.y) * p.z);
}

float Random_NoSine(float3 pos, bool smallRange = true)
{
    pos *= lerp(1, 43758.5453123, smallRange);
    pos = frac(pos * float3(.1031, .1030, .0973));
    pos += dot(pos, pos.zyx + 31.32);
    return frac((pos.x + pos.y) * pos.z);
}

float Random_NoSine(float4 pos, bool smallRange = true)
{
    pos *= lerp(1, 43758.5453123, smallRange);
    pos = frac(pos * float4(.1031, .1030, .0973, .1099));
    pos += dot(pos, pos.wzxy + 33.33);
    return frac((pos.x + pos.y) * (pos.z + pos.w));
}

float2 Random_NoSine2(float x, bool smallRange = true)
{
    x *= lerp(1, 43758.5453123, smallRange);
    float3 p = frac(float3(x, x, x) * float3(.1031, .1030, .0973));
    p += dot(p, p.yzx + 33.33);
    return frac((p.xx + p.yz) * p.zy);
}

float2 Random_NoSine2(float2 uv, bool smallRange = true)
{
    uv *= lerp(1, 43758.5453123, smallRange);
    float3 p = frac(uv.xyx * float3(.1031, .1030, .0973));
    p += dot(p, p.yzx + 33.33);
    return frac((p.xx + p.yz) * p.zy);
}

float2 Random_NoSine2(float3 pos, bool smallRange = true)
{
    pos *= lerp(1, 43758.5453123, smallRange);
    pos = frac(pos * float3(.1031, .1030, .0973));
    pos += dot(pos, pos.yzx + 33.33);
    return frac((pos.xx + pos.yz) * pos.zy);
}

float3 Random_NoSine3(float x, bool smallRange = true)
{
    x *= lerp(1, 43758.5453123, smallRange);
    float3 p = frac(float3(x, x, x) * float3(.1031, .1030, .0973));
    p += dot(p, p.yzx + 33.33);
    return frac((p.xxy + p.yzz) * p.zyx); 
}

float3 Random_NoSine3(float2 uv, bool smallRange = true)
{
    uv *= lerp(1, 43758.5453123, smallRange);
    float3 p = frac(uv.xyx * float3(.1031, .1030, .0973));
    p += dot(p, p.yxz + 33.33);
    return frac((p.xxy + p.yzz) * p.zyx);
}

float3 Random_NoSine3(float3 pos, bool smallRange = true)
{
    pos *= lerp(1, 43758.5453123, smallRange);
    pos = frac(pos * float3(.1031, .1030, .0973));
    pos += dot(pos, pos.yxz + 33.33);
    return frac((pos.xxy + pos.yxx) * pos.zyx);
}

float4 Random_NoSine4(float x, bool smallRange = true)
{
    x *= lerp(1, 43758.5453123, smallRange);
    float4 p = frac(float4(x, x, x, x) * float4(.1031, .1030, .0973, .1099));
    p += dot(p, p.wzxy + 33.33);
    return frac((p.xxyz + p.yzzw) * p.zywx);
}

float4 Random_NoSine4(float2 uv, bool smallRange = true)
{
    uv *= lerp(1, 43758.5453123, smallRange);
    float4 p = frac(uv.xyxy * float4(.1031, .1030, .0973, .1099));
    p += dot(p, p.wzxy + 33.33);
    return frac((p.xxyz + p.yzzw) * p.zywx);
}

float4 Random_NoSine4(float3 pos, bool smallRange = true)
{
    pos *= lerp(1, 43758.5453123, smallRange);
    float4 p = frac(pos.xyzx * float4(.1031, .1030, .0973, .1099));
    p += dot(p, p.wzxy + 33.33);
    return frac((p.xxyz + p.yzzw) * p.zywx);
}

float4 Random_NoSine4(float4 pos, bool smallRange = true)
{
    pos *= lerp(1, 43758.5453123, smallRange);
    pos = frac(pos * float4(.1031, .1030, .0973, .1099));
    pos += dot(pos, pos.wzxy + 33.33);
    return frac((pos.xxyz + pos.yzzw) * pos.zywx);
}

#endif