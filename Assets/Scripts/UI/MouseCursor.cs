#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MouseCursor
{
    /// <summary>
    /// The default tint.
    /// </summary>
    public static readonly Color DefaultTint = Color.white;

    private GameObject cursorGO;
    private SpriteRenderer cursorSR;
    private Vector2 cursorTextBoxSize = new Vector2(140, 70);
    private Texture2D cursorTexture;
    private GUIStyle style;
    private bool shouldShowCursor;
    private RectTransform cachedTransform; // The overhead of calling gameobject.transform every update is expensive

    /// <summary>
    /// Uses the fact that <see cref="TextAnchor"/> is an enum and we can cast it to int.
    /// This not only is more memory safe, and contingent which makes it faster, but also
    /// allows us to iterate through it with ease which is called every frame to reset it,
    /// and the cast overhead is minimal.
    /// </summary>
    private CursorTextBox[] textBoxes;

    /// <summary>
    /// Create a new mouse cursor with the supplied style.
    /// </summary>
    /// <param name="style"> If null it will instead use a default style which is 15pt Arial, alignment being middle center.</param>
    public MouseCursor(GUIStyle style = null)
    {
        shouldShowCursor = true;
        UIMode = false;

        if (style == null)
        {
            this.style = new GUIStyle() { font = Resources.Load<Font>("Fonts/Arial/Arial") as Font, fontSize = 15, alignment = TextAnchor.MiddleCenter };
        }

        cursorTexture = Resources.Load<Texture2D>("UI/Cursors/Ship");

        KeyboardManager.Instance.RegisterInputAction("ToggleCursorTextBox", KeyboardMappedInputType.KeyUp, () => { shouldShowCursor = !shouldShowCursor; });
    }

    /// <summary>
    /// Enable this to let the mouse cursor show tooltips on UI elements.
    /// </summary>
    public bool UIMode { get; set; }

    /// <summary>
    /// Build builds the cursor.
    /// </summary>
    /// <returns> The cursor object. </returns>
    public GameObject BuildCursor()
    {
        cursorGO = new GameObject("CURSOR");

        cachedTransform = cursorGO.AddComponent<RectTransform>();
        cachedTransform.sizeDelta = new Vector2(64, 64);
        cursorSR = cursorGO.AddComponent<SpriteRenderer>();
        cursorSR.sortingLayerName = "TileUI";

        Cursor.SetCursor(cursorTexture, new Vector2(0, 0), CursorMode.Auto);

        textBoxes = new CursorTextBox[]
        {
            new CursorTextBox(cursorGO, TextAnchor.MiddleRight, style, new Vector3(-64f, 32f, 0), cursorTextBoxSize),   // UpperLeft
            new CursorTextBox(cursorGO, TextAnchor.MiddleCenter, style, new Vector3(0, 32f, 0), cursorTextBoxSize),     // UpperCenter
            new CursorTextBox(cursorGO, TextAnchor.MiddleLeft, style, new Vector3(96f, 32f, 0), cursorTextBoxSize),     // UpperRight
            new CursorTextBox(cursorGO, TextAnchor.MiddleRight, style, new Vector3(-64f, 0, 0), cursorTextBoxSize),     // MiddleLeft,
            new CursorTextBox(cursorGO, TextAnchor.MiddleCenter, style, new Vector3(0, 0, 0), cursorTextBoxSize),       // MiddleCenter
            new CursorTextBox(cursorGO, TextAnchor.MiddleLeft, style, new Vector3(96f, 0, 0), cursorTextBoxSize),       // MiddleRight

            new CursorTextBox(cursorGO, TextAnchor.MiddleRight, style,  new Vector3(-64f, -32f, 0), cursorTextBoxSize), // LowerLeft
            new CursorTextBox(cursorGO, TextAnchor.MiddleCenter, style, new Vector3(0, -32f, 0), cursorTextBoxSize),    // LowerCenter
            new CursorTextBox(cursorGO, TextAnchor.MiddleLeft, style,  new Vector3(96f, -32f, 0), cursorTextBoxSize),   // LowerRight
        };

        return cursorGO;
    }

    /// <summary>
    /// Resets all the text on this cursor.
    /// </summary>
    public void Reset()
    {
        for (int i = 0; i < textBoxes.Length; i++)
        {
            textBoxes[i].Set(string.Empty, DefaultTint, false);
        }
    }

    /// <summary>
    /// Displays cursor information given the parameters.
    /// </summary>
    /// <param name="pos"> The text box to place the text in. </param>
    /// <param name="text"> The text to place. </param>
    /// <param name="color"> The color for the text. </param>
    public void DisplayCursorInfo(TextAnchor pos, string text, Color color, bool withBackground)
    {
        textBoxes[(int)pos].Set(text, color, withBackground);
    }

    /// <summary>
    /// Update the cursor should be called by the parent class.
    /// </summary>
    public void UpdateCursor()
    {
        // If we should show cursor and we aren't over a ui element (unless we are UI mode) then show cursor.
        cursorGO.SetActive(shouldShowCursor && (EventSystem.current.IsPointerOverGameObject() == false || UIMode));
        cachedTransform.position = Input.mousePosition;
    }

    /// <summary>
    /// Handles the text boxes for this cursor.
    /// </summary>
    private class CursorTextBox
    {
        public static readonly Color BackgroundColor = Color.blue;

        private Image background;
        private Text textComponent;

        public CursorTextBox(GameObject parentObject, TextAnchor textAlignment, GUIStyle style, Vector3 localPosition, Vector2 textWidthHeight)
        {
            background = new GameObject("Background: " + textAlignment).AddComponent<Image>();
            background.color = BackgroundColor;
            background.enabled = false;
            background.gameObject.AddComponent<ContentSizeFitter>();
            background.transform.SetParent(parentObject.transform);
            background.transform.localPosition = Vector3.zero;

            GameObject textObject = new GameObject("Cursor-Text: " + textAlignment);
            textObject.transform.SetParent(background.transform);
            textObject.transform.localPosition = localPosition;

            textComponent = textObject.AddComponent<Text>();
            textComponent.alignment = textAlignment;
            textComponent.font = style.font;
            textComponent.fontSize = style.fontSize;

            Outline outline = textObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1.5f, 1.5f);

            textObject.GetComponentInChildren<RectTransform>().sizeDelta = textWidthHeight;
        }

        public void Set(string text, Color color, bool withBackground)
        {
            textComponent.text = text;
            textComponent.color = color;
            background.enabled = withBackground;
        }
    }
}
