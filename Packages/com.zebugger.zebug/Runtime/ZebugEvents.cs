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

using System.Runtime.CompilerServices;

using UnityEngine;

namespace ZebugProject
{
    public class ZebugEvents : MonoBehaviour
    {
        public static ZebugEvents Instance
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            private set;
        }

        public static bool Exists
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get;
            private set;
        }

        protected void Awake()
        {
            Instance = this;
            Exists = true;
        }

        protected void OnDestroy()
        {
            Instance = null;
            Exists = false;

            Updated = ()=>{};
            LateUpdated = ()=>{};
            FixedUpdated = ()=>{};
        }

        public static event Action Updated = ()=>{};
        protected void Update() => Updated?.Invoke();

        public static event Action LateUpdated = ()=>{};
        protected void LateUpdate() => LateUpdated?.Invoke();

        public static event Action FixedUpdated = ()=>{};
        protected void FixedUpdate() => FixedUpdated?.Invoke();

        //  ----------------------------------------------------------------------------------------

        [RuntimeInitializeOnLoadMethod]
        protected static void InitializeOnLoad()
        {
            #if UNITY_WEBGL
            return;
            #endif
            if (Instance != null)
            {
                return;
            }

            var go = new GameObject("Zebug Events Helper GO");
            Instance = go.AddComponent<ZebugEvents>();
            DontDestroyOnLoad(go);

            //  --- Todo: move this here
            Zebug.RaiseOnLoad();

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged += OnExitPlayMode;
            #endif
        }
        //  ----------------------------------------------------------------------------------------

        #if UNITY_EDITOR
        private static void OnExitPlayMode(UnityEditor.PlayModeStateChange state)
        {
            if(state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
            {
                Zebug.Log("Exiting Play mode: Unregistering handler.");

                // Unregister the handler so it doesn't affect the next Play mode run
                UnityEditor.EditorApplication.playModeStateChanged -= OnExitPlayMode;

                Zebug.RaiseOnExit();
            }
        }
        #endif

        //  ----------------------------------------------------------------------------------------
    }
}
