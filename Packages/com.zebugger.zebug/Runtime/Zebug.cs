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
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;

using UnityEngine;
using ZebugProject.Util;

using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace ZebugProject
{
    /*
     |  --- Zebug
     |
     |      A debugging library/tool, heavily inspired by XDebug, may it rest in peace.
     |      
     |      See package README for more documentation
     |
     |      Author: Dan Kyles
     */
    public sealed class Zebug : Channel<Zebug>
    {
        public const bool ColorTagsOnlyInEditor = true;

        public static List<IChannel> s_Channels = new List<IChannel>();

        public static ILogger s_Logger = Debug.unityLogger;

        //  --- Gizmo drawing API
        public static Dictionary<IChannel, ChannelLineData> s_ChannelLines
            = new Dictionary<IChannel, ChannelLineData>();

        public Zebug() : base("ZebugBase", Color.white) { }
    }

    public interface IChannel
    {
        bool LogEnabled();
        bool LocalLogEnabled();
        void SetLogEnabled(bool enabled);
        bool ParentLogEnabled();

        bool GizmosEnabled();
        bool LocalGizmosEnabled();
        void SetGizmosEnabled(bool enabled);
        bool ParentGizmosEnabled();

        string Name();
        string FullName();
        Color GetColor();
        int Depth();

        //  --- should really be pseudo non-public
        IList<IChannel> Children();
        void AddChild(IChannel channel);
        event Action<bool> OnLocalLogEnabled;
        event Action<bool> OnLocalGizmosEnabled;
    }

    public partial class Channel<T> : IChannel where T : Channel<T>, new()
    {
        public bool m_GizmosEnabled;
        public bool m_LogEnabled;

        protected bool AllowWarningAndErrorMuting = true;

        private static Channel<T> s_Instance;
        private Color m_ChannelColor;
        private string m_ColorString;
        private string m_ChannelName;
        private IChannel m_Parent;
        private int m_Depth;


        public static Channel<T> Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    // ReSharper disable once ObjectCreationAsStatement
                    new T(); 
                    // --- T() will assign s_Instance.
                }

                return s_Instance;
            }
        }

        public string Name()
        {
            return m_ChannelName;
        }

        public Color GetColor()
        {
            return m_ChannelColor;
        }

        public int Depth()
        {
            return m_Depth;
        }

        public IList<IChannel> Children()
        {
            return m_Children;
        }

        public void AddChild(IChannel channel)
        {
            if (!m_Children.Contains(channel))
            {
                m_Children.Add(channel);
            }
        }

        public event Action<bool> OnLocalLogEnabled;
        public event Action<bool> OnLocalGizmosEnabled;

        public bool GizmosEnabled()
        {
            bool enabled = m_GizmosEnabled;
            if (m_Parent != null)
            {
                enabled &= m_Parent.GizmosEnabled();
            }

            return enabled;
        }

        public void SetGizmosEnabled(bool enabled)
        {
            bool wasEnabled = m_GizmosEnabled;
            m_GizmosEnabled = enabled;
            ZebugPreferences.SetGizmo(FullName(), enabled);
            
            if (wasEnabled != enabled)
            {
                OnLocalGizmosEnabled?.Invoke(enabled);
            }
        }

        public bool ParentGizmosEnabled() {
            if (m_Parent == null) { return true; }
            return m_Parent.GizmosEnabled();
        }
        public bool LogEnabled()
        {
            bool enabled = m_LogEnabled;
            if (m_Parent != null)
            {
                enabled &= m_Parent.LogEnabled();
            }

            return enabled;
        }

        public bool LocalLogEnabled()
        {
            return m_LogEnabled;
        }

        public bool LocalGizmosEnabled()
        {
            return m_GizmosEnabled;
        }

        public void SetLogEnabled(bool enabled)
        {
            bool wasEnabled = m_LogEnabled;
            m_LogEnabled = enabled;
            
            ZebugPreferences.SetLog(FullName(), enabled);
            
            if (wasEnabled != enabled)
            {
                OnLocalLogEnabled?.Invoke(enabled);
            }
        }

        public bool ParentLogEnabled() {
            if (m_Parent == null) { return true; }
            return m_Parent.LogEnabled();
        }

        private List<IChannel> m_Children = new List<IChannel>();

        protected Channel(string channelName, Color channelColor, IChannel parent = null)
        {
            if (s_Instance != null)
            {
                return;
            }

            s_Instance = this;

            m_ChannelName = channelName;
            m_ChannelColor = channelColor;
            m_ColorString = !Zebug.ColorTagsOnlyInEditor || Application.isEditor
                                ? $"<color={channelColor.ToHexString()}>{channelName}: </color>"
                                : channelName + ": ";

            #if PLATFORM_IOS
            {
                if (ZebugPreferences.Instance.UseAdditionalPrefixOnIos)
                {
                    m_ColorString = ZebugPreferences.Instance.AdditionalIosPrefix + m_ColorString; 
                }
            }
            #endif
            
            if (parent != null)
            {
                m_Parent = parent;
                m_Depth = m_Parent.Depth() + 1;
            }
            else
            {
                bool isBase = channelName == "ZebugBase"; // todo this isn't great
                if (!isBase)
                {
                    //  --- This should potentially be a serializable null, depending on how I want the
                    //      hierarchy editor code to look.
                    m_Parent = Zebug.Instance;
                    m_Depth = 1;
                }
            }
            
            string fullName = FullName();
            m_LogEnabled = ZebugPreferences.GetLog(fullName); 
            m_GizmosEnabled = ZebugPreferences.GetGizmo(fullName);

            if (m_Parent != null)
            {
                m_Parent.AddChild(this);
            }
            
            Zebug.s_Channels.Add(this);
        }

        public string FullName()
        {
            if (m_Parent == null)
            {
                return Name();
            }

            return m_Parent.FullName() + "/" + Name();
        }

        //  ----------------------------------------------------------------------------------------

        public ILogger ILoggerWrapper()
        {
            return new ZebugILoggerWrapper<T>(this);
        }
        
        //  ----------------------------------------------------------------------------------------
        //  ----------------------------------------------------------------------------------------

        public static void Log(object message)
        {
            if (!Instance.LogEnabled())
            {
                return;
            }

            Zebug.s_Logger.Log(LogType.Log, Instance.m_ColorString + message);
        }

        public static void Log(object message, Object context)
        {
            if (!Instance.LogEnabled())
            {
                return;
            }

            Zebug.s_Logger.Log(LogType.Log, message: Instance.m_ColorString + message, context);
        }

        [StringFormatMethod("format")]
        public static void LogFormat(string format, params object[] args)
        {
            if (!Instance.LogEnabled())
            {
                return;
            }

            Zebug.s_Logger.LogFormat(LogType.Log, Instance.m_ColorString + format, args);
        }

        [StringFormatMethod("format")]
        public static void LogFormat(Object context, string format, params object[] args)
        {
            if (!Instance.LogEnabled())
            {
                return;
            }

            Zebug.s_Logger.LogFormat(LogType.Log, context, Instance.m_ColorString + format, args);
        }

        [StringFormatMethod("format")]
        public static void LogFormat( LogType logType
                                    , LogOption logOptions
                                    , Object context
                                    , string format
                                    , params object[] args)
        {
            bool mutingThisLog = !Instance.LogEnabled();

            if (logType != LogType.Log)
            {
                if (!Instance.AllowWarningAndErrorMuting)
                {
                    mutingThisLog = false;
                }
            }

            if (mutingThisLog)
            {
                return;
            }

            Debug.LogFormat(logType, logOptions, context, Instance.m_ColorString + format, args);
        }
        
        public static bool ShouldLog(LogType logType)
        {
            Channel<T> instance = Instance;
            if (!instance.AllowWarningAndErrorMuting)
            {
                if (logType == LogType.Assert 
                    || logType == LogType.Warning
                    || logType == LogType.Error)
                {
                    return true;
                }
            }
            return instance.LogEnabled();
        }
        
        [StringFormatMethod("format")]
        public static void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            if (ShouldLog(logType)) 
            {
                Zebug.s_Logger.LogFormat(logType, context, Instance.m_ColorString + format, args);    
            }
        }

        public static void LogError(object message)
        {
            if (!Instance.AllowWarningAndErrorMuting || Instance.LogEnabled())
            {
                Zebug.s_Logger.Log(LogType.Error, Instance.m_ColorString + message);
            }
        }

        public static void LogError(object message, Object context) 
        {
            if (!Instance.AllowWarningAndErrorMuting || Instance.LogEnabled())
            {
                Zebug.s_Logger.Log(LogType.Error, message: Instance.m_ColorString + message, context);
            }
        }

        [StringFormatMethod("format")]
        public static void LogErrorFormat(string format, params object[] args)
        {
            if (!Instance.AllowWarningAndErrorMuting || Instance.LogEnabled())
            {
                Zebug.s_Logger.LogFormat(LogType.Error, Instance.m_ColorString + format, args);
            }
        }

        [StringFormatMethod("format")]
        public static void LogErrorFormat(Object context, string format, params object[] args)
        {
            if (!Instance.AllowWarningAndErrorMuting || Instance.LogEnabled())
            {
                Zebug.s_Logger.LogFormat(LogType.Error, context, Instance.m_ColorString + format, args);
            }
        }

        public static void LogException(Exception exception) 
        {
            if (!Instance.AllowWarningAndErrorMuting || Instance.LogEnabled())
            {
                Zebug.s_Logger.LogException(exception);
            }
        }
        
        public static void LogException(Exception exception, Object context) 
        {
            if (!Instance.AllowWarningAndErrorMuting || Instance.LogEnabled())
            {
                Zebug.s_Logger.LogException(exception, context);
            }
        }

        public static void LogWarning(object message)
        {
            if (!Instance.AllowWarningAndErrorMuting || Instance.LogEnabled())
            {
                Zebug.s_Logger.Log(LogType.Warning, Instance.m_ColorString + message);
            }
        }

        public static void LogWarning(object message, Object context) 
        {
            if (!Instance.AllowWarningAndErrorMuting || Instance.LogEnabled())
            {
                string coloredMessage = Instance.m_ColorString + message;
                Zebug.s_Logger.Log(LogType.Warning, message: coloredMessage, context: context);
            }
        }

        [StringFormatMethod("format")]
        public static void LogWarningFormat(string format, params object[] args)
        {
            if (!Instance.AllowWarningAndErrorMuting || Instance.LogEnabled())
            {
                Zebug.s_Logger.LogFormat(LogType.Warning, Instance.m_ColorString + format, args);
            }
        }

        [StringFormatMethod("format")]
        public static void LogWarningFormat(Object context, string format, params object[] args)
        {
            if (!Instance.AllowWarningAndErrorMuting || Instance.LogEnabled())
            {
                Zebug.s_Logger.LogFormat(LogType.Warning, context, Instance.m_ColorString + format, args);
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void Assert(bool condition)
        {
            if (condition)
            {
                return;
            }

            if (!Instance.AllowWarningAndErrorMuting || Instance.LogEnabled())
            {
                Zebug.s_Logger.Log(LogType.Assert, Instance.m_ColorString + "Assertion failed");
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void Assert(bool condition, Object context)
        {
            if (condition)
            {
                return;
            }

            if (!Instance.AllowWarningAndErrorMuting || Instance.LogEnabled())
            {
                Zebug.s_Logger.Log(LogType.Assert, message: Instance.m_ColorString + "Assertion failed", context);
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void Assert(bool condition, object message)
        {
            if (condition)
            {
                return;
            }

            if (!Instance.AllowWarningAndErrorMuting || Instance.LogEnabled())
            {
                Zebug.s_Logger.Log(LogType.Assert, Instance.m_ColorString + message);
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void Assert(bool condition, string message)
        {
            if (condition)
            {
                return;
            }

            if (!Instance.AllowWarningAndErrorMuting || Instance.LogEnabled())
            {
                Zebug.s_Logger.Log(LogType.Assert, Instance.m_ColorString + message);
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void Assert(bool condition, object message, Object context) {
            if (condition) {
                return;
            }
       
            Zebug.s_Logger.Log(LogType.Assert, message: Instance.m_ColorString + message, context);
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void Assert(bool condition, string message, Object context)
        {
            if (condition)
            {
                return;
            }

            if (!Instance.AllowWarningAndErrorMuting || Instance.LogEnabled())
            {
                Zebug.s_Logger.Log(LogType.Assert, message:Instance.m_ColorString + message, context);
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        [StringFormatMethod("format")]
        public static void AssertFormat(bool condition, string format, params object[] args)
        {
            if (condition)
            {
                return;
            }

            if (!Instance.AllowWarningAndErrorMuting || Instance.LogEnabled())
            {
                Zebug.s_Logger.LogFormat(LogType.Assert, Instance.m_ColorString + format, args);
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        [StringFormatMethod("format")]
        public static void AssertFormat(
            bool condition,
            Object context,
            string format,
            params object[] args)
        {
            if (condition)
            {
                return;
            }

            if (!Instance.AllowWarningAndErrorMuting || Instance.LogEnabled())
            {
                Zebug.s_Logger.LogFormat(LogType.Assert, context, Instance.m_ColorString + format, args);
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void LogAssertion(object message)
        {
            if (Instance.AllowWarningAndErrorMuting || Instance.LogEnabled())
            {
                Zebug.s_Logger.Log(LogType.Assert, message);
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void LogAssertion(object message, Object context) {
            if (!Instance.AllowWarningAndErrorMuting || Instance.LogEnabled()) 
            {
                Zebug.s_Logger.Log(LogType.Assert, message: Instance.m_ColorString + message, context);
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        [StringFormatMethod("format")]
        public static void LogAssertionFormat(string format, params object[] args)
        {
            if (!Instance.AllowWarningAndErrorMuting || Instance.LogEnabled())
            {
                Zebug.s_Logger.LogFormat(LogType.Assert, Instance.m_ColorString + format, args);
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        [StringFormatMethod("format")]
        public static void LogAssertionFormat(Object context, string format, params object[] args)
        {
            if (!Instance.AllowWarningAndErrorMuting || Instance.LogEnabled())
            {
                Zebug.s_Logger.LogFormat(LogType.Assert, context, Instance.m_ColorString + format, args);
            }
        }

    }

}