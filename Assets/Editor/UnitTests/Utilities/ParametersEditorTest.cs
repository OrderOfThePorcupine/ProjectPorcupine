#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using NUnit.Framework;

public class ParametersEditorTest
{
    private Parameter param1;

    [Test]
    public void ParameterAccessingKeyNotDefined()
    {
        Assert.That(param1.ContainsKey("bad_key"), Is.False);

        // Accessing it creates a new empty Parameter.
        Parameter param2 = param1["bad_key"];
        Assert.That(param1.ContainsKey("bad_key"), Is.True);
        Assert.That(param2.ToString(), Is.Null);
        Assert.That(param2.ToFloat(), Is.EqualTo(0));
        Assert.That(param2.Keys(), Is.EqualTo(new string[] { }));
        Assert.That(param2.HasContents(), Is.False);
    }

    [Test]
    public void ParameterAddParameter()
    {
        Assert.That(param1.ContainsKey("bad_key"), Is.False);

        Parameter param2 = new Parameter("bad_key", "hello world");
        param1.AddParameter(param2);

        Assert.That(param1.ContainsKey("bad_key"), Is.True);
        Assert.That(param1["bad_key"].ToString(), Is.EqualTo("hello world"));
    }

    [Test]
    public void ParameterWithValueAndContents()
    {
        Parameter param2 = new Parameter("Alice");
        Assert.That(param2.ToString(), Is.Null);

        param2.SetValue("test");
        Assert.That(param2.ToString(), Is.EqualTo("test"));
        Assert.That(param2.ContainsKey("Bob"), Is.False);

        param2.AddParameter(new Parameter("Bob"));
        Assert.That(param2.ToString(), Is.EqualTo("test"));
        Assert.That(param2.ContainsKey("Bob"), Is.True);
        Assert.That(param2["Bob"].GetName(), Is.EqualTo("Bob"));
    }

    [Test]
    public void ParameterCopyConstructorDoesDeepCopy()
    {
        Parameter param2 = new Parameter(param1);

        // Old value was copied.
        Assert.That(param2["gas_limit"].ToString(), Is.EqualTo("0.2"));

        // But changing param1 does not change param2.
        param1["gas_limit"].SetValue("1.0");
        Assert.That(param1["gas_limit"].ToString(), Is.EqualTo("1.0"));
        Assert.That(param2["gas_limit"].ToString(), Is.EqualTo("0.2"));
    }

    [Test]
    public void ParameterChangeFloatValue()
    {
        param1["gas_limit"].ChangeFloatValue(1.0f);
        Assert.That(param1["gas_limit"].ToString(), Is.EqualTo("1.2"));
    }

    [Test]
    public void ParameterToJson()
    {
        Parameter p = new Parameter("p", "test");
        string paramJson = Newtonsoft.Json.JsonConvert.SerializeObject(p.ToJson());

        Assert.That(paramJson, Is.EqualTo("\"test\""));
    }

    [Test]
    public void ParameterGroupToJson()
    {
        Parameter p = new Parameter("p", "test");
        Parameter container = new Parameter("c");
        container.AddParameter(p);
        string paramJson = Newtonsoft.Json.JsonConvert.SerializeObject(container.ToJson());

        Assert.That(paramJson, Is.EqualTo("{\"p\":\"test\"}"));
    }
}
