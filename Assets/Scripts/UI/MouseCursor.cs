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
    public enum TextPosition
    {
        upperLeft,
        upperRight,
        lowerLeft,
        lowerRight
    }

    public static readonly Color defaultTint = Color.white;

    public bool cursorOverride = false;
    public bool forceShow = false;

    private MouseController mc;

    private GameObject cursorGO;
    private SpriteRenderer cursorSR;

    private Dictionary<TextPosition, CursorTextBox> textBoxes = new Dictionary<TextPosition, CursorTextBox>();

    private Vector3 upperLeftPostion = new Vector3(-64f, 32f, 0);
    private Vector3 upperRightPostion = new Vector3(96f, 32f, 0);
    private Vector3 lowerLeftPostion = new Vector3(-64f, -32f, 0);
    private Vector3 lowerRightPostion = new Vector3(96f, -32f, 0);

    private Vector2 cursorTextBoxSize = new Vector2(140, 70);

    private Texture2D cursorTexture;

    private GUIStyle style = new GUIStyle();

    public MouseCursor(MouseController mouseController)
    {
        mc = mouseController;

        style.font = Resources.Load<Font>("Fonts/Arial/Arial") as Font;
        style.fontSize = 15;

        LoadCursorTexture();
        BuildCursor();

        KeyboardManager.Instance.RegisterInputAction("ToggleCursorTextBox", KeyboardMappedInputType.KeyUp, () => { cursorOverride = !cursorOverride; });
    }

    public void Update()
    {
        ShowCursor();
        UpdateCursor();
    }

    public void Reset()
    {
        for (int i = 0; i < 4; i++)
        {
            Text text = textBoxes[(TextPosition)i].text;
            text.text = string.Empty;
            text.color = defaultTint;
        }
    }

    public void DisplayCursorInfo(TextPosition pos, string text, Color color)
    {
        textBoxes[pos].text.text = text;
        textBoxes[pos].text.color = color;
    }

    private void LoadCursorTexture()
    {
        cursorTexture = Resources.Load<Texture2D>("UI/Cursors/Ship");
    }

    private void BuildCursor()
    {
        cursorGO = new GameObject();
        cursorGO.name = "CURSOR";
        cursorGO.transform.SetParent(mc.GetCursorParent().transform, true);
        mc.GetCursorParent().name = "Cursor Canvas";

        Canvas cursor_canvas = mc.GetCursorParent().AddComponent<Canvas>();
        cursor_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        cursor_canvas.worldCamera = Camera.main;
        cursor_canvas.sortingLayerName = "TileUI";
        cursor_canvas.referencePixelsPerUnit = 411.1f;
        RectTransform rt = mc.GetCursorParent().GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 200);

        CanvasScaler cs = mc.GetCursorParent().AddComponent<CanvasScaler>();
        cs.dynamicPixelsPerUnit = 100.6f;
        cs.referencePixelsPerUnit = 411.1f;

        RectTransform rt1 = cursorGO.AddComponent<RectTransform>();
        rt1.sizeDelta = new Vector2(64, 64);
        cursorSR = cursorGO.AddComponent<SpriteRenderer>();
        cursorSR.sortingLayerName = "TileUI";

        Cursor.SetCursor(cursorTexture, new Vector2(0, 0), CursorMode.Auto);

        textBoxes[TextPosition.upperLeft] = new CursorTextBox(cursorGO, TextAnchor.MiddleRight, style, upperLeftPostion, cursorTextBoxSize);
        textBoxes[TextPosition.upperRight] = new CursorTextBox(cursorGO, TextAnchor.MiddleLeft, style, upperRightPostion, cursorTextBoxSize);
        textBoxes[TextPosition.lowerLeft] = new CursorTextBox(cursorGO, TextAnchor.MiddleRight, style, lowerLeftPostion, cursorTextBoxSize);
        textBoxes[TextPosition.lowerRight] = new CursorTextBox(cursorGO, TextAnchor.MiddleLeft, style, lowerRightPostion, cursorTextBoxSize);
    }

    private void UpdateCursor()
    {
        cursorGO.transform.position = Input.mousePosition;
    }

    private void ShowCursor()
    {
        if (forceShow || (cursorOverride == false && EventSystem.current.IsPointerOverGameObject() == false))
        {
            cursorGO.SetActive(true);
        }
        else
        {
            cursorGO.SetActive(false);
        }
    }

    public class CursorTextBox
    {
        public GameObject textObject;
        public Text text;
        public RectTransform rectTranform;

        public CursorTextBox(GameObject parentObject, TextAnchor textAlignment, GUIStyle style, Vector3 localPosition, Vector2 textWidthHeight)
        {
            textObject = new GameObject("Cursor-Text");
            textObject.transform.SetParent(parentObject.transform);
            textObject.transform.localPosition = localPosition;

            text = textObject.AddComponent<Text>();
            text.alignment = textAlignment;
            text.font = style.font;
            text.fontSize = style.fontSize;

            Outline outline = textObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1.5f, 1.5f);

            rectTranform = textObject.GetComponentInChildren<RectTransform>();
            rectTranform.sizeDelta = textWidthHeight;
        }
    }
}
