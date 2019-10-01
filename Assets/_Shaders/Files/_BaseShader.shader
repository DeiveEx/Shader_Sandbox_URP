//Here we define in which category the shader will fit
Shader "Unlit/NewUnlitShader"
{
	Properties
	{
		//Here are the properties for the shader that are showed in the editor
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		//Here are the tags for the shader (is it Opaque? Transparent? etc.)
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			//CGPROGRAM says that we're importing the library from nVidia
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			//This is the entry information from Unity to the shader
			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			//This is the data structure that the vertex shader sends to the fragment/pixel shader
			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			//Here are the variables/properties of our shader. We HAVE to declare the variables of the same name as the one we created in the properties. This is called "Bind"
			sampler2D _MainTex; //This is the main texture defined in the properties
			float4 _MainTex_ST; //And this is some info that unity uses to tile/offset the texture
			
			//This is the vertex shader, used to manipulate the vertex of the object
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			//And this is the Fragment/Pixel shader, used to paint the pixels on the screen
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
