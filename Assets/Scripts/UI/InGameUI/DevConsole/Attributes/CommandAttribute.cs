#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System;

namespace DeveloperConsole.Core
{
    /// <summary>
    /// An attribute to wrap up a command through reflection.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        /// <summary>
        /// The description of what the command does.
        /// </summary>
        public string description;

        /// <summary>
        /// The detailed description of what the command does.
        /// </summary>
        public string detailedDescription;

        /// <summary>
        /// The title of the command, used for calling.
        /// </summary>
        public string title;

        /// <summary>
        /// Create a command attribute.
        /// </summary>
        /// <param name="tags"> With tags... </param>
        public CommandAttribute(params string[] tags)
        {
            this.Tags = tags;
        }

        /// <summary>
        /// The tags of this command, 
        /// you can perform help actions on a specific tag.
        /// </summary>
        public string[] Tags
        {
            get; protected set;
        }
    }
}
