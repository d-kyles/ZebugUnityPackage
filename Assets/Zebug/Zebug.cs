﻿//  --- Zebug --------------------------------------------------------------------------------------
//  Copyright (c) 2020 Dan Kyles
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
//  associated documentation files (the "Software"), to deal in the Software without restriction,
//  including without limitation the rights to use, copy, modify, merge, publish, distribute,
//  sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//
//  The above copyright notice and this permission notice shall be included in all copies or
//  substantial portions of the Software.
//
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
//  NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//  NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
//  DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//  -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace ZebugProject {
    /*
     |  --- TODO(dan): Make sure that you can set a channel and force it to be on in a build.
     |                 A reasonably common use case for a library like this would be to
     |                 replace ```if (m_Debug) { Debug.Log(...); }```, but also
     |                 ```#ifdef SUPER_VERBOSE_LOGGING \n Debug.Log(...); \n #endif```
     | Author: Dan Kyles
     */

    public static class ColorConverterExtensions {
        // Modified to write alpha too
        // https://stackoverflow.com/questions/2395438/convert-system-drawing-color-to-rgb-and-hex-value
        public static string ToHexString(this Color c) {
            return $"#{(int)(c.r*255):X2}{(int)(c.g*255):X2}{(int)(c.b*255):X2}{(int)(c.a*255):X2}";
        }
    }

    public interface IChannel {
        bool GizmosEnabled();
        bool LogEnabled();
        bool LocalLogEnabled();
        void SetLogEnabled(bool enabled);
        string Name();
        string FullName();
        Color GetColor();
        int Depth();
    }


    public class Channel<T> : IChannel where T : Channel<T>, new()  {

        public bool m_GizmosEnabled;
        public bool m_LogEnabled;

        private static Channel<T> s_Instance;
        private Color m_ChannelColor;
        private string m_ColorString;
        private string m_ChannelName;
        private IChannel m_Parent;
        private int m_Depth;


        public static Channel<T> Instance {
            get {
                if (s_Instance == null) {
                    s_Instance = new T();
                }
                return s_Instance;
            }
        }

        public string Name() { return m_ChannelName; }
        public Color GetColor() { return m_ChannelColor; }
        public int Depth() { return m_Depth; }

        public bool GizmosEnabled() {
            bool enabled = m_GizmosEnabled;
            if (m_Parent != null) {
                enabled &= m_Parent.GizmosEnabled();
            }
            return enabled;
        }

        public void SetGizmosEnabled(bool enabled) {
            m_GizmosEnabled = enabled;
            string gizmoKey = kGizmoKeyPrefix + FullName();
            PlayerPrefs.SetInt(gizmoKey, enabled ? 1 : 0);
        }

        public bool LogEnabled() {
            bool enabled = m_LogEnabled;
            if (m_Parent != null) {
                enabled &= m_Parent.LogEnabled();
            }
            return enabled;
        }

        public bool LocalLogEnabled() {
            return m_LogEnabled;
        }

        public void SetLogEnabled(bool enabled) {
            m_LogEnabled = enabled;
            string logKey = kLogKeyPrefix + FullName();
            PlayerPrefs.SetInt(logKey, enabled ? 1 : 0);
        }

        private const string kLogKeyPrefix = "ZebugLogsEnabled--";
        private const string kGizmoKeyPrefix = "ZebugGizmosEnabled--";

        protected Channel(string channelName, Color channelColor, IChannel parent = null) {
            m_ChannelName = channelName;
            m_ChannelColor = channelColor;
            m_ColorString = $"<color={channelColor.ToHexString()}>{channelName}: </color>";

            if (parent != null) {
                m_Parent = parent;
                m_Depth = m_Parent.Depth() + 1;
            } else if (channelName != "ZebugBase") { // todo this isn't great
                m_Parent = Zebug.Instance;
                m_Depth = 1;
            }

            string fullName = FullName();
            string logKey = kLogKeyPrefix + fullName;
            if (!PlayerPrefs.HasKey(logKey)) {
                PlayerPrefs.SetInt(logKey, 1);
            }
            m_LogEnabled = PlayerPrefs.GetInt(logKey) == 1;

            string gizmoKey = kGizmoKeyPrefix + fullName;
            if (!PlayerPrefs.HasKey(gizmoKey)) {
                PlayerPrefs.SetInt(gizmoKey, 1);
            }
            m_GizmosEnabled = PlayerPrefs.GetInt(gizmoKey) == 1;

            Zebug.s_Channels.Add(this);
        }

        public string FullName() {
            if (m_Parent == null) {
                return Name();
            } else {
                return m_Parent.FullName() + Name();
            }
        }

        //  ----------------------------------------------------------------------------------------
        //  ----------------------------------------------------------------------------------------

        public static void Log(object message) {
            if (!Instance.LogEnabled()) { return; }
            Zebug.s_Logger.Log(LogType.Log, Instance.m_ColorString + message);
        }

        public static void Log(object message, Object context) {
            Zebug.s_Logger.Log(LogType.Log, message, context);
        }

        public static void LogFormat(string format, params object[] args) {
            Zebug.s_Logger.LogFormat(LogType.Log, format, args);
        }

        public static void LogFormat(Object context, string format, params object[] args) {
            Zebug.s_Logger.LogFormat(LogType.Log, context, format, args);
        }

        public static void LogFormat(
            LogType logType,
            LogOption logOptions,
            Object context,
            string format,
            params object[] args) {
            Debug.LogFormat(logType, logOptions, context, format, args);
        }

        public static void LogError(object message) {
            Zebug.s_Logger.Log(LogType.Error, message);
        }

        public static void LogError(object message, Object context) {
            Zebug.s_Logger.Log(LogType.Error, message, context);
        }

        public static void LogErrorFormat(string format, params object[] args) {
            Zebug.s_Logger.LogFormat(LogType.Error, format, args);
        }

        public static void LogErrorFormat(Object context, string format, params object[] args) {
            Zebug.s_Logger.LogFormat(LogType.Error, context, format, args);
        }

        public static void LogException(Exception exception) {
            Zebug.s_Logger.LogException(exception, null);
        }

        public static void LogException(Exception exception, Object context) {
            Zebug.s_Logger.LogException(exception, context);
        }

        public static void LogWarning(object message) {
            Zebug.s_Logger.Log(LogType.Warning, message);
        }

        public static void LogWarning(object message, Object context) {
            Zebug.s_Logger.Log(LogType.Warning, message, context);
        }

        public static void LogWarningFormat(string format, params object[] args) {
            Zebug.s_Logger.LogFormat(LogType.Warning, format, args);
        }

        public static void LogWarningFormat(Object context, string format, params object[] args) {
            Zebug.s_Logger.LogFormat(LogType.Warning, context, format, args);
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void Assert(bool condition) {
            if (condition) {
                return;
            }

            Zebug.s_Logger.Log(LogType.Assert, "Assertion failed");
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void Assert(bool condition, Object context) {
            if (condition) {
                return;
            }

            Zebug.s_Logger.Log(LogType.Assert, (object) "Assertion failed", context);
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void Assert(bool condition, object message) {
            if (condition) {
                return;
            }

            Zebug.s_Logger.Log(LogType.Assert, message);
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void Assert(bool condition, string message) {
            if (condition) {
                return;
            }

            Zebug.s_Logger.Log(LogType.Assert, message);
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void Assert(bool condition, object message, Object context) {
            if (condition) {
                return;
            }

            Zebug.s_Logger.Log(LogType.Assert, message, context);
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void Assert(bool condition, string message, Object context) {
            if (condition) {
                return;
            }

            Zebug.s_Logger.Log(LogType.Assert, (object) message, context);
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void AssertFormat(bool condition, string format, params object[] args) {
            if (condition) {
                return;
            }

            Zebug.s_Logger.LogFormat(LogType.Assert, format, args);
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void AssertFormat(
            bool condition,
            Object context,
            string format,
            params object[] args) {
            if (condition) {
                return;
            }

            Zebug.s_Logger.LogFormat(LogType.Assert, context, format, args);
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void LogAssertion(object message) {
            Zebug.s_Logger.Log(LogType.Assert, message);
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void LogAssertion(object message, Object context) {
            Zebug.s_Logger.Log(LogType.Assert, message, context);
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void LogAssertionFormat(string format, params object[] args) {
            Zebug.s_Logger.LogFormat(LogType.Assert, format, args);
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void LogAssertionFormat(Object context, string format, params object[] args) {
            Zebug.s_Logger.LogFormat(LogType.Assert, context, format, args);
        }

    }

    public class Zebug : Channel<Zebug> {

        public static List<IChannel> s_Channels = new List<IChannel>();

        public static ILogger s_Logger = Debug.unityLogger;

        public Zebug() : base("ZebugBase", Color.white) {}
    }
}