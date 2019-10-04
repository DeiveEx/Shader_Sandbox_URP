#ifndef MYHLSLINCLUDE_INCLUDED
#define MYHLSLINCLUDE_INCLUDED

//The name of the function must the EXACTLY the same as the name of the function. The function MUST have a precision suffix (the "_float"), but it MUST NOT be in the name of the function on the custom function node. (In this case, the node will call a function named "MyFunction" instead of "MyFunction_float")
void MyFunction_float(float A, float B, out float Out1, out float Out2) {
	Out1 = 0;
	Out2 = 1;
}

//Voronoi with Distance to edges
inline float2 unity_voronoi_noise_randomVector (float2 UV, float offset)
{
    float2x2 m = float2x2(15.27, 47.63, 99.41, 89.98);
    UV = frac(sin(mul(UV, m)) * 46839.32);
    return float2(sin(UV.y*+offset)*0.5+0.5, cos(UV.x*offset)*0.5+0.5);
}

void Unity_Voronoi_float(float2 UV, float AngleOffset, float CellDensity, out float Out, out float Cells, out float EdgeDistance)
{
	float2 baseCell = float2(floor(UV.x), floor(UV.y));

    float2 g = floor(UV * CellDensity);
    float2 f = frac(UV * CellDensity);
    float t = 8.0;
    float3 res = float3(8.0, 0.0, 0.0);
	
	float2 closestCell;

    for(int y=-1; y<=1; y++)
    {
        for(int x=-1; x<=1; x++)
        {
            float2 lattice = float2(x,y);
            float2 offset = unity_voronoi_noise_randomVector(lattice + g, AngleOffset);
            float d = distance(lattice + offset, f);
			
			float2 cell = baseCell + lattice;
			
            if(d < res.x)
            {
                res = float3(d, offset.x, offset.y);
                Out = res.x;
                Cells = res.y;
				closestCell = cell;
            }
        }
    }
	
	//Second pass to find the Edge distance
	for(int y2=-1; y2<=1; y2++)
    {
        for(int x2=-1; x2<=1; x2++)
        {
            float2 lattice = float2(x2,y2);
            float2 offset = unity_voronoi_noise_randomVector(lattice + g, AngleOffset);
            float d = distance(lattice + offset, f);
			
			float2 diffToClosestCell = abs(closestCell - cell);
            bool isClosestCell = diffToClosestCell.x + diffToClosestCell.y < 0.1;
			
            if(d < res.x)
            {
                res = float3(d, offset.x, offset.y);
                Out = res.x;
                Cells = res.y;
            }
        }
    }
}

#endif