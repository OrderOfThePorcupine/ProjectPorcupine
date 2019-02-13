#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using NUnit.Framework;
using Newtonsoft.Json.Linq;

public class ParametersEditorTest
{
    private string JsonTest1;
    private Parameter param1;

    [SetUp]
    public void Init()
    {
        JsonTest1 = @"{'Parameters':{
            'gas_limit': '0.2',
            'gas_per_second': '0.16',
            'gas_gen':{
                'O2':{ 'gas_limit':'0.2'},
                'N2':{ 'gas_limit':'0.8'}
                }
            }}";

        JToken token = JToken.Parse(JsonTest1)["Parameters"];
        param1 = new Parameter();
        param1.FromJson(token);
    }

    [Test]
    public void ParameterReadKeys()
    {
        Assert.That(param1, Is.Not.Null);

        Assert.That(param1.ContainsKey("gas_limit"), Is.True);
        Assert.That(param1.ContainsKey("gas_per_second"), Is.True);
        Assert.That(param1.ContainsKey("gas_gen"), Is.True);

        Assert.That(param1["gas_gen"].ContainsKey("O2"), Is.True);
        Assert.That(param1["gas_gen"].ContainsKey("N2"), Is.True);
    }

    [Test]
    public void ParameterReadValues()
    {
        Assert.That(param1["gas_limit"], Is.TypeOf(typeof(Parameter)));
        Assert.That(param1["gas_limit"].HasContents(), Is.False);
        Assert.That(param1["gas_limit"].ToString(), Is.EqualTo("0.2"));

        Assert.That(param1["gas_per_second"], Is.TypeOf(typeof(Parameter)));
        Assert.That(param1["gas_per_second"].HasContents(), Is.False);
        Assert.That(param1["gas_per_second"].ToString(), Is.EqualTo("0.16"));

        Assert.That(param1["gas_gen"], Is.TypeOf(typeof(Parameter)));
        Assert.That(param1["gas_gen"].HasContents(), Is.True);
        Assert.That(param1["gas_gen"].ToFloat(), Is.EqualTo(0));
        Assert.That(param1["gas_gen"].ToString(), Is.EqualTo(null));

        Assert.That(param1["gas_gen"]["O2"], Is.TypeOf(typeof(Parameter)));
        Assert.That(param1["gas_gen"]["O2"].HasContents(), Is.True);
        Assert.That(param1["gas_gen"]["O2"].ToString(), Is.EqualTo(null));
        Assert.That(param1["gas_gen"]["O2"].Keys().Length, Is.EqualTo(1));
        Assert.That(param1["gas_gen"]["O2"].ContainsKey("gas_limit"), Is.True);
        Assert.That(param1["gas_gen"]["O2"]["gas_limit"].ToString(), Is.EqualTo("0.2"));

        Assert.That(param1["gas_gen"]["N2"], Is.TypeOf(typeof(Parameter)));
        Assert.That(param1["gas_gen"]["N2"].HasContents(), Is.True);
        Assert.That(param1["gas_gen"]["N2"].ToString(), Is.EqualTo(null));
        Assert.That(param1["gas_gen"]["N2"].Keys().Length, Is.EqualTo(1));
        Assert.That(param1["gas_gen"]["N2"].ContainsKey("gas_limit"), Is.True);
        Assert.That(param1["gas_gen"]["N2"]["gas_limit"].ToString(), Is.EqualTo("0.8"));
    }

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
