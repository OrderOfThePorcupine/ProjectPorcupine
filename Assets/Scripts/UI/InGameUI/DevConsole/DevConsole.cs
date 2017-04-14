﻿#region License
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
using DeveloperConsole.Core;
using MoonSharp.Interpreter;
using UnityEngine;
using UnityEngine.UI;

namespace DeveloperConsole
{
    /// <summary>
    /// A delegate for the parse operation.
    /// If object is null it failed.
    /// </summary>
    /// <param name="args"> Arguments passed in. </param>
    /// <returns> The parsed value of the arguments.  Null if arguments are wrong. </returns>
    public delegate object ParseDelegate(string args);

    [MoonSharpUserData]
    public class DevConsole : MonoBehaviour
    {
        /// <summary>
        /// Max lines before cleaning.
        /// </summary>
        private const int AutoclearThreshold = 18000;

        /// <summary>
        /// Singleton patterned instnace.
        /// Private since there are static functions only for controlled access.
        /// </summary>
        private static DevConsole instance;

        /// <summary>
        /// The last inputed text.
        /// </summary>
        private string inputText = string.Empty;

        /// <summary>
        /// Whole list of commands available.
        /// </summary>
        private List<CommandBase> consoleCommands = new List<CommandBase>();

        /// <summary>
        /// History of commands.
        /// </summary>
        private List<string> history = new List<string>();

        /// <summary>
        /// What index the history is at.
        /// </summary>
        private int currentHistoryIndex = -1;

        /// <summary>
        /// Is the command console closed.
        /// </summary>
        private bool closed = true;

        /// <summary>
        /// Possible options that the user could be trying to type.
        /// </summary>
        private List<string> possibleCandidates = new List<string>();

        /// <summary>
        /// Index of the option chosen.
        /// </summary>
        private int selectedCandidate = -1;

        /// <summary>
        /// The console text logger.
        /// </summary>
        [SerializeField]
        private Text textArea;

        /// <summary>
        /// The input field for console commands.
        /// </summary>
        [SerializeField]
        private InputField inputField;

        /// <summary>
        /// The autocomplete object.
        /// </summary>
        [SerializeField]
        private ScrollRect autoComplete;

        /// <summary>
        /// Holds possible candidates.
        /// </summary>
        [SerializeField]
        private GameObject contentAutoComplete;

        /// <summary>
        /// The scrolling rect for the main console logger.
        /// </summary>
        [SerializeField]
        private ScrollRect scrollRect;

        /// <summary>
        /// The root object to disable/enable for '\' or whatever consoleActivationKey is.
        /// </summary>
        [SerializeField]
        private GameObject root;

        /// <summary>
        /// All the parsers available.
        /// </summary>
        public static Dictionary<Type, ParseDelegate> Parsers { get; private set; }

        /// <summary>
        /// Is the command line open.
        /// </summary>
        private bool Opened
        {
            get
            {
                return !closed;
            }

            set
            {
                closed = !value;
            }
        }

        /// <summary>
        /// Is autocomplete currently showing.
        /// </summary>
        private bool ShowingAutoComplete
        {
            get
            {
                return autoComplete.gameObject.activeInHierarchy;
            }
        }

        /// <summary>
        /// Opens the console.
        /// </summary>
        public static void Open()
        {
            if (instance == null || instance.Opened)
            {
                return;
            }

            if (SettingsKeyHolder.EnableDevConsole == false)
            {
                Close();
                return;
            }

            instance.Opened = true;
            instance.root.SetActive(true);
            instance.inputField.ActivateInputField();
        }

        /// <summary>
        /// Closes the console.
        /// </summary>
        public static void Close()
        {
            if (instance == null || instance.closed)
            {
                return;
            }

            instance.closed = true;
            instance.inputText = string.Empty;
            instance.root.SetActive(false);
        }

        #region LogManagement

        /// <summary>
        /// Logs to the console.
        /// </summary>
        /// <param name="obj"> Must be convertable to string.</param>
        public static void Log(object obj)
        {
            if (instance == null)
            {
                return;
            }

            BasePrint(obj.ToString());
        }

        /// <summary>
        /// Logs to the console.
        /// </summary>
        /// <param name="obj"> Must be convertable to string.</param>
        /// <param name="color"> Can either be the name of one the common names or can be in HTML format.</param>
        public static void Log(object obj, string color)
        {
            if (instance == null)
            {
                return;
            }

            BasePrint("<color=" + (color.ToLower() != "green" ? color : "#7CFC00") + ">" + obj.ToString() + "</color>");
        }

        /// <summary>
        /// Logs to the console with color yellow.
        /// </summary>
        /// <param name="obj"> Must be convertable to string.</param>
        public static void LogWarning(object obj)
        {
            if (instance == null)
            {
                return;
            }

            BasePrint("<color=yellow>" + obj.ToString() + "</color>");
        }

        /// <summary>
        /// Logs to the console with color red.
        /// </summary>
        /// <param name="obj"> Must be convertable to string.</param>
        public static void LogError(object obj)
        {
            if (instance == null)
            {
                return;
            }

            BasePrint("<color=red>" + obj.ToString() + "</color>");
        }

        /// <summary>
        /// Logs to the console.
        /// </summary>
        /// <param name="text"> Text to print.</param>
        public static void BasePrint(string text)
        {
            if (instance == null)
            {
                return;
            }

            instance.textArea.text += text + (SettingsKeyHolder.TimeStamps ? "\t[" + System.DateTime.Now.ToShortTimeString() + "]" : string.Empty) + "\n";

            // Clear if limit exceeded
            if (instance.textArea.cachedTextGenerator.characterCount >= AutoclearThreshold || instance.textArea.cachedTextGenerator.vertexCount > 55000)
            {
                instance.textArea.text = "\nAUTO-CLEAR\n";
            }

            // Update scroll bar
            Canvas.ForceUpdateCanvases();
            instance.scrollRect.verticalNormalizedPosition = 0;
            Canvas.ForceUpdateCanvases();
        }

        #endregion

        /// <summary>
        /// Executes the command passed.
        /// </summary>
        public static void Execute(string command)
        {
            // Guard
            if (instance == null || command.Trim() == string.Empty)
            {
                return;
            }

            // Get method and arguments

            // We want to ONLY split once!
            string[] methodNameAndArgs = command.Trim().TrimEnd(')').Split(new char[] { '(' }, 2);
            string method = command;
            string args = string.Empty;

            IEnumerable<CommandBase> commandsToCall;

            if (methodNameAndArgs.Length == 2)
            {
                // If both method and arguments exist split them
                method = methodNameAndArgs[0];
                args = methodNameAndArgs[1];
            }
            else if (method.Trim().EndsWith("?"))
            {
                // This is help
                string testString = method.ToLower().Trim().TrimEnd('?');
                commandsToCall = instance.consoleCommands.Where(cB => cB.Title.ToLower() == testString);

                foreach (CommandBase commandToCall in commandsToCall)
                {
                    ShowHelpMethod(commandToCall);
                }

                return;
            }
            else
            {
                // Do a default
                // We will do a null checker
                // If its null then set to default which is "" by default :P
                args = null;
            }

            // Execute command
            commandsToCall = instance.consoleCommands.Where(cB => cB.Title.ToLower() == method.ToLower().Trim());

            if (commandsToCall.Count() > 0)
            {
                foreach (CommandBase commandToCall in commandsToCall)
                {
                    if (commandToCall != null)
                    {
                        if (commandToCall.Parameters == null || commandToCall.Parameters == string.Empty)
                        {
                            args = string.Empty;
                        }
                        else
                        {
                            if (args == null && commandToCall.DefaultValue != null)
                            {
                                args = commandToCall.DefaultValue;
                            }

                            // They really need a better literal system...
                            // This is the closet we can get basically
                            if (string.IsNullOrEmpty(args) || args == '"'.ToString())
                            {
                                args = @"""";
                            }
                        }

                        commandToCall.ExecuteCommand(args);
                    }
                }
            }
            else
            {
                // User entered a command that doesn't 'exist'
                LogWarning("Command doesn't exist?  You entered: " + command);

                LogWarning("Did you mean?");
                IEnumerable<CommandBase> commandsToShow = instance.consoleCommands.Where(cB => cB.Title.ToLower().Contains(method.Substring(0, (method.Length >= 3) ? (int)Mathf.Ceil(method.Length / 1.5f) : method.Length).ToLower()));

                if (commandsToShow.Count() == 0)
                {
                    LogWarning("No close matches found so looking with less precision");
                    commandsToShow = instance.consoleCommands.Where(cB => cB.Title.ToLower().Contains(method.Substring(0, (int)Mathf.Ceil(method.Length / 3f)).ToLower()));
                }

                foreach (CommandBase commandToShow in commandsToShow)
                {
                    Log(commandToShow.Title, "green");
                }
            }
        }

        /// <summary>
        /// Runs the help method of the passed interface.
        /// </summary>
        public static void ShowHelpMethod(CommandBase help)
        {
            if (string.IsNullOrEmpty(help.DetailedDescriptiveText) && string.IsNullOrEmpty(help.DescriptiveText))
            {
                DevConsole.Log("<color=yellow>Command Info:</color> <color=red>There's no help for this command</color>");
                return;
            }

            Log("<color=yellow>Command Info:</color> " + (string.IsNullOrEmpty(help.DetailedDescriptiveText) ? help.DescriptiveText : help.DetailedDescriptiveText));
            Log("<color=yellow>Call it like </color><color=orange> " + help.Title + GetParameters(help) + "</color>");
        }

        /// <summary>
        /// Adds the given commands.
        /// </summary>
        public static void AddCommands(IEnumerable<CommandBase> commands)
        {
            // Guard
            if (instance == null)
            {
                return;
            }

            instance.consoleCommands.AddRange(commands);
        }

        /// <summary>
        /// Adds the given commands.
        /// </summary>
        public static void AddCommands(params CommandBase[] commands)
        {
            // Guard
            if (instance == null)
            {
                return;
            }

            instance.consoleCommands.AddRange(commands);
        }

        /// <summary>
        /// Adds the specified command.
        /// </summary>
        public static void AddCommand(CommandBase command)
        {
            // Guard
            if (instance == null)
            {
                return;
            }

            instance.consoleCommands.Add(command);
        }

        /// <summary>
        /// Returns true if the command exists.
        /// </summary>
        public static bool CommandExists(string commandTitle)
        {
            // Guard
            if (instance == null)
            {
                return false;
            }

            // Could just stop at first, but its a lazy search so its pretty darn fast anyways
            return (instance.consoleCommands.Count(cB => cB.Title.ToLower() == commandTitle.ToLower()) > 0) ? true : false;
        }

        /// <summary>
        /// Removes the commmand from the list of commands.
        /// </summary>
        public static void RemoveCommand(string commandTitle)
        {
            // Guard
            if (instance == null)
            {
                return;
            }

            int oldCount = instance.consoleCommands.Count;

            // Safe delete
            instance.consoleCommands.RemoveAll(cB => cB.Title.ToLower() == commandTitle.ToLower());

            Log("Deleted " + (instance.consoleCommands.Count - oldCount).ToString() + " commands");
        }

        /// <summary>
        /// Returns an IEnumerator that allows iteration consisting of all the commands.
        /// </summary>
        public static IEnumerator<CommandBase> CommandIterator(string withTag = "")
        {
            if (instance == null)
            {
                return new List<CommandBase>.Enumerator();
            }

            if (withTag != string.Empty)
            {
                return instance.consoleCommands.Where(x => x.Tags != null && x.Tags.Count() > 0 && x.Tags.Contains(withTag, System.StringComparer.OrdinalIgnoreCase)).GetEnumerator();
            }
            else
            {
                return instance.consoleCommands.GetEnumerator();
            }
        }

        /// <summary>
        /// Returns an array of all the commands.
        /// </summary>
        public static CommandBase[] CommandArray(string withTag = "")
        {
            if (instance == null)
            {
                return new CommandBase[] { };
            }

            if (withTag != string.Empty)
            {
                return instance.consoleCommands.Where(x => x.Tags != null && x.Tags.Count() > 0 && x.Tags.Contains(withTag, System.StringComparer.OrdinalIgnoreCase)).ToArray();
            }
            else
            {
                return instance.consoleCommands.ToArray();
            }
        }

        /// <summary>
        /// Adds the specified command to the history.
        /// </summary>
        public static void AddHistory(string command)
        {
            if (instance == null)
            {
                return;
            }

            instance.history.Add(command);
        }

        /// <summary>
        /// Removes specified command from the history.
        /// </summary>
        public static void RemoveHistory(string command)
        {
            if (instance == null || instance.history.Contains(command) == false)
            {
                return;
            }

            instance.history.Remove(command);
        }

        /// <summary>
        /// Clears the command history.
        /// </summary>
        public static void ClearHistory()
        {
            if (instance == null)
            {
                return;
            }

            instance.history.Clear();
        }

        /// <summary>
        /// Returns a parameter list using the correct command mode.
        /// </summary>
        /// <param name="description"> The description of the command.</param>
        public static string GetParameters(CommandBase description)
        {
            if (instance == null)
            {
                return string.Empty;
            }

            return " (" + description.Parameters + ")";
        }

        /// <summary>
        /// Passes the Text Object.
        /// </summary>
        public static Text TextObject()
        {
            if (instance == null)
            {
                return null;
            }

            return instance.textArea;
        }

        /// <summary>
        /// Refreshes variables based on the settings.
        /// </summary>
        public static void DirtySettings()
        {
            if (instance == null)
            {
                return;
            }

            instance.textArea.fontSize = SettingsKeyHolder.FontSize;
            instance.scrollRect.scrollSensitivity = SettingsKeyHolder.ScrollSensitivity;
        }

        /// <summary>
        /// Add a parser through reflection of method info.
        /// </summary>
        /// <param name="target"> The target type of the parser. </param>
        /// <param name="methodInfo"> The reflected method that takes in a string and returns an object. </param>
        public static void AddParser(Type target, MethodInfo methodInfo)
        {
            Parsers.Add(target, (ParseDelegate)Delegate.CreateDelegate(typeof(ParseDelegate), methodInfo));
        }

        /// <summary>
        /// Add a parser through a parse delegate.
        /// </summary>
        /// <param name="target"> The target type of the parser. </param>
        /// <param name="method"> A function that takes in a string and returns an object. </param>
        public static void AddParser(Type target, ParseDelegate method)
        {
            Parsers.Add(target, method);
        }

        /// <summary>
        /// Add a parser through a function.
        /// </summary>
        /// <param name="target"> The target type of the parser. </param>
        /// <param name="method"> A function that takes in a string and returns an object. </param>
        public static void AddParser(Type target, Func<string, object> method)
        {
            Parsers.Add(target, new ParseDelegate(method));
        }

        #region AttributeImplementations

        /// <summary>
        /// Logs all the tags in a nice format.
        /// </summary>
        [Command("System", description = "Logs all the tags used currently by the system", title = "AllTags")]
        public static void AllTags()
        {
            Log("All the tags: ", "green");
            Log(string.Join(", ", CommandArray().SelectMany(x => x.Tags).Select(x => x.Trim()).Distinct().ToArray()));
        }

        /// <summary>
        /// Just returns help dependent on each command.
        /// </summary>
        /// <param name="tag"> The tag to search for.  If "" then do all tags. </param>
        [Command("System", description = "Returns information on all commands.  Can take in a parameter as a tag to search for all commands with that tag", title = "Help")]
        public static void Help(string tag = "")
        {
            Log("-- Help --", "green");

            string text = string.Empty;

            CommandBase[] consoleCommands = CommandArray(tag);

            for (int i = 0; i < consoleCommands.Length; i++)
            {
                text += "\n<color=orange>" + consoleCommands[i].Title + GetParameters(consoleCommands[i]) + "</color>" + (consoleCommands[i].DescriptiveText == null ? string.Empty : " //" + consoleCommands[i].DescriptiveText);
            }

            Log(text);

            Log("\n<color=orange>Note:</color> If the function has no parameters you <color=red> don't</color> need to use the parameter modifier.");
            Log("<color=orange>Note:</color> You <color=red>don't</color> need to use the trailing parameter modifier either");
            Log("You can use constants to replace common parameters (they are case insensitive but require ' ' around them):");
            Log("- 'Center' (or 'Centre') is the position of the center/centre of the map.");
            Log("- 'MousePos' is the position of the mouse");
            Log("- 'TimeScale' is the current time scale");
            Log("- 'Pi' is 3.141...");
            Log("- 'E' is 2.718...");
        }

        /// <summary>
        /// Clears the text area and history.
        /// </summary>
        [Command("System", description = "Clears the developer console", title = "Clear")]
        public static void Clear()
        {
            ClearHistory();
            Text textObj = TextObject();

            if (textObj != null)
            {
                TextObject().text = "\n<color=#7CFC00>Clear Successful :D</color>\n";
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

        #endregion

        /// <summary>
        /// Button delegate action to handle command.
        /// </summary>
        public void EnterPressedForInput(Text newValue)
        {
            inputField.ActivateInputField();

            currentHistoryIndex = -1;

            // Clear input but retrieve
            inputText = newValue.text;
            inputField.text = string.Empty;

            if (inputText != string.Empty)
            {
                // Add Text to log, history then execute
                Log(inputText);
                history.Add(inputText);
                Execute(inputText);
            }
        }

        /// <summary>
        /// Regenerates the autocomplete (button delegate for text field changed).
        /// </summary>
        public void RegenerateAutoComplete(Text newValue)
        {
            inputText = newValue.text;

            if (inputText == string.Empty || consoleCommands == null || consoleCommands.Count == 0)
            {
                return;
            }

            string candidateTester = string.Empty;

            if (possibleCandidates.Count != 0 && selectedCandidate >= 0 && possibleCandidates.Count > selectedCandidate)
            {
                // Set our candidates tester because we had a candidate before and we want it to carry over
                candidateTester = possibleCandidates[selectedCandidate];
            }

            List<string> possible = consoleCommands
                .Where(cB => cB.Title.ToLower().StartsWith(inputText.ToLower()))
                .Select(cB => cB.Title).ToList();

            if (possible != null)
            {
                possibleCandidates = possible;
            }

            if (candidateTester == string.Empty)
            {
                selectedCandidate = 0;
                return;
            }

            for (int i = 0; i < possibleCandidates.Count; i++)
            {
                if (possibleCandidates[i] == candidateTester)
                {
                    selectedCandidate = i;
                    DirtyAutocomplete();
                    return;
                }
            }

            if (possibleCandidates.Count == 0)
            {
                selectedCandidate = -1;
                autoComplete.gameObject.SetActive(false);
            }
            else
            {
                selectedCandidate = 0;
            }

            DirtyAutocomplete();
        }

        /// <summary>
        /// Raises the enable event.  It just does some instance checking.
        /// </summary>
        private void OnEnable()
        {
            // Guard
            if (instance != this && instance != null)
            {
                // Destroy instance.
                UnityDebugger.Debugger.LogError("DevConsole", "There can only be one Console per project.  Deleting instance with name: " + instance.gameObject.name);
                Destroy(instance.gameObject);
            }

            // Saves a small amount of extra calls and what not
            if (instance != this)
            {
                instance = this;

                KeyboardManager.Instance.RegisterModalInputField(inputField);
                KeyboardManager.Instance.RegisterInputAction("DevConsole", KeyboardMappedInputType.KeyUp, ToggleConsole);
            }

            if (SettingsKeyHolder.EnableDevConsole == false || closed)
            {
                Close();
            }
        }

        /// <summary>
        /// Set transform and do guard.
        /// </summary>
        private void Start()
        {
            if (Parsers == null)
            {
                Parsers = new Dictionary<Type, ParseDelegate>();
            }

            transform.SetAsLastSibling();

            // Guard
            if (textArea == null || inputField == null || autoComplete == null || scrollRect == null || root == null)
            {
                gameObject.SetActive(false);
                UnityDebugger.Debugger.LogError("DevConsole", "Missing gameobjects, look at the serializable fields");
            }

            textArea.fontSize = SettingsKeyHolder.FontSize;
            textArea.text = "\n";

            AddParsersByReflection();

            // Load all the commands
            LoadCommands();

            if (root != null)
            {
                Opened = root.activeInHierarchy;

                if (Opened)
                {
                    inputField.ActivateInputField();
                }
            }
        }

        /// <summary>
        /// Toggles the console on/off.
        /// </summary>
        private void ToggleConsole()
        {
            if (closed)
            {
                Open();
            }
            else
            {
                Close();
            }
        }

        /// <summary>
        /// Late Update, just does some key checking for history / AutoComplete.
        /// </summary>
        private void Update()
        {
            if (!inputField.isFocused)
            {
                currentHistoryIndex = -1;
                selectedCandidate = -1;
                autoComplete.gameObject.SetActive(false);
                return;
            }

            KeyboardManager.Instance.TriggerActionIfValid("DevConsole");

            // If input field is focused
            if (Input.GetKeyUp(KeyCode.UpArrow))
            {
                if (ShowingAutoComplete)
                {
                    // Render autocomplete
                    if (selectedCandidate > 0)
                    {
                        selectedCandidate -= 1;

                        // Re-select
                        DirtyAutocomplete();
                    }
                }
                else if (currentHistoryIndex < (history.Count - 1))
                {
                    // (history.Count -1) is the max index

                    // This way the first index will be the last one so first in last out
                    // Kind of like a stack but you can go up and down it
                    currentHistoryIndex += 1;
                    inputField.text = history[(history.Count - 1) - currentHistoryIndex];
                }

                inputField.MoveTextEnd(false);
            }
            else if (Input.GetKeyUp(KeyCode.DownArrow))
            {
                if (ShowingAutoComplete)
                {
                    // Render autocomplete
                    if (selectedCandidate < (possibleCandidates.Count - 1))
                    {
                        selectedCandidate += 1;

                        // Re-select
                        DirtyAutocomplete();
                    }
                }
                else if (currentHistoryIndex > 0)
                {
                    // This way the first index will be the last one so first in last out
                    // Kind of like a stack but you can go up and down it
                    currentHistoryIndex -= 1;
                    inputField.text = history[history.Count - (currentHistoryIndex + 1)];
                }

                inputField.MoveTextEnd(false);
            }
            else if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Escape))
            {
                selectedCandidate = -1;
                autoComplete.gameObject.SetActive(false);
            }
            else if (Input.GetKeyUp(KeyCode.Tab))
            {
                if (possibleCandidates.Count == 0)
                {
                    selectedCandidate = -1;
                    autoComplete.gameObject.SetActive(false);
                    inputField.MoveTextEnd(false);
                    return;
                }

                // Handle autocomplete
                if (ShowingAutoComplete)
                {
                    inputField.text = possibleCandidates[selectedCandidate];
                    selectedCandidate = -1;
                    autoComplete.gameObject.SetActive(false);
                    inputField.MoveTextEnd(false);
                }
                else
                {
                    inputField.text = inputField.text.TrimEnd('\t');
                    autoComplete.gameObject.SetActive(true);
                    selectedCandidate = 0;
                    RegenerateAutoComplete(inputField.textComponent);
                }
            }
        }

        /// <summary>
        /// Re-render autocomplete.
        /// </summary>
        private void DirtyAutocomplete()
        {
            if (selectedCandidate != -1)
            {
                autoComplete.gameObject.SetActive(true);

                // Delete current children
                foreach (Transform child in contentAutoComplete.transform)
                {
                    Destroy(child.gameObject);
                }

                // Recreate from possible candidates
                for (int i = 0; i < possibleCandidates.Count; i++)
                {
                    GameObject go = Instantiate(Resources.Load<GameObject>("UI/Console/DevConsole_AutoCompleteOption"));
                    go.transform.SetParent(contentAutoComplete.transform);

                    // Quick way to add component / text
                    if (i != selectedCandidate)
                    {
                        go.GetComponent<Text>().text = "<color=white>" + possibleCandidates[i] + "</color>";
                    }
                    else
                    {
                        go.GetComponent<Text>().text = "<color=yellow>" + possibleCandidates[i] + "</color>";
                    }
                }
            }
            else
            {
                autoComplete.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Adds all parsers that implement <see cref="ParserAttribute"/>.
        /// </summary>
        private void AddParsersByReflection()
        {
            // Find each method with the command attribute then add it to the console commands
            foreach (MethodInfo method in Assembly.GetCallingAssembly().GetTypes().SelectMany(x => x.GetMethods().Where(y => y.GetCustomAttributes(typeof(ParserAttribute), false).Count() > 0)))
            {
                ParserAttribute attribute = (ParserAttribute)method.GetCustomAttributes(typeof(ParserAttribute), false).First();
                AddParser(attribute.Target, method);
            }
        }

        /// <summary>
        /// Adds all commands that implement <see cref="CommandAttribute"/>.
        /// </summary>
        private void AddCommandsByReflection()
        {
            // Find each method with the command attribute then add it to the console commands
            foreach (MethodInfo method in Assembly.GetCallingAssembly().GetTypes().SelectMany(x => x.GetMethods().Where(y => y.GetCustomAttributes(typeof(CommandAttribute), false).Count() > 0)))
            {
                CommandAttribute attribute = (CommandAttribute)method.GetCustomAttributes(typeof(CommandAttribute), false).First();
                string parameters = string.Join(",", method.GetParameters().Select(x => x.ParameterType.Name + " " + x.Name).ToArray());
                consoleCommands.Add(new InternalCommand(attribute.title, method, attribute.description, method.GetParameters().Select(x => x.ParameterType).ToArray(), parameters, attribute.detailedDescription, attribute.Tags));
            }
        }

        /// <summary>
        /// Clears commands and re-loads them.
        /// </summary>
        private void LoadCommands()
        {
            consoleCommands.Clear();

            AddCommandsByReflection();

            // Load Commands from XML (will be changed to JSON AFTER the current upgrade)
            // Covers both CSharp and LUA
            AddCommands(PrototypeManager.DevConsole.Values.Select(x => x.ConsoleCommand).ToArray());
        }
    }
}
