#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public class SpriteToJson : EditorWindow
{
    private string spriteToJson;
    private string outputDirPath = string.Empty;
    private string inputDirPath = string.Empty;

    private Texture2D[] images;
    private Sprite[] sprites;
    private string[] filesInDir;

    private Version version = Version.v1;

    private string[] pixelPerUnitOptions = new string[] { "16", "32", "64", "128", "256", "512", "1024" };
    private int index = 2;

    private string[] columnRowOptions = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" };
    private int columnIndex = 0;
    private int rowIndex = 0;

    private bool isMultipleSprite = false;
    private bool showInstructions = false;
    private bool useCustomPivot = false;
    private bool textureLoaded = false;

    private float pivotX = 0.5f;
    private float pivotY = 0.5f;

    private string imageName = string.Empty;
    private string imageExt = string.Empty;

    private int spriteCount = 0;    

    private Texture2D myTexture = null;

    private Vector2 scrollPosition;
    private EditorWindow window;

    private enum Version
    {
        v1,
        v2
    }

    [MenuItem("Window/Sprite To JSON")]
    public static void ShowWindow()
    {
        GetWindow(typeof(SpriteToJson));
    }

    private void OnEnable()
    {
        spriteToJson = Application.dataPath + "/Resources/Editor/SpriteToJSON/";
    }

    private void OnLostFocus()
    {
        Close();
    }

    private int PixelsPerUnit()
    {
        return int.Parse(pixelPerUnitOptions[index]);
    }

    private void Awake()
    {
        window = GetWindow(typeof(SpriteToJson));
        window.minSize = new Vector2(460, 680);
    }

    private void OnGUI()
    {
        GUILayout.BeginHorizontal("Box");

        switch (version)
        {
            case Version.v1:
                if (GUILayout.Button("Unity Based"))
                {
                    version = Version.v1;
                }

                if (GUILayout.Button("Settings Based", EditorStyles.miniButton))
                {
                    version = Version.v2;
                }

                break;

            case Version.v2:
                if (GUILayout.Button("Unity Based", EditorStyles.miniButton))
                {
                    version = Version.v1;
                }

                if (GUILayout.Button("Settings Based"))
                {
                    version = Version.v2;
                }

                break;
        }        

        GUILayout.EndHorizontal();
        showInstructions = EditorGUILayout.ToggleLeft("Instructions", showInstructions);
        EditorGUILayout.Space();

        switch (version)
        {
            case Version.v1:
                ShowVersion1();
                break;
            case Version.v2:
                ShowVersion2();
                break;
        }        
    }

    private void ShowVersion1()
    {        
        GUILayout.Label("Unity Sprite Editor Based - Only 1 sprite at a time", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (showInstructions)
        {
            EditorGUILayout.BeginVertical("Box");
            GUILayout.Label("Instructions", EditorStyles.boldLabel);            
            GUILayout.Label("1. Sprite must be in 'Resources/Editor/SpriteToJson'.");
            GUILayout.Label("2. Edit your sprite in Unity's sprite editor as normal.");
            GUILayout.Label("3. Select the folder to output the sprite and Json.");
            GUILayout.Label("4. Press 'Export' button");
            GUILayout.Label("5. Json will be generated moved along with the sprite to the specified folder");
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();            
        }
        
        EditorGUILayout.BeginVertical("Box");
        GUILayout.Label("Image must be in this folder: " + spriteToJson);
        if (GUILayout.Button("Open Image Folder"))
        {
            if (Directory.Exists(spriteToJson))
            {
                EditorUtility.RevealInFinder(spriteToJson);
            }
            else
            {
                Directory.CreateDirectory(spriteToJson);
                EditorUtility.RevealInFinder(spriteToJson);
            }            
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        if (GUILayout.Button("Set Output Folder"))
        {
            outputDirPath = EditorUtility.OpenFolderPanel("Select folder to save Json", outputDirPath, string.Empty);
        }

        if (GUILayout.Button("Export Sprite to Json"))
        {
            images = Resources.LoadAll<Texture2D>("Editor/SpriteToJson");
            sprites = Resources.LoadAll<Sprite>("Editor/SpriteToJson");
            
            filesInDir = Directory.GetFiles(spriteToJson);

            if (images.Length > 1)
            {
                UnityDebugger.Debugger.LogError("SpriteToJson", "Place only one sprite in 'Resources/Editor/SpriteToJson'");
                return;
            }

            ExportSprites();
        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical("Box");
        GUILayout.Label("Current Path: " + outputDirPath);
        if (GUILayout.Button("Open Output Folder"))
        {
            EditorUtility.RevealInFinder(outputDirPath);
        }

        EditorGUILayout.EndVertical();
    }

    private void ExportSprites()
    {
        UnityDebugger.Debugger.Log("SpriteToJson", "Files saved to: " + outputDirPath);

        foreach (string fn in filesInDir)
        {
            UnityDebugger.Debugger.Log("SpriteToJson", "files in dir: " + fn);
        }   
            
        foreach (Texture2D t in images)
        {
            UnityDebugger.Debugger.Log("SpriteToJson", "Filename: " + t.name);            
        }

        WriteDataFile();        
    }   

    private void WriteDataFile()
    {
        if (outputDirPath == string.Empty)
        {
            UnityDebugger.Debugger.LogError("SpriteToJson", "Please select a folder");
            return;
        }
        
        for (int i = 0; i < images.Length; i++)
        {
            string filePath = Path.Combine(outputDirPath, images[i].name + ".json");
            StreamWriter sw = new StreamWriter(filePath);
            JsonWriter writer = new JsonTextWriter(sw);

            JArray array = new JArray();

            foreach (Sprite s in sprites)
            {
                array.Add(SingleSpriteToJson(s));
            }

            SaveJsonToHdd(array, writer);

            // Move the .png and meta file to the same directory as the json.
            foreach (string s in filesInDir)
            {
                if (s.Contains(".png"))
                {
                    FileUtil.MoveFileOrDirectory(Application.dataPath + "/Resources/Editor/SpriteToJSON/" + images[i].name + ".png", outputDirPath + "/" + images[i].name + ".png");
                    FileUtil.MoveFileOrDirectory(Application.dataPath + "/Resources/Editor/SpriteToJSON/" + images[i].name + ".png.meta", outputDirPath + "/" + images[i].name + ".meta");
                }
                else if (s.Contains(".jpg"))
                {
                    FileUtil.MoveFileOrDirectory(Application.dataPath + "/Resources/Editor/SpriteToJSON/" + images[i].name + ".jpg", outputDirPath + "/" + images[i].name + ".jpg");
                    FileUtil.MoveFileOrDirectory(Application.dataPath + "/Resources/Editor/SpriteToJSON/" + images[i].name + ".jpg.meta", outputDirPath + "/" + images[i].name + ".meta");
                }
                else
                {
                    continue;
                }
            }                       
        }
    }

    private void ShowVersion2()
    {
        GUILayout.Label("Generates JSon based on image and settings selected", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (showInstructions)
        {
            EditorGUILayout.BeginVertical("Box");
            GUILayout.Label("Instructions", EditorStyles.boldLabel);
            GUILayout.Label("1. Select the image you want an Json for ");
            GUILayout.Label("2. Specify the rows and columns of the image ");
            GUILayout.Label("3. Select the folder to output the sprite and Json");
            GUILayout.Label("4. Press 'Export' button");
            GUILayout.Label("5. Json will generate and sprite will be moved to the specified folder");
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Select Image"))
        {
            inputDirPath = EditorUtility.OpenFilePanelWithFilters("Select file to generate a Json for", inputDirPath, new string[] { "Image files", "png,jpg,jpeg" });
            if (File.Exists(inputDirPath))
            {
                LoadImage(inputDirPath);
            }
        }

        if (GUILayout.Button("Set Output Folder"))
        {
            outputDirPath = EditorUtility.OpenFolderPanel("Select folder to save Json", outputDirPath, string.Empty);
        }

        GUILayout.EndHorizontal();
        EditorGUILayout.Space();        

        GUILayout.Label("Settings:", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("Box");        
        GUILayout.BeginHorizontal();

        GUILayout.Label("Pixels Per Unit:", EditorStyles.boldLabel);
        index = EditorGUILayout.Popup(index, pixelPerUnitOptions);

        GUILayout.EndHorizontal();
        EditorGUILayout.Space();       
        GUILayout.BeginHorizontal();

        isMultipleSprite = EditorGUILayout.ToggleLeft("Is this a Multiple Sprite image?", isMultipleSprite);
        useCustomPivot = EditorGUILayout.ToggleLeft("Use a custom pivot?", useCustomPivot);

        GUILayout.EndHorizontal();
        EditorGUILayout.Space();        

        if (isMultipleSprite)
        {
            GUILayout.BeginVertical("Box");
            GUILayout.Label("Select Rows/Columns:", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            GUILayout.BeginHorizontal();

            GUILayout.Label("Rows:", EditorStyles.boldLabel);
            rowIndex = EditorGUILayout.Popup(rowIndex, columnRowOptions);

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();

            GUILayout.Label("Columns:", EditorStyles.boldLabel);
            columnIndex = EditorGUILayout.Popup(columnIndex, columnRowOptions);

            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            EditorGUILayout.Space();            
        }
        
        if (useCustomPivot)
        {
            EditorGUILayout.BeginVertical("Box");
            GUILayout.Label("Custom Pivot Settings:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal("Box");
            pivotX = Mathf.Clamp01(EditorGUILayout.FloatField("Pivot X", pivotX));
            pivotY = Mathf.Clamp01(EditorGUILayout.FloatField("Pivot Y", pivotY));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        if (textureLoaded == true)
        {            
            GUILayout.Label(imageName + " Preview:", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("Box");

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Width(446), GUILayout.Height(210));
            GUILayout.Label(myTexture);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }                 

        if (textureLoaded == true && outputDirPath != string.Empty)
        {
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Export " + imageName + ".json"))
            {
                if ((columnIndex == 0 && rowIndex == 0) && isMultipleSprite == true)
                {
                    EditorUtility.DisplayDialog("Select proper Row/Column count", "Please select more than 1 Row/Column!", "OK");
                    UnityDebugger.Debugger.LogError("SpriteToJson", "Please select more than 1 Row/Column");
                }
                else
                {
                    GenerateJSON(inputDirPath, myTexture);
                }
            }
        }

        if (outputDirPath != string.Empty)
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("Box");
            GUILayout.Label("Current Path: " + outputDirPath);

            if (GUILayout.Button("Open Output Folder"))
            {
                EditorUtility.RevealInFinder(outputDirPath);
            }

            EditorGUILayout.EndVertical();
        }
    }

    private void LoadImage(string filePath)
    {        
        // Load the file into a texture.
        byte[] imageBytes = System.IO.File.ReadAllBytes(filePath);

        // Create some kind of dummy instance of Texture2D.
        // LoadImage will correctly resize the texture based on the image file.
        Texture2D imageTexture = new Texture2D(2, 2);
        myTexture = new Texture2D(2, 2);

        // Image was successfully loaded.
        if (imageTexture.LoadImage(imageBytes))
        {
            myTexture = imageTexture;
            imageName = Path.GetFileNameWithoutExtension(filePath);
            imageExt = Path.GetExtension(filePath);
            UnityDebugger.Debugger.Log("SpriteToJson", imageName + " Loaded");
            textureLoaded = true;            
        }        
    }

    private void GenerateJSON(string filePath, Texture2D imageTexture)
    {
        StreamWriter sw = new StreamWriter(filePath);
        JsonWriter writer = new JsonTextWriter(sw);
        JArray array = null;

        switch (isMultipleSprite)
        {
            case false:
                array = SingleSpriteJson(imageTexture);
                break;
            case true:
                array = MultiSpriteJson(imageTexture);
                break;
        }

        SaveJsonToHdd(array, writer);

        MoveImage(filePath);
        ResetForm();        
    }

    private JObject SingleSpriteToJson(Sprite s)
    {
        JObject obj = new JObject();
        obj.Add("name", s.name);
        obj.Add("x", s.rect.x / s.pixelsPerUnit);
        obj.Add("y", s.rect.y / s.pixelsPerUnit);
        obj.Add("w", s.rect.width / s.pixelsPerUnit);
        obj.Add("h", s.rect.height / s.pixelsPerUnit);
        obj.Add("pixelPerUnit", s.pixelsPerUnit);

        float pivotX = s.pivot.x / s.rect.width;
        obj.Add("pivotX", pivotX);

        float pivotY = s.pivot.y / s.rect.height;
        obj.Add("pivotY", pivotY);

        return obj;
    }

    private JArray SingleSpriteJson(Texture2D imageTexture)
    {
        JObject obj = new JObject();
        obj.Add("name", imageName);
        obj.Add("x", "0");
        obj.Add("y", "0");
        obj.Add("w", imageTexture.width / PixelsPerUnit());
        obj.Add("h", imageTexture.height / PixelsPerUnit());
        obj.Add("pixelPerUnit", PixelsPerUnit());

        if (useCustomPivot)
        {
            obj.Add("pivotX", pivotX);
            obj.Add("pivotY", pivotY);
        }

        JArray array = new JArray();
        array.Add(obj);
        return array;      
    }

    private JArray MultiSpriteJson(Texture2D imageTexture)
    {
        JArray array = new JArray();
        for (int y = int.Parse(columnRowOptions[rowIndex]) - 1; y > -1; y--)
        {
            for (int x = 0; x < int.Parse(columnRowOptions[columnIndex]); x++)                
            {
                JObject obj = new JObject();
                obj.Add("name", imageName);
                obj.Add("x", ((imageTexture.width / int.Parse(columnRowOptions[columnIndex])) / PixelsPerUnit()) * x);
                obj.Add("y", ((imageTexture.height / int.Parse(columnRowOptions[rowIndex])) / PixelsPerUnit()) * y);
                obj.Add("w", (imageTexture.width / int.Parse(columnRowOptions[columnIndex])) / PixelsPerUnit());
                obj.Add("h", (imageTexture.height / int.Parse(columnRowOptions[rowIndex])) / PixelsPerUnit());
                obj.Add("pixelPerUnit", PixelsPerUnit());

                if (useCustomPivot)
                {
                    obj.Add("pivotX", pivotX);
                    obj.Add("pivotY", pivotY);
                }

                spriteCount++;
                array.Add(obj);
            }
        }

        return array;      
    }

    private void SaveJsonToHdd(JToken json, JsonWriter writer)
    {
        JsonSerializer serializer = new JsonSerializer()
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented
        };
        serializer.Serialize(writer, json);

        writer.Flush();
        writer.Close();
    }

    private void MoveImage(string filePath)
    {
        string destPath = Path.Combine(outputDirPath, imageName + imageExt);
            
        if (File.Exists(destPath))
        {
            try
            {
                File.Replace(filePath, destPath, destPath + ".bak");
                UnityDebugger.Debugger.LogWarning("SpriteToJson", "Image already exsists, backing old one up to: " + destPath + ".bak");
            }
            catch (Exception ex)
            {
                UnityDebugger.Debugger.LogWarning("SpriteToJson", ex.Message + " - " + imageName + imageExt + " not moved.");
                EditorUtility.DisplayDialog(imageName + imageExt + " not moved.", "The original and output directories cannot be the same!" + "\n\n" + "Json was still generated.", "OK");
            }
        }
        else
        {
            File.Move(filePath, destPath);            
            UnityDebugger.Debugger.Log("SpriteToJson", "Image moved to: " + destPath);
        }

        if (File.Exists(filePath + ".meta") && File.Exists(destPath + ".meta"))
        {
            try
            {
                File.Replace(filePath + ".meta", destPath + ".meta", destPath + ".meta.bak");
            }
            catch (Exception ex)
            {
                UnityDebugger.Debugger.LogWarning("SpriteToJson", ex.Message + " - " + imageName + imageExt + ".meta not moved.");
            }            
        }
        else
        {
            File.Move(filePath + ".meta", destPath + ".meta");
        }
    }

    private void ResetForm()
    {
        textureLoaded = false;
        spriteCount = 0;
        inputDirPath = imageExt = imageName = string.Empty;
        myTexture = null;
    }

    private bool IsPowerOfTwo(int x)
    {
        return (x != 0) && ((x & (x - 1)) == 0);
    }
}
