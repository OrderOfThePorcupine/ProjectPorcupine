#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneController
{
    // Our current scenes Names
    public const string MainSceneName = "_World";
    public const string MainMenuSceneName = "MainMenu";

    public static string LoadWorldFromFileName { get; set; }

    public static Vector3 NewWorldSize { get; private set; }

    public static int Seed { get; private set; }

    public static bool GenerateAsteroids { get; private set; }

    public static string GeneratorFile { get; private set; }

    // Load the main scene.
    public static void LoadNewWorld(int width, int height, int depth, int seed, string generatorFile = "Default.xml", bool generateAsteroids = true)
    {
        NewWorldSize = new Vector3(width, height, depth);
        Seed = seed;
        GeneratorFile = generatorFile;
        GenerateAsteroids = generateAsteroids;
        CleanInstancesBeforeLoadingScene();
        SceneManager.LoadScene(MainSceneName);
    }

    public static World CreateNewWorld(string generatorFile = "Default.xml", bool generateAsteroids = true)
    {
        GeneratorFile = generatorFile;
        GenerateAsteroids = generateAsteroids;

        if (NewWorldSize == Vector3.zero)
        {
            return new World(100, 100, 5);
        }
        else
        {
            return new World((int)NewWorldSize.x, (int)NewWorldSize.y, (int)NewWorldSize.z);
        }
    }

    public static void ConfigureNewWorld()
    {
        GameObject.FindObjectOfType<DialogBoxManager>().dialogBoxNewGame.ShowDialog();
    }

    // Load a save file.
    public static void LoadWorld(string fileName)
    {
        LoadWorldFromFileName = fileName;
        CleanInstancesBeforeLoadingScene();
        SceneManager.LoadScene(MainSceneName);
    }

    // Load Main Menu.
    public static void LoadMainMenu()
    {
        CleanInstancesBeforeLoadingScene();
        SceneManager.LoadScene(MainMenuSceneName);
    }

    // Quit the app whether in editor or a build version.
    public static void QuitGame()
    {
        // Maybe ask the user if he want to save or is sure they want to quit??
#if UNITY_EDITOR
        // Allows you to quit in the editor.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Return the name of the current scene.
    public static string GetCurrentScene()
    {
        return SceneManager.GetActiveScene().name;
    }

    // Return the name of the current scene.
    public static bool IsAtIntroScene()
    {
        return (GetCurrentScene() == MainMenuSceneName) ? true : false;
    }

    // Return the name of the current scene.
    public static bool IsAtMainScene()
    {
        return (GetCurrentScene() == MainSceneName) ? true : false;
    }

    private static void CleanInstancesBeforeLoadingScene()
    {
        ProjectPorcupine.Localization.LocalizationTable.UnregisterDelegates();
    }
}