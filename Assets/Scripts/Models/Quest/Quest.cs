#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software,
// and you are welcome to redistribute it under certain conditions; See
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System.Collections.Generic;
using MoonSharp.Interpreter;
using Newtonsoft.Json.Linq;

[MoonSharpUserData]
public class Quest : IPrototypable
{
    public string Name { get; set; }

    public string Type
    {
        get { return Name; }
    }

    public string Description { get; set; }

    public List<QuestGoal> Goals { get; set; }

    public bool IsAccepted { get; set; }

    public bool IsCompleted { get; set; }

    public List<QuestReward> Rewards { get; set; }

    public List<string> RequiredQuests { get; set; }

    /// <summary>
    /// Reads the prototype from the specified JObject.
    /// </summary>
    /// <param name="jsonProto">The JProperty containing the prototype.</param>
    public void ReadJsonPrototype(JProperty jsonProto)
    {
        Name = jsonProto.Name;
        JToken innerJson = jsonProto.Value;

        Goals = new List<QuestGoal>();
        Rewards = new List<QuestReward>();
        RequiredQuests = new List<string>();

        Description = PrototypeReader.ReadJson(Description, innerJson["Description"]);

        if (innerJson["RequiredQuests"] != null)
        {
            foreach (JToken token in innerJson["RequiredQuests"])
            {
                RequiredQuests.Add((string)token);
            }
        }

        if (innerJson["Goals"] != null)
        {
            foreach (JToken token in innerJson["Goals"])
            {
                QuestGoal goal = new QuestGoal();
                goal.ReadJsonPrototype(token);
                Goals.Add(goal);
            }
        }

        if (innerJson["Rewards"] != null)
        {
            foreach (JToken token in innerJson["Rewards"])
            {
                QuestReward reward = new QuestReward();
                reward.ReadJsonPrototype(token);
                Rewards.Add(reward);
            }
        }
    }
}