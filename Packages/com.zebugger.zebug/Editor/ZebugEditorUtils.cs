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

using UnityEditor;
using UnityEngine;

namespace ZebugProject {
    /*
     |  --- ZebugEditorUtils
     | Author: Dan Kyles 21/01/2020
     */
    internal static class ZebugEditorUtils {
        private const string kPackageRoot = "Packages/com.zebugger.zebug/";
        private const string kInProjectRoot = "Assets/Plugins/Zebug/";

        public static T LoadFromZebugRelative<T>(string relativeAssetName) where T : Object {
            T result = AssetDatabase.LoadAssetAtPath<T>(kPackageRoot + relativeAssetName);
            if (result != null) {
                return result;
            }

            //  --- Support for dragging the Zebug package to the Assets/Plugins folder.
            result = AssetDatabase.LoadAssetAtPath<T>(kInProjectRoot + relativeAssetName);
            return result;
        }
    }
}