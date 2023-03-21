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
        private static readonly int s_ZWriteId = Shader.PropertyToID("_ZWrite");
        private static readonly int s_OccludedAlphaId = Shader.PropertyToID("_OccludedAlpha");

        protected void Awake()
        {
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
                Shader shader = Shader.Find("Zebug/Simple-Colored");
                s_LineMaterial = new Material(shader);
                s_LineMaterial.hideFlags = HideFlags.HideAndDontSave;
                // Turn on alpha blending
                s_LineMaterial.SetInt(s_SrcBlendId, (int)BlendMode.SrcAlpha);
                s_LineMaterial.SetInt(s_DstBlendId, (int)BlendMode.OneMinusSrcAlpha);
                // Turn backface culling off
                s_LineMaterial.SetInt(s_CullId, (int)CullMode.Off);
                s_LineMaterial.SetFloat(s_OccludedAlphaId, 0.5f);
            }
        }

        // Will be called after all regular rendering is done
        //public void OnRenderObject()
        private void OnCamPostRender(Camera cam)
        {
            float time = Time.time;
            
            int lineCount = 0;
            foreach (KeyValuePair<IChannel, ChannelLineData> data in Zebug.s_ChannelLines)
            {
                (IChannel channel, ChannelLineData channelLineData) = (data.Key, data.Value);
                
                if (channelLineData.type == ChannelLineData.Type.Runtime && channel.GizmosEnabled())
                {
                    lineCount += channelLineData.lines.Count;
                }
            }
            
            if (lineCount == 0)
            {
                return;
            }
            
            NativeArray<Vector3> verts = new NativeArray<Vector3>(lineCount*2, Allocator.Temp); 
            NativeArray<Color> colors = new NativeArray<Color>(lineCount, Allocator.Temp);

            int curLineIdx = 0;
            
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
                    
                    List<LineData> lines = channelLineData.lines;
                    for (int i = lines.Count - 1; i >= 0; i--)
                    {
                        LineData line = lines[i];
                        if (drawLines)
                        {
                            colors[curLineIdx] = line.color;
                            verts[curLineIdx*2] = line.startPosition;
                            verts[curLineIdx*2 + 1] = line.endPosition;
                            
                            curLineIdx++;
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
            
            GL.InvalidateState();
            GL.RenderTargetBarrier();
            
            GL.PushMatrix();
            // Apply the line material

            GL.LoadProjectionMatrix(cam.projectionMatrix);
            //GL.MultMatrix(transform.localToWorldMatrix);
            
            // Set transformation matrix for drawing to
            // match our transform

            // Draw lines
            
            s_LineMaterial.SetPass(0);
            GL.Begin(GL.LINES);
            for (var i = 0; i < lineCount; i++)
            {
                GL.Color(colors[i]);
                GL.Vertex(verts[i*2]);
                GL.Vertex(verts[i*2 + 1]);
            }
            GL.End();
            
            s_LineMaterial.SetPass(1);
            GL.Begin(GL.LINES);
            for (var i = 0; i < lineCount; i++)
            {
                GL.Color(colors[i]);
                GL.Vertex(verts[i*2]);
                GL.Vertex(verts[i*2 + 1]);
            }
            GL.End();
            
            GL.PopMatrix();
            
            verts.Dispose();
            colors.Dispose();
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