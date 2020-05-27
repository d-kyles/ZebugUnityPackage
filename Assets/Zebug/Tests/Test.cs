

#define ZEBUG

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using NUnit.Framework;
using ZebugProject;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

public class Test {


    [SetUp]
    public void SetUp() {

    }

    [Test]
    public void ZebugLog() {
        Zebug.Log("This is a log");
    }



}
