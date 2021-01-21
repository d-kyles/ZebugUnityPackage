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
                //  --- TODO(dan): It's just straight up too hard to turn on a channel, or figure out
                //                 if the channel is turned on. Fix this as the highest prio.
                m_LogEnabled = true;
                m_GizmosEnabled = true;
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

        private float m_ThetaInc = 1f;
        private Transform m_Transform;
        private Vector3 m_NextStartPos;
        private float m_LastStart;


        //  --------------------------------------------------------------------------------------------
        protected void Awake() {
            m_Transform = transform;
            m_NextStartPos = m_Transform.position + new Vector3(0, 0.1f, 0);
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

            m_LastStart += m_Speed;
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