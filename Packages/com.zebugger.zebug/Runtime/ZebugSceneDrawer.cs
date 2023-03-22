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
using Unity.Collections;

using UnityEngine;
using UnityEngine.Rendering;

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

        public class LineRenderData
        {
            public WidthType type;
            public int lineCount;
            public Vector3[] verts = new Vector3[0];
            public Color[] colors = new Color[0];
            public float[] widths = new float[0];

            public int currentIdx;
            
            public void BeginAddingLines()
            {
                currentIdx = 0;
                if (colors.Length < lineCount)
                {
                    Array.Resize(ref verts, lineCount * 2);
                    Array.Resize(ref colors, lineCount);
                    Array.Resize(ref widths, lineCount);
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
            }
            
            CreateLineMaterial();

            GL.PushMatrix();
            GL.LoadProjectionMatrix(cam.projectionMatrix);
            
            Vector3 camUp = cam.transform.up;
            
            
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
                            DrawLinesQuadsAdaptiveWidth(data);
            
                            s_LineMaterial.SetPass(1);
                            DrawLinesQuadsAdaptiveWidth(data);
                            break;
                        }
                        
                        case WidthType.World:
                        {
                            //  --- Draw quads
                            s_LineMaterial.SetPass(0);
                            DrawLinesQuadsWorldWidth(data);
            
                            s_LineMaterial.SetPass(1);
                            DrawLinesQuadsWorldWidth(data);
                            break;
                        }
                        
                        case WidthType.Pixels:
                        {
                            //  --- Draw quads
                            s_LineMaterial.SetPass(0);
                            DrawLinesQuadsPixelsWidth(data);
            
                            s_LineMaterial.SetPass(1);
                            DrawLinesQuadsPixelsWidth(data);
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
            
            void DrawLinesQuadsAdaptiveWidth(LineRenderData lineRenderData)
            {
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

                    Vector3 startOffset = offsetDir * (pixelWidth + worldOffsetStartScreenSize)*0.5f; 
                    Vector3 q0 = cam.ScreenToWorldPoint(screenStart + startOffset);
                    Vector3 q1 = cam.ScreenToWorldPoint(screenStart - startOffset);
                    
                    Vector3 worldOffsetEndPos = endPos + worldOffset;
                    Vector3 worldOffsetEndScreenPoint = cam.WorldToScreenPoint(worldOffsetEndPos);
                    Vector3 worldOffsetEndScreenDelta = screenEnd - worldOffsetEndScreenPoint;
                    worldOffsetEndScreenDelta.z = 0;
                    float worldOffsetEndScreenSize = worldOffsetEndScreenDelta.magnitude;

                    Vector3 endOffset = offsetDir * (pixelWidth + worldOffsetEndScreenSize)*0.5f; 
                    Vector3 q3 = cam.ScreenToWorldPoint(screenEnd - endOffset);
                    Vector3 q2 = cam.ScreenToWorldPoint(screenEnd + endOffset);

                    GL.Vertex(q0);
                    GL.Vertex(q1);
                    GL.Vertex(q3);
                    GL.Vertex(q2);
                }

                GL.End();
            }
            
            void DrawLinesQuadsWorldWidth(LineRenderData lineRenderData)
            {
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
            
            void DrawLinesQuadsPixelsWidth(LineRenderData lineRenderData)
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

            GL.PopMatrix();
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

}