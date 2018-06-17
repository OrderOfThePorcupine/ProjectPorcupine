#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using MoonSharp.Interpreter;
using Newtonsoft.Json.Linq;

namespace DeveloperConsole.Core
{
    /// <summary>
    /// Invoke some code from either C# Function Manager or LUA Function Manager.
    /// </summary>
    [MoonSharpUserData]
    public sealed class InvokeCommand : CommandBase, IPrototypable
    {
        /// <summary>
        /// The name of the function to execute.
        /// </summary>
        public string FunctionName
        {
            get; private set;
        }

        /// <summary>
        /// All the parameters in format Type name, with a comma seperator.
        /// </summary>
        public override string Parameters
        {
            get; protected set;
        }

        /// <summary>
        /// The type of command.
        /// </summary>
        public string Type
        {
            get
            {
                return this.Title;
            }
        }

        /// <summary>
        /// Execute this command.
        /// </summary>
        /// <param name="arguments"> The arguments to pass in. </param>
        public override void ExecuteCommand(string arguments)
        {
            try
            {
                FunctionsManager.DevConsole.CallWithError(FunctionName, ParseArguments(arguments));
            }
            catch (Exception e)
            {
                DevConsole.LogError(e.Message);
            }
        }

        /// <summary>
        /// Reads from the reader provided.
        /// </summary>
        public void ReadXmlPrototype(XmlReader reader)
        {
            Title = reader.GetAttribute("Title");
            FunctionName = reader.GetAttribute("FunctionName");
            Description = reader.GetAttribute("Description");
            DetailedDescription = reader.GetAttribute("DetailedDescription");
            Parameters = reader.GetAttribute("Parameters");
            ParseParameterToTypeInfo();
            Tags = reader.GetAttribute("Tags").Split(',').Select(x => x.Trim()).ToArray();
            DefaultValue = reader.GetAttribute("DefaultValue");

            // This is an optional checker basically
            if (Tags == null)
            {
                Tags = new string[0];
            }

            if (DefaultValue == null)
            {
                DefaultValue = string.Empty;
            }
        }

        /// <summary>
        /// Reads the prototype from the specified JProperty.
        /// </summary>
        /// <param name="jsonProto">The JProperty containing the prototype.</param>
        public void ReadJsonPrototype(JProperty jsonProto)
        {
            Title = jsonProto.Name;
            JToken innerJson = jsonProto.Value;
            FunctionName = (string)innerJson["FunctionName"];
            Description = (string)innerJson["Description"];
            DetailedDescription = (string)innerJson["DetailedDescription"];
            Parameters = PrototypeReader.ReadJson(Parameters, innerJson["Parameters"]);
            ParseParameterToTypeInfo();
            DefaultValue = (string)innerJson["DefaultValue"] ?? string.Empty;

            Tags = ((JArray)innerJson["Tags"]).ToObject<string[]>();
        }

        private void ParseParameterToTypeInfo() 
        {
            if (Parameters == null)
            {
                // Json can return a null Parameters rather than empty string, if that's the case set it to empty and return.
                Parameters = string.Empty;
                return;
            }

            // If the parameters contains a ';' then it'll exclude the 'using' statement.
            // Just makes the declaration help look nicer.
            if (Parameters.Contains(';'))
            {
                int indexOfSemiColon = Parameters.IndexOf(';') + 1;

                if (Parameters.Length > indexOfSemiColon)
                {
                    Parameters = Parameters.Substring(indexOfSemiColon).Trim();
                }
                else
                {
                    // Something weird happened here so we are just setting it to ''
                    // This will only happen if the semi colon is the last element in the string
                    Parameters = string.Empty;

                    UnityDebugger.Debugger.LogWarning("DevConsole", "Parameters for " + Title + " had a semicolon as a last character this is an illegal string.");
                }
            }

            // Parse the parameters
            // We are using regex since we only want a very specific part of the parameters
            // This is relatively fast, (actually quite fast compared to other methods and is in start)
            // Something like text: String, value: Int, on: Bool
            // Old Regex: \s*(.*?\;)?.*?\:(.*?)\s*(?:\,|$)
            // Koosemoose's Regex: /using\s+([^\s]+)\s+([^\s]+)\s+([^\s]+)/
            // My Adjustments to Koosemoose's Regex (optimises for groups not needed): (using\s+[^\s]+)?\s*([^\s]+)\s+[^\s]+
            // Note: Regex would be faster than using a for loop, cause it would require a lot of splits, and other heavily costing operations.
            string regexExpression = @"\s*([^\s]+)\s+[^\s]+";

            // This will just get the types
            string[] parameterTypes = Regex.Matches(Parameters, regexExpression)
                .Cast<Match>()
                .Where(m => m.Groups.Count >= 1 && m.Groups[1].Value != string.Empty)
                .Select(m => (m.Groups[1].Value.Contains('.') ? ", " + m.Groups[1].Value.Trim().Split('.')[0] : string.Empty) + ";" + m.Groups[1].Value.Trim())
                .ToArray();

            Type[] types = new Type[parameterTypes.Length];

            // Now we just cycle through and get types
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                if (parameterTypes[i] == string.Empty)
                {
                    types[i] = typeof(object);
                }
                else
                {
                    string[] parameterSections = parameterTypes[i].Split(';');

                    // This is just to have a safety, that may trigger in some cases??  Better than nothing I guess
                    // Could try to remove, but in most cases won't be part of DevConsole, till you open, or it starts.
                    try
                    {
                        // We just split, since its a decently appropriate solution.
                        types[i] = System.Type.GetType((parameterSections[1].Contains('.') ? parameterSections[1] : "System." + parameterSections[1]) + parameterSections[0], true, true);
                    }
                    catch (Exception e)
                    {
                        // Also replace the string splitting and containing
                        // with a more efficient algo.
                        // Slight hack fix later @ TODO
                        // Try with UnityEngine.X, UnityEngine.Core
                        try
                        {
                            types[i] = System.Type.GetType("UnityEngine." + parameterSections[1] + ", UnityEngine.CoreModule", true, true);
                        }
                        catch (Exception inner)
                        {
                            // This means invalid type, so we set it to object.
                            // This in most cases is fine, just means that when you call it, 
                            // it won't work (unless the type is object)
                            types[i] = typeof(object);
                            UnityDebugger.Debugger.LogError("DevConsole", typeof(UnityEngine.Vector3).Assembly.FullName + "; " + typeof(UnityEngine.Vector3).AssemblyQualifiedName);
                            UnityDebugger.Debugger.LogError("DevConsole", e.Message + "\n" + inner.Message);
                        }
                    }
                }
            }

            // Assign array
            this.TypeInfo = types;
        }
    }
}