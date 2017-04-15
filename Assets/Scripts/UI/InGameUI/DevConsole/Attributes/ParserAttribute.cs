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
    public class ParserAttribute : Attribute
    {
        /// <summary>
        /// Create a parser attribute.
        /// </summary>
        /// <param name="target"> With target type. </param>
        public ParserAttribute(Type target)
        {
            this.Target = target;
        }

        /// <summary>
        /// The target that this parser is converting to.
        /// </summary>
        public Type Target
        {
            get; protected set;
        }
    }
}
