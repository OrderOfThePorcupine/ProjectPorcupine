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

public class OrderActionTest
{
    [Test]
    public void TestOrderActionCreation()
    {
        string inputXML = @"<OrderAction type='Build' >
            <Job time='1' />
            <Inventory type='Steel Plate' amount='5' />
            <Inventory type='Copper Plate' amount='2' />
                           </OrderAction>";

        XmlReader reader = new XmlTextReader(new StringReader(inputXML));
        reader.Read();

        OrderAction component = ProjectPorcupine.OrderActions.OrderAction.Deserialize(reader);

        Assert.NotNull(component);
        Assert.AreEqual("Build", component.Type);
    }

    [Test]
    public void TestBuildXmlSerialization()
    {
        Build buildOrder = new Build();

        buildOrder.JobTime = 1;
        buildOrder.Inventory = new Dictionary<string, int>()
        {
            { "Steel Plate", 5 },
            { "Copper Plate", 2 }
        };

        // serialize
        StringWriter writer = new StringWriter();
        XmlSerializer serializer = new XmlSerializer(typeof(OrderAction), new Type[] { typeof(Build) });

        serializer.Serialize(writer, buildOrder);

        StringReader sr = new StringReader(writer.ToString());

        // if you want to dump file to disk for visual check, uncomment this
        ////File.WriteAllText("Build.xml", writer.ToString());

        // deserialize
        Build deserializedBuildOrder = (Build)serializer.Deserialize(sr);

        Assert.NotNull(deserializedBuildOrder);
        Assert.IsTrue(deserializedBuildOrder.Inventory.ContainsKey("Steel Plate"));
    }

    [Test]
    public void TestDeconstructXmlSerialization()
    {
        Deconstruct deconstructOrder = new Deconstruct();

        deconstructOrder.JobTime = 1;
        deconstructOrder.Inventory = new Dictionary<string, int>()
        {
            { "Steel Plate", 5 },
            { "Copper Plate", 2 }
        };

        // serialize
        StringWriter writer = new StringWriter();
        XmlSerializer serializer = new XmlSerializer(typeof(OrderAction), new Type[] { typeof(Deconstruct) });

        serializer.Serialize(writer, deconstructOrder);

        StringReader sr = new StringReader(writer.ToString());

        // if you want to dump file to disk for visual check, uncomment this
        ////File.WriteAllText("Deconstruct.xml", writer.ToString());

        // deserialize
        Deconstruct deserializedDeconstructOrder = (Deconstruct)serializer.Deserialize(sr);

        Assert.NotNull(deserializedDeconstructOrder);
        Assert.IsTrue(deserializedDeconstructOrder.Inventory.ContainsKey("Copper Plate"));
    }
}