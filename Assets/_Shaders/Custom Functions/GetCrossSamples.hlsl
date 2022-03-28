#ifndef GETCROSSSAMPLEUVS_INCLUDED
#define GETCROSSSAMPLEUVS_INCLUDED

void GetCrossSampleUVs_float(float4 uv, float2 texelSize, float offsetMultiplier, out float2 uvOriginal, 
out float2 uvTopRight, out float2 uvTopLeft,
out float2 uvBottomRight, out float2 uvBottomLeft)
{
    uvOriginal = uv;
    uvTopRight = uv.xy + float2(texelSize.x, texelSize.y) * offsetMultiplier;
    uvTopLeft = uv.xy + float2(-texelSize.x, texelSize.y) * offsetMultiplier;
    uvBottomRight = uv.xy + float2(texelSize.x, -texelSize.y) * offsetMultiplier;
    uvBottomLeft = uv.xy + float2(-texelSize.x, -texelSize.y) * offsetMultiplier;
}

#endif