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

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ZebugProject {
    public partial class Channel<T> {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        public static void DrawLine(Vector3 startPosition, Vector3 endPosition) {
            DrawLine(startPosition, endPosition, Instance.m_ChannelColor);
        }

        public static void DrawLine(Vector3 startPosition, Vector3 endPosition, Color color
            , float duration = 0) {
            if (!Instance.GizmosEnabled()) {
                return;
            }
            if (!Zebug.s_ChannelLines.TryGetValue(Instance, out List<LineData> lines)) {
                lines = new List<LineData>();
                Zebug.s_ChannelLines.Add(Instance, lines);
            }

            lines.Add(new LineData {
                startPosition = startPosition,
                endPosition = endPosition,
                color = color,
                endTime = Time.time + duration,
            });
        }

        //  ----------------------------------------------------------------------------------------
        //  ----------------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawBox(Vector3 center, Quaternion rotation, Vector3 size)
        {
            DrawBox(center, rotation, size, Instance.m_ChannelColor);
        }

        //  ----------------------------------------------------------------------------------------

        public static void DrawBox( Vector3    center
                                  , Quaternion rotation
                                  , Vector3    size
                                  , Color      color
                                  , float      duration = 0)
        {
            if (!Instance.GizmosEnabled())
            {
                return;
            }
            else
            {
                Vector3 halfSize = size * 0.5f;

                Vector3 ruf = (rotation * Vector3.Scale(new Vector3(1, 1, 1), halfSize)) + center;
                Vector3 rub = (rotation * Vector3.Scale(new Vector3(1, 1, -1), halfSize)) + center;
                Vector3 rdf = (rotation * Vector3.Scale(new Vector3(1, -1, 1), halfSize)) + center;
                Vector3 rdb = (rotation * Vector3.Scale(new Vector3(1, -1, -1), halfSize)) + center;
                Vector3 luf = (rotation * Vector3.Scale(new Vector3(-1, 1, 1), halfSize)) + center;
                Vector3 lub = (rotation * Vector3.Scale(new Vector3(-1, 1, -1), halfSize)) + center;
                Vector3 ldf = (rotation * Vector3.Scale(new Vector3(-1, -1, 1), halfSize)) + center;
                Vector3 ldb = (rotation * Vector3.Scale(new Vector3(-1, -1, -1), halfSize)) + center;

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
        }

        //  ----------------------------------------------------------------------------------------
    }
}