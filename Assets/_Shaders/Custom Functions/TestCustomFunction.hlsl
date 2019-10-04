#ifndef MYHLSLINCLUDE_INCLUDED
#define MYHLSLINCLUDE_INCLUDED

//The name of the function must the EXACTLY the same as the name of the function. The function MUST have a precision suffix (the "_float"), but it MUST NOT be in the name of the function on the custom function node. (In this case, the node will call a function named "MyFunction" instead of "MyFunction_float")
void MyFunction_float(float A, float B, out float Out1, out float Out2) {
	Out1 = 0;
	Out2 = 1;
}

//Voronoi with Distance to edges
inline float2 unity_voronoi_noise_randomVector(float2 UV, float offset)
{
    float2x2 m = float2x2(15.27, 47.63, 99.41, 89.98);
    UV = frac(sin(mul(UV, m)) * 46839.32);
    return float2(sin(UV.y * +offset) * 0.5 + 0.5, cos(UV.x * offset) * 0.5 + 0.5);
}

#endif