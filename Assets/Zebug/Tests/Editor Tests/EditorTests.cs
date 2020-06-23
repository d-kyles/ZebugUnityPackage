
using NUnit.Framework;
using UnityEngine;

using ZebugProject;

namespace Tests {

    public class EditorTests {

         public class BlueZebug : Channel<BlueZebug> {
             public BlueZebug() : base("Tests", Color.blue) { }
         }

         public class RedZebug : Channel<RedZebug> {
             public RedZebug() : base("Tests", Color.red) { }
         }

         public class ChildOfRedZebug : Channel<ChildOfRedZebug> {
             public ChildOfRedZebug() : base("Tests", Color.magenta, RedZebug.Instance) { }
         }

         [SetUp]
         public void Setup() {
             BlueZebug.Instance.SetLogEnabled(true);
             RedZebug.Instance.SetLogEnabled(true);
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
    }
}
