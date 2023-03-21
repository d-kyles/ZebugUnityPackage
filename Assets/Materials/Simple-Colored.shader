// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Simple "just colors" shader that's used for built-in debug visualizations,
// in the editor etc. Just outputs _Color * vertex color; and blend/Z/cull/bias
// controlled by material parameters.

Shader "Zebug/Simple-Colored"
{ 
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _SrcBlend ("SrcBlend", Int) = 5.0 // SrcAlpha
        _DstBlend ("DstBlend", Int) = 10.0 // OneMinusSrcAlpha
        _ZWrite ("ZWrite", Int) = 1.0 // On
        _ZTest ("ZTest", Int) = 4.0 // LEqual
        _Cull ("Cull", Int) = 0.0 // Off
        _ZBias ("ZBias", Float) = 0.0
        _OccludedAlpha ("OccludedAlpha", Float) = 0.5
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        
        Blend [_SrcBlend] [_DstBlend]
        Cull [_Cull]
        Offset [_ZBias], [_ZBias]
        ZWrite Off

        Pass
        {
            // This more transparent pass draws _if_ the fragments are occluded
            ZTest Greater

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct appdata_t {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };
            struct v2f {
                fixed4 color : COLOR;
                float4 vertex : SV_POSITION;
            };
            float4 _Color;
            float _OccludedAlpha;
            
            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                i.color.a *= _OccludedAlpha; 
                return i.color;
            }
            ENDCG
        }  
        
        
        Pass
        {
            // This pass draws _if_ the fragments aren't occluded
            ZTest LEqual
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct appdata_t {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };
            struct v2f {
                fixed4 color : COLOR;
                float4 vertex : SV_POSITION;
            };
            float4 _Color;
            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }  
        
        
    }
}