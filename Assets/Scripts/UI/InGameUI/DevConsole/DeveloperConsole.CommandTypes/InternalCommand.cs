#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System;
using System.Reflection;
using MoonSharp.Interpreter;

namespace DeveloperConsole.Core
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
            this.TypeInfo = typeInfo == null ? new Type[0] : typeInfo;
        }

        /// <summary>
        /// Get all the parameters for this function.
        /// A string of all the parameters with a comma between them.
        /// </summary>
        public override string Parameters { get; protected set; }

        public MethodInfo Method { get; protected set; }

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
    }
}