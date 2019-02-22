#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software,
// and you are welcome to redistribute it under certain conditions; See
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using Newtonsoft.Json.Linq;

namespace ProjectPorcupine.Entities
{
    public class Stat : IPrototypable
    {
        public Stat()
        {
        }

        private Stat(Stat other)
        {
            Type = other.Type;
            Name = other.Name;
        }

        public string Type { get; set; }

        public string Name { get; set; }

        public int Value { get; set; }

        /// <summary>
        /// Reads the prototype from the specified JObject.
        /// </summary>
        /// <param name="jsonProto">The JProperty containing the prototype.</param>
        public void ReadJsonPrototype(JProperty jsonProto)
        {
            Type = jsonProto.Name;
            JToken innerJson = jsonProto.Value;
            Name = PrototypeReader.ReadJson(Name, innerJson["Name"]);
        }

        public Stat Clone()
        {
            return new Stat(this);
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", Type, Value);
        }
    }
}
