#ifndef VORONOIDISTANCE_INCLUDED
#define VORONOIDISTANCE_INCLUDED

#include "Assets/_Shaders/Custom Functions/VoronoiDistance.hlsl"

void GetVoronoi_float(float2 uv, float angleOffset, float cellDensity, out float Out , out float cells, out float3 color)
{
    voronoi(uv * cellDensity, angleOffset, Out, cells, color);
}

#endif