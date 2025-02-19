#ifndef YPIPELINE_TONEMAPPING_LIBRARY_INCLUDED
#define YPIPELINE_TONEMAPPING_LIBRARY_INCLUDED

// TODO: 好好花时间了解一下 Tonemapping、color grading 等等。

// ----------------------------------------------------------------------------------------------------
// ACES
// ----------------------------------------------------------------------------------------------------

// https://knarkowicz.wordpress.com/2016/01/06/aces-filmic-tone-mapping-curve/
float3 ACESFilm(float3 x)
{
    float a = 2.51f;
    float b = 0.03f;
    float c = 2.43f;
    float d = 0.59f;
    float e = 0.14f;
    return x * (a * x + b)/(x * (c * x + d) + e);
}

// 函数待更改，有时间去找一下更好的逆 ACES Tonemapping
// thanks to https://www.wolframalpha.com/input?i=2.51y%5E2%2B.03y%3Dx%282.43y%5E2%2B.59y%2B.14%29+solve+for+y
float3 ACESFilm_Inv(float3 x) {
    return (sqrt(-10127. * x * x + 13702. * x + 9.) + 59. * x - 3.) / (502. - 486. * x);
}

#endif