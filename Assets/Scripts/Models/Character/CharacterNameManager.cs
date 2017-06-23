#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software,
// and you are welcome to redistribute it under certain conditions; See
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System.Linq;
using UnityEngine;

namespace ProjectPorcupine.Entities
{
    /// <summary>
    /// Character name manager that holds all the possible names and tries to give a random and unused name everytime is requested.
    /// </summary>
    public class CharacterNameManager
    {
        private static string[] characterNames;
        private static int ptr;

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterNameManager"/> class.
        /// </summary>
        public CharacterNameManager()
        {
            characterNames = new string[0];
        }

        /// <summary>
        /// Load new names to use for Characters.
        /// </summary>
        /// <param name="nameStrings">An array of name strings.</param>
        public static void LoadNames(string[] nameStrings)
        {
            // Randomize the given strings
            // Add all the names to the unused queue in the random order
            characterNames = characterNames.Concat(nameStrings.OrderBy(c => Random.value)).ToArray();
        }

        /// <summary>
        /// Returns a randomly chosen name, prioritizing names which have not been used yet.
        /// </summary>
        /// <returns>A randomly chosen name.</returns>
        public static string GetNewName()
        {
            // If character names doesn't exist then just return null
            if (characterNames == null || characterNames.Length == 0)
            {
                return null;
            }

            // Assign name then iterate pointer, the modulo section bounds it to be within the range of the array
            return characterNames[ptr++ % characterNames.Length];
        }
    }
}
