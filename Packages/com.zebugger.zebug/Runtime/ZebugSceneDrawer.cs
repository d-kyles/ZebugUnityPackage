//  --- Zebug --------------------------------------------------------------------------------------
//  Copyright (c) 2022 Dan Kyles
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy of this software
//  and associated documentation files (the "Software"), to deal in the Software without
//  restriction, including without limitation the rights to use, copy, modify, merge, publish,
//  distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
//  Software is furnished to do so, subject to the following conditions:
//
//  The above copyright notice and this permission notice shall be included in all copies or
//  substantial portions of the Software.
//
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
//  BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//  NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
//  DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//  ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Unity.Collections;
using Unity.Mathematics;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace ZebugProject
{
    public class ZebugSceneDrawer : MonoBehaviour
    {
        private static ZebugSceneDrawer s_Instance;
        
        static Material s_LineMaterial;
        
        private static readonly int s_SrcBlendId = Shader.PropertyToID("_SrcBlend");
        private static readonly int s_DstBlendId = Shader.PropertyToID("_DstBlend");
        private static readonly int s_CullId = Shader.PropertyToID("_Cull");
        private static readonly int s_OccludedAlphaId = Shader.PropertyToID("_OccludedAlpha");
        private static readonly int s_LineDataParamId = Shader.PropertyToID("_LineData");

        private ZebugRenderPass _zebugRenderPass;

        struct LineInstanceData
        {
            // Struct needs to align with float4
            public float4 startPosition; // width is packed into w
            public float3 endPosition;
            public Color32 color;
            
            public const int Size = 32;
        };

        public class ZebugRenderPass : ScriptableRenderPass
        {
            private readonly Material _material;
            private readonly MaterialPropertyBlock _mpb;
            private readonly GraphicsBuffer _indices;

            private int _lineBufferLength;
            private GraphicsBuffer _lineData;
            private int _lineCount;

            public ZebugRenderPass()
            {
                _indices = new GraphicsBuffer(GraphicsBuffer.Target.Index, 6, sizeof(ushort));
                
                // 0 - 0,0
                // 1 - 0,1
                // 2 - 1,1
                // 3 - 1,0
                _indices.SetData(new ushort[] { 0, 1, 2, 0, 2, 3, });

                renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

                _mpb = new MaterialPropertyBlock();

                Shader resourceShader = Resources.Load<Shader>("Simple-Colored-Procedural");
                _material = new Material(resourceShader);
                _material.hideFlags = HideFlags.HideAndDontSave;
                // Turn on alpha blending
                _material.SetInt(s_SrcBlendId, (int)BlendMode.SrcAlpha);
                _material.SetInt(s_DstBlendId, (int)BlendMode.OneMinusSrcAlpha);
                // Turn backface culling off
                _material.SetInt(s_CullId, (int)CullMode.Off);
                _material.SetFloat(s_OccludedAlphaId, 0.125f);
                _material.enableInstancing = true;
                _material.SetKeyword(new LocalKeyword(_material.shader, "PROCEDURAL_INSTANCING_ON"), true);

                _lineBufferLength = 2048;

                _lineData = new GraphicsBuffer(GraphicsBuffer.Target.Structured
                                              , GraphicsBuffer.UsageFlags.LockBufferForWrite
                                              , _lineBufferLength
                                              , LineInstanceData.Size);
                _material.SetBuffer(s_LineDataParamId, _lineData);
            }

            private class PassData
            {
                public Material              material;
                public MaterialPropertyBlock mpb;
                public GraphicsBuffer        indices;
                public int                   lineBufferLength;
                public GraphicsBuffer        lineData;
                public int                   lineCount;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
            {
                const string passNameString = "Zebug Lines";
                
                using (var builder = renderGraph.AddRasterRenderPass(passNameString, out PassData passData))
                {
                    UniversalResourceData resourceData = frameContext.Get<UniversalResourceData>();
                    
                    passData.lineCount = _lineCount;
                    passData.indices = _indices;
                    passData.material = _material;
                    passData.mpb = _mpb;
                    
                    //  --- NOTE(dan): hack while figuring out how the rendergraph api actually works
                    builder.AllowPassCulling(false);
                    
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);
                    
                    builder.SetRenderFunc((PassData data, RasterGraphContext context)
                        => ExecutePass(data, context));
                }
            }

            private static void ExecutePass(PassData data, RasterGraphContext context)
            {
                var cmd = context.cmd;

                int instanceCount = data.lineCount;
                int everyPass = -1;
                int indexCount = 6;

                cmd.DrawProcedural(data.indices
                                  , Matrix4x4.identity
                                  , data.material
                                  , everyPass
                                  , MeshTopology.Triangles
                                  , indexCount
                                  , instanceCount
                                  , data.mpb);
            }
            
            public void UpdateLineData()
            {
                float time = Time.time;
                
                int lineCount = 0;
                foreach (KeyValuePair<IChannel, ChannelLineData> data in Zebug.s_ChannelLines)
                {
                    (IChannel channel, ChannelLineData channelLineData) = (data.Key, data.Value);

                    if (channel.GizmosEnabled())
                    {
                        lineCount += channelLineData.lines.Count;
                    }
                }

                if (_lineBufferLength < lineCount)
                {
                    while (_lineBufferLength < lineCount)
                    {
                        _lineBufferLength *= 2;
                    }
                    
                    _lineData.Dispose();
                    _lineData = new GraphicsBuffer(GraphicsBuffer.Target.Structured
                                                  , GraphicsBuffer.UsageFlags.LockBufferForWrite
                                                  , _lineBufferLength
                                                  , LineInstanceData.Size);
                    _material.SetBuffer(s_LineDataParamId, _lineData);
                }

                NativeArray<LineInstanceData> lineBuffer 
                    = _lineData.LockBufferForWrite<LineInstanceData>(0, lineCount);

                int lineIdx = 0;
                
                foreach (KeyValuePair<IChannel, ChannelLineData> data in Zebug.s_ChannelLines)
                {
                    (IChannel channel, ChannelLineData channelLineData) = (data.Key, data.Value);

                    bool drawLines = channel.GizmosEnabled();
                    var lines = channelLineData.lines;
                    
                    int lineListCount = lines.Count;
                    
                    if (drawLines)
                    {
                        Vector3 startPos;
                        Vector3 endPos;
                        
                        for (var lIdx = 0; lIdx < lineListCount; lIdx++)
                        {
                            var lineData = lines[lIdx];
                            
                            startPos = lineData.startPosition;
                            endPos = lineData.endPosition;
                            
                            lineBuffer[lineIdx++] = new LineInstanceData
                            {
                                startPosition = new float4(startPos.x, startPos.y, startPos.z, lineData.width),
                                endPosition = new float3(endPos.x, endPos.y, endPos.z),
                                color = ToColor32(lineData.color),
                            };
                        }
                    }

                    for (var idx = lineListCount - 1; idx >= 0; idx--)
                    {
                        var line = lines[idx];
                        if (time < line.endTime)
                        {
                            continue;
                        }

                        // List is non-ordered... switch end to remove to prevent memory reshuffle
                        int lastIdx = lineListCount - 1;
                        lines[idx] = lines[lastIdx];
                        lines.RemoveAt(lastIdx);
                        lineListCount--;
                        LineData.ReturnPooled(line);
                    }
                }
                
                _lineData.UnlockBufferAfterWrite<LineInstanceData>(lineCount);
                _lineCount = lineCount;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color32 ToColor32(Color c)
        {
            return new Color32((byte) (c.r * byte.MaxValue + 0.5f)
                             , (byte) (c.g * byte.MaxValue + 0.5f)
                             , (byte) (c.b * byte.MaxValue + 0.5f)
                             , (byte) (c.a * byte.MaxValue + 0.5f));
        }
        
        protected void Awake()
        {
            //  --- If URP / HDRP
            RenderPipelineManager.beginCameraRendering += OnSrpBeginCamRendering;
        }

        private void OnSrpBeginCamRendering(ScriptableRenderContext ctx, Camera cam)
        {
            if (_zebugRenderPass == null)
            {
                _zebugRenderPass = new ZebugRenderPass();
            }
            
            if (cam.cameraType != CameraType.SceneView && cam.cameraType != CameraType.Game)
            {
                return;
            }

            _zebugRenderPass.UpdateLineData();
            cam.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(_zebugRenderPass);
        }

        // protected void OnGUI()
        // {
        //     ZebugDebugGuiLayout.Instance.OnGUI();
        // }

        protected void OnDestroy()
        {
            RenderPipelineManager.beginCameraRendering -= OnSrpBeginCamRendering;
        }
        
        //  ----------------------------------------------------------------------------------------

        ///
        /// Significantly faster than calling cam.WorldToScreenPoint()
        ///
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector3 CamWorldToScreen( Vector3 targetPoint
                                       , Matrix4x4 worldToClip
                                       , Vector3 camPos
                                       , Vector3 camGLForward
                                       , Vector2 pixelRectMin
                                       , Vector2 pixelRectMax
                                       , bool isPerspective)
        {
            Vector3 clipPoint = worldToClip.MultiplyPoint(targetPoint);
            Vector3 camToPoint = targetPoint - camPos;
            float pointDist = Vector3.Dot(camToPoint, camGLForward);
            Vector3 screenPoint = default;
            screenPoint.x = pixelRectMin.x + (1f + clipPoint.x) * 0.5f * pixelRectMax.x;
            screenPoint.y = pixelRectMin.y + (1f + clipPoint.y) * 0.5f * pixelRectMax.y;
            screenPoint.z = pointDist;

            return screenPoint;
        }

        //  ----------------------------------------------------------------------------------------

        ///
        ///  Significantly faster than calling cam.ScreenToWorldPoint(), skips a full matrix inverse
        ///
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector3 ScreenToWorldPoint( Vector3 screenPoint
                                         , Matrix4x4 clipToWorld
                                         , Vector3 camPos
                                         , Vector3 camGLForward
                                         , Vector2 pixelRectMin
                                         , Vector2 pixelRectMax
                                         , bool isPerspective)
        {
            Vector3 clipPoint;
            clipPoint.x = (screenPoint.x - pixelRectMin.x) * 2f / pixelRectMax.x - 1f;
            clipPoint.y = (screenPoint.y - pixelRectMin.y) * 2f / pixelRectMax.y - 1f;
            clipPoint.z = 0.95f;

            Vector3 pointOnPlane = clipToWorld.MultiplyPoint(clipPoint);
            Vector3 dir = pointOnPlane - camPos;
            float distToPlane = Vector3.Dot(dir, camGLForward);

            Vector3 worldPoint = new Vector3(0,0,0);
            if (Math.Abs(distToPlane) >= 1.0e-6f)
            {
                if (isPerspective)
                {
                    dir *= screenPoint.z / distToPlane;
                    worldPoint = camPos + dir;
                }
                else
                {
                    worldPoint = pointOnPlane - camGLForward * (distToPlane - screenPoint.z);
                }
            }

            return worldPoint;
        }

        //  ----------------------------------------------------------------------------------------

        [RuntimeInitializeOnLoadMethod]
        protected static void InitializeOnLoad()
        {
            #if UNITY_WEBGL
            return;
            #endif
            if (s_Instance != null)
            {
                return;
            }

            var go = new GameObject("ZebugSceneDrawer Helper GO");
            s_Instance = go.AddComponent<ZebugSceneDrawer>();
            
            //go.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(go);

            Zebug.Log("Initializing Scene Drawer");
            
            Zebug.RaiseOnLoad();
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged += OnExitPlayMode;
            #endif
        }
        
        
        #if UNITY_EDITOR
        private static void OnExitPlayMode(UnityEditor.PlayModeStateChange state)
        {
            if(state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
            {
                Zebug.Log("Exiting Play mode: Unregistering handler.");
                
                // Unregister the handler so it doesn't affect the next Play mode run
                UnityEditor.EditorApplication.playModeStateChanged -= OnExitPlayMode;
                
                Zebug.RaiseOnExit();
            }
        }
        #endif
        
        //  ----------------------------------------------------------------------------------------
    }
}