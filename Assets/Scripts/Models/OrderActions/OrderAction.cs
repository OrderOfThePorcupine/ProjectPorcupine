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
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json.Linq;

namespace ProjectPorcupine.OrderActions
{
    [Serializable]
    public abstract class OrderAction
    {
        protected static readonly string OrderActionsLogChannel = "OrderActions";

        private static Dictionary<string, Type> orderActionTypes;

        public OrderAction()
        {
        }

        public OrderAction(OrderAction other)
        {
            JobTimeFunction = other.JobTimeFunction;
            Inventory = other.Inventory;
            Type = other.Type;
            JobTime = other.JobTime;
        }

        [XmlIgnore]
        public string Type { get; set; }

        public float JobTime { get; set; }

        public string JobTimeFunction { get; set; }

        public Dictionary<string, int> Inventory { get; set; }

        public static OrderAction Deserialize(XmlReader xmlReader)
        {
            if (orderActionTypes == null)
            {
                orderActionTypes = FindOrderActionsInAssembly();
            }

            string orderActionType = xmlReader.GetAttribute("type");
            if (orderActionTypes.ContainsKey(orderActionType))
            {
                xmlReader = xmlReader.ReadSubtree();
                Type t = orderActionTypes[orderActionType];
                XmlSerializer serializer = new XmlSerializer(t);
                OrderAction orderAction = (OrderAction)serializer.Deserialize(xmlReader);
                //// need to set name explicitly (not part of deserialization as it's passed in)
                orderAction.Initialize(orderActionType);
                return orderAction;
            }
            else
            {
                UnityDebugger.Debugger.Log(OrderActionsLogChannel, string.Format("There is no deserializer for OrderAction '{0}'", orderActionType));
                return null;
            }
        }

        public static OrderAction FromJson(JProperty orderActionProp)
        {
            if (orderActionTypes == null)
            {
                orderActionTypes = FindOrderActionsInAssembly();
            }

            string orderActionType = orderActionProp.Name;

            if (orderActionTypes.ContainsKey(orderActionType))
            {
                Type t = orderActionTypes[orderActionType];

                OrderAction orderAction = (OrderAction)orderActionProp.Value.ToObject(t);
                orderAction.Type = orderActionProp.Name;

                // TODO: Probably don't need to set any of this here, if this workse right... not sure.
                // TODO: It seems we in fact don't need any of this, remove if loading works fine.
//                UnityDebugger.Debugger.LogWarning(orderAction.JobTime + " - " + orderAction.JobTimeFunction);
//                orderAction.JobTime = PrototypeReader.ReadJson(orderAction.JobTime, orderActionProp.Value["JobTime"]);
//                orderAction.JobTimeFunction = PrototypeReader.ReadJson(orderAction.JobTimeFunction, orderActionProp.Value["JobTimeFunction"]);

//                if (orderActionProp.Value["Inventory"] != null)
//                {
//                    UnityDebugger.Debugger.LogWarning(orderActionType);
//                    foreach (JProperty inventory in orderActionProp.Value["Inventory"])
//                    {
//                        UnityDebugger.Debugger.LogWarning(inventory.Name);
//                        orderAction.Inventory.Add(inventory.Name, (int)inventory.Value);
//                    }
//                }

                return orderAction;
            }
            else
            {
                UnityDebugger.Debugger.Log(OrderActionsLogChannel, string.Format("There is no deserializer for OrderAction '{0}'", orderActionType));
                return null;
            }
        }

        public virtual void Initialize(string type)
        {
            Type = type;
        }

        public abstract Job CreateJob(Tile tile, string type);

        public override string ToString()
        {
            return Type;
        }

        public abstract OrderAction Clone();

        protected Job CheckJobFromFunction(string functionName, Furniture furniture)
        {
            Job job = null;
            if (!string.IsNullOrEmpty(functionName))
            {
                job = FunctionsManager.Furniture.Call<Job>(functionName, furniture, null);
                job.OrderName = Type;
            }

            return job;
        }

        private static Dictionary<string, System.Type> FindOrderActionsInAssembly()
        {
            var orderActionTypes = new Dictionary<string, System.Type>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies().Where(asm => !CSharpFunctions.IsDynamic(asm)))
            {
                foreach (Type type in assembly.GetTypes())
                {
                    OrderActionNameAttribute[] attribs = (OrderActionNameAttribute[])type.GetCustomAttributes(typeof(OrderActionNameAttribute), false);
                    if (attribs != null && attribs.Length > 0)
                    {
                        foreach (OrderActionNameAttribute compNameAttr in attribs)
                        {
                            orderActionTypes.Add(compNameAttr.OrderActionName, type);
                            UnityDebugger.Debugger.Log(OrderActionsLogChannel, string.Format("Found OrderAction in assembly: {0}", compNameAttr.OrderActionName));
                        }
                    }
                }
            }

            return orderActionTypes;
        }
    }
}
