//  --- Zebug --------------------------------------------------------------------------------------
//  Copyright (c) 2020 Dan Kyles
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
using UnityEngine;

namespace ZebugProject {
    public class ZebugSceneDrawer : MonoBehaviour {

        private static ZebugSceneDrawer s_Instance;

        //  ----------------------------------------------------------------------------------------

        [RuntimeInitializeOnLoadMethod]
        protected static void InitializeOnLoad() {
            if (s_Instance != null) {
                return;
            }

            var go = new GameObject("ZebugSceneDrawer Helper GO");
            s_Instance = go.AddComponent<ZebugSceneDrawer>();
            //go.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(go);

            Zebug.Log("Initializing Scene Drawer");
        }

        //  ----------------------------------------------------------------------------------------

        private void OnDrawGizmos() {
            Color oldGizmoColor = Gizmos.color;
            float time = Time.time;

            foreach (KeyValuePair<IChannel,List<LineData>> lineChannel in Zebug.s_ChannelLines) {
                IChannel channel = lineChannel.Key;
                bool drawGizmos = channel.GizmosEnabled();
                List<LineData> lines =  lineChannel.Value;
                for (int i = lines.Count - 1; i >= 0; i--) {
                    LineData line = lines[i];
                    if (drawGizmos) {
                        Gizmos.DrawLine(line.startPosition, line.endPosition);
                    }
                    if (time > line.endTime) {
                        lines.RemoveAt(i);
                    }
                }
            }

            Gizmos.color = oldGizmoColor;
        }
    }
}
