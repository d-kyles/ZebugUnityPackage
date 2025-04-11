
Shader "Zebug/Simple-Colored-URP-Procedural"
{
    Properties
    {
        [MainColor] _Color ("Color", Color) = (1,1,1,1)
        _SrcBlend ("SrcBlend", Int) = 5.0 // SrcAlpha
        _DstBlend ("DstBlend", Int) = 10.0 // OneMinusSrcAlpha
        _ZWrite ("ZWrite", Int) = 1.0 // On
        _ZTest ("ZTest", Int) = 4.0 // LEqual
        _Cull ("Cull", Int) = 0.0 // Off
        _ZBias ("ZBias", Float) = 0.0
        _OccludedAlpha ("OccludedAlpha", Float) = 0.125
    }

    SubShader
    {
        Tags {
            "Queue"           = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalPipeline" 
        }

        Pass
        {
            Name "In Front"

            ZTest LEqual

            //Blend [_SrcBlend] [_DstBlend]
            Cull [_Cull]
            //Offset [_ZBias], [_ZBias]
            ZWrite On

            HLSLPROGRAM

            // --- This is mainly used for the purposes of debugging shader variants 
            #if defined(SHADER_API_METAL)
            #   define SHADER_TARGET 45
            #endif

            #pragma multi_compile PROCEDURAL_INSTANCING_ON
            #define UNITY_INSTANCING_PROCEDURAL_FUNC unity_instancing_procedural_func
            #pragma multi_compile _ STEREO_MULTIVIEW_ON STEREO_INSTANCING_ON

            #include "./Simple-Colored-Procedural_inc.hlsl"

            #pragma vertex vert
            #pragma fragment frag


            half4 frag (Varyings IN) : SV_Target
            {
                half4 color = IN.color;
                return color;
            }
            
            ENDHLSL
        }

        Pass
        {
            Name "Behind"

            ZTest Greater

            Blend One OneMinusSrcAlpha
            Cull Off
            //Offset [_ZBias], [_ZBias]
            ZWrite Off

            HLSLPROGRAM

            // --- This is mainly used for the purposes of debugging shader variants 
            #if defined(SHADER_API_METAL)
            #   define SHADER_TARGET 45
            #endif

            #pragma multi_compile PROCEDURAL_INSTANCING_ON
            #define UNITY_INSTANCING_PROCEDURAL_FUNC unity_instancing_procedural_func
            #pragma multi_compile _ STEREO_MULTIVIEW_ON STEREO_INSTANCING_ON

            #include "./Simple-Colored-Procedural_inc.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            half4 frag (Varyings IN) : SV_Target
            {
                half4 color = IN.color;
                color *= _OccludedAlpha;
                return color;
            }
            
            ENDHLSL
        }

    }
}