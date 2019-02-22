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
using MoonSharp.Interpreter;
using Newtonsoft.Json.Linq;
using UnityEngine;

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

                    // First try to load var with System
                    types[i] = System.Type.GetType("System." + parameterSections[1], false, true);
                    
                    // If that doesn't resolve try with UnityEngine
                    if (types[i] == null)
                    {
                        types[i] = System.Type.GetType("UnityEngine." + parameterSections[1] + ", UnityEngine", false, true);
                    }

                    if (types[i] == null)
                    {
                        types[i] = System.Type.GetType("UnityEngine." + parameterSections[1] + ", UnityEngine.CoreModule", false, true);
                    }

                    // If that doesn't work fallback to object and throw a warning
                    if (types[i] == null)
                    {
                        types[i] = typeof(object);
                        UnityDebugger.Debugger.LogWarning("DevConsole", "VariableType " + parameterSections[1] + " is not found in the System or UnityEngine namespaces, type is now object");
                    }
                }
            }

            // Assign array
            this.TypeInfo = types;
        }
    }
}