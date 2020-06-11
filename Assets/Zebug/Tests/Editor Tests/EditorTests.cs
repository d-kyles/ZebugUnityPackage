
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests {

    public class EditorTests {

         public class BlueZebug : global::ZebugProject.Channel<BlueZebug> {
             public BlueZebug() : base("Tests", Color.blue) { }
         }

         public class RedZebug : global::ZebugProject.Channel<RedZebug> {
             public RedZebug() : base("Tests", Color.red) { }
         }

         public class ChildOfRedZebug : global::ZebugProject.Channel<ChildOfRedZebug> {
             public ChildOfRedZebug() : base("Tests", Color.red, RedZebug.Instance) { }
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



    //     // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    //     // `yield return null;` to skip a frame.
    //     [UnityTest]
    //     public IEnumerator EditorTestsWithEnumeratorPasses() {
    //         // Use the Assert class to test conditions.
    //         // Use yield to skip a frame.
    //         yield return null;
    //     }
    }
}
