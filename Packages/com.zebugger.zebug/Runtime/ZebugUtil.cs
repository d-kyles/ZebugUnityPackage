<<<<<<<< HEAD:Packages/com.zebugger.zebug/Runtime/ZebugUtil.cs
﻿//  --- Zebug --------------------------------------------------------------------------------------
//  Copyright (c) 2022 Dan Kyles
========
﻿//  --- Zebug v0.4.0 -------------------------------------------------------------------------------
//  Copyright (c) 2020 Dan Kyles
>>>>>>>> github.com/main:Runtime/ZebugUtil.cs
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

using UnityEngine;

namespace ZebugProject.Util
{
    public static class ZebugUtil {

        public static Vector3 RightVec   = new Vector3(1, 0, 0);
        public static Vector3 UpVec      = new Vector3(0, 1, 0);
        public static Vector3 ForwardVec = new Vector3(0, 0, 1);

    }

    /*
     |  --- ColorExtensions
     | Author: Dan Kyles 17/12/2020
     */
    public static class ColorExtensions
    {
        // https://stackoverflow.com/questions/2395438/convert-system-drawing-color-to-rgb-and-hex-value
        //  --- NOTE(dan): Modified to write alpha
        public static string ToHexString(this Color c)
        {
            return $"#{(int)(c.r * 255):X2}{(int)(c.g * 255):X2}{(int)(c.b * 255):X2}{(int)(c.a * 255):X2}";
        }
    }
}