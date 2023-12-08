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
using System.Runtime.InteropServices;

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace ZebugProject
{
    public class ZebugSceneDrawer : MonoBehaviour
    {
        [SerializeField] private bool _jobMode = true;

        private static ZebugSceneDrawer s_Instance;
        
        static Material s_LineMaterial;
        
        private static readonly int s_SrcBlendId = Shader.PropertyToID("_SrcBlend");
        private static readonly int s_DstBlendId = Shader.PropertyToID("_DstBlend");
        private static readonly int s_CullId = Shader.PropertyToID("_Cull");
        private static readonly int s_OccludedAlphaId = Shader.PropertyToID("_OccludedAlpha");

        protected void Awake()
        {
            for (int i = 0; i < _renderTypeData.Length; i++)
            {
                _renderTypeData[i] = new LineRenderData();
                _renderTypeData[i].type = (WidthType)i;
            }
                
            //  --- If Built-in RP
            Camera.onPostRender += OnCamPostRender;
            
            //  --- If URP / HDRP
            RenderPipelineManager.endCameraRendering += OnSrpEndCamRendering;
        }

        private void OnSrpEndCamRendering(ScriptableRenderContext context, Camera cam)
        {
            OnCamPostRender(cam);
        }

        protected void OnDestroy()
        {
            Camera.onPostRender -= OnCamPostRender;
            RenderPipelineManager.endCameraRendering -= OnSrpEndCamRendering;
            
            for (int i = 0; i < _renderTypeData.Length; i++)
            {
                _renderTypeData[i].Dispose();
            }   
        }
        
        private void CreateLineMaterial()
        {
            if (!s_LineMaterial)
            {
                // Unity has a built-in shader that is useful for drawing
                // simple colored things.
                Shader resourceShader = Resources.Load<Shader>("Simple-Colored");
                s_LineMaterial = new Material(resourceShader);
                s_LineMaterial.hideFlags = HideFlags.HideAndDontSave;
                // Turn on alpha blending
                s_LineMaterial.SetInt(s_SrcBlendId, (int)BlendMode.SrcAlpha);
                s_LineMaterial.SetInt(s_DstBlendId, (int)BlendMode.OneMinusSrcAlpha);
                // Turn backface culling off
                s_LineMaterial.SetInt(s_CullId, (int)CullMode.Off);
                s_LineMaterial.SetFloat(s_OccludedAlphaId, 0.125f);
            }
        }

        public class LineRenderData : IDisposable
        {
            public WidthType type;
            public int lineCount;
            public NativeArray<float3> verts = new (0, Allocator.Persistent);
            public NativeArray<Color> colors = new (0, Allocator.Persistent);
            public NativeArray<float> widths = new (0, Allocator.Persistent);

            public int currentIdx;
            
            public void BeginAddingLines()
            {
                currentIdx = 0;
                if (colors.Length < lineCount)
                {
                    verts.Dispose();
                    colors.Dispose();
                    widths.Dispose();
                    verts = new NativeArray<float3>(lineCount * 2, Allocator.Persistent);
                    colors = new NativeArray<Color>(lineCount, Allocator.Persistent);
                    widths = new NativeArray<float>(lineCount, Allocator.Persistent);
                }
            }

            public void AddLine(LineData line)
            {
                colors[currentIdx] = line.color;
                verts[currentIdx*2] = line.startPosition;
                verts[currentIdx*2 + 1] = line.endPosition;
                widths[currentIdx] = line.width; 
                currentIdx++;
            }

            public bool HasLines()
            {
                return currentIdx > 0;
            }

            public void Dispose()
            {
                if (verts.IsCreated) { verts.Dispose(); }
                if (colors.IsCreated) { colors.Dispose(); }
                if (widths.IsCreated) { widths.Dispose(); }
            }
        }
        
        private LineRenderData[] _renderTypeData = new LineRenderData[(int)WidthType.Count];

        //  --- NOTE(dan): OnRenderObject is a better time to be calling this, because it's before
        //                 the depth buffer gets wiped out by post-processing or something.
        //                 occlusion transparency isn't working great in OnCamPostRender.
        //                 But weird things happen if I try using OnCamPostRender... the scene is
        //                 shifted up by 1m if I can see the origin. This is extraordinarily vexing.
        //                 The real way to do this is using CommandBuffer.DrawProcedural, I expect.
        //  --- TODO(dan): Convert to using CommandBuffer.DrawProcedural so we can make sure it
        //                 happens at the correct time in the rendering pipe. I expect it will be
        //                 quicker, especially on platforms where we can write directly into GPU
        //                 memory when we write verts into a CommandBuffer. Bonus points for doing
        //                 the points-to-quads generation in a compute shader or something.
        
        private void OnCamPostRender(Camera cam)
        {
            if (cam.cameraType != CameraType.SceneView && cam.cameraType != CameraType.Game)
            {
                return;
            }  
            
            float time = Time.time;

            foreach (LineRenderData data in _renderTypeData)
            {
                data.lineCount = 0;
            }

            int lineCount = 0;
            foreach (KeyValuePair<IChannel, ChannelLineData> data in Zebug.s_ChannelLines)
            {
                (IChannel channel, ChannelLineData channelLineData) = (data.Key, data.Value);

                if (channelLineData.type == ChannelLineData.Type.Runtime && channel.GizmosEnabled())
                {
                    LineRenderData lineRenderData = _renderTypeData[(int)channelLineData.widthType];
                    lineRenderData.lineCount += channelLineData.lines.Count;
                    lineCount += channelLineData.lines.Count;
                }
            }

            if (lineCount == 0)
            {
                return;
            }

            foreach (LineRenderData data in _renderTypeData)
            {
                data.BeginAddingLines();
            }

            foreach (KeyValuePair<IChannel, ChannelLineData> data in Zebug.s_ChannelLines)
            {
                (IChannel channel, ChannelLineData channelLineData) = (data.Key, data.Value);

                if (!Application.isEditor)
                {
                    if (channelLineData.type == ChannelLineData.Type.Editor)
                    {
                        channelLineData.lines.Clear();
                    }
                }

                if (channelLineData.type == ChannelLineData.Type.Runtime)
                {
                    bool drawLines = channel.GizmosEnabled();

                    LineRenderData lineRenderData = _renderTypeData[(int)channelLineData.widthType];

                    List<LineData> lines = channelLineData.lines;
                    for (int i = lines.Count - 1; i >= 0; i--)
                    {
                        LineData line = lines[i];
                        if (drawLines)
                        {
                            lineRenderData.AddLine(line);
                        }

                        if (time >= line.endTime)
                        {
                            // List is non-ordered... switch end to remove to prevent memory reshuffle
                            int lastIdx = lines.Count - 1;
                            lines[i] = lines[lastIdx];
                            lines.RemoveAt(lastIdx);
                            continue;
                        }
                    }
                }
            }

            CreateLineMaterial();

            GL.PushMatrix();
            GL.LoadProjectionMatrix(cam.projectionMatrix);
            GL.modelview = cam.worldToCameraMatrix;
            
            //  --- Draw lines
            foreach (LineRenderData data in _renderTypeData)
            {
                if (data.HasLines())
                {
                    switch (data.type)
                    {
                        case WidthType.Adaptive:
                        {
                            //  --- Draw quads
                            s_LineMaterial.SetPass(0);
                            DrawLinesQuadsAdaptiveWidth(data, cam, _jobMode);

                            s_LineMaterial.SetPass(1);
                            DrawLinesQuadsAdaptiveWidth(data, cam, _jobMode);
                            break;
                        }

                        case WidthType.World:
                        {
                            //  --- Draw quads
                            s_LineMaterial.SetPass(0);
                            DrawLinesQuadsWorldWidth(data, cam);

                            s_LineMaterial.SetPass(1);
                            DrawLinesQuadsWorldWidth(data, cam);
                            break;
                        }

                        case WidthType.Pixels:
                        {
                            //  --- Draw quads
                            s_LineMaterial.SetPass(0);
                            DrawLinesQuadsPixelsWidth(data, cam);

                            s_LineMaterial.SetPass(1);
                            DrawLinesQuadsPixelsWidth(data, cam);
                            break;
                        }

                        case WidthType.SinglePixel:
                        {
                            //  --- Draw quads
                            s_LineMaterial.SetPass(0);
                            DrawLinesSingleWidth(data);

                            s_LineMaterial.SetPass(1);
                            DrawLinesSingleWidth(data);
                            break;
                        }

                        default: throw new ArgumentOutOfRangeException();
                    }
                }
            }

            GL.PopMatrix();
        }

        //  ----------------------------------------------------------------------------------------

        static void DrawLinesQuadsAdaptiveWidth(LineRenderData lineRenderData, Camera cam, bool useJob)
        {
            // "view matrix"
            Matrix4x4 worldToCam = cam.worldToCameraMatrix;
            Matrix4x4 camToWorld = cam.cameraToWorldMatrix;
            Matrix4x4 projMat = cam.projectionMatrix;

            Matrix4x4 viewProjMat = projMat * worldToCam;

            Matrix4x4 clipToWorld = camToWorld * projMat.inverse;

            Vector3 camGLForward = (camToWorld * -Vector3.forward).normalized;
            Vector3 camPos = camToWorld * (Vector4)new Vector4(0,0,0,1);
            Vector3 camUp = cam.transform.up;

            Rect pixelRect = cam.pixelRect;
            Vector2 pixelRectMin = pixelRect.min;
            Vector2 pixelRectMax = pixelRect.max;
            bool isPerspective = !cam.orthographic;

            if (useJob)
            {
                int lineCount = lineRenderData.lineCount;

                const int kVertsPerLineSegment = 2;
                const int kVertsPerLineQuad = 4;

                NativeArray<float3> verts
                    = new NativeArray<float3>(lineCount * kVertsPerLineSegment, Allocator.TempJob);
                NativeArray<float> widths
                    = new NativeArray<float>(lineCount, Allocator.TempJob);

                NativeArray<float3> outVerts
                    = new NativeArray<float3>(lineCount * kVertsPerLineQuad, Allocator.TempJob);
                JobHandle calcAdaptiveVertsJobHandle = default;

                try {

                NativeArray<float3>.Copy(lineRenderData.verts, verts, lineCount * kVertsPerLineSegment);
                NativeArray<float>.Copy(lineRenderData.widths, widths, lineCount);

                var calcAdaptiveVertsJob = new AdaptiveLineVertsJob()
                {
                    viewProjMat = viewProjMat,
                    clipToWorldMat = clipToWorld,
                    camGLForward = new float4(camGLForward, 0),
                    pixelRectMin = new float4(pixelRect.min, 0),
                    pixelRectMax = new float4(pixelRect.max, 0),
                    _camUp = camUp,
                    _camPos = new float4(camPos, 1),
                    _isPerspective = isPerspective,
                    _lineCount = lineRenderData.lineCount,
                    _verts = verts.AsReadOnly(),
                    _widths = widths.AsReadOnly(),
                    _outVerts = outVerts,
                };

                //calcAdaptiveVertsJob.Run();
                calcAdaptiveVertsJobHandle = calcAdaptiveVertsJob.Schedule();
                calcAdaptiveVertsJobHandle.Complete();

                GL.Begin(GL.QUADS);
                for (int i = 0; i < lineCount; i++)
                {
                    GL.Color(lineRenderData.colors[i]);
                    int baseIdx = i * 4;
                    GL.Vertex(outVerts[baseIdx + 0]);
                    GL.Vertex(outVerts[baseIdx + 1]);
                    GL.Vertex(outVerts[baseIdx + 2]);
                    GL.Vertex(outVerts[baseIdx + 3]);
                }
                GL.End();

                } finally {
                    calcAdaptiveVertsJobHandle.Complete();
                    verts.Dispose();
                    widths.Dispose();
                    outVerts.Dispose();
                }

            } else
            {



            GL.Begin(GL.QUADS);
            int lineDataCount = lineRenderData.lineCount;
            for (int i = 0; i < lineDataCount; i++)
            {
                float pixelWidth = lineRenderData.widths[i];
                float worldSize = pixelWidth * 0.001f;
                Vector3 worldOffset = camUp * worldSize;

                GL.Color(lineRenderData.colors[i]);

                Vector3 startPos = lineRenderData.verts[i * 2];
                Vector3 endPos = lineRenderData.verts[i * 2 + 1];

                Vector3 screenStart = CamWorldToScreen(startPos, viewProjMat, camPos, camGLForward, pixelRectMin, pixelRectMax, isPerspective);
                Vector3 screenEnd = CamWorldToScreen(endPos, viewProjMat, camPos, camGLForward, pixelRectMin, pixelRectMax, isPerspective);

                Vector3 screenDir = (screenEnd - screenStart);
                screenDir.z = 0;

                Vector3 offsetDir = Vector3.Cross(screenDir, Vector3.forward);
                if (offsetDir.sqrMagnitude < 0.01)
                {
                    offsetDir = new Vector3(0, 1, 0);
                }
                offsetDir = offsetDir.normalized;

                Vector3 worldOffsetStartPos = startPos + worldOffset;
                Vector3 worldOffsetStartScreenPoint = CamWorldToScreen(worldOffsetStartPos, viewProjMat, camPos, camGLForward, pixelRectMin, pixelRectMax, isPerspective);
                Vector3 worldOffsetStartScreenDelta = screenStart - worldOffsetStartScreenPoint;
                worldOffsetStartScreenDelta.z = 0;
                float worldOffsetStartScreenSize = worldOffsetStartScreenDelta.magnitude;

                Vector3 startOffset = offsetDir * (pixelWidth + worldOffsetStartScreenSize)*0.5f;
                Vector3 q0 = ScreenToWorldPoint(screenStart + startOffset, clipToWorld, camPos, camGLForward, pixelRectMin, pixelRectMax, isPerspective);
                Vector3 q1 = ScreenToWorldPoint(screenStart - startOffset, clipToWorld, camPos, camGLForward, pixelRectMin, pixelRectMax, isPerspective);

                Vector3 worldOffsetEndPos = endPos + worldOffset;
                Vector3 worldOffsetEndScreenPoint = CamWorldToScreen(worldOffsetEndPos, viewProjMat, camPos, camGLForward, pixelRectMin, pixelRectMax, isPerspective);
                Vector3 worldOffsetEndScreenDelta = screenEnd - worldOffsetEndScreenPoint;
                worldOffsetEndScreenDelta.z = 0;
                float worldOffsetEndScreenSize = worldOffsetEndScreenDelta.magnitude;

                Vector3 endOffset = offsetDir * (pixelWidth + worldOffsetEndScreenSize)*0.5f;
                Vector3 q3 = ScreenToWorldPoint(screenEnd - endOffset, clipToWorld, camPos, camGLForward, pixelRectMin, pixelRectMax, isPerspective);
                Vector3 q2 = ScreenToWorldPoint(screenEnd + endOffset, clipToWorld, camPos, camGLForward, pixelRectMin, pixelRectMax, isPerspective);

                GL.Vertex(q0);
                GL.Vertex(q1);
                GL.Vertex(q3);
                GL.Vertex(q2);
            }

            GL.End();
            }
        }

        void DrawLinesQuadsWorldWidth(LineRenderData lineRenderData, Camera cam)
        {
            Vector3 camUp = cam.transform.up;

            GL.Begin(GL.QUADS);
            int lineDataCount = lineRenderData.lineCount;
            for (var i = 0; i < lineDataCount; i++)
            {
                float pixelWidth = lineRenderData.widths[i];
                float worldSize = pixelWidth * 0.001f;
                Vector3 worldOffset = camUp * worldSize;

                GL.Color(lineRenderData.colors[i]);

                Vector3 startPos = lineRenderData.verts[i * 2];
                Vector3 endPos = lineRenderData.verts[i * 2 + 1];

                Vector3 screenStart = cam.WorldToScreenPoint(startPos);
                Vector3 screenEnd = cam.WorldToScreenPoint(endPos);

                Vector3 screenDir = (screenEnd - screenStart);
                screenDir.z = 0;

                Vector3 offsetDir = Vector3.Cross(screenDir, Vector3.forward);
                if (offsetDir.sqrMagnitude < 0.01)
                {
                    offsetDir = new Vector3(0, 1, 0);
                }
                offsetDir = offsetDir.normalized;

                Vector3 worldOffsetStartPos = startPos + worldOffset;
                Vector3 worldOffsetStartScreenPoint = cam.WorldToScreenPoint(worldOffsetStartPos);
                Vector3 worldOffsetStartScreenDelta = screenStart - worldOffsetStartScreenPoint;
                worldOffsetStartScreenDelta.z = 0;
                float worldOffsetStartScreenSize = worldOffsetStartScreenDelta.magnitude;

                Vector3 startOffset = offsetDir * worldOffsetStartScreenSize * 0.5f;
                Vector3 q0 = cam.ScreenToWorldPoint(screenStart + startOffset);
                Vector3 q1 = cam.ScreenToWorldPoint(screenStart - startOffset);

                Vector3 worldOffsetEndPos = endPos + worldOffset;
                Vector3 worldOffsetEndScreenPoint = cam.WorldToScreenPoint(worldOffsetEndPos);
                Vector3 worldOffsetEndScreenDelta = screenEnd - worldOffsetEndScreenPoint;
                worldOffsetEndScreenDelta.z = 0;
                float worldOffsetEndScreenSize = worldOffsetEndScreenDelta.magnitude;

                Vector3 endOffset = offsetDir * worldOffsetEndScreenSize * 0.5f;
                Vector3 q3 = cam.ScreenToWorldPoint(screenEnd - endOffset);
                Vector3 q2 = cam.ScreenToWorldPoint(screenEnd + endOffset);

                GL.Vertex(q0);
                GL.Vertex(q1);
                GL.Vertex(q3);
                GL.Vertex(q2);
            }

            GL.End();
        }

        void DrawLinesQuadsPixelsWidth(LineRenderData lineRenderData, Camera cam)
        {
            GL.Begin(GL.QUADS);
            int lineDataCount = lineRenderData.lineCount;
            for (var i = 0; i < lineDataCount; i++)
            {
                GL.Color(lineRenderData.colors[i]);

                Vector3 startPos = lineRenderData.verts[i * 2];
                Vector3 endPos = lineRenderData.verts[i * 2 + 1];

                Vector3 screenStart = cam.WorldToScreenPoint(startPos);
                Vector3 screenEnd = cam.WorldToScreenPoint(endPos);

                Vector3 screenDir = (screenEnd - screenStart);
                screenDir.z = 0;

                Vector3 offsetDir = Vector3.Cross(screenDir, Vector3.forward).normalized;
                Vector3 offset = offsetDir * lineRenderData.widths[i] * 0.5f;

                GL.Vertex(cam.ScreenToWorldPoint(screenStart + offset));
                GL.Vertex(cam.ScreenToWorldPoint(screenStart - offset));
                GL.Vertex(cam.ScreenToWorldPoint(screenEnd - offset));
                GL.Vertex(cam.ScreenToWorldPoint(screenEnd + offset));
            }

            GL.End();
        }

        void DrawLinesSingleWidth(LineRenderData lineRenderData)
        {
            GL.Begin(GL.LINES);
            int lineDataCount = lineRenderData.lineCount;
            for (var i = 0; i < lineDataCount; i++)
            {
                GL.Color(lineRenderData.colors[i]);
                GL.Vertex(lineRenderData.verts[i * 2]);
                GL.Vertex(lineRenderData.verts[i * 2 + 1]);
            }

            GL.End();
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
            if (s_Instance != null)
            {
                return;
            }

            var go = new GameObject("ZebugSceneDrawer Helper GO");
            s_Instance = go.AddComponent<ZebugSceneDrawer>();
            //go.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(go);

            Zebug.Log("Initializing Scene Drawer");
        }
        //  ----------------------------------------------------------------------------------------

        private void OnDrawGizmos()
        {
            Color oldGizmoColor = Gizmos.color;
            float time = Time.time;

            foreach (KeyValuePair<IChannel, ChannelLineData> channelData in Zebug.s_ChannelLines)
            {
                IChannel channel = channelData.Key;
                ChannelLineData linesChannelData = channelData.Value;
                
                bool drawGizmos = channel.GizmosEnabled()
                                  && linesChannelData.type == ChannelLineData.Type.Editor;
                
                List<LineData> lines = channelData.Value.lines;
                for (int i = lines.Count - 1; i >= 0; i--)
                {
                    LineData line = lines[i];
                    if (drawGizmos)
                    {
                        Gizmos.color = line.color;
                        Gizmos.DrawLine(line.startPosition, line.endPosition);
                    }

                    if (time > line.endTime)
                    {
                        // List is non-ordered... switch end to remove to prevent memory reshuffle
                        int lastIdx = lines.Count - 1; 
                        lines[i] = lines[lastIdx];
                        lines.RemoveAt(lastIdx);
                        continue;
                    }
                }
            }

            Gizmos.color = oldGizmoColor;
        }
    }

    
    [BurstCompile]
    public struct AdaptiveLineVertsJob : IJob
    {
        [ReadOnly] public float4x4 viewProjMat;
        [ReadOnly] public float4x4 clipToWorldMat;
        [ReadOnly] public float4 camGLForward;
        [ReadOnly] public float4 pixelRectMin;
        [ReadOnly] public float4 pixelRectMax;
        [ReadOnly] public float3 _camUp;
        [ReadOnly] public float4 _camPos;
        [ReadOnly] public bool _isPerspective;
        
        public int _lineCount;
        [ReadOnly] public NativeArray<float3>.ReadOnly _verts;
        [ReadOnly] public NativeArray<float>.ReadOnly _widths;
        
        //  --- Must be 2x the size of _verts
        [NoAlias, WriteOnly] public NativeArray<float3> _outVerts;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]    
        private float3 CamWorldToScreen(float3 targetPoint)                        
        {
            float4 targetPointH = new float4(targetPoint, 1);
            math.mul(viewProjMat, targetPointH);

            float4 clipPointH = math.mul(viewProjMat, new float4(targetPoint, 1));
            float4 clipPoint = clipPointH / clipPointH.w;

            float4 camToPoint = new float4(targetPoint,1) - _camPos;
            float pointDist = math.dot(camToPoint, camGLForward);

            float4 screenPoint = pixelRectMin + (1f + clipPoint) * 0.5f * pixelRectMax;
            screenPoint.z = pointDist;
                 
            return screenPoint.xyz;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]    
        private float3 ScreenToWorldPoint(float3 screenPoint)                        
        {
            float4 clipPoint = (new float4(screenPoint, 1) - pixelRectMin) * 2f / pixelRectMax - 1f;
            clipPoint.z = 0.95f;
            clipPoint.w = 1f;
            float4 pointOnPlane = math.mul(clipToWorldMat, clipPoint);
            pointOnPlane /= pointOnPlane.w;
            float4 dir = pointOnPlane - _camPos;

            float distToPlane = math.dot(dir, camGLForward);
            float4 worldPoint;
            if (math.abs(distToPlane) >= 1.0e-6f)
            {
                if (_isPerspective)
                {
                    dir *= screenPoint.z / distToPlane;
                    worldPoint = _camPos + dir;
                }
                else
                {
                    worldPoint = pointOnPlane - camGLForward * (distToPlane - screenPoint.z);
                }
                return worldPoint.xyz;
            }

            return float3.zero;
        } 

        public void Execute()
        {
            for (int i = 0; i < _lineCount; i++)
            {
                float pixelWidth = _widths[i];
                float worldSize = pixelWidth * 0.001f;
                float3 worldOffset = _camUp * worldSize;
            
                float3 startPos = _verts[i * 2];
                float3 endPos = _verts[i * 2 + 1];
            
                float3 screenStart = CamWorldToScreen(startPos);
                float3 screenEnd = CamWorldToScreen(endPos);
            
                float3 screenDir = (screenEnd - screenStart);
                screenDir.z = 0;
            
                float3 offsetDir = math.cross(screenDir, math.forward());
                if (math.lengthsq(offsetDir) < 0.01)
                {
                    offsetDir = new float3(0, 1, 0);
                }
                offsetDir = math.normalize(offsetDir);
            
                float3 worldOffsetStartPos = startPos + worldOffset;
                float3 worldOffsetStartScreenPoint = CamWorldToScreen(worldOffsetStartPos);
                float3 worldOffsetStartScreenDelta = screenStart - worldOffsetStartScreenPoint;
                worldOffsetStartScreenDelta.z = 0;
                float worldOffsetStartScreenSize = math.length(worldOffsetStartScreenDelta);
            
                float3 startOffset = offsetDir * (pixelWidth + worldOffsetStartScreenSize)*0.5f;
                float3 q0 = ScreenToWorldPoint(screenStart + startOffset);
                float3 q1 = ScreenToWorldPoint(screenStart - startOffset);
            
                float3 worldOffsetEndPos = endPos + worldOffset;
                float3 worldOffsetEndScreenPoint = CamWorldToScreen(worldOffsetEndPos);
                float3 worldOffsetEndScreenDelta = screenEnd - worldOffsetEndScreenPoint;
                worldOffsetEndScreenDelta.z = 0;
                float worldOffsetEndScreenSize = math.length(worldOffsetEndScreenDelta);
            
                float3 endOffset = offsetDir * (pixelWidth + worldOffsetEndScreenSize)*0.5f;
                float3 q3 = ScreenToWorldPoint(screenEnd - endOffset);
                float3 q2 = ScreenToWorldPoint(screenEnd + endOffset);
         
                _outVerts[i * 4 + 0] = q0;
                _outVerts[i * 4 + 1] = q1;
                _outVerts[i * 4 + 2] = q3;
                _outVerts[i * 4 + 3] = q2;
            }
        }
    }


}