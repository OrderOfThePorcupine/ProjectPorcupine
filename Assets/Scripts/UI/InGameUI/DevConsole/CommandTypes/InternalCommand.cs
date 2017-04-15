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
using System.Linq;

namespace DeveloperConsole.Core
{
    /// <summary>
    /// A core class for Internal Commands.
    /// </summary>
    [MoonSharpUserData]
    public sealed class InternalCommand : CommandBase
    {
        /// <summary>
        /// Standard with title, method, and help text.
        /// </summary>
        /// <param name="title"> The title for the command. </param>
        /// <param name="method"> The command to execute. </param>
        /// <param name="descriptiveText"> The help text to display. </param>
        /// <param name="detailedDescriptiveText"> More detailed help text to display. </param>
        /// <param name="parameters"> In format of 'Type name' i.e. Int myInt.  With a comma separator. </param>
        /// <param name="tags"> Tags group commands for easy access. </param>
        /// <param name="typeInfo"> All the types in respect to this command. </param>
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

        /// <summary>
        /// The method to call to perform the required action.
        /// </summary>
        public MethodInfo Method { get; private set; }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="arguments"> Passes the arguments.</param>
        public override void ExecuteCommand(string arguments)
        {
            try
            {
                object[] objs = ParseArguments(arguments);
                if (objs.Length > 0)
                {
                    ParameterInfo[] pInfo = Method.GetParameters();
                    for (int i = 0; i < objs.Length; i++)
                    {
                        if (objs[i] == null)
                        {
                            // Add default
                            objs[i] = pInfo[i].DefaultValue;
                        }
                    }
                    Method.Invoke(null, objs);
                }
                else
                {
                    Method.Invoke(null, null);
                }
            }
            catch (Exception e)
            {
                DevConsole.LogError("An execute error as occured, this could be the method raising an error (or causing an error).  We could not locate the specific error however.");
                UnityDebugger.Debugger.LogError("DevConsole", e.ToString());
            }
        }
    }
}