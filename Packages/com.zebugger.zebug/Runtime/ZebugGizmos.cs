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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Profiling;
using ZebugProject.Util;

namespace ZebugProject
{
    using static ZebugUtil;

    //  --------------------------------------------------------------------------------------------
    //  --------------------------------------------------------------------------------------------
    
    public class LineData
    {
        public Vector3 startPosition;
        public Vector3 endPosition;
        public Color color;
        public float endTime;
        public float width;
        
        private static List<LineData> s_Pool = new List<LineData>();
        public static LineData GetPooled()
        {
            if (s_Pool.Count > 0)
            {
                LineData line = s_Pool[s_Pool.Count-1];
                s_Pool.RemoveAt(s_Pool.Count-1);
                return line;
            }
            else
            {
                return new LineData();
            }
        }
        
        public static void ReturnPooled(LineData line)
        {
            line.startPosition = Vector3.zero;
            line.endPosition = Vector3.zero;
            line.color = Color.clear;
            line.endTime = 0f;
            line.width = 0f;
            
            s_Pool.Add(line);
        }
    }
    
    //  --------------------------------------------------------------------------------------------
    //  --------------------------------------------------------------------------------------------

    public class ChannelLineData
    {
        public List<LineData> lines = new List<LineData>();
    }

    //  --------------------------------------------------------------------------------------------
    //  --------------------------------------------------------------------------------------------

    public partial class Channel<T>
    {
        //  --- Your inheriting class can override this value to do all its drawing on device
        protected float m_LineDrawingWidth = 2f;
        
        //  ----------------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("DEVELOPMENT_BUILD")]
        [Conditional("UNITY_EDITOR")]
        public static void DrawRay(Ray ray, float maxDist)
        {
            DrawRay(ray, maxDist, Instance.m_ChannelColor);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("DEVELOPMENT_BUILD")]
        [Conditional("UNITY_EDITOR")]
        public static void DrawRay(Ray ray, float maxDist, Color color, float duration = 0)
        {
            DrawLine(ray.origin, ray.origin + ray.direction * maxDist, color);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("DEVELOPMENT_BUILD")]
        [Conditional("UNITY_EDITOR")]
        public static void DrawLine(Vector3 startPosition, Vector3 endPosition)
        {
            DrawLine(startPosition, endPosition, Instance.m_ChannelColor);
        }

        [Conditional("DEVELOPMENT_BUILD")]
        [Conditional("UNITY_EDITOR")]
        public static void DrawLine( Vector3 startPosition
                                   , Vector3 endPosition
                                   , Color color
                                   , float duration = 0)
        {
            Channel<T> instance = Instance;
            
            if (!instance.GizmosEnabled())
            {
                return;
            }

            if (!Zebug.s_ChannelLines.TryGetValue(instance, out ChannelLineData data))
            {
                data = new ChannelLineData();

                Zebug.s_ChannelLines.Add(instance, data);
            }

            var line = LineData.GetPooled();
            line.startPosition = startPosition;
            line.endPosition = endPosition;
            line.color = color;
            line.endTime = Time.time + duration;
            line.width = instance.m_LineDrawingWidth;
            
            data.lines.Add(line);
        }

        //  ----------------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("DEVELOPMENT_BUILD")]
        [Conditional("UNITY_EDITOR")]
        public static void DrawTransformLocator(Transform tForm, float scale = 0.1f, float duration = 0)
        {
            DrawLocator(tForm.position, scale, tForm.rotation, duration);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("DEVELOPMENT_BUILD")]
        [Conditional("UNITY_EDITOR")]
        public static void DrawLocator(Vector3 position, float scale = 0.1f, Quaternion rotation = default, float duration = 0)
        {

            if (Math.Abs(rotation.x + rotation.x + rotation.x + rotation.x) < 0.0001f)
            {
                rotation = new Quaternion(0,0,0,1);
            }

            DrawLine(position, position + (rotation * RightVec * scale),   Color.red,   duration);
            DrawLine(position, position + (rotation * UpVec * scale),      Color.green, duration);
            DrawLine(position, position + (rotation * ForwardVec * scale), Color.blue,  duration);
        }

        //  ----------------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("DEVELOPMENT_BUILD")]
        [Conditional("UNITY_EDITOR")]
        public static void DrawBurst(Vector3 position, float size, Color color = new Color(), float duration = 0f)
        {
            if (Instance.m_GizmosEnabled)
            {
                DrawLine(position + new Vector3(0, -size, 0), position + new Vector3(0, size, 0), color, duration);
                DrawLine(position + new Vector3(-size, 0, 0), position + new Vector3(size, 0, 0), color, duration);
                DrawLine(position + new Vector3(0, 0, -size), position + new Vector3(0, 0, size), color, duration);

                //  --- diagonal
                size = size/1.73f;
                DrawLine(position + new Vector3(-size, -size, -size), position + new Vector3(size, size, size), color, duration);
                DrawLine(position + new Vector3(-size, -size, size), position + new Vector3(size, size, -size), color, duration);
                DrawLine(position + new Vector3(-size, size, size), position + new Vector3(size, -size, -size), color, duration);
                DrawLine(position + new Vector3(-size, size, -size), position + new Vector3(size, -size, size), color, duration);
            }
        }

        //  ----------------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("DEVELOPMENT_BUILD")]
        [Conditional("UNITY_EDITOR")]
        public static void DrawPlus(Vector3 center, float size, Color color = default, float duration = 0f)
        {
            if (color == default)
            {
                color = Instance.m_ChannelColor;
            }
            
            if (Instance.m_GizmosEnabled)
            {
                DrawLine(center + new Vector3(-size, 0, 0), center + new Vector3(size, 0, 0), color, duration);
                DrawLine(center + new Vector3(0, 0, -size), center + new Vector3(0, 0, size), color, duration);
            }
        }
        
        //  ----------------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("DEVELOPMENT_BUILD")]
        [Conditional("UNITY_EDITOR")]
        public static void DrawBox(Vector3 center, Quaternion rotation, Vector3 size)
        {
            DrawBox(center, rotation, size, Instance.m_ChannelColor);
        }

        //  ----------------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("DEVELOPMENT_BUILD")]
        [Conditional("UNITY_EDITOR")]
        public static void DrawBox(Bounds sceneBounds, Color color, float duration)
        {
            DrawBox(sceneBounds.center, Quaternion.identity, sceneBounds.size, color, duration);
        }
        
        //  ----------------------------------------------------------------------------------------

        [Conditional("DEVELOPMENT_BUILD")]
        [Conditional("UNITY_EDITOR")]
        public static void DrawBox( Vector3 center
                                  , Quaternion rotation
                                  , Vector3 size
                                  , Color color
                                  , float duration = 0)
        {
            if (!Instance.GizmosEnabled())
            {
                return;
            }

            Vector3 halfSize = size * 0.5f;

            Vector3 ruf = rotation * Vector3.Scale(new Vector3(1, 1, 1), halfSize) + center;
            Vector3 rub = rotation * Vector3.Scale(new Vector3(1, 1, -1), halfSize) + center;
            Vector3 rdf = rotation * Vector3.Scale(new Vector3(1, -1, 1), halfSize) + center;
            Vector3 rdb = rotation * Vector3.Scale(new Vector3(1, -1, -1), halfSize) + center;
            Vector3 luf = rotation * Vector3.Scale(new Vector3(-1, 1, 1), halfSize) + center;
            Vector3 lub = rotation * Vector3.Scale(new Vector3(-1, 1, -1), halfSize) + center;
            Vector3 ldf = rotation * Vector3.Scale(new Vector3(-1, -1, 1), halfSize) + center;
            Vector3 ldb = rotation * Vector3.Scale(new Vector3(-1, -1, -1), halfSize) + center;

            // --- up square
            DrawLine(ruf, rub, color, duration);
            DrawLine(rub, lub, color, duration);
            DrawLine(lub, luf, color, duration);
            DrawLine(luf, ruf, color, duration);

            // --- edges down
            DrawLine(ruf, rdf, color, duration);
            DrawLine(rub, rdb, color, duration);
            DrawLine(lub, ldb, color, duration);
            DrawLine(luf, ldf, color, duration);

            // --- down square
            DrawLine(rdf, rdb, color, duration);
            DrawLine(rdb, ldb, color, duration);
            DrawLine(ldb, ldf, color, duration);
            DrawLine(ldf, rdf, color, duration);
        }

        //  ----------------------------------------------------------------------------------------
    }

}