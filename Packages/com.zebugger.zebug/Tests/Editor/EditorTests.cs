
using System;
using NUnit.Framework;
using UnityEngine;

using ZebugProject;
using Object = UnityEngine.Object;

namespace Tests {

    public class EditorTests {

         public class BlueZebug : Channel<BlueZebug> {
             public BlueZebug() : base("BlueZebug", Color.blue) { }
         }

         public class RedZebug : Channel<RedZebug> {
             public RedZebug() : base("RedZebug", Color.red) { }
         }

         public class ChildOfRedZebug : Channel<ChildOfRedZebug> {
             public ChildOfRedZebug() : base("ChildOfRedZebug", Color.magenta, RedZebug.Instance) { }
         }

         [SetUp]
         public void Setup() {
             BlueZebug.Instance.SetLogEnabled(true);
             RedZebug.Instance.SetLogEnabled(true);
             ChildOfRedZebug.Instance.SetLogEnabled(true);
             Zebug.s_Logger = Debug.unityLogger;
         }
         
         [Test]
         public void EditorTestsSimplePasses() {

             // Inside this method call, I need to be able to query which child class it's coming
             // from, so I can tell which static bool has been set _for that class_
             //
             BlueZebug.Log("this and the next thing");
             RedZebug.Log("this and the next thing");
         }

         [Test]
         public void ThereShouldOnlyBeBlue() {
             RedZebug.Instance.SetLogEnabled(false);

             BlueZebug.Log("Bleu, seulement");
             RedZebug.Log("Error if seen");
         }

         [Test]
         public void ThereShouldOnlyBeRed() {
             BlueZebug.Instance.SetLogEnabled(false);

             BlueZebug.Log("Error if seen");
             RedZebug.Log("Rouge, seulement");
         }

         [Test]
         public void ChildOfRedDisabledWhenRedIs() {
             RedZebug.Instance.SetLogEnabled(false);

             ChildOfRedZebug.Log("Error if seen.");
         }

         private class AssertOnceLogger : ILogger
         {
             public int count;
             public void LogFormat(LogType logType, Object context, string format, params object[] args) { count++; }
             public void LogException(Exception exception, Object context) { count++; }
             public bool IsLogTypeAllowed(LogType logType) { return true; }
             public void Log(LogType logType, object message) { count++; }
             public void Log(LogType logType, object message, Object context) { count++; }
             public void Log(LogType logType, string tag, object message) { count++; }
             public void Log(LogType logType, string tag, object message, Object context) { count++; }
             public void Log(object message) { count++; }
             public void Log(string tag, object message) { count++; }
             public void Log(string tag, object message, Object context) { count++; }
             public void LogWarning(string tag, object message) { count++; }
             public void LogWarning(string tag, object message, Object context) { count++; }
             public void LogError(string tag, object message) { count++; }
             public void LogError(string tag, object message, Object context) { count++; }
             public void LogFormat(LogType logType, string format, params object[] args) { count++; }
             public void LogException(Exception exception) { count++; }
             public ILogHandler logHandler { get; set; }
             public bool logEnabled { get; set; }
             public LogType filterLogType { get; set; }
         }

         [Test]
         public void AssertOnceOnlyPrintsOnce()
         {
             var assertOnceLogger = new AssertOnceLogger();
             assertOnceLogger.count = 0;
             Zebug.s_Logger = assertOnceLogger;

             for (int i = 0; i < 3; i++)
             {
                 Zebug.AssertOnce(false);
             }

             Assert.That(assertOnceLogger.count == 1);
         }



    }
}
