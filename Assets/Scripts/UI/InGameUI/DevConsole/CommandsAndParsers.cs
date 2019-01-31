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
using UnityEngine;
using UnityEngine.UI;

namespace DeveloperConsole.Core
{
    public static class CommandsAndParsers
    {
        [Command("System", Description = "Logs the current status of the game", Title = "Status")]
        public static void Status()
        {
            // Purely a placeholder, add more content as required.
            DevConsole.Log("Developer Mode is " + (SettingsKeyHolder.DeveloperMode ? "on" : "off"), "yellow");
            DevConsole.Log("Time is " + (TimeManager.Instance.IsPaused ? "paused" : TimeManager.Instance.TimeScale + "x"), "yellow");
        }

        [Command("System", "Console", Description = "Sets the font size, must be between 10 and 20 (inclusive)", DetailedDescription = "This command sets the font size of the console.  It takes a single parameter which is a whole number (integer) and has to be between 10 and 20 (inclusive).  The default is 14.", Title = "SetFontSize")]
        public static void SetFontSize(int size = 14)
        {
            if (size < 10)
            {
                DevConsole.LogError("Font size would be too small");
            }
            else if (size > 20)
            {
                DevConsole.LogError("Font size would be too big");
            }
            else
            {
                Text textObj = DevConsole.TextObject();
                if (textObj != null)
                {
                    textObj.fontSize = size;
                }

                DevConsole.Log("Change successful :D", "green");
            }
        }

        /// <summary>
        /// Exits the console.
        /// </summary>
        [Command("System", "Console", Description = "Exits the console", Title = "Exit")]
        public static void Exit()
        {
            DevConsole.Close();
        }

        /// <summary>
        /// Logs all possible parsers.
        /// </summary>
        [Command("System", "Console", Description = "Logs all the types the devconsole can parse", Title = "GetParsers")]
        public static void GetParsers()
        {
            DevConsole.Log("The system can parse all of the following...", "green");
            foreach (Type possibleType in DevConsole.Parsers.Keys)
            {
                DevConsole.Log("I can parse " + possibleType.Name);
            }
        }

        /// <summary>
        /// Sets the text of the dev console.
        /// </summary>
        /// <param name="text"> The text to set to. </param>
        [Command("System", "Console", Description = "Sets the console text", Title = "SetText")]
        public static void SetText(string text = "", int cat = 2)
        {
            Text textObj = DevConsole.TextObject();

            if (textObj != null)
            {
                textObj.text = "\n" + text;
            }
        }

        /// <summary>
        /// Logs all the tags in a nice format.
        /// </summary>
        [Command("System", Description = "Logs all the tags used currently by the system", Title = "AllTags")]
        public static void AllTags()
        {
            DevConsole.Log("All the tags: ", "green");
            DevConsole.Log(string.Join(", ", DevConsole.CommandArray().SelectMany(x => x.Tags).Select(x => x.Trim()).Distinct().ToArray()));
        }

        /// <summary>
        /// Just returns help dependent on each command.
        /// </summary>
        /// <param name="tag"> The tag to search for.  If "" then do all tags. </param>
        [Command("System", Description = "Returns information on all commands.  Can take in a parameter as a tag to search for all commands with that tag", Title = "Help")]
        public static void Help(string tag = "")
        {
            DevConsole.Log("-- Help --", "green");

            string text = string.Empty;

            CommandBase[] consoleCommands = DevConsole.CommandArray(tag);

            for (int i = 0; i < consoleCommands.Length; i++)
            {
                text += "\n<color=orange>" + consoleCommands[i].Title + DevConsole.GetParameters(consoleCommands[i]) + "</color>" + (consoleCommands[i].Description == null ? string.Empty : " //" + consoleCommands[i].Description);
            }

            DevConsole.Log(text);

            DevConsole.Log("\n<color=orange>Note:</color> If the function has no parameters you <color=red> don't</color> need to use the parameter modifier.");
            DevConsole.Log("<color=orange>Note:</color> You <color=red>don't</color> need to use the trailing parameter modifier either");
            DevConsole.Log("You can use constants to replace common parameters (they are case insensitive but require ' ' around them):");
            DevConsole.Log("- 'Center' (or 'Centre') is the position of the center/centre of the map.");
            DevConsole.Log("- 'MousePos' is the position of the mouse");
            DevConsole.Log("- 'TimeScale' is the current time scale");
            DevConsole.Log("- 'Pi' is 3.141...");
            DevConsole.Log("- 'E' is 2.718...");
        }

        /// <summary>
        /// Clears the text area and history.
        /// </summary>
        [Command("System", "Console", Description = "Clears the developer console", Title = "Clear")]
        public static void Clear()
        {
            DevConsole.ClearHistory();
            Text textObj = DevConsole.TextObject();

            if (textObj != null)
            {
                textObj.text = "\n<color=#7CFC00>Clear Successful :D</color>\n";
            }
        }

        /// <summary>
        /// Parse an integer 16 from arguments given.
        /// </summary>
        /// <param name="args"> The string version of the end result. </param>
        /// <returns> Null if arguments are wrong else the converted arg in object form. </returns>
        [Parser(typeof(short))]
        public static object HandleInt16(string args)
        {
            short outValue;
            return short.TryParse(args, out outValue) ? (object)outValue : null;
        }

        /// <summary>
        /// Parse an integer 32 from arguments given.
        /// </summary>
        /// <param name="args"> The string version of the end result. </param>
        /// <returns> Null if arguments are wrong else the converted arg in object form. </returns>
        [Parser(typeof(int))]
        public static object HandleInt32(string args)
        {
            int outValue;
            return int.TryParse(args, out outValue) ? (object)outValue : null;
        }

        /// <summary>
        /// Parse an integer 64 from arguments given.
        /// </summary>
        /// <param name="args"> The string version of the end result. </param>
        /// <returns> Null if arguments are wrong else the converted arg in object form. </returns>
        [Parser(typeof(long))]
        public static object HandleInt64(string args)
        {
            long outValue;
            return long.TryParse(args, out outValue) ? (object)outValue : null;
        }

        /// <summary>
        /// Parse an unsigned integer 16 from arguments given.
        /// </summary>
        /// <param name="args"> The string version of the end result. </param>
        /// <returns> Null if arguments are wrong else the converted arg in object form. </returns>
        [Parser(typeof(ushort))]
        public static object HandleUInt16(string args)
        {
            ushort outValue;
            return ushort.TryParse(args, out outValue) ? (object)outValue : null;
        }

        /// <summary>
        /// Parse an unsigned integer 32 from arguments given.
        /// </summary>
        /// <param name="args"> The string version of the end result. </param>
        /// <returns> Null if arguments are wrong else the converted arg in object form. </returns>
        [Parser(typeof(uint))]
        public static object HandleUInt32(string args)
        {
            uint outValue;
            return uint.TryParse(args, out outValue) ? (object)outValue : null;
        }

        /// <summary>
        /// Parse an unsigned integer 64 from arguments given.
        /// </summary>
        /// <param name="args"> The string version of the end result. </param>
        /// <returns> Null if arguments are wrong else the converted arg in object form. </returns>
        [Parser(typeof(ulong))]
        public static object HandleUInt64(string args)
        {
            ulong outValue;
            return ulong.TryParse(args, out outValue) ? (object)outValue : null;
        }

        /// <summary>
        /// Parse a float from arguments given.
        /// </summary>
        /// <param name="args"> The string version of the end result. </param>
        /// <returns> Null if arguments are wrong else the converted arg in object form. </returns>
        [Parser(typeof(float))]
        public static object HandleFloat(string args)
        {
            float outValue;
            return float.TryParse(args, out outValue) ? (object)outValue : null;
        }

        /// <summary>
        /// Parse a double from arguments given.
        /// </summary>
        /// <param name="args"> The string version of the end result. </param>
        /// <returns> Null if arguments are wrong else the converted arg in object form. </returns>
        [Parser(typeof(double))]
        public static object HandleDouble(string args)
        {
            double outValue;
            return double.TryParse(args, out outValue) ? (object)outValue : null;
        }

        /// <summary>
        /// Parse a decimal from arguments given.
        /// </summary>
        /// <param name="args"> The string version of the end result. </param>
        /// <returns> Null if arguments are wrong else the converted arg in object form. </returns>
        [Parser(typeof(decimal))]
        public static object HandleDecimal(string args)
        {
            decimal outValue;
            return decimal.TryParse(args, out outValue) ? (object)outValue : null;
        }

        /// <summary>
        /// Parse a boolean from arguments given.
        /// </summary>
        /// <param name="args"> The string version of the end result. </param>
        /// <returns> Null if arguments are wrong else the converted arg in object form. </returns>
        [Parser(typeof(bool))]
        public static object HandleBoolean(string args)
        {
            bool outValue;
            return bool.TryParse(args, out outValue) ? (object)outValue : null;
        }

        /// <summary>
        /// Parse a string from arguments given.
        /// </summary>
        /// <param name="args"> The string version of the end result. </param>
        /// <returns> Null if arguments are wrong else the converted arg in object form. </returns>
        [Parser(typeof(string))]
        public static object HandleString(string args)
        {
            return args;
        }

        /// <summary>
        /// Parse an float from arguments given.
        /// </summary>
        /// <param name="args"> The string version of the end result. </param>
        /// <returns> Null if arguments are wrong else the converted arg in object form. </returns>
        [Parser(typeof(char))]
        public static object HandleChar(string args)
        {
            return args.Length >= 1 ? (object)args.ToCharArray()[0] : null;
        }

        /// <summary>
        /// Parse a vector2 from arguments given.
        /// </summary>
        /// <param name="args"> The string version of the end result. </param>
        /// <returns> Null if arguments are wrong else the converted arg in object form. </returns>
        [Parser(typeof(Vector2))]
        public static object HandleVector2(string args)
        {
            string[] coordinates = args.Split(',');
            float x, y;
            return coordinates.Length >= 2 && float.TryParse(coordinates[0], out x) && float.TryParse(coordinates[1], out y) ? (object)new Vector2(x, y) : null;
        }

        /// <summary>
        /// Parse a vector3 from arguments given.
        /// </summary>
        /// <param name="args"> The string version of the end result. </param>
        /// <returns> Null if arguments are wrong else the converted arg in object form. </returns>
        [Parser(typeof(Vector3))]
        public static object HandleVector3(string args)
        {
            string[] coordinates = args.Split(',');
            float x, y, z;
            return coordinates.Length >= 3 && float.TryParse(coordinates[0], out x) && float.TryParse(coordinates[1], out y) && float.TryParse(coordinates[2], out z) ? (object)new Vector3(x, y, z) : null;
        }

        /// <summary>
        /// Parse a vector4 from arguments given.
        /// </summary>
        /// <param name="args"> The string version of the end result. </param>
        /// <returns> Null if arguments are wrong else the converted arg in object form. </returns>
        [Parser(typeof(Vector4))]
        public static object HandleVector4(string args)
        {
            string[] coordinates = args.Split(',');
            float x, y, z, w;
            return coordinates.Length >= 4 && float.TryParse(coordinates[0], out x) && float.TryParse(coordinates[1], out y) && float.TryParse(coordinates[2], out z) && float.TryParse(coordinates[3], out w) ? (object)new Vector4(x, y, z, w) : null;
        }
    }
}
