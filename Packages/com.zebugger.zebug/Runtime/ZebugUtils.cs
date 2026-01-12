// -------------------------------------------------------------------------------------------------
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ZebugProject
{
    public static class ZebugUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float RemapRange(float t, float fromMin, float fromMax, float toMin, float toMax)
        {
            return (t - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
        }
        
        /// Smoothing rate dictates the proportion of source remaining after one second
        /// ref: https://www.rorydriscoll.com/2016/03/07/frame-rate-independent-damping-using-lerp/
        ///
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Damp(float source, float target, float smoothing, float dt)
        {
            return Mathf.Lerp(source, target, 1 - Mathf.Pow(smoothing, dt));
        }
        
        /// Alternative way of expressing frame rate independent damping.
        /// Lambda range: [0, Infinity] 
        /// ref: https://www.rorydriscoll.com/2016/03/07/frame-rate-independent-damping-using-lerp/
        ///
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ExpDecayDamp(float a, float b, float lambda, float dt)
        {
            return Mathf.Lerp(a, b, 1 - Mathf.Exp(-lambda * dt));
        }

        /// Returns lineColor unless its alpha value is transparent. 
        /// 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color VisibleColorOrDefault(Color lineColor, Color defaultColor)
        {
            return (lineColor.a > 0.001f)
                       ? lineColor
                       : defaultColor;
        }
        
        
        public static class ArrayPool<T> where T: struct
        {
            private static List<T[]> s_Pool = new();

            public static T[] CheckOut(int minLength)
            {
                const int minArraySize = 4;
            
                if (minLength < minArraySize)
                {
                    minLength = minArraySize;
                }
            
                int tooLong = minLength * 3;
            
                var foundItem = default(T[]);
                var foundIdx = -1;
            
                //  --- Checking from the back first is better for cases where the arraysize is
                //      correlated between CheckOut calls. It is the most efficient for repeated calls. 
                for (var idx = s_Pool.Count - 1; idx >= 0; idx--)
                {
                    var item = s_Pool[idx];
                    if (item.Length >= minLength && item.Length < tooLong)
                    {
                        foundItem = item;
                        foundIdx = idx;
                        break;
                    }
                }

                if (foundIdx > 0)
                {
                    // swap with last element, so that removal is cheap
                    int count = s_Pool.Count -1;
                    s_Pool[foundIdx] = s_Pool[count];
                    s_Pool.RemoveAt(count);
                
                    return foundItem;
                }
                else
                {
                    if (minLength > minArraySize)
                    {
                        minLength = Mathf.NextPowerOfTwo(minLength);
                    }
                
                    return new T[minLength];
                }
            }

            public static void Return(T[] pooledArray)
            {
                s_Pool.Add(pooledArray);
            }
        }
    }

}