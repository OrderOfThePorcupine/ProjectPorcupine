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

namespace DeveloperConsole.Core.CommandTypes
{
    /// <summary>
    /// Invoke some code from either C# Function Manager or LUA Function Manager.
    /// </summary>
    [MoonSharpUserData]
    public sealed class InvokeCommand : CommandBase
    {
        /// <summary>
        /// Standard constructor.
        /// </summary>
        /// <param name="title"> The title of the command. </param>
        /// <param name="functionName"> The function name of the command (can be C# or LUA). </param>
        /// <param name="descriptiveText"> The text that describes this command. </param>
        /// <param name="helpFunctionName"> The help function name of the command (can be C# or LUA). </param>
        /// <param name="parameters"> The parameters ( should be invokeable format of (using [any character];)? type variableName, ... ). </param>
        public InvokeCommand(string title, string functionName, string descriptiveText, string detailedDescriptiveText, string parameters, string[] tags, string defaultValue)
        {
            Title = title;
            FunctionName = functionName;
            DescriptiveText = descriptiveText;
            DetailedDescriptiveText = detailedDescriptiveText;
            Tags = tags;
            DefaultValue = defaultValue;

            // If the parameters contains a ';' then it'll exclude the 'using' statement.
            // Just makes the declaration help look nicer.
            if (parameters.Contains(';'))
            {
                int indexOfSemiColon = parameters.IndexOf(';') + 1;

                if (parameters.Length > indexOfSemiColon)
                {
                    Parameters = parameters.Substring(indexOfSemiColon).Trim();
                }
                else
                {
                    // Something weird happened here so we are just setting it to ''
                    // This will only happen if the semi colon is the last element in the string
                    Parameters = string.Empty;

                    UnityDebugger.Debugger.LogWarning("DevConsole", "Parameters for " + title + " had a semicolon as a last character this is an illegal string.");
                }
            }
            else
            {
                // No ';' exists so free to just copy it over
                // We can't just do a substring cause it'll return an error and this isn't safely done
                Parameters = parameters;
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
            string[] parameterTypes = Regex.Matches(parameters, regexExpression)
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
                    // This is just to have a safety, that may trigger in some cases??  Better than nothing I guess
                    // Could try to remove, but in most cases won't be part of DevConsole, till you open, or it starts.
                    try
                    {
                        // We just split, since its a decently appropriate solution.
                        string[] parameterSections = parameterTypes[i].Split(';');

                        types[i] = Type.GetType((parameterSections[1].Contains('.') ? parameterSections[1] : "System." + parameterSections[1]) + parameterSections[0], true, true);
                    }
                    catch (Exception e)
                    {
                        // This means invalid type, so we set it to object.
                        // This in most cases is fine, just means that when you call it, 
                        // it won't work (unless the type is object)
                        types[i] = typeof(object);
                        UnityDebugger.Debugger.LogError("DevConsole", e.Message);
                    }
                }
            }

            // Assign array
            this.TypeInfo = types;
        }

        public string FunctionName
        {
            get; private set;
        }

        public override string Parameters
        {
            get; protected set;
        }

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
    }
}