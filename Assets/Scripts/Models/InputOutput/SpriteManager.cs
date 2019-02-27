#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// Sprite Manager isn't responsible for actually creating GameObjects.
/// That is going to be the job of the individual ________SpriteController scripts.
/// Our job is simply to load all sprites from disk and keep the organized.
/// </summary>
public static class SpriteManager
{
    private static Texture2D noResourceTexture;
    
    private static Dictionary<string, Dictionary<string, Sprite>> sprites;

    private static bool isInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpriteManager"/> class.
    /// </summary>
    public static void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        sprites = new Dictionary<string, Dictionary<string, Sprite>>();

        CreateEmptyTexture();

        isInitialized = true;
    }

    /// <summary>
    /// Creates a sprite with an error texture.
    /// </summary>
    /// <returns>The error sprite.</returns>
    public static Sprite CreateErrorSprite()
    {
        return Sprite.Create(noResourceTexture, new Rect(Vector2.zero, new Vector3(32, 32)), new Vector2(0.5f, 0.5f), 32);
    }

    /// <summary>
    /// Gets the sprite for the given category and name.
    /// </summary>
    /// <returns>The sprite.</returns>
    /// <param name="categoryName">Category name.</param>
    /// <param name="spriteName">Sprite name.</param>
    public static Sprite GetSprite(string categoryName, string spriteName)
    {
        Dictionary<string, Sprite> categorySprites;
        Sprite sprite;
        if (sprites.TryGetValue(categoryName, out categorySprites))
        {
            if (categorySprites.TryGetValue(spriteName, out sprite))
            {
                return sprite;
            }
        }
        
        // Return a pink square as a error indication
        UnityDebugger.Debugger.LogWarningFormat("SpriteManager", "No sprite: {0}, using fallback sprite.", spriteName);
        return CreateErrorSprite();
    }

    /// <summary>
    /// Gets a random sprite from a category.
    /// </summary>
    /// <returns>The sprite.</returns>
    /// <param name="categoryName">Category name.</param>
    public static Sprite GetRandomSprite(string categoryName)
    {
        Dictionary<string, Sprite> spritesFromCategory;
        Sprite sprite = null;

        if (sprites.TryGetValue(categoryName, out spritesFromCategory))
        {
            if (spritesFromCategory.Count > 0)
            {
                System.Random rand = new System.Random();
                sprite = spritesFromCategory.ElementAt(rand.Next(0, spritesFromCategory.Count)).Value;
            }
        }

        return sprite;
    }

    /// <summary>
    /// Determines if there is a sprite with the specified category and name.
    /// </summary>
    /// <returns><c>true</c> if there is a sprite with the specified category and name; otherwise, <c>false</c>.</returns>
    /// <param name="categoryName">Category name.</param>
    /// <param name="spriteName">Sprite name.</param>
    public static bool HasSprite(string categoryName, string spriteName)
    {
        // NOTE! This method is a bad idea. Every time we want to know
        // if a sprite exists we always want the sprite too
        // So it's wastefull to check before
        Dictionary<string, Sprite> categorySprites;
        if (sprites.TryGetValue(categoryName, out categorySprites))
        {
            return categorySprites.ContainsKey(spriteName);
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Loads the sprites from the given directory path.
    /// </summary>
    /// <param name="directoryPath">Directory path.</param>
    public static void LoadSpriteFiles(string directoryPath)
    {
        // First, we're going to see if we have any more sub-directories,
        // if so -- call LoadSpritesFromDirectory on that.
        string[] subDirectories = Directory.GetDirectories(directoryPath);
        foreach (string subDirectory in subDirectories)
        {
            LoadSpriteFiles(subDirectory);
        }

        string[] filesInDir = Directory.GetFiles(directoryPath);
        foreach (string fileName in filesInDir)
        {
            // Is this an image file?
            // Unity's LoadImage seems to support only png and jpg
            // NOTE: We **could** try to check file extensions, but why not just
            // have Unity **attempt** to load the image, and if it doesn't work,
            // then I guess it wasn't an image! An advantage of this, is that we
            // don't have to worry about oddball filenames, nor do we have to worry
            // about what happens if Unity adds support for more image format
            // or drops support for existing ones.
            string spriteCategory = new DirectoryInfo(directoryPath).Name;

            LoadImage(spriteCategory, fileName);
        }
    }

    /// <summary>
    /// Loads a single image from the given filePath, if possible.
    /// </summary>
    /// <param name="spriteCategory">Sprite category.</param>
    /// <param name="filePath">File path.</param>
    private static void LoadImage(string spriteCategory, string filePath)
    {
        // TODO:  LoadImage is returning TRUE for things like .meta and .json files.  What??!
        //      So as a temporary fix, let's just bail if we have something we KNOW should not
        //      be an image.
        if (filePath.Contains(".json") || filePath.Contains(".meta") || filePath.Contains(".db"))
        {
            return;
        }

        // Load the file into a texture
        byte[] imageBytes = File.ReadAllBytes(filePath);

        // Create some kind of dummy instance of Texture2D
        Texture2D imageTexture = new Texture2D(2, 2, TextureFormat.ARGB32, false);

        // LoadImage will correctly resize the texture based on the image file
        if (imageTexture.LoadImage(imageBytes))
        {
            // Image was successfully loaded.
            imageTexture.filterMode = FilterMode.Point;

            // So let's see if there's a matching JSON file for this image.
            string baseSpriteName = Path.GetFileNameWithoutExtension(filePath);
            string basePath = Path.GetDirectoryName(filePath);

            // NOTE: The extension must be in lower case!
            string jsonPath = Path.Combine(basePath, baseSpriteName + ".json");

            if (File.Exists(jsonPath))
            {
                StreamReader reader = File.OpenText(jsonPath);

                JToken protoJson = JToken.ReadFrom(new JsonTextReader(reader));
                reader.Close();

                JArray array = (JArray)protoJson;

                // Loop through the json file for each object
                // and calling LoadSprite once for each of them.
                foreach (JObject obj in array)
                {
                    try
                    {
                        ReadSpriteFromJson(spriteCategory, obj, imageTexture);
                    }
                    catch (Exception e)
                    {
                        UnityDebugger.Debugger.LogWarning("SpriteManager", obj);
                        throw new Exception("Error in file " + jsonPath, e);
                    }
                }
            }
            else
            {
                // File couldn't be read, probably because it doesn't exist
                // so we'll just assume the whole image is one sprite with pixelPerUnit = 64
                LoadSprite(spriteCategory, baseSpriteName, imageTexture, new Rect(0, 0, imageTexture.width, imageTexture.height), 64, new Vector2(0.5f, 0.5f));
            }

            // Attempt to load/parse the data file to get information on the sprite(s)
        }

        // Else, the file wasn't actually a image file, so just move on.
    }

    /// <summary>
    /// Reads the sprite from data file for the image.
    /// </summary>
    /// <param name="spriteCategory">Sprite category.</param>
    /// <param name="obj">The Json Object Reader.</param>
    /// <param name="imageTexture">Image texture.</param>
    private static void ReadSpriteFromJson(string spriteCategory, JObject obj, Texture2D imageTexture)
    {
        string name = PrototypeReader.ReadJson(string.Empty, obj["name"]);
        int x = PrototypeReader.ReadJson(0, obj["x"]);
        int y = PrototypeReader.ReadJson(0, obj["y"]);
        int w = PrototypeReader.ReadJson(1, obj["w"]);
        int h = PrototypeReader.ReadJson(1, obj["h"]);

        float pivotX = PrototypeReader.ReadJson(0.5f, obj["pivotX"]);
        float pivotY = PrototypeReader.ReadJson(0.5f, obj["pivotY"]);
        if (pivotX < 0 || pivotX > 1 || pivotY < 0 || pivotY > 1)
        {
            UnityDebugger.Debugger.LogWarning("SpriteManager", "Pivot for object " + name + " has pivots of " + pivotX + "," + pivotY);
        }

        int pixelPerUnit = int.Parse(obj["pixelPerUnit"].ToString());

        LoadSprite(spriteCategory, name, imageTexture, new Rect(x * pixelPerUnit, y * pixelPerUnit, w * pixelPerUnit, h * pixelPerUnit), pixelPerUnit, new Vector2(pivotX, pivotY));
    }

    /// <summary>
    /// Creates and stores the sprite.
    /// </summary>
    /// <param name="spriteCategory">Sprite category.</param>
    /// <param name="spriteName">Sprite name.</param>
    /// <param name="imageTexture">Image texture.</param>
    /// <param name="spriteCoordinates">Sprite coordinates.</param>
    /// <param name="pixelsPerUnit">Pixels per unit.</param>
    /// <param name="pivotPoint">Pivot point.</param>
    private static void LoadSprite(string spriteCategory, string spriteName, Texture2D imageTexture, Rect spriteCoordinates, int pixelsPerUnit, Vector2 pivotPoint)
    {
        Sprite s = Sprite.Create(imageTexture, spriteCoordinates, pivotPoint, pixelsPerUnit);

        Dictionary<string, Sprite> categorySprites;
        if (sprites.TryGetValue(spriteCategory, out categorySprites) == false)
        {
            // If this category didn't exist until now, we create it
            categorySprites = new Dictionary<string, Sprite>();
            sprites[spriteCategory] = categorySprites;
        }

        // Add the sprite to the category
        categorySprites[spriteName] = s;
    }

    /// <summary>
    /// Creates the no resource texture.
    /// </summary>
    private static void CreateEmptyTexture()
    {
        // Generate a 32x32 magenta image
        noResourceTexture = new Texture2D(32, 32, TextureFormat.ARGB32, false);
        Color32[] pixels = noResourceTexture.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color32(255, 0, 255, 255);
        }

        noResourceTexture.SetPixels32(pixels);
        noResourceTexture.Apply();
    }
}
