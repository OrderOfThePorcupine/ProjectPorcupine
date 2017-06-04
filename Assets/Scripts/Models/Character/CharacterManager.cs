#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software,
// and you are welcome to redistribute it under certain conditions; See
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MoonSharp.Interpreter;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ProjectPorcupine.Entities
{
    /// <summary>
    /// Character manager that holds all the characters.
    /// </summary>
    [MoonSharpUserData]
    public class CharacterManager : IEnumerable<Character>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterManager"/> class.
        /// </summary>
        public CharacterManager()
        {
            Characters = new List<Character>();
        }

        /// <summary>
        /// Occurs when a character is created.
        /// </summary>
        public event Action<Character> Created;

        public List<Character> Characters { get; private set; }

        /// <summary>
        /// Create a Character in the specified tile.
        /// </summary>
        /// <param name="tile">The tile where the Character is placed.</param>
        /// <param name="name">The name of the character, will get a new one if it is null.</param>
        public Character Create(Tile tile, string name = null)
        {
            return Create(tile, ColorUtilities.RandomColor(), ColorUtilities.RandomGrayColor(), ColorUtilities.RandomSkinColor(), name != null ? name : CharacterNameManager.GetNewName());
        }

        /// <summary>
        /// Create a Character in the specified tile, with the specified color, uniform color and skin color.
        /// </summary>
        /// <param name="tile">The tile where the Character is placed.</param>
        /// <param name="color">The uniform strip color.</param>
        /// <param name="uniformColor">The uniform color.</param>
        /// <param name="skinColor">The skin color.</param>
        /// <param name="name">The name of the character, will get a new one if it is null.</param>
        public Character Create(Tile tile, Color color, Color uniformColor, Color skinColor, string name = null)
        {
            Character character = new Character(tile, color, uniformColor, skinColor, name != null ? name : CharacterNameManager.GetNewName());
            Characters.Add(character);
            TimeManager.Instance.RegisterFastUpdate(character);

            if (Created != null)
            {
                Created(character);
            }

            return character;
        }

        /// <summary>
        /// A function to return all characters that match the given name.
        /// </summary>
        /// <param name="name">The name of the character.</param>
        /// <returns>The character with that name.</returns>
        public IEnumerable<Character> GetAllFromName(string name)
        {
            return Characters.Where(x => x.Name == name);
        }

        /// <summary>
        /// Returns the character with the ID wanted.
        /// </summary>
        /// <param name="ID"> ID of the character. </param>
        /// <returns> The character or null if no character has ID supplied. </returns>
        public Character GetFromID(int ID)
        {
            return Characters.FirstOrDefault(x => x.ID == ID);
        }

        /// <summary>
        /// Gets the characters enumerator.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator GetEnumerator()
        {
            return Characters.GetEnumerator();
        }

        /// <summary>
        /// Gets each character.
        /// </summary>
        /// <returns>Each character.</returns>
        IEnumerator<Character> IEnumerable<Character>.GetEnumerator()
        {
            foreach (Character character in Characters)
            {
                yield return character;
            }
        }

        public JToken ToJson()
        {
            JArray charactersJson = new JArray();
            foreach (Character character in Characters)
            {
                charactersJson.Add(character.ToJSON());
            }

            return charactersJson;
        }

        public void FromJson(JToken charactersToken)
        {
            if (charactersToken == null)
            {
                return;
            }

            JArray charactersJArray = (JArray)charactersToken;

            foreach (JToken characterToken in charactersJArray)
            {
                Character character;
                int x = (int)characterToken["X"];
                int y = (int)characterToken["Y"];
                int z = (int)characterToken["Z"];
                if (characterToken["Colors"] != null)
                {
                    JToken colorToken = characterToken["Colors"];
                    Color color = ColorUtilities.ParseColorFromString((string)colorToken["CharacterColor"][0], (string)colorToken["CharacterColor"][1], (string)colorToken["CharacterColor"][2]);
                    Color colorUni = ColorUtilities.ParseColorFromString((string)colorToken["UniformColor"][0], (string)colorToken["UniformColor"][1], (string)colorToken["UniformColor"][2]);
                    Color colorSkin = ColorUtilities.ParseColorFromString((string)colorToken["SkinColor"][0], (string)colorToken["SkinColor"][1], (string)colorToken["SkinColor"][2]);
                    character = Create(World.Current.GetTileAt(x, y, z), color, colorUni, colorSkin, (string)characterToken["Name"]);
                }
                else
                {
                    character = Create(World.Current.GetTileAt(x, y, z), (string)characterToken["Name"]);
                }

                if (characterToken["Inventories"] != null)
                {
                    foreach (JToken inventoryToken in characterToken["Inventories"])
                    {
                        Inventory inventory = new Inventory();
                        inventory.FromJson(inventoryToken);
                        World.Current.InventoryManager.PlaceInventory(character, inventory);
                    }
                }

                if (characterToken["Stats"] != null)
                {
                    foreach (string stat in character.Stats.Keys)
                    {
                        if (characterToken["Stats"][stat] != null)
                        {
                            character.Stats[stat].Value = (int)characterToken["Stats"][stat];
                        }
                    }
                }

                if (characterToken["Needs"] != null)
                {
                    foreach (Need need in character.Needs)
                    {
                        if (characterToken["Needs"][need.Type] != null)
                        {
                            need.Amount = (int)characterToken["Needs"][need.Type];
                        }
                    }
                }
            }
        }
    }
}
