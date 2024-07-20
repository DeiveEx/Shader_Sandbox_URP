﻿// The MIT License
// Copyright © 2013 Inigo Quilez
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

// I've not seen anybody out there computing correct cell interior distances for Voronoi
// patterns yet. That's why they cannot shade the cell interior correctly, and why you've
// never seen cell boundaries rendered correctly. 
//
// However, here's how you do mathematically correct distances (note the equidistant and non
// degenerated grey isolines inside the cells) and hence edges (in yellow):
//
// http://www.iquilezles.org/www/articles/voronoilines/voronoilines.htm
//
// More Voronoi shaders:
//
// Exact edges:  https://www.shadertoy.com/view/ldl3W8
// Hierarchical: https://www.shadertoy.com/view/Xll3zX
// Smooth:       https://www.shadertoy.com/view/ldB3zc
// Voronoise:    https://www.shadertoy.com/view/Xd23Dh

inline float2 unity_voronoi_noise_randomVector (float2 UV, float offset) //This function was taken from Unity's Voronoi node documentation, and not from ShaderToy
{
    float2x2 m = float2x2(15.27, 47.63, 99.41, 89.98);
    UV = frac(sin(mul(UV, m)) * 46839.32);
    return float2(sin(UV.y*offset)*0.5+0.5, cos(UV.x*offset)*0.5+0.5);
}

void voronoi( in float2 x, in float offset, out float Out , out float cells, out float3 color)
{
    Out = 0;
    cells = 0;
    color = float3(0, 0, 0);
    
    float2 n = floor(x); //Integer part. Basically the grid Pos
    float2 f = frac(x); //Fractional part

    //----------------------------------
    // first pass: regular voronoi
    //----------------------------------
	float2 mg; //Minimum Grid Pos
    float2 mr; //Minimum Relative Pos

    float md = 8.0; //Minimum distance
    for( int j=-1; j<=1; j++ )
    {
        for( int i=-1; i<=1; i++ )
        {
            float2 g = float2(float(i),float(j)); //Grid Pos
		    float2 o = unity_voronoi_noise_randomVector(n + g, offset);//Cell Seed Pos. Originally: hash2( (n + g) * offset ); This makes sure that each grid pos will have an unique "seed"

            float2 r = g + o - f; //RelativePos from gridPos to cell seed pos
            float d = dot(r,r); //SquaredDistance. Since we're just comparing distances, doing dot with itself saves the need of calculating the actual distance, which uses an expensive sqrt operation

            if(d < md)
            {
                md = d;
                mr = r;
                mg = g;

                Out = d;
                cells = o.x;
            }
        }
    }

    //----------------------------------
    // second pass: distance to borders
    //----------------------------------
    md = 8.0;
    for( int j=-2; j<=2; j++ )
    {
        for( int i=-2; i<=2; i++ )
        {
            float2 g = mg + float2(float(i),float(j));
		    float2 o = unity_voronoi_noise_randomVector(n + g, offset);
    
            float2 r = g + o - f;
    
            if( dot(mr-r,mr-r)>0.00001 )
            {
                float d = dot( 0.5 * (mr+r), normalize(r-mr) ); //Projection
                md = min( md, d );
            }
        }
    }

    color = float3( md, mr );
}