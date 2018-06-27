#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using NUnit.Framework;
using ProjectPorcupine.OrderActions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class OrderActionTest
{
    [Test]
    public void TestOrderActionCreation()
    {
        string inputJSON = @"{""Build"": {
          ""JobTime"": 1.0,
          ""Inventory"": {
            ""Steel Plate"": 5,
            ""Copper Plate"": 2
          }
        }}";

        Build expected = new Build();
        expected.JobTime = 1.0f;
        expected.Inventory = new Dictionary<string, int>()
        {
            { "Steel Plate", 5 },
            { "Copper Plate", 2 }
        };

        JProperty protoJson = (JProperty)JToken.ReadFrom(new JsonTextReader(new StringReader(inputJSON))).First;
        OrderAction component = ProjectPorcupine.OrderActions.OrderAction.FromJson(protoJson);
        Assert.NotNull(component);
        Assert.AreEqual("Build", component.Type);
        Assert.AreEqual(expected.Inventory["Steel Plate"], component.Inventory["Steel Plate"]);
        Assert.AreEqual(expected.Inventory["Copper Plate"], component.Inventory["Copper Plate"]);
        Assert.AreEqual(expected.JobTime, component.JobTime);
    }

    [Test]
    public void TestBuildSerialization()
    {
        Build buildOrder = new Build();

        buildOrder.JobTime = 1;
        buildOrder.Inventory = new Dictionary<string, int>()
        {
            { "Steel Plate", 5 },
            { "Copper Plate", 2 }
        };

        string serialized = JsonConvert.SerializeObject(buildOrder);
        Build deserialized = JsonConvert.DeserializeObject<Build>(serialized);
        Assert.NotNull(deserialized);
        Assert.AreEqual(deserialized.Inventory["Steel Plate"], buildOrder.Inventory["Steel Plate"]);
        Assert.AreEqual(deserialized.Inventory["Copper Plate"], buildOrder.Inventory["Copper Plate"]);
        Assert.IsTrue(deserialized.JobTime == buildOrder.JobTime);
    }

    [Test]
    public void TestDeconstructSerialization()
    {
        Deconstruct deconstructOrder = new Deconstruct();

        deconstructOrder.JobTime = 1;
        deconstructOrder.Inventory = new Dictionary<string, int>()
        {
            { "Steel Plate", 5 },
            { "Copper Plate", 2 }
        };

        string serialized = JsonConvert.SerializeObject(deconstructOrder);
        Deconstruct deserialized = JsonConvert.DeserializeObject<Deconstruct>(serialized);
        Assert.NotNull(deserialized);
        Assert.IsTrue(deserialized.JobTime == deconstructOrder.JobTime);
        Assert.AreEqual(deserialized.Inventory["Steel Plate"], deconstructOrder.Inventory["Steel Plate"]);
        Assert.AreEqual(deserialized.Inventory["Copper Plate"], deconstructOrder.Inventory["Copper Plate"]);
    }
}