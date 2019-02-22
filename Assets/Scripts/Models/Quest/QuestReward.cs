#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software,
// and you are welcome to redistribute it under certain conditions; See
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using MoonSharp.Interpreter;
using Newtonsoft.Json.Linq;

[MoonSharpUserData]
public class QuestReward
{
    public string Description { get; set; }

    public string OnRewardLuaFunction { get; set; }

    public Parameter Parameters { get; set; }

    public bool IsCollected { get; set; }

    public void ReadJsonPrototype(JToken token)
    {
        if (token != null)
        {
            Description = PrototypeReader.ReadJson(Description, token["Description"]);
            OnRewardLuaFunction = PrototypeReader.ReadJson(OnRewardLuaFunction, token["OnRewardLuaFunction"]);

            Parameters = new Parameter();
            Parameters.FromJson(token["Parameters"]);
        }
    }
}