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
using System.Globalization;
using System.Runtime.CompilerServices;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ZebugProject
{
    /*
     |  --- ZebugILoggerWrapper
     |      
     |      So I can pass a channel to a third party package and have that package use Zebug!
     |
     |      WARNING do not assign this ILogger to Zebug.s_Logger, that would be recursively bad. 
     |      
     | 2022-08-30
     */
    public class ZebugILoggerWrapper<T> : ILogger where T : Channel<T>, new()
    {
        private readonly Channel<T> _channel;
        private ZebugILoggerWrapper() { }
        
        public ZebugILoggerWrapper(Channel<T> channel)
        {
            _channel = channel;
        }

        public bool IsLogTypeAllowed(LogType logType)
        {
            return Channel<T>.ShouldLog(logType);
        }

        //  --- TODO(dan): Not implemented
        public ILogHandler logHandler { get; set; }
        public bool logEnabled { get; set; }
        public LogType filterLogType { get; set; }

        private static string GetString(object message)
        {
            if (message == null)
                return "Null";
          
            return message is IFormattable formattable ? formattable.ToString((string) null, CultureInfo.InvariantCulture) : message.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log(LogType logType, object message)
        {
            Channel<T>.LogFormat(logType, null, "{0}", GetString(message));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log(LogType logType, object message, Object context)
        {
            Channel<T>.LogFormat(logType, context, "{0}", GetString(message));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log(LogType logType, string tag, object message)
        {
            Channel<T>.LogFormat(logType, null, "{0}: {1}", tag, GetString(message));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log(LogType logType, string tag, object message, Object context)
        {
            Channel<T>.LogFormat(logType, context, "{0}: {1}", tag, GetString(message));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log(object message)
        {
            Channel<T>.LogFormat(LogType.Log, null, "{0}", GetString(message));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log(string tag, object message)
        {
            Channel<T>.LogFormat(LogType.Log, null, "{0}: {1}", tag, GetString(message));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log(string tag, object message, Object context)
        {
            Channel<T>.LogFormat(LogType.Log, context, "{0}: {1}", tag, GetString(message));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogWarning(string tag, object message)
        {
            Channel<T>.LogFormat(LogType.Warning, null, "{0}: {1}", tag, GetString(message));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogWarning(string tag, object message, Object context)
        {
            Channel<T>.LogFormat(LogType.Warning, context, "{0}: {1}", tag, GetString(message));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogError(string tag, object message)
        {
            Channel<T>.LogFormat(LogType.Error, null, "{0}: {1}", tag, GetString(message));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogError(string tag, object message, Object context)
        {
            Channel<T>.LogFormat(LogType.Error, context, "{0}: {1}", tag, GetString(message));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogException(Exception exception)
        {
            Channel<T>.LogException(exception, null);
        }

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogException(Exception exception, Object context)
        {
            Channel<T>.LogException(exception, context);
        }

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogFormat(LogType logType, string format, params object[] args)
        {
            Channel<T>.LogFormat(logType, null, format, args);
        }

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            Channel<T>.LogFormat(logType, context, format, args);
        }
    }

}