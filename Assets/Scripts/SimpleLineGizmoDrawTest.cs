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

using System;
using UnityEngine;
using UnityTemplateProjects;

namespace ZebugProject
{

    public class SimpleLineGizmoDrawTest : MonoBehaviour
    {

        //  ----------------------------------------------------------------------------------------

        public class Zebug : Channel<Zebug>
        {
            public Zebug() : base("Zebug Line Draw Test"
                , new Color(0.25f, 0.66f, 0.95f)
                , ZebugProject.Zebug.Instance) { }

        }
        
        public class AnotherDebugChannel : Channel<AnotherDebugChannel>
        {
            public AnotherDebugChannel() : base("Another Zebug Line Draw Test"
                , new Color(0.25f, 0.66f, 0.95f)
                , ZebugProject.Zebug.Instance)
            {
                m_LineDrawingType = ChannelLineData.Type.Runtime;
                m_LineDrawingWidth = 3f;
            }
        }
        
        //  ----------------------------------------------------------------------------------------

        [SerializeField] [Range(0.01f, 10f)] private float m_Step = 1f;
        [SerializeField] private float m_Range = 260f;

        [SerializeField] private float m_Speed = 0.2f;

        [SerializeField] private float m_PhiRange = 2f;
        [SerializeField] private float m_RadiusPhiRange = 2f;
        [SerializeField] private float m_Radius = 1.5f;
        [SerializeField] private float m_PhiBase = 10f;
        [SerializeField] private float m_ThetaBase = 30f;

        [SerializeField] private bool m_LogChannelsEnabled;

        [SerializeField] private ChannelLineData.Type m_RenderType;
        [SerializeField] private WidthType m_LineRenderWidthType;

        private float _thetaInc = 1f;
        private Transform _transform;
        private Vector3 _nextStartPos;
        private float _lastStart;
        private float _lastBurstTime;
        private float _burstDuration = 1f;
        private float _lastPeriodicLogTime;
        private WidthType _lastLineRenderWidthType;
        private ChannelLineData.Type _lastRenderType;
        private SimpleCameraController _simpleCameraController;
        private Camera _cam;

        //  ----------------------------------------------------------------------------------------
        
        protected void Awake()
        {
            _transform = transform;
            _nextStartPos = _transform.position + new Vector3(0, 0.1f, 0);

            Quaternion ninety = Quaternion.Euler(0, 90, 0);
            Quaternion negNinety = Quaternion.Euler(0, -90, 0);
            Quaternion twoSeventy = Quaternion.Euler(0, 270, 0);
            Quaternion negTwoSeventy = Quaternion.Euler(0, -270, 0);

            Zebug.Log($"90: {ninety}");
            Zebug.Log($"-90: {negNinety}");
            Zebug.Log($"270: {twoSeventy}");
            Zebug.Log($"-270: {negTwoSeventy}");
            Zebug.Log($"90inv: {Quaternion.Inverse(Quaternion.Euler(0, 90, 0))}");
            Zebug.Log($"-90inv: {Quaternion.Inverse(negNinety)}");
            Zebug.Log($"270inv: {Quaternion.Inverse(twoSeventy)}");
            Zebug.Log($"-270inv: {Quaternion.Inverse(negTwoSeventy)}");


            Quaternion mightBeShort = Quaternion.Slerp(Quaternion.identity, negTwoSeventy, 1f);
            Zebug.Log($"shortPath(-270) = {mightBeShort}");
            Zebug.Log($"90: {ninety}");

            Zebug.Log("Zebug.GizmosEnabled = " + Zebug.Instance.GizmosEnabled());

            SetRenderType(m_RenderType);
            SetLineWidth(m_LineRenderWidthType);
            
            _cam = Camera.main;
            _simpleCameraController = _cam.GetComponent<SimpleCameraController>();

            Zebug.AddDebugGuiButton("TestAutoButton", OnTestAutoButtonClicked);
            Zebug.AddDebugGuiButton("Parent1/P1_Child0", OnTestAutoButtonClicked);
            Zebug.AddDebugGuiButton("Parent2/P2_Child0", OnTestAutoButtonClicked);
            Zebug.AddDebugGuiButton("Parent2/P2_Child1", OnTestAutoButtonClicked);
        }

        //  ----------------------------------------------------------------------------------------

        protected void Update()
        {
            Vector3 transformPos = _transform.position;
            Vector3 pos = transformPos;
            var offset = new Vector3((float) Math.Sin(6.28f * Time.time)
                , 0
                , (float) Math.Cos(6.28f * Time.time));

            Zebug.DrawLine(pos, pos + offset);
            
            Vector3 pp = _nextStartPos;
            float inc = Time.deltaTime * 0.01f;
            
            if ((int) (Time.time / 20f) % 2 == 1)
            {
                inc = -inc;
            }
            
            m_PhiRange += inc;
            m_RadiusPhiRange += inc;
            _thetaInc += inc;
            
            Vector3 lastPos = pp;
            for (float i = _lastStart; i < _lastStart + m_Range; i += m_Step)
            {
                float phi = m_PhiBase * Mathf.PerlinNoise(i * 0.05f, m_PhiRange);
                float theta = m_ThetaBase * Mathf.PerlinNoise(i * 0.02f, 0f) * _thetaInc;
                float radius = m_Radius + m_Radius * Mathf.PerlinNoise(i * 0.05f, m_RadiusPhiRange);
                pp = AddSpherical(pp, radius, theta, phi);
                pp *= 0.87f;
            
                Zebug.DrawLine(lastPos, pp);
                lastPos = pp;
            
                if (i < _lastStart + m_Speed)
                {
                    _nextStartPos = lastPos;
                }
            }
            
            if (Time.time > _lastBurstTime + _burstDuration)
            {
                AnotherDebugChannel.DrawBurst(new Vector3(-2, 2, -2), 0.25f, new Color(1f, 1f, 0f) * 0.8f, 0.5f * _burstDuration);
                _lastBurstTime = Time.time;
            }
            
            Zebug.DrawBox(new Vector3(2f, 2f, 2f), Quaternion.identity, new Vector3(1, 1, 2));
            Zebug.DrawLocator(new Vector3(0, 0, 0));

            _lastStart += m_Speed;


            if (m_LogChannelsEnabled && Time.time > _lastPeriodicLogTime + 5f)
            {
                _lastPeriodicLogTime = Time.time;

                void TestLogAll(IChannel channel)
                {
                    ZebugProject.Zebug.LogFormat("Channel ({0}) is enabled? LE({1}), LLE({2})"
                        , channel.Name()
                        , channel.LogEnabled()
                        , channel.LocalLogEnabled());

                    foreach (IChannel child in channel.Children())
                    {
                        TestLogAll(child);
                    }
                }

                TestLogAll(ZebugProject.Zebug.Instance);
            }

            if (_lastRenderType != m_RenderType)
            {
                SetRenderType(m_RenderType);
            }
            
            if (_lastLineRenderWidthType != m_LineRenderWidthType)
            {
                SetLineWidth(m_LineRenderWidthType);
            }
        }

        private void SetRenderType(ChannelLineData.Type renderType)
        {
            Zebug.Instance.SetLineRenderType(renderType);
            _lastRenderType = renderType;
        }
        
        private void SetLineWidth(WidthType lineRenderWidthType)
        {
            Zebug.Instance.SetLineRenderWidthType(lineRenderWidthType);
            _lastLineRenderWidthType = lineRenderWidthType;
        }


        private Vector3 AddSpherical(Vector3 p, float r, float theta, float phi)
        {
            float cosPhi = (float) Math.Cos(phi);
            p.x += (float) (r * Math.Cos(theta) * cosPhi);
            p.y += (float) (r * Math.Sin(theta) * cosPhi);
            p.z += (float) (r * Math.Sin(phi));
            return p;
        }

        //  ----------------------------------------------------------------------------------------

        private void OnTestAutoButtonClicked()
        {
            Zebug.Log("OnTestAutoButtonClicked called");
        }

        //  ----------------------------------------------------------------------------------------

        private void OnGUI()
        {
            Rect camRect = _cam.pixelRect;

            float size = Application.isMobilePlatform ? Screen.dpi * 0.5f : 100;
            float half = size * 0.5f;

            float cMaxY = camRect.yMax; // bottom of screen
            float cMinX = camRect.xMin;
            float midX = camRect.center.x;

            if (_simpleCameraController)
            {
                Rect upRect = new Rect(midX - half, cMaxY - size - size, size, size);
                Rect leftRect = new Rect(midX - half - size, cMaxY - size, size, size);
                Rect rightRect = new Rect(midX - half + size, cMaxY - size, size, size);
                Rect downRect = new Rect(midX - half, cMaxY - size, size, size);

                if (GUI.RepeatButton(upRect, "W"))
                {
                    _simpleCameraController.Forward();
                }

                if (GUI.RepeatButton(leftRect, "A"))
                {
                    _simpleCameraController.Left();
                }

                if (GUI.RepeatButton(downRect, "S"))
                {
                    _simpleCameraController.Back();
                }

                if (GUI.RepeatButton(rightRect, "D"))
                {
                    _simpleCameraController.Right();
                }
            }

            Rect channelToggleBox = new Rect(cMinX, cMaxY - 3 * size, size, size);

            if (GUI.Button(channelToggleBox, "Toggle channel"))
            {
                Zebug.Instance.SetGizmosEnabled(!Zebug.Instance.LocalGizmosEnabled());
            }
        }

    }

}