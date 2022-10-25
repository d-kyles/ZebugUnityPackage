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

namespace ZebugProject {
    public class SimpleLineGizmoDrawTest : MonoBehaviour {
        //  --------------------------------------------------------------------------------------------

        public class Zebug : Channel<Zebug> {
            public Zebug() : base("Zebug Line Draw Test"
                , new Color(0.25f, 0.66f, 0.95f)
                , ZebugProject.Zebug.Instance) {
            }
        }

        //  --------------------------------------------------------------------------------------------

        [SerializeField, Range(0.01f, 10f)] private float m_Step = 1f;
        [SerializeField] private float m_Range = 260f;

        [SerializeField] private float m_Speed = 0.2f;

        [SerializeField] private float m_PhiRange = 2f;
        [SerializeField] private float m_RadiusPhiRange = 2f;
        [SerializeField] private float m_Radius = 1.5f;
        [SerializeField] private float m_PhiBase = 10f;
        [SerializeField] private float m_ThetaBase = 30f;

        [SerializeField] private bool m_LogChannelsEnabled;
        
        private float m_ThetaInc = 1f;
        private Transform m_Transform;
        private Vector3 m_NextStartPos;
        private float m_LastStart;
        private float m_LastBurstTime;
        private float m_BurstDuration = 1f;
        private float m_LastPeriodicLogTime = 0f;


        //  --------------------------------------------------------------------------------------------
        protected void Awake() {
            m_Transform = transform;
            m_NextStartPos = m_Transform.position + new Vector3(0, 0.1f, 0);

            Quaternion ninety = Quaternion.Euler(0,90, 0);
            Quaternion negNinety = Quaternion.Euler(0,-90, 0);
            Quaternion twoSeventy = Quaternion.Euler(0,270, 0);
            Quaternion negTwoSeventy = Quaternion.Euler(0,-270, 0);

            Zebug.Log($"90: {ninety}");
            Zebug.Log($"-90: {negNinety}");
            Zebug.Log($"270: {twoSeventy}");
            Zebug.Log($"-270: {negTwoSeventy}");
            Zebug.Log($"90inv: {Quaternion.Inverse(Quaternion.Euler(0,90, 0))}");
            Zebug.Log($"-90inv: {Quaternion.Inverse(negNinety)}");
            Zebug.Log($"270inv: {Quaternion.Inverse(twoSeventy)}");
            Zebug.Log($"-270inv: {Quaternion.Inverse(negTwoSeventy)}");


            Quaternion mightBeShort = Quaternion.Slerp(Quaternion.identity, negTwoSeventy, 1f);
            Zebug.Log($"shortPath(-270) = {mightBeShort}");
            Zebug.Log($"90: {ninety}");
        }

        //  --------------------------------------------------------------------------------------------

        protected void Update() {
            Vector3 transformPos = m_Transform.position;
            Vector3 pos = transformPos;
            Vector3 offset = new Vector3((float) Math.Sin(6.28f*Time.time)
                , 0
                , (float) Math.Cos(6.28f*Time.time));

            Zebug.DrawLine(pos, pos + offset);

            Vector3 pp = m_NextStartPos;
            float inc = Time.deltaTime*0.01f;

            if ((int) (Time.time/20f)%2 == 1) {
                inc = -inc;
            }

            m_PhiRange += inc;
            m_RadiusPhiRange += inc;
            m_ThetaInc += inc;

            Vector3 lastPos = pp;
            for (float i = m_LastStart; i < m_LastStart + m_Range; i += m_Step) {
                float phi = m_PhiBase*Mathf.PerlinNoise(i*0.05f, m_PhiRange);
                float theta = m_ThetaBase*Mathf.PerlinNoise(i*0.02f, 0f)*m_ThetaInc;
                float radius = m_Radius + m_Radius*Mathf.PerlinNoise(i*0.05f, m_RadiusPhiRange);
                pp = AddSpherical(pp, radius, theta, phi);
                pp *= 0.87f;

                Zebug.DrawLine(lastPos, pp);
                lastPos = pp;

                if (i < m_LastStart + m_Speed) {
                    m_NextStartPos = lastPos;
                }
            }

            if (Time.time > m_LastBurstTime + m_BurstDuration) {
                Zebug.DrawBurst(new Vector3(-2, 2, -2), 0.25f, new Color(1f, 1f, 0f) * 0.8f, 0.5f * m_BurstDuration);
                m_LastBurstTime = Time.time;
            }

            Zebug.DrawBox(new Vector3(2f, 2f, 2f), Quaternion.identity, new Vector3(1, 1, 2));
            Zebug.DrawLocator(new Vector3(0,0,0));

            m_LastStart += m_Speed;
            
            
            if (m_LogChannelsEnabled && Time.time > m_LastPeriodicLogTime + 5f)
            {
                m_LastPeriodicLogTime = Time.time;
                
                void TestLogAll(IChannel channel)
                {
                    global::ZebugProject.Zebug.LogFormat("Channel ({0}) is enabled? LE({1}), LLE({2})"
                                                        , channel.Name()
                                                        , channel.LogEnabled()
                                                        , channel.LocalLogEnabled());

                    foreach (IChannel child in channel.Children())
                    {
                        TestLogAll(child);
                    }
                }
                
                TestLogAll(global::ZebugProject.Zebug.Instance);
            }
            
        }

        private Vector3 AddSpherical(Vector3 p, float r, float theta, float phi) {
            float cosPhi = (float) Math.Cos(phi);
            p.x += (float) (r*Math.Cos(theta)*cosPhi);
            p.y += (float) (r*Math.Sin(theta)*cosPhi);
            p.z += (float) (r*Math.Sin(phi));
            return p;
        }
    }
}