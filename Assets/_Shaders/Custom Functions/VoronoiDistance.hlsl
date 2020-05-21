#ifndef VORONOIDISTANCE_INCLUDED
#define VORONOIDISTANCE_INCLUDED

// The MIT License
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
float2 hash2( float2 p )
{
    // procedural white noise	
	return frac(sin(float2(dot(p,float2(127.1, 311.7)),dot(p,float2(269.5, 183.3)))) * 43758.5453);
}

float3 voronoi( in float2 x, in float offset )
{
    float2 n = floor(x);
    float2 f = frac(x); 

    //----------------------------------
    // first pass: regular voronoi
    //----------------------------------
	float2 mg, mr;

    float md = 8.0;
    for( int j=-1; j<=1; j++ ){
        for( int i=-1; i<=1; i++ )
        {
            float2 g = float2(float(i),float(j));
		    float2 o = hash2( (n + g) * offset );

		    #ifdef ANIMATE
            o = 0.5 + 0.5*sin( iTime + 6.2831*o );
            #endif	

            float2 r = g + o - f;
            float d = dot(r,r);

            if(d < md)
            {
                md = d;
                mr = r;
                mg = g;
            }
        }
    }

    //----------------------------------
    // second pass: distance to borders
    //----------------------------------
    md = 8.0;
    for( int j=-2; j<=2; j++ ){
        for( int i=-2; i<=2; i++ )
        {
            float2 g = mg + float2(float(i),float(j));
		    float2 o = hash2( (n + g) * offset );

		    #ifdef ANIMATE
            o = 0.5 + 0.5*sin( iTime + 6.2831*o );
            #endif	

            float2 r = g + o - f;

            if( dot(mr-r,mr-r)>0.00001 ){
                md = min( md, dot( 0.5*(mr+r), normalize(r-mr) ) );
            }
        }
    }

    return float3( md, mr );
}

void GetVoronoi_float(float2 uv, float angleOffset, float cellDensity, out float4 Color){
    
    float3 c = voronoi(uv * cellDensity, angleOffset / 100);

    Color = float4(c, 0);
}

#endif