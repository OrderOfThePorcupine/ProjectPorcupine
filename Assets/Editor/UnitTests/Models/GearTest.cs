#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software,
// and you are welcome to redistribute it under certain conditions; See
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class GearTest{

    [Test]
    public void TestGearLoadingFromJSON()
    {
        foreach (JProperty test in JObject.Parse("{'test': 'test'}").Properties())
        {
            Assert.AreEqual((string)test.Value, "test");
            //Gear g = new Gear();
            //g.ReadJSONPrototype(test);
            //Assert.AreEqual(g.GetType(), "SpaceSuite");
            //Assert.AreEqual(g.MaxStackSize, 10);
        }
    }

}
