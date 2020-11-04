//  --- Zebug v0.2 ---------------------------------------------------------------------------------
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
    public class Zebug : Channel<Zebug> {
        public Zebug() : base("Zebug Line Draw Test"
            , new Color(0.25f, 0.66f, 0.95f)
            , ZebugProject.Zebug.Instance) { }
    }

    private Transform m_Transform;

    //  --------------------------------------------------------------------------------------------
    protected void Awake() {
        m_Transform = transform;
    }

    //  --------------------------------------------------------------------------------------------

    protected void Update() {
        Vector3 pos = m_Transform.position + new Vector3(0, 0.1f, 0);
        Vector3 offset = new Vector3((float) Math.Sin(6.28f*Time.time)
            , 0
            , (float) Math.Cos(6.28f*Time.time));
        Zebug.DrawLine(pos, pos + offset);
    }
}
}
