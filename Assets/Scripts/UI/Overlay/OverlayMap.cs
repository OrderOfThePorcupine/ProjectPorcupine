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
using MoonSharp.Interpreter;
using ProjectPorcupine.Localization;
using UnityEngine;

/// <summary>
/// This game object manages a mesh+texture+renderer+material that is
/// used to superimpose a semi-transparent "overlay" to the map.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class OverlayMap : MonoBehaviour
{
    /// <summary>
    /// Internal storage of color map width.
    /// </summary>
    public const int ColorMapWidth = 20;

    /// <summary>
    /// Transparency of overlay.
    /// </summary>
    [Range(0, 1)]
    private const float Transparency = 0.8f;

    /// <summary>
    /// Update interval (0 for every Update, inf for never).
    /// </summary>
    private const float UpdateInterval = 5f;

    /// <summary>
    /// The panel this gameobject is associated with.
    /// </summary>
    private GameObject parentPanel;

    /// <summary>
    /// Random colours.
    /// </summary>
    private static List<Color32> randomColors;

    /// <summary>
    /// All the palette colors.
    /// </summary>
    private static List<Color32> paletteColors;

    /// <summary>
    /// The current layer, held so we can detect when the layers change.
    /// </summary>
    private int currentLayer;

    /// <summary>
    /// Whether or not to generate the map.
    /// </summary>
    private bool generateMap = false;

    /// <summary>
    /// Current Overlay.
    /// </summary>
    public string CurrentOverlay { get; private set; }

    /// <summary>
    /// You can set any function, overlay will display value of func at point (x,y)
    /// Depending on how many colors the ColorMapSG has, the displayed values will cycle.
    /// </summary>
    public Func<int, int, int, int> ValueAt { get; private set; }

    /// <summary>
    /// In memory color map lookup per overlay to speed up the overlay texture generation
    /// and avoid too much call to the GetPixel method.
    /// </summary>
    public Dictionary<string, Dictionary<int, Color>> OverlayColorMapLookup { get; private set; }

    /// <summary>
    /// Starting left corner (x,y) and z-coordinate of mesh and (3d left corner).
    /// </summary>
    private Vector3 leftBottomCorner = new Vector3(-0.5f, -0.5f, -1f);

    /// <summary>
    /// Time since last update.
    /// </summary>
    private float elapsed = 0f;

    /// <summary>
    /// Current color map, setting the map causes the colorMapArray to be recreated.
    /// </summary>
    private OverlayDescriptor.ColorMapOption colorMap;

    /// <summary>
    /// Storage for color map as texture, copied from using copyTexture on GPUs.
    /// This texture is made of n*x times y pixels, where n is the size of the "ColorMapSG"
    /// x and y is the size of 1 tile of the map (20x20 by default).
    /// Constructed from the ColorMapSG.
    /// </summary>
    private Texture2D colorMapTexture;

    /// <summary>
    /// Mesh data.
    /// </summary>
    private Vector3[] newVertices;
    private Vector3[] newNormals;
    private Vector2[] newUV;

    private int[] newTriangles;

    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;

    /// <summary>
    /// Array with colors for overlay (colormap).
    /// Each element is a color that will be part of the color palette.
    /// </summary>
    private Color32[] colorMapArray;

    /// <summary>
    /// True if Init() has been called (i.e. there is a mesh and a color map).
    /// </summary>
    private bool initialized = false;

    private GameObject colorMapView;
    private GameObject textView;

    // The texture applied to the entire overlay map.
    private Texture2D texture;

    public OverlayDescriptor.ColorMapOption ColorMapSG
    {
        get
        {
            return colorMap;
        }

        set
        {
            colorMap = value;
            GenerateColorMap();
        }
    }

    /// <summary>
    /// Returns an array of color, using the preset colormap with the name "name".
    /// Sets the alpha channel to alpha and uses "size" colors.
    /// </summary>
    /// <param name="colorMap">Name of colormap.</param>
    /// <param name="size">Number of colors to use.</param>
    /// <param name="alpha">Alpha channel of color.</param>
    /// <returns></returns>
    public static Color32[] ColorMap(
        OverlayDescriptor.ColorMapOption colorMap,
        int size = 256,
        byte alpha = 128)
    {
        Color32[] cm = new Color32[size];
        Func<int, Color32> map;

        switch (colorMap)
        {
            default:
            case OverlayDescriptor.ColorMapOption.Jet:
                map = (int v) =>
                {
                    Color32 c = new Color32(255, 255, 255, alpha);
                    if (v == 64)
                    {
                        c.r = 0;
                        c.g = 255;
                    }
                    else if (v == 128)
                    {
                        c.r = 0;
                        c.b = 255;
                    }
                    else if (v == 192)
                    {
                        c.g = 255;
                        c.b = 0;
                    }
                    else if (v < 64)
                    {
                        c.r = 0;
                        c.g = (byte)(4 * v);
                    }
                    else if (v < 128)
                    {
                        c.r = 0;
                        c.b = (byte)(256 + (4 * (64 - v)));
                    }
                    else if (v < 192)
                    {
                        c.r = (byte)(4 * (v - 128));
                        c.b = 0;
                    }
                    else
                    {
                        c.g = (byte)(256 + (4 * (192 - v)));
                        c.b = 0;
                    }

                    return c;
                };
                break;
            case OverlayDescriptor.ColorMapOption.Random:
                GenerateRandomColors(size);
                map = (int v) =>
                {
                    return randomColors[v];
                };
                break;
            case OverlayDescriptor.ColorMapOption.Palette:
                GeneratePaletteColors();
                map = (int v) =>
                {
                    return paletteColors[v % paletteColors.Count];
                };
                break;
        }

        for (int i = 0; i < size; i++)
        {
            cm[i] = map(i);
        }

        return cm;
    }

    /// <summary>
    /// Assign a parent panel.
    /// </summary>
    /// <param name="parentPanel"> The panel to assign. </param>
    public void Init(GameObject parentPanel)
    {
        this.parentPanel = parentPanel;
        Material mat = Resources.Load<Material>("Shaders/Transparent-Diffuse");
        meshRenderer.material = mat;
        CreateGUI();
        initialized = true;
        Bake();
        SetOverlay("None");
    }

    /// <summary>
    /// World changed.
    /// </summary>
    public void OnWorldChange(World world)
    {
        if (world != null)
        {
            generateMap = true;
            GenerateMesh();
            GenerateColorMap();
        }
        else
        {
            generateMap = false;
        }
    }

    /// <summary>
    /// Paint the texture.
    /// </summary>
    public void Bake()
    {
        if (initialized && ValueAt != null)
        {
            GenerateTexture();
        }
    }

    /// <summary>
    /// Set overlay to display perototype with name "name".
    /// </summary>
    /// <param name="name">Name of overlay prototype.</param>
    public void SetOverlay(string name)
    {
        if (name == "None")
        {
            meshRenderer.enabled = false;
            CurrentOverlay = name;
            HideGUITooltip();
            return;
        }
        else if (PrototypeManager.Overlay.Has(name))
        {
            meshRenderer.enabled = true;
            CurrentOverlay = name;
            OverlayDescriptor descr = PrototypeManager.Overlay.Get(name);

            if (FunctionsManager.Overlay.HasFunction(descr.LuaFunctionName) == false)
            {
                UnityDebugger.Debugger.LogError("OverlayMap", string.Format("Couldn't find a function named '{0}' in Overlay functions", descr.LuaFunctionName));
                return;
            }

            bool loggedOnce = false;
            ValueAt = (x, y, z) =>
            {
                if (GameController.Instance == null)
                {
                    return 0;
                }

                Tile tile = GameController.Instance.CurrentWorld.GetTileAt(x, y, z);

                DynValue result = FunctionsManager.Overlay.Call(descr.LuaFunctionName, new object[] { tile, World.Current });
                double? value = result.CastToNumber();
                if (value == null)
                {
                    if (loggedOnce == false)
                    {
                        UnityDebugger.Debugger.LogError("OverlayMap", string.Format("The return value from the function named '{0}' was null for tile at ({1}, {2}, {3})", descr.LuaFunctionName, x, y, z));
                        loggedOnce = true;
                    }

                    return 0;
                }

                return (int)value;
            };

            ColorMapSG = descr.ColorMap;
            Bake();
            ShowGUITooltip();
        }
        else
        {
            UnityDebugger.Debugger.LogWarning("OverlayMap", string.Format("Overlay with name {0} not found in prototypes", name));
        }
    }

    private static void GenerateRandomColors(int size)
    {
        if (randomColors == null)
        {
            randomColors = new List<Color32>();
        }

        for (int i = randomColors.Count; i < size; i++)
        {
            randomColors.Add(UnityEngine.Random.ColorHSV());
        }
    }

    private static void GeneratePaletteColors()
    {
        if (paletteColors == null)
        {
            paletteColors = new List<Color32>();
        }

        // basic colors
        paletteColors.Add(new Color32(255, 255, 255, 255));
        paletteColors.Add(new Color32(255, 0, 0, 255));
        paletteColors.Add(new Color32(0, 255, 0, 255));
        paletteColors.Add(new Color32(0, 0, 255, 255));
        paletteColors.Add(new Color32(255, 255, 0, 255));
        paletteColors.Add(new Color32(0, 255, 255, 255));
        paletteColors.Add(new Color32(255, 0, 255, 255));
        paletteColors.Add(new Color32(192, 192, 192, 255));
        paletteColors.Add(new Color32(128, 128, 128, 255));
        paletteColors.Add(new Color32(128, 0, 0, 255));
        paletteColors.Add(new Color32(128, 128, 0, 255));
        paletteColors.Add(new Color32(0, 128, 0, 255));
        paletteColors.Add(new Color32(128, 0, 128, 255));
        paletteColors.Add(new Color32(0, 128, 128, 255));
        paletteColors.Add(new Color32(0, 0, 128, 255));
    }

    /// <summary>
    /// If update is required, redraw texture ("bake") (kinda expensive).
    /// </summary>
    private void Update()
    {
        elapsed += Time.deltaTime;
        if (CurrentOverlay != "None" && elapsed > UpdateInterval)
        {
            Bake();
            elapsed = 0f;
        }

        if (CurrentOverlay != "None" && currentLayer != GameController.Instance.CurrentSystem.CameraController.CurrentLayer)
        {
            Bake();
            currentLayer = GameController.Instance.CurrentSystem.CameraController.CurrentLayer;
            elapsed = 0f;
        }

        // TODO: Prettify.
        Vector2 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (ValueAt != null)
        {
            textView.GetComponent<UnityEngine.UI.Text>().text = string.Format("[DEBUG] Currently over: {0}", ValueAt((int)(pos.x + 0.5f), (int)(pos.y + 0.5f), GameController.Instance.CurrentSystem.CameraController.CurrentLayer));
        }
    }

    /// <summary>
    /// If overlay is toggled on, it should be "baked".
    /// </summary>
    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        OverlayColorMapLookup = new Dictionary<string, Dictionary<int, Color>>();
    }

    /// <summary>
    /// Create the colormap texture from the color set.
    /// </summary>
    private void GenerateColorMap()
    {
        // TODO: make the map configurable.
        colorMapArray = ColorMap(ColorMapSG, 255);

        // Colormap texture.
        int textureWidth = colorMapArray.Length * ColorMapWidth;
        int textureHeight = ColorMapWidth;
        colorMapTexture = new Texture2D(textureWidth, textureHeight);

        // Loop over each color in the palette and build a noisy texture.
        int n = 0;
        foreach (Color32 baseColor in colorMapArray)
        {
            for (int y = 0; y < ColorMapWidth; y++)
            {
                for (int x = 0; x < ColorMapWidth; x++)
                {
                    Color colorCopy = baseColor;
                    colorCopy.a = Transparency;

                    // Add some noise to "prettify".
                    colorCopy.r += UnityEngine.Random.Range(-0.03f, 0.03f);
                    colorCopy.b += UnityEngine.Random.Range(-0.03f, 0.03f);
                    colorCopy.g += UnityEngine.Random.Range(-0.03f, 0.03f);
                    colorMapTexture.SetPixel((n * ColorMapWidth) + x, y, colorCopy);
                }
            }

            ++n;
        }

        colorMapTexture.Apply();
        colorMapView.GetComponent<UnityEngine.UI.Image>().material.mainTexture = colorMapTexture;
    }

    /// <summary>
    /// Build the huge overlay texture.
    /// </summary>
    private void GenerateTexture()
    {
        if (generateMap == false)
        {
            UnityDebugger.Debugger.LogWarning("Tried to generate map, when no map is selected");
            return;
        }

        if (colorMapTexture == null)
        {
            UnityDebugger.Debugger.LogError("OverlayMap", "No color map texture setted!");
        }

        if (!OverlayColorMapLookup.ContainsKey(CurrentOverlay))
        {
            OverlayColorMapLookup.Add(CurrentOverlay, new Dictionary<int, Color>());
        }

        Dictionary<int, Color> colorMapLookup = OverlayColorMapLookup[CurrentOverlay];

        // Size in pixels of overlay texture and create texture.
        int textureWidth = GameController.Instance.CurrentWorld.Width;
        int textureHeight = GameController.Instance.CurrentWorld.Height;
        Color[] pixels = new Color[textureHeight * textureWidth];

        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                float v = ValueAt(x, y, GameController.Instance.CurrentSystem.CameraController.CurrentLayer);
                Debug.Assert(v >= 0 && v < 256, "v >= 0 && v < 256");

                int sampleX = ((int)v % 256) * ColorMapWidth;

                if (!colorMapLookup.ContainsKey(sampleX))
                {
                    colorMapLookup.Add(sampleX, colorMapTexture.GetPixel(sampleX, 0));
                }

                Color pixel = colorMapLookup[sampleX];
                int tilePixelIndex = (y * textureWidth) + x;
                pixels[tilePixelIndex] = pixel;
            }
        }

        texture = new Texture2D(textureWidth, textureHeight)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        texture.SetPixels(pixels);
        texture.Apply();
        meshRenderer.material.mainTexture = texture;
    }

    /// <summary>
    /// Build mesh.
    /// </summary>
    private void GenerateMesh()
    {
        if (generateMap == false)
        {
            UnityDebugger.Debugger.LogWarning("Tried to generate map, when no map is selected");
            return;
        }

        Mesh mesh = new Mesh();
        if (meshFilter != null)
        {
            meshFilter.mesh = mesh;
        }

        int sizePixelX = GameController.Instance.CurrentWorld.Width + 1;
        int sizePixelY = GameController.Instance.CurrentWorld.Height + 1;

        newVertices = new Vector3[sizePixelX * sizePixelY];
        newNormals = new Vector3[sizePixelX * sizePixelY];
        newUV = new Vector2[sizePixelX * sizePixelY];
        newTriangles = new int[(sizePixelX - 1) * (sizePixelY - 1) * 6];

        for (int y = 0; y < sizePixelY; y++)
        {
            for (int x = 0; x < sizePixelX; x++)
            {
                newVertices[(y * sizePixelX) + x] = new Vector3(x, y, 0) + leftBottomCorner;
                newNormals[(x * sizePixelY) + y] = Vector3.up;
                newUV[(y * sizePixelX) + x] = new Vector2((float)x / GameController.Instance.CurrentWorld.Width, (float)y / GameController.Instance.CurrentWorld.Height);
            }
        }

        int offset = 0;
        for (int y = 0; y < sizePixelY - 1; y++)
        {
            for (int x = 0; x < sizePixelX - 1; x++)
            {
                int index = (y * sizePixelX) + x;
                newTriangles[offset + 0] = index;
                newTriangles[offset + 1] = index + sizePixelX;
                newTriangles[offset + 2] = index + sizePixelX + 1;

                newTriangles[offset + 3] = index;
                newTriangles[offset + 5] = index + 1;
                newTriangles[offset + 4] = index + sizePixelX + 1;

                offset += 6;
            }
        }

        mesh.vertices = newVertices;
        mesh.uv = newUV;
        mesh.triangles = newTriangles;
        mesh.normals = newNormals;
    }

    private void CreateGUI()
    {
        UnityEngine.UI.Dropdown dropdown = parentPanel.GetComponentInChildren<UnityEngine.UI.Dropdown>();
        if (dropdown == null)
        {
            UnityDebugger.Debugger.LogWarning("OverlayMap", "No parent panel was selected!");
            return;
        }

        colorMapView = new GameObject("Preview");
        colorMapView.AddComponent<UnityEngine.UI.Image>();
        colorMapView.transform.SetParent(parentPanel.transform);
        colorMapView.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(120f, -40f, 0);
        colorMapView.AddComponent<UnityEngine.UI.Text>();
        colorMapView.AddComponent<UnityEngine.UI.LayoutElement>();
        colorMapView.GetComponent<UnityEngine.UI.LayoutElement>().minHeight = 30;
        colorMapView.GetComponent<UnityEngine.UI.LayoutElement>().minWidth = 150;
        Material overlayMaterial = new Material(Resources.Load<Material>("Shaders/UI-Unlit-Transparent"));
        colorMapView.GetComponent<UnityEngine.UI.Image>().material = overlayMaterial;

        textView = new GameObject("DEBUG");
        textView.AddComponent<UnityEngine.UI.Text>();
        textView.AddComponent<UnityEngine.UI.LayoutElement>();
        textView.GetComponent<UnityEngine.UI.LayoutElement>().minHeight = 30;
        textView.GetComponent<UnityEngine.UI.LayoutElement>().minWidth = 150;
        textView.transform.SetParent(parentPanel.transform);
        textView.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(120, -40f, 0);
        textView.GetComponent<UnityEngine.UI.Text>().text = "Currently Selected:";
        textView.GetComponent<UnityEngine.UI.Text>().fontSize = 14;
        textView.GetComponent<UnityEngine.UI.Text>().font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        List<string> options = new List<string> { LocalizationTable.GetLocalization("overlay_none") };
        List<string> types = new List<string> { "None" };
        foreach (OverlayDescriptor descr in PrototypeManager.Overlay.Values)
        {
            options.Add(descr.Name);
            types.Add(descr.Type);
        }

        dropdown.AddOptions(options);
        dropdown.onValueChanged.AddListener((index) => { SetOverlay(types[index]); });
    }

    private void HideGUITooltip()
    {
        textView.SetActive(false);
        colorMapView.SetActive(false);
        parentPanel.GetComponentInChildren<UnityEngine.UI.Image>().enabled = false;
    }

    private void ShowGUITooltip()
    {
        textView.SetActive(true);
        parentPanel.GetComponentInChildren<UnityEngine.UI.Image>().enabled = true;

        colorMapView.SetActive(true);

        Material overlayMaterial = new Material(Resources.Load<Material>("Shaders/UI-Unlit-Transparent"));
        colorMapView.GetComponent<UnityEngine.UI.Image>().material = overlayMaterial;
        GenerateColorMap();
    }
}
