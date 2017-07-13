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
    public static readonly Color DefaultTint = Color.white;

    public static readonly Dictionary<TextAnchor, Vector3> Positions = new Dictionary<TextAnchor, Vector3>
    {
        { TextAnchor.UpperLeft, new Vector3(-64f, 32f, 0) },
        { TextAnchor.UpperCenter, new Vector3(0, 32f, 0) },
        { TextAnchor.UpperRight, new Vector3(96f, 32f, 0) },
        { TextAnchor.MiddleLeft, new Vector3(-64f, 0, 0) },
        { TextAnchor.MiddleCenter, new Vector3(0, 0, 0) },
        { TextAnchor.MiddleRight, new Vector3(96f, 0, 0) },
        { TextAnchor.LowerLeft, new Vector3(-64f, -32f, 0) },
        { TextAnchor.LowerCenter, new Vector3(0, -32f, 0) },
        { TextAnchor.LowerRight, new Vector3(96f, -32f, 0) },
    };

    private GameObject cursorGO;
    private SpriteRenderer cursorSR;
    private Vector2 cursorTextBoxSize = new Vector2(140, 70);
    private Texture2D cursorTexture;
    private GUIStyle style = new GUIStyle();
    private bool shouldShowCursor; // A more gentle variant to ForceShow.

    private Dictionary<TextAnchor, CursorTextBox> textBoxes = new Dictionary<TextAnchor, CursorTextBox>();

    public MouseCursor()
    {
        shouldShowCursor = false;
        UIMode = false;

        style.font = Resources.Load<Font>("Fonts/Arial/Arial") as Font;
        style.fontSize = 15;
        cursorTexture = Resources.Load<Texture2D>("UI/Cursors/Ship");

        KeyboardManager.Instance.RegisterInputAction("ToggleCursorTextBox", KeyboardMappedInputType.KeyUp, () => { shouldShowCursor = !shouldShowCursor; });
    }

    /// <summary>
    /// Forcefully show the cursor.
    /// </summary>
    public bool UIMode { get; set; }

    public GameObject BuildCursor()
    {
        cursorGO = new GameObject("CURSOR");

        RectTransform rt1 = cursorGO.AddComponent<RectTransform>();
        rt1.sizeDelta = new Vector2(64, 64);
        cursorSR = cursorGO.AddComponent<SpriteRenderer>();
        cursorSR.sortingLayerName = "TileUI";

        Cursor.SetCursor(cursorTexture, new Vector2(0, 0), CursorMode.Auto);

        textBoxes = new Dictionary<TextAnchor, CursorTextBox>()
        {
            { TextAnchor.UpperLeft, new CursorTextBox(cursorGO, TextAnchor.MiddleRight, style, Positions[TextAnchor.UpperLeft], cursorTextBoxSize) },
            { TextAnchor.UpperCenter, new CursorTextBox(cursorGO, TextAnchor.MiddleCenter, style, Positions[TextAnchor.UpperCenter], cursorTextBoxSize) },
            { TextAnchor.UpperRight, new CursorTextBox(cursorGO, TextAnchor.MiddleLeft, style, Positions[TextAnchor.UpperRight], cursorTextBoxSize) },

            { TextAnchor.MiddleLeft, new CursorTextBox(cursorGO, TextAnchor.MiddleRight, style, Positions[TextAnchor.MiddleLeft], cursorTextBoxSize) },
            { TextAnchor.MiddleCenter, new CursorTextBox(cursorGO, TextAnchor.MiddleCenter, style, Positions[TextAnchor.MiddleCenter], cursorTextBoxSize) },
            { TextAnchor.MiddleRight, new CursorTextBox(cursorGO, TextAnchor.MiddleLeft, style, Positions[TextAnchor.MiddleRight], cursorTextBoxSize) },

            { TextAnchor.LowerLeft, new CursorTextBox(cursorGO, TextAnchor.MiddleRight, style, Positions[TextAnchor.LowerLeft], cursorTextBoxSize) },
            { TextAnchor.LowerCenter, new CursorTextBox(cursorGO, TextAnchor.MiddleCenter, style, Positions[TextAnchor.LowerCenter], cursorTextBoxSize) },
            { TextAnchor.LowerRight, new CursorTextBox(cursorGO, TextAnchor.MiddleLeft, style, Positions[TextAnchor.LowerRight], cursorTextBoxSize) },
        };

        return cursorGO;
    }

    public void Reset()
    {
        foreach (CursorTextBox box in textBoxes.Values)
        {
            box.Set(string.Empty, DefaultTint);
        }
    }

    public void DisplayCursorInfo(TextAnchor pos, string text, Color color)
    {
        textBoxes[pos].Set(text, color);
    }

    public void Update()
    {
        // If we should show cursor and we aren't over a ui element (unless we are UI mode) then show cursor.
        if (shouldShowCursor && (EventSystem.current.IsPointerOverGameObject() == false || UIMode))
        {
            cursorGO.SetActive(true);
        }
        else
        {
            cursorGO.SetActive(false);
        }

        cursorGO.transform.position = Input.mousePosition;
    }

    public class CursorTextBox
    {
        public GameObject textObject;
        public Text textComponent;
        public RectTransform rectTranform;

        public CursorTextBox(GameObject parentObject, TextAnchor textAlignment, GUIStyle style, Vector3 localPosition, Vector2 textWidthHeight)
        {
            textObject = new GameObject("Cursor-Text");
            textObject.transform.SetParent(parentObject.transform);
            textObject.transform.localPosition = localPosition;

            textComponent = textObject.AddComponent<Text>();
            textComponent.alignment = textAlignment;
            textComponent.font = style.font;
            textComponent.fontSize = style.fontSize;

            Outline outline = textObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1.5f, 1.5f);

            rectTranform = textObject.GetComponentInChildren<RectTransform>();
            rectTranform.sizeDelta = textWidthHeight;
        }

        public void Set(string text, Color color)
        {
            textComponent.text = text;
            textComponent.color = color;
        }
    }
}
