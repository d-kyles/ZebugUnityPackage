
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct LineInstanceData
{
    // size must be multiple of 4
    float4 startPosition; // 'w' is width 
    float3 endPosition;
    uint color;
};

struct Attributes
{
    uint vertexID : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionHCS : SV_POSITION;
    float4 color : COLOR0;
    UNITY_VERTEX_OUTPUT_STEREO
};

CBUFFER_START(UnityPerMaterial)
half _OccludedAlpha;
StructuredBuffer<LineInstanceData> _LineData;
CBUFFER_END

void unity_instancing_procedural_func()
{
    #if defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    // unity_StereoEyeIndex should be set by gl_ViewID (OVR_multiview)
    // unity_InstanceID will have been set by UNITY_SETUP_INSTANCE_ID
    #elif defined(UNITY_STEREO_INSTANCING_ENABLED)
    // This should have been run as a result of UNITY_SETUP_INSTANCE_ID
    //unity_StereoEyeIndex = unity_InstanceID & 0x01;
    //unity_InstanceID = localBaseInstanceId + (unity_InstanceID >> 1);
    #else
    // unity_InstanceID will have been set by UNITY_SETUP_INSTANCE_ID
    #endif
}

Varyings vert (Attributes IN)
{
    Varyings OUT;

    //  --- Note(dan): omg this is terrible.
    //                 Somehow (unity_BaseInstanceID > 4294967295) is true. How does that work?
    //                 Nevertheless, Unity should have been setting unity_BaseInstanceID.
    //                 I'm using DrawProcedural, it shouldn't be possible to start at an offset.
    //                 Just found this in another project:
    //                 ```
    //                      wtf, if I don't mod this number, it's based at  (0x40800000) (1082130432)
    //                      uint id = unity_InstanceID % _PointCloudDataCount;
    //                 ```
    //                 Hmmmmmmm.
    //                 HACK
    #if defined(UNITY_ANY_INSTANCING_ENABLED)
    unity_BaseInstanceID = 0;
    #endif
    
	ZERO_INITIALIZE(Varyings, OUT);
    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    
    #if defined(UNITY_ANY_INSTANCING_ENABLED)
    uint id = unity_InstanceID;
    #else
    uint id = 0;
    #endif

    LineInstanceData lineVertData = _LineData[id];

    float pixelWidth = lineVertData.startPosition.w;
    float worldSize = pixelWidth * 0.0005f;
    
    float3 vertStartWorldPos = lineVertData.startPosition.xyz;
    float3 vertEndWorldPos = lineVertData.endPosition.xyz;
    
    uint u = IN.vertexID >> 1;
    uint v = ((IN.vertexID >> 1) + (IN.vertexID & 1)) & 1;

    float3 vertPos = (u == 0)
                        ? vertStartWorldPos// camPos + float3(1, 1, 1) * 0.001 //vertStartWorldPos
                        : vertEndWorldPos;

    float pixelOffsetSize;
    if (!unity_OrthoParams.w)
    {
        //  --- We only use the w value though
        // float4 vertPosHCS = TransformWorldToHClip(vertPos);
        // float4 vertPosHCS = mul(UNITY_MATRIX_VP, float4(vertPos, 1.0));
        float resultW = mul(UNITY_MATRIX_VP._m30_m31_m32_m33, float4(vertPos, 1));
        float screenYInv = _ScreenParams.z - 1;
        pixelOffsetSize = (pixelWidth * screenYInv) * resultW;
        pixelOffsetSize *= _ScreenParams.x * screenYInv; // * aspect
    }
    else
    {
        // bump up by 2x... I don't know why :(
        pixelOffsetSize = pixelWidth * (unity_OrthoParams.xy / _ScreenParams.xy) * 2.0f;
    }

    //  --- World size means close ends are a little bigger, pixelSize means far ends don't disappear
    float width = (worldSize + pixelOffsetSize) * 0.5f;

    float3 up = normalize(cross(vertEndWorldPos.xyz - vertStartWorldPos.xyz, vertPos.xyz - _WorldSpaceCameraPos.xyz));
    float3 worldOffset = up * width;
    
    vertPos += (v == 0)
                   ? worldOffset
                   : -worldOffset;

    //  --- Transform final vert position
    float4 vertPosHCS = TransformWorldToHClip(vertPos);

    float scale = 1.0f / 255.0f;
	OUT.color.a = (lineVertData.color >> 24) * scale;
    OUT.color.b = ((lineVertData.color & 0x00FFFFFF) >> 16) * scale;
    OUT.color.g = ((lineVertData.color & 0x0000FFFF) >> 8) * scale;
    OUT.color.r = (lineVertData.color & 0x000000FF) * scale;
    
    // lightsaber boooost: OUT.color.rgb *= OUT.color.rgb * 40;
    
    OUT.positionHCS = vertPosHCS;
    
    return OUT;
}
