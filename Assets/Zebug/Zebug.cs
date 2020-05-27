//  --- Zebug --------------------------------------------------------------------------------------
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
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace ZebugProject {

public static class Zebug {

    [Conditional("ZEBUG")]
    public static void Log(string thisIsALog) {
        Debug.Log(thisIsALog);
    }

[Conditional("ZEBUG")]
	public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration) {
		Debug.DrawLine(start, end, color, duration);
	}

	[Conditional("ZEBUG")]
	public static void DrawLine(Vector3 start, Vector3 end, Color color) {
		Debug.DrawLine(start, end, color);
	}

	[Conditional("ZEBUG")]
	public static void DrawLine(Vector3 start, Vector3 end) {
		Debug.DrawLine(start, end);
	}

	[Conditional("ZEBUG")]
	public static void DrawLine() {
		Debug.DrawLine();
	}

	[Conditional("ZEBUG")]
	public static void DrawRay(Vector3 start, Vector3 dir, Color color, float duration) {
		Debug.DrawRay(start, dir, color, duration);
	}

	[Conditional("ZEBUG")]
	public static void DrawRay(Vector3 start, Vector3 dir, Color color) {
		Debug.DrawRay(start, dir, color);
	}

	[Conditional("ZEBUG")]
	public static void DrawRay(Vector3 start, Vector3 dir) {
		Debug.DrawRay(start, dir);
	}

	[Conditional("ZEBUG")]
	public static void DrawRay() {
		Debug.DrawRay();
	}

	[Conditional("ZEBUG")]
	public static void Break() {
		Debug.Break();
	}

	[Conditional("ZEBUG")]
	public static void DebugBreak() {
		Debug.DebugBreak();
	}

	[Conditional("ZEBUG")]
	public static void Log(object message) {
		Debug.Log(message);
	}

	[Conditional("ZEBUG")]
	public static void Log(object message, Object context) {
		Debug.Log(message, context);
	}

	[Conditional("ZEBUG")]
	public static void LogFormat(string format, params object[] args) {
		Debug.LogFormat(format, args);
	}

	[Conditional("ZEBUG")]
	public static void LogFormat(Object context, string format, params object[] args) {
		Debug.LogFormat(context, format, args);
	}

	[Conditional("ZEBUG")]
	public static void LogFormat() {
		Debug.LogFormat();
	}

	[Conditional("ZEBUG")]
	public static void LogError(object message) {
		Debug.LogError(message);
	}

	[Conditional("ZEBUG")]
	public static void LogError(object message, Object context) {
		Debug.LogError(message, context);
	}

	[Conditional("ZEBUG")]
	public static void LogErrorFormat(string format, params object[] args) {
		Debug.LogErrorFormat(format, args);
	}

	[Conditional("ZEBUG")]
	public static void LogErrorFormat(Object context, string format, params object[] args) {
		Debug.LogErrorFormat(context, format, args);
	}

	[Conditional("ZEBUG")]
	public static void ClearDeveloperConsole() {
		Debug.ClearDeveloperConsole();
	}

	[Conditional("ZEBUG")]
	public static void LogException(Exception exception) {
		Debug.LogException(exception);
	}

	[Conditional("ZEBUG")]
	public static void LogException(Exception exception, Object context) {
		Debug.LogException(exception, context);
	}

	[Conditional("ZEBUG")]
	public static void LogWarning(object message) {
		Debug.LogWarning(message);
	}

	[Conditional("ZEBUG")]
	public static void LogWarning(object message, Object context) {
		Debug.LogWarning(message, context);
	}

	[Conditional("ZEBUG")]
	public static void LogWarningFormat(string format, params object[] args) {
		Debug.LogWarningFormat(format, args);
	}

	[Conditional("ZEBUG")]
	public static void LogWarningFormat(Object context, string format, params object[] args) {
		Debug.LogWarningFormat(context, format, args);
	}

	[Conditional("ZEBUG")]
	public static void Assert(bool condition) {
		Debug.Assert(condition);
	}

	[Conditional("ZEBUG")]
	public static void Assert(bool condition, Object context) {
		Debug.Assert(condition, context);
	}

	[Conditional("ZEBUG")]
	public static void Assert(bool condition, object message) {
		Debug.Assert(condition, message);
	}

	[Conditional("ZEBUG")]
	public static void Assert(bool condition, string message) {
		Debug.Assert(condition, message);
	}

	[Conditional("ZEBUG")]
	public static void Assert(bool condition, object message, Object context) {
		Debug.Assert(condition, message, context);
	}

	[Conditional("ZEBUG")]
	public static void AssertFormat(bool condition, string format, params object[] args) {
		Debug.AssertFormat(condition, format, args);
	}

	[Conditional("ZEBUG")]
	public static void LogAssertion(object message) {
		Debug.LogAssertion(message);
	}

	[Conditional("ZEBUG")]
	public static void LogAssertion(object message, Object context) {
		Debug.LogAssertion(message, context);
	}

	[Conditional("ZEBUG")]
	public static void LogAssertionFormat(string format, params object[] args) {
		Debug.LogAssertionFormat(format, args);
	}

	[Conditional("ZEBUG")]
	public static void LogAssertionFormat(Object context, string format, params object[] args) {
		Debug.LogAssertionFormat(context, format, args);
	}

}
}