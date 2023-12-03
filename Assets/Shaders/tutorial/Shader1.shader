Shader "Unlit/Shader1"
{
    //Defines the unity editor properties
    Properties
    {
        _ColorA ("ColorA", Color) = (1,1,1,1)
        _ColorB ("ColorB", Color) = (1,1,1,1)
        _Scale ("UV SCALE", Float) = 1
        _Offset ("Offset", Float) = 0
        _ColorStart ("Color Start", Float) = 1
        _ColorEnd ("Color End", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            
            //These are like uniforms from GLSL, copied from inspector
            float4 _ColorA;
            float4 _ColorB;
            float _Offset;
            float _Scale;
            float _ColorStart;
            float _ColorEnd;


            /**
            Semantics with in the meshData struct (the struct taken in by the vertex shader) Must
            have semantics as this defines the data we are pulling into the shader
            */

            //auto filled by unity with the items after the colons. 
            struct meshdata //per-vertex meshdata
            {
                float4 vert : POSITION; // vertex position
                float2 uv : TEXCOORD0; //uv coordinates
                float3 normals : NORMAL;
            };

            /**
            Semantics in structs that go into the fragment shaders are just convention. Docs of sorts
            to describe what kind of data the variables hold. These can be anything
            */

            //data passed from vertext into fragment shader
            struct v2f
            {
                float4 vert : SV_POSITION; //clip space position
                float2 uv: TEXCOORD0;
                float3 normal : NORMAL;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            //VERTEX SHADER
            v2f vert (meshdata v)
            {
                v2f o;
                o.uv = (v.uv + _Offset) * _Scale;
                o.vert = UnityObjectToClipPos(v.vert); // local space to clip space;
                o.normal = UnityObjectToWorldNormal(v.normals); //convert the normals to world space
                return o;
            }

            float inverseLerp(float a, float b, float v){
                return (v-a)/(b-a);
            }

            //FRAGMENT SHADER
            fixed4 frag (v2f i) : SV_Target //this states that the output of the fragment should output to screen
            {   
                // blend between two colors based on the X UV coordinate
                float t = inverseLerp( _ColorStart, _ColorEnd, i.uv.x );
                //return t;
                //clamp(t, 0, 1);
                t = saturate(t);
                //t = frac(t);
                //blend between two colors based on the x UV coord
                float4 outColor = lerp(_ColorA, _ColorB, t);
                return fixed4(outColor.xyz, 1);
            }
            ENDCG
        }
    }
}
