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
using DeveloperConsole.Interfaces;
using MoonSharp.Interpreter;
using System.Reflection;

namespace DeveloperConsole.CommandTypes
{
    /// <summary>
    /// A core class for Internal Commands.
    /// </summary>
    [MoonSharpUserData]
    public class InternalCommand : CommandBase
    {
        /// <summary>
        /// Standard with title, method, and help text.
        /// </summary>
        /// <param name="title"> The title for the command.</param>
        /// <param name="method"> The command to execute.</param>
        /// <param name="helpText"> The help text to display.</param>
        public InternalCommand(string title, MethodInfo method, string descriptiveText, Type[] typeInfo, string parameters, string detailedDescriptiveText = "", string[] tags = null)
        {
            this.Title = title;
            this.Method = method;
            this.DescriptiveText = descriptiveText;
            this.Parameters = parameters;
            this.DetailedDescriptiveText = string.IsNullOrEmpty(detailedDescriptiveText) ? descriptiveText : detailedDescriptiveText;
            this.Tags = tags == null ? new string[0] : tags;
            this.TypeInfo = typeInfo;
        }

        /// <summary>
        /// Get all the parameters for this function.
        /// A string of all the parameters with a comma between them.
        /// </summary>
        public override string Parameters { get; protected set; }

        public MethodInfo Method { get; protected set; }

        public Type[] TypeInfo { get; protected set; }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="arguments"> Passes the arguments.</param>
        public override void ExecuteCommand(string arguments)
        {
            try
            {
                Method.Invoke(null, ParseArguments(arguments));
            }
            catch (Exception e)
            {
                DevConsole.LogError(Errors.UnknownError(this));
                UnityDebugger.Debugger.LogError("DevConsole", e.ToString());
            }
        }

        protected override object[] ParseArguments(string args)
        {
            object[] convertedArgs = new object[] { };

            try
            {
                string[] arguments = RegexToStandardPattern(args);
                convertedArgs = new object[arguments.Length];

                // If TypeInfo null then no new parameters to pass (we'll we will pass an array of strings, which could be empty)
                if (TypeInfo == null)
                {
                    return arguments;
                }

                for (int i = 0; i < TypeInfo.Length; i++)
                {
                    // Guard to make sure we don't actually go overboard
                    if (arguments.Length > i)
                    {
                        // This just wraps then unwraps, works quite fast actually, since its a easy wrap/unwrap.
                        convertedArgs[i] = GetValueType<object>(arguments[i], TypeInfo[i]);
                    }
                    else
                    {
                        // No point running through the rest, 
                        // this means 'technically' you could have 100 parameters at the end (not tested)
                        // However, that may break for other reasons
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                UnityDebugger.Debugger.LogError("DevConsole", e.ToString());
            }

            return convertedArgs;
        }
    }
}