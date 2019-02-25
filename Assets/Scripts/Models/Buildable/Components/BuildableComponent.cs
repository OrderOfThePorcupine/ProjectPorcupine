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
using System.Linq;
using System.Reflection;
using MoonSharp.Interpreter;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace ProjectPorcupine.Buildable.Components
{
    [Serializable]
    public abstract class BuildableComponent
    {
        protected static readonly string ComponentLogChannel = "FurnitureComponents";

        protected Requirements componentRequirements = Requirements.None;

        private static Dictionary<string, Type> componentTypes;

        private bool initialized = false;

        public BuildableComponent()
        {
            // need to set it, for some reason GetHashCode is called during serialization (when Name is still null)
            Type = string.Empty;
        }

        public BuildableComponent(BuildableComponent other)
        {
            Type = other.Type;
        }

        [Flags]
        public enum Requirements
        {
            None = 0,
            Power = 1,
            Production = 1 << 1,
            Gas = 1 << 2,
            Fluid = 1 << 3
        }

        public enum ConditionType
        {
            IsGreaterThanZero,
            IsLessThanOne,
            IsZero,
            IsTrue,
            IsFalse
        }

        public string Type { get; set; }

        public Requirements Needs
        {
            get
            {
                return componentRequirements;
            }
        }

        public bool Initialized
        {
            get
            {
                return initialized;
            }
        }

        public virtual bool RequiresSlowUpdate
        {
            get
            {
                return false;
            }
        }

        public virtual bool RequiresFastUpdate
        {
            get
            {
                return false;
            }
        }

        protected Furniture ParentFurniture { get; set; }

        protected Parameter FurnitureParams
        {
            get { return ParentFurniture.Parameters; }
        }

        public static BuildableComponent Deserialize(JToken jtoken)
        {
            if (componentTypes == null)
            {
                componentTypes = FindComponentsInAssembly();
            }

            string componentTypeName = jtoken["Component"]["Type"].ToString();

            Type t;
            if (componentTypes.TryGetValue(componentTypeName, out t))
            {
                BuildableComponent component = (BuildableComponent)jtoken["Component"].ToObject(t);

                // need to set name explicitly (not part of deserialization as it's passed in)
                component.Type = componentTypeName;
                return component;
            }
            else
            {
                UnityDebugger.Debugger.LogErrorFormat(ComponentLogChannel, "There is no deserializer for component '{0}'", componentTypeName);
                return null;
            }
        }

       public static BuildableComponent FromJson(JToken componentToken)
        {
            if (componentTypes == null)
            {
                componentTypes = FindComponentsInAssembly();
            }

            JProperty componentProperty = (JProperty)componentToken;
            string componentTypeName = componentProperty.Name;
            Type t;
            if (componentTypes.TryGetValue(componentTypeName, out t))
            {
                // t.GetType();

                BuildableComponent component = (BuildableComponent)componentProperty.Value.ToObject(t);

                // need to set name explicitly (not part of deserialization as it's passed in)
                component.Type = componentProperty.Name;

                return component;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Initializes after loading the prototype.
        /// </summary>
        /// <param name="protoFurniture">Reference to prototype of furniture.</param>
        public virtual void InitializePrototype(Furniture protoFurniture)
        {
        }

        /// <summary>
        /// Initializes after placed into world.
        /// </summary>
        /// <param name="parentFurniture">Reference to furniture placed in world.</param>
        public void Initialize(Furniture parentFurniture)
        {
            ParentFurniture = parentFurniture;
            Initialize();
            initialized = true;
        }

        /// <summary>
        /// Determines if the configuration. Checked immediately after parsing the JSON config files.
        /// </summary>
        /// <returns>true if valid.</returns>
        public abstract bool IsValid();

        public virtual bool CanFunction()
        {
            return true;
        }

        public virtual void FixedFrequencyUpdate(float deltaTime)
        {
        }

        public virtual void EveryFrameUpdate(float deltaTime)
        {
        }

        public virtual List<ContextMenuAction> GetContextMenu()
        {
            return null;
        }

        public virtual IEnumerable<string> GetDescription()
        {
            return null;
        }

        public override string ToString()
        {
            return Type;
        }

        public abstract BuildableComponent Clone();

        protected abstract void Initialize();

        protected ContextMenuAction CreateComponentContextMenuItem(ComponentContextMenu componentContextMenuAction)
        {
            return new ContextMenuAction
            {
                LocalizationKey = componentContextMenuAction.Name, 
                RequireCharacterSelected = false,
                Action = (cma, c) => InvokeContextMenuAction(componentContextMenuAction.Function, componentContextMenuAction.Name)
            };
        }

        protected void InvokeContextMenuAction(Action<Furniture, string> function, string arg)
        {
            function(ParentFurniture, arg);
        }

        protected bool AreParameterConditionsFulfilled(List<ParameterCondition> conditions)
        {
            bool conditionsFulFilled = true;
            //// here evaluate all parameter conditions
            if (conditions != null)
            {
                foreach (ParameterCondition condition in conditions)
                {
                    bool partialEval = true;
                    switch (condition.Condition)
                    {
                        case ConditionType.IsZero:
                            partialEval = FurnitureParams[condition.ParameterName].ToFloat().Equals(0);
                            break;
                        case ConditionType.IsGreaterThanZero:
                            partialEval = FurnitureParams[condition.ParameterName].ToFloat() > 0f;
                            break;
                        case ConditionType.IsLessThanOne:
                            partialEval = FurnitureParams[condition.ParameterName].ToFloat() < 1f;
                            break;
                        case ConditionType.IsTrue:
                            partialEval = FurnitureParams[condition.ParameterName].ToBool() == true;
                            break;
                        case ConditionType.IsFalse:
                            partialEval = FurnitureParams[condition.ParameterName].ToBool() == false;
                            break;
                    }

                    conditionsFulFilled &= partialEval;
                }
            }

            return conditionsFulFilled;
        }

        protected string RetrieveStringFor(SourceDataInfo sourceDataInfo, Furniture furniture)
        {
            string retString = null;
            if (sourceDataInfo != null)
            {
                if (!string.IsNullOrEmpty(sourceDataInfo.Value))
                {
                    retString = sourceDataInfo.Value;
                }
                else if (!string.IsNullOrEmpty(sourceDataInfo.FromFunction))
                {
                    DynValue ret = FunctionsManager.Furniture.Call(sourceDataInfo.FromFunction, furniture);
                    retString = ret.String;
                }
                else if (!string.IsNullOrEmpty(sourceDataInfo.FromParameter))
                {
                    retString = furniture.Parameters[sourceDataInfo.FromParameter].ToString();
                }
            }

            return retString;
        }

        protected float RetrieveFloatFor(SourceDataInfo sourceDataInfo, Furniture furniture)
        {
            float retFloat = 0f;
            if (sourceDataInfo != null)
            {
                if (!string.IsNullOrEmpty(sourceDataInfo.Value))
                {
                    retFloat = float.Parse(sourceDataInfo.Value);
                }
                else if (!string.IsNullOrEmpty(sourceDataInfo.FromFunction))
                {
                    DynValue ret = FunctionsManager.Furniture.Call(sourceDataInfo.FromFunction, furniture);
                    retFloat = (float)ret.Number;
                }
                else if (!string.IsNullOrEmpty(sourceDataInfo.FromParameter))
                {
                    retFloat = furniture.Parameters[sourceDataInfo.FromParameter].ToFloat();
                }
            }

            return retFloat;
        }

        private static Dictionary<string, System.Type> FindComponentsInAssembly()
        {
            componentTypes = new Dictionary<string, System.Type>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies().Where(asm => !CSharpFunctions.IsDynamic(asm)))
            {
                foreach (Type type in assembly.GetTypes())
                {
                    BuildableComponentNameAttribute[] attribs = (BuildableComponentNameAttribute[])type.GetCustomAttributes(typeof(BuildableComponentNameAttribute), false);
                    if (attribs != null && attribs.Length > 0)
                    {
                        foreach (BuildableComponentNameAttribute compNameAttr in attribs)
                        {
                            componentTypes.Add(compNameAttr.ComponentName, type);
                            UnityDebugger.Debugger.LogFormat(ComponentLogChannel, "Found component in assembly: {0}", compNameAttr.ComponentName);
                        }
                    }
                }
            }

            return componentTypes;
        }

        [Serializable]
        [JsonObject(MemberSerialization.OptOut)]
        public class UseAnimation
        {
            public string Name { get; set; }

            public string ValueBasedParameterName { get; set; }

            [JsonConverter(typeof(ConditionsJsonConvertor))]
            public Conditions RunConditions { get; set; }
        }

        [Serializable]
        [JsonObject(MemberSerialization.OptOut)]
        public class ParameterCondition
        {
            public string ParameterName { get; set; }

            [JsonConverter(typeof(StringEnumConverter))]
            public ConditionType Condition { get; set; }
        }

        [Serializable]
        [JsonObject(MemberSerialization.OptOut)]
        public class ParameterDefinition
        {
            public ParameterDefinition()
            {
            }

            public ParameterDefinition(string paramName)
            {
                this.ParameterName = paramName;
            }

            public string ParameterName { get; set; }

            public string Type { get; set; }
        }

        [Serializable]
        [JsonObject(MemberSerialization.OptOut)]
        public class SourceDataInfo
        {
            public string Value { get; set; }

            public string FromParameter { get; set; }

            public string FromFunction { get; set; }
        }

        [Serializable]
        [JsonObject(MemberSerialization.OptOut)]
        public class Info
        {
            public float Rate { get; set; }

            public float Capacity { get; set; }

            public int CapacityThresholds { get; set; }

            public bool CanUseVariableEfficiency { get; set; }
        }

        public class ConditionsJsonConvertor : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Conditions);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                Conditions condition = new Conditions();
                condition.ParamConditions = new List<ParameterCondition>();
                JToken jtoken = JToken.ReadFrom(reader);
                foreach (JProperty prop in jtoken)
                {
                    ConditionType type = (ConditionType)Enum.Parse(typeof(ConditionType), prop.Value.ToString(), true);
                    condition.ParamConditions.Add(new ParameterCondition()
                    {
                        Condition = type,
                        ParameterName = prop.Name
                    });
                }

                return condition;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                Conditions conditions = (Conditions)value;
                JObject obj = new JObject();
                foreach (ParameterCondition param in conditions.ParamConditions)
                {
                    JProperty prop = new JProperty(param.ParameterName, param.Condition.ToString());
                    obj.Add(prop);
                }

                obj.WriteTo(writer);
            }
        }

        [Serializable]
        [JsonObject(MemberSerialization.OptIn)]
        public class Conditions
        {
            public List<ParameterCondition> ParamConditions { get; set; }
        }
    }
}
