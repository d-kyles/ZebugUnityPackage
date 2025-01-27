// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Simple "just colors" shader that's used for built-in debug visualizations,
// in the editor etc. Just outputs _Color * vertex color; and blend/Z/cull/bias
// controlled by material parameters.

Shader "Zebug/Simple-Colored-URP-Procedural"
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
        _OccludedAlpha ("OccludedAlpha", Float) = 0.125
    }

    SubShader
    {

        Tags {
            "Queue"           = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType"      = "Opaque"
        }

        Pass
        {
            Name "Opaque"

            ZTest LEqual

            Blend [_SrcBlend] [_DstBlend]
            Cull [_Cull]
            Offset [_ZBias], [_ZBias]
            ZWrite Off

            HLSLPROGRAM

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Library/PackageCache/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


#include "Library/PackageCache/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

#include "Library/PackageCache/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"

#pragma vertex vert
#pragma fragment frag

struct LineInstanceData
{
    // size must be multiple of 4
    float3 startPosition;
    float3 endPosition;
    uint color;
    float width;
};
StructuredBuffer<LineInstanceData> _LineData;

struct Attributes
{
    uint vertexID : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};


struct Varyings
{
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_OUTPUT_STEREO
};

half4 _Color;
half _OccludedAlpha;

Varyings vert (Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    #if defined(UNITY_ANY_INSTANCING_ENABLED)
    uint id = unity_InstanceID;
    #else
    uint id = 0;
    #endif

    float4 vertStartWorldPos = _LineData[id].startPosition;
    float4 vertEndWorldPos = _LineData[id].startPosition;


    float2 pixelSize =  output.positionCS.w;
    pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

    float4 vertStartPosView =

    //float4 vertPos = GetQuadVertexPosition(input.vertexID);

    return output;
}

half4 frag (Varyings i) : SV_Target
{
    i.color.a *= _OccludedAlpha;
    return i.color;
}
ENDHLSL


        }

    }
}