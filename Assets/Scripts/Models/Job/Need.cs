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

namespace ProjectPorcupine.Entities
{
    [MoonSharpUserData]
    public class Need : IPrototypable
    {
        private bool highToLow = true;
        private float amount = 0;

        // Use this for initialization
        public Need()
        {
            Amount = 0;
            RestoreNeedAmount = 100;
            EventActions = new EventActions();
        }

        private Need(Need other)
        {
            Amount = 0;
            Type = other.Type;
            LocalizationName = other.LocalizationName;
            GrowthRate = other.GrowthRate;
            highToLow = other.highToLow;
            RestoreNeedFurn = other.RestoreNeedFurn;
            RestoreNeedTime = other.RestoreNeedTime;
            RestoreNeedAmount = other.RestoreNeedAmount;
            Damage = other.Damage;

            if (other.EventActions != null)
            {
                EventActions = other.EventActions.Clone();
            }
        }

        public Character Character { get; set; }

        public string Type { get; private set; }

        public string LocalizationName { get; private set; }

        public float Amount
        {
            get
            {
                return amount;
            }

            set
            {
                amount = value.Clamp(0.0f, 100.0f);
            }
        }

        public float RestoreNeedAmount { get; private set; }

        public float GrowthRate { get; private set; }

        public float Damage { get; private set; }

        public bool CompleteOnFail { get; private set; }

        public Furniture RestoreNeedFurn { get; private set; }

        public float RestoreNeedTime { get; private set; }

        public string DisplayAmount
        {
            get
            {
                if (highToLow)
                {
                    return (100 - (int)Amount) + "%";
                }

                return ((int)Amount) + "%";
            }
        }

        /// <summary>
        /// Gets the EventAction for the current furniture.
        /// These actions are called when an event is called. They get passed the furniture
        /// they belong to, plus a deltaTime (which defaults to 0).
        /// </summary>
        /// <value>The event actions that is called on update.</value>
        public EventActions EventActions { get; private set; }

        // Update is called once per frame
        public void Update(float deltaTime)
        {
            if (EventActions != null && EventActions.HasEvent("OnUpdate"))
            {
                EventActions.Trigger("OnUpdate", this, deltaTime);
            }
            else
            {
                DefaultNeedDecay(deltaTime);
            }

            if (Amount.AreEqual(100))
            {
                if (EventActions != null && EventActions.HasEvent("OnEmptyNeed"))
                {
                    EventActions.Trigger("OnEmptyNeed", this, deltaTime);
                }
                else
                {
                    DefaultEmptyNeed();
                }
            }
            else if (Amount > 90f)
            {
                if (EventActions != null)
                {
                    EventActions.Trigger("OnSevereNeed", this, deltaTime);
                }
            }
            else if (Amount > 75f)
            {
                if (EventActions != null)
                {
                    EventActions.Trigger("OnCriticalNeed", this, deltaTime);
                }
            }
            else if (Amount > 50f)
            {
                if (EventActions != null)
                {
                    EventActions.Trigger("OnModerateNeed", this, deltaTime);
                }
            }
        }

        /// <summary>
        /// Reads the prototype from the specified JObject.
        /// </summary>
        /// <param name="jsonProto">The JProperty containing the prototype.</param>
        public void ReadJsonPrototype(JProperty jsonProto)
        {
            Type = jsonProto.Name;
            JToken innerJson = jsonProto.Value;
            string furniture = PrototypeReader.ReadJson(string.Empty, innerJson["RestoreNeedFurn"]);
            if (furniture != string.Empty)
            {
                RestoreNeedFurn = PrototypeManager.Furniture.Get(furniture);
            }

            RestoreNeedTime = PrototypeReader.ReadJson(RestoreNeedTime, innerJson["RestoreNeedTime"]);
            Damage = PrototypeReader.ReadJson(Damage, innerJson["Damage"]);
            CompleteOnFail = PrototypeReader.ReadJson(CompleteOnFail, innerJson["CompleteOnFail"]);
            highToLow = PrototypeReader.ReadJson(highToLow, innerJson["HighToLow"]);
            GrowthRate = PrototypeReader.ReadJson(GrowthRate, innerJson["GrowthRate"]);
            RestoreNeedAmount = PrototypeReader.ReadJson(RestoreNeedAmount, innerJson["RestoreNeedAmount"]);
            LocalizationName = PrototypeReader.ReadJson(LocalizationName, innerJson["LocalizationName"]);
            EventActions.ReadJson(innerJson["EventActions"]);
        }

        public void CompleteJobNorm(Job job)
        {
            Amount -= RestoreNeedAmount;
        }

        public void CompleteJobCrit(Job job)
        {
            Amount -= RestoreNeedAmount / 4;
        }

        public Need Clone()
        {
            return new Need(this);
        }

        public void DefaultNeedDecay(float deltaTime)
        {
            Amount += this.GrowthRate * deltaTime;
        }

        public void DefaultEmptyNeed()
        {
            // TODO: Default for empty need should probably be taking damage, but shouldn't be implemented until characters are 
            //       better able to handle getting their oxygen and maybe have real space suits.
        }
    }
}
