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
using UnityEngine;

namespace DeveloperConsole.Core
{
    /// <summary>
    /// A command base that all commands derive from.
    /// </summary> 
    [MoonSharpUserData]
    public abstract class CommandBase
    {
        /// <summary>
        /// Text describing the command.
        /// </summary>
        public string Description
        {
            get; protected set;
        }

        /// <summary>
        /// The parameter list.
        /// </summary>
        public abstract string Parameters
        {
            get; protected set;
        }

        /// <summary>
        /// The title of the command.
        /// </summary>
        public string Title
        {
            get; protected set;
        }

        /// <summary>
        /// The detailed help description.
        /// </summary>
        public string DetailedDescription
        {
            get; protected set;
        }

        /// <summary>
        /// The tags associated with this command.
        /// </summary>
        public string[] Tags
        {
            get; protected set;
        }

        /// <summary>
        /// All types associated with this command.
        /// </summary>
        public Type[] TypeInfo
        {
            get; protected set;
        }

        /// <summary>
        /// The default value of this command.
        /// Implemented only for LUA.
        /// </summary>
        public string DefaultValue
        {
            get; protected set;
        }

        /// <summary>
        /// Execute this command.
        /// </summary>
        /// <param name="arguments"></param>
        public abstract void ExecuteCommand(string arguments);

        /// <summary>
        /// Regexs the character set properly, should always be called instead of you trying to do it yourself.
        /// </summary>
        /// <param name="arguments"> The string to split and trim.</param>
        /// <param name="atCharacter"> What character to split at.</param>
        protected string[] RegexToStandardPattern(string arguments)
        {
            // To kinds of arguments
            // The first is a simple argument such as '1' or 'A'
            // The second is a class argument such as [x, y, z] or [Bob hates Jim, and Jim hates Bob]
            // If we find a second then we don't break up the arguments within that second we pass it as a SINGLE argument, 
            // else we break them into multiple

            // E.G. Example1 ( 1, 2, [x, y, z] ) would return an array of [ 1, 2, [x, y, z] ]

            /*
                What we are saying is:

                [x, y, z] then normal

                so we match [...] + (,[...])* or NOT',' + (,NOT',')*

                [...] | NOT',' then check for (,[...]|NOT',')*
            */

            string mutableArgs = arguments == null ? string.Empty : arguments;
            string constantsPattern = @"(?:\'(.*?)\')";

            // Replace 'x' with constant y where x represents the constant name and y the constant value
            mutableArgs = Regex.Replace(arguments, constantsPattern, GetConstantForMatch, RegexOptions.IgnoreCase);

            string pattern = @"\s*((?:\[.*?\])|(?:[^,]*))\s*";

            MatchCollection result = Regex.Matches(mutableArgs, pattern);

            return result
                .Cast<Match>()
                .Select(m => m.Value.Trim('[', ']', ' '))
                .Where(m => string.IsNullOrEmpty(m) == false)
                .ToArray();
        }

        /// <summary>
        /// Finds any constant matches in the match.
        /// </summary>
        /// <param name="match"> A match from a regex expression. </param>
        /// <returns> The constant value if one exists in the match. </returns>
        protected string GetConstantForMatch(Match match)
        {
            if (match.Groups.Count < 2)
            {
                return string.Empty;
            }

            World world;
            bool worldSuccess = ModUtils.GetCurrentWorld(out world);

            // 0 is the full text 1 is the match text.
            switch (match.Groups[1].Value.ToLower())
            {
                case "center":
                case "centre":
                    if (worldSuccess)
                    {
                        Tile t = world.GetCenterTile();
                        return "[" + t.X + ", " + t.Y + ", " + t.Z + "]";
                    }

                    break;
                case "mousePos":
                    Vector3 mousePos = Input.mousePosition;
                    return "[" + mousePos.x + ", " + mousePos.y + ", " + mousePos.z + "]";
                case "timeScale":
                    return (TimeManager.Instance != null) ? TimeManager.Instance.TimeScale.ToString() : string.Empty;
                case "pi":
                    return Math.PI.ToString();
                case "e":
                    return Math.E.ToString();
                default:
                    DevConsole.LogWarning("You entered an constant identifier that doesn't exist?  Check spelling.");
                    break;
            }

            return string.Empty;
        }

        /// <summary>
        /// Parse the arguments.
        /// </summary>
        /// <param name="arguments"> Arguments to parse.</param>
        /// <returns> The parsed arguments.</returns>
        protected virtual object[] ParseArguments(string args)
        {
            object[] convertedArgs = new object[] { };

            try
            {
                string[] arguments = RegexToStandardPattern(args);
                convertedArgs = new object[TypeInfo.Length];

                // If TypeInfo null then no new parameters to pass (we'll we will pass an array of strings, which could be empty)
                if (TypeInfo == null || TypeInfo.Length == 0)
                {
                    return arguments;
                }

                for (int i = 0; i < TypeInfo.Length; i++)
                {
                    if (arguments.Length > i && arguments[i] != null)
                    {
                        convertedArgs[i] = DevConsole.Parsers[TypeInfo[i]](arguments[i]);
                        if (convertedArgs[i] == null)
                        {
                            string errorMessage = "The entered parameters do not conform to the types (in order): " + this.Parameters;
                            DevConsole.LogError(errorMessage);
                            throw new Exception(errorMessage);
                        }
                    }
                    else
                    {
                        convertedArgs[i] = null;
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