#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using ProjectPorcupine.OrderActions;

public class JobTest
{
    JobCategory constructCategory;
    JobCategory haulCategory;
    OrderAction defaultOrderAction;
    OrderAction badOrderAction;
    OrderAction updatedOrderAction;

    [SetUp]
    public void Setup()
    {
        string BuildOrderActionJson = @"{""Build"": {
          ""JobTime"": 1.0,
          ""Inventory"": {
            ""Steel Plate"": 5,
            ""Copper Plate"": 2
          }
        }}";

        string DeconstructOrderActionJson = @"
        {'Deconstruct': {
          'JobTime': 1.0,
          'JobCategory':'invalid',
          'Inventory': {
            'Steel Plate': 5,
            'Copper Plate': 2
          }
        }}";

        string MineOrderActionJson = @"{""Uninstall"": {
          ""JobTime"": 1.0,
          ""JobCategory"":""hauling"",
          ""JobPriority"":2,
          ""Inventory"": {
            ""Steel Plate"": 5,
            ""Copper Plate"": 2
          }
        }}";

        string JobCategoryJson = @"
        {   
            'JobCategory':{
	            'construct':{ 'localizationName':'job_category_construct'},
                'hauling':{ 'localizationName':'job_category_haul'}
            }
        }
        ";

        ModsManager modManager = new ModsManager();
        modManager.SetupPrototypeHandlers();

        Dictionary<string, JToken> jsonPrototypes = new Dictionary<string, JToken>();
        JToken protoJson = JToken.ReadFrom(new JsonTextReader(new StringReader(JobCategoryJson)));
        string tagName = ((JProperty)protoJson.First).Name;

        jsonPrototypes.Add(tagName, protoJson);
        modManager.LoadPrototypesFromJTokens(jsonPrototypes);

        constructCategory = PrototypeManager.JobCategory.Get("construct");
        haulCategory = PrototypeManager.JobCategory.Get("hauling");

        protoJson = JToken.ReadFrom(new JsonTextReader(new StringReader(BuildOrderActionJson))).First;
        defaultOrderAction = ProjectPorcupine.OrderActions.OrderAction.FromJson((JProperty)protoJson);

        protoJson = JToken.ReadFrom(new JsonTextReader(new StringReader(DeconstructOrderActionJson))).First;
        badOrderAction = ProjectPorcupine.OrderActions.OrderAction.FromJson((JProperty)protoJson);

        protoJson = JToken.ReadFrom(new JsonTextReader(new StringReader(MineOrderActionJson))).First;
        updatedOrderAction = ProjectPorcupine.OrderActions.OrderAction.FromJson((JProperty)protoJson);
    }

    [Test]
    public void T00_ValidJobCategories()
    {
        Assert.NotNull(constructCategory);
        Assert.NotNull(haulCategory);
    }

    [Test]
    public void T01_DefaultJobPriorities()
    {
        Assert.AreEqual(defaultOrderAction.Category, constructCategory);
        Assert.AreEqual(defaultOrderAction.Priority, Job.JobPriority.Medium);
    }

    [Test]
    public void T02_CreateJob()
    {
        UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, "Invalid tile detected. If this wasn't a test, you have an issue.");
        Job job = ((Build)defaultOrderAction).CreateJob(null, "test");
        Assert.AreEqual(job.Category, constructCategory);
        Assert.AreEqual(job.Priority, Job.JobPriority.Medium);
    }

    [Test]
    public void T03_ChangeJobPriorities()
    {
        UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, "Invalid tile detected. If this wasn't a test, you have an issue.");
        Job job = ((Build)defaultOrderAction).CreateJob(null, "test");
        job.Category = haulCategory;
        job.Priority = Job.JobPriority.Low;

        Assert.AreNotEqual(job.Category, constructCategory);
        Assert.AreEqual(job.Category, haulCategory);
        Assert.AreEqual(job.Priority, Job.JobPriority.Low);
    }

    [Test]
    public void T04_InvalidPriority()
    {
        UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, "Invalid tile detected. If this wasn't a test, you have an issue.");
        Job job = ((Build)defaultOrderAction).CreateJob(null, "test");
        job.Category = haulCategory;
        job.Priority = Job.JobPriority.Low;

        Assert.AreNotEqual(job.Category, constructCategory);
        Assert.AreEqual(job.Category, haulCategory);
        Assert.AreEqual(job.Priority, Job.JobPriority.Low);
    }

    [Test]
    public void T05_InvalidOrderAction()
    {
        Assert.IsNull(badOrderAction.Category);
    }

    [Test]
    public void T06_InvalidJobCategory()
    {
        UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, "Invalid tile detected. If this wasn't a test, you have an issue.");
        UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, "Invalid category detected.");
        Job job = ((Deconstruct)badOrderAction).CreateJob(null, "test");
    }

    [Test]
    public void T07_DefaultJobPriorities()
    {
        Assert.AreEqual(updatedOrderAction.Category, haulCategory);
        Assert.AreEqual(updatedOrderAction.Priority, Job.JobPriority.Low);
    }

    [Test]
    public void T08_CreateJob()
    {
        UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, "Invalid tile detected. If this wasn't a test, you have an issue.");
        Job job = ((Uninstall)updatedOrderAction).CreateJob(null, "test");
        Assert.AreEqual(job.Category, haulCategory);
        Assert.AreEqual(job.Priority, Job.JobPriority.Low);
    }
}