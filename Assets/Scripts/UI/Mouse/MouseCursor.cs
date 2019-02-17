#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectPorcupine.Mouse
{
    public class MouseCursor
    {
        /// <summary>
        /// The default color for the text.
        /// </summary>
        public static readonly Color TextColor = Color.white;

        /// <summary>
        /// The color of the background.
        /// </summary>
        public static readonly Color BackgroundColor = new Color(7f / 255f, 69f / 255f, 87f / 255f, 1f);

        /// <summary>
        /// The color of the outline of the text.
        /// </summary>
        public static readonly Color TextOutlineColor = new Color(7f / 255f, 70f / 255f, 92f / 255f, 1f);

        /// <summary>
        /// The color of the outline of the text.
        /// </summary>
        public static readonly Color BackgroundOutlineColor = new Color(206f / 255f, 242f / 255f, 242f / 255f, 1f);

        private Texture2D cursorTexture;
        private GUIStyle style;
        private RectTransform cachedTransform; // The overhead of calling gameobject.transform every update is expensive

        /// <summary>
        /// User override.
        /// </summary>
        private bool shouldShowCursor;

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

            cursorTexture = Resources.Load<Texture2D>("UI/Cursors/Ship.png");

            BuildCursor();
        }

        /// <summary>
        /// Deconstructor.
        /// </summary>
        ~MouseCursor()
        {
            KeyboardManager.Instance.UnRegisterInputAction("ToggleCursorTextBox");
        }

        /// <summary>
        /// The cursor game object.
        /// </summary>
        public GameObject CursorGameObject { get; private set; }

        /// <summary>
        /// The cursor game object.
        /// </summary>
        public GameObject CursorCanvasGameObject { get; private set; }

        /// <summary>
        /// Enable this to let the mouse cursor show tooltips on UI elements.
        /// </summary>
        public bool UIMode { get; set; }

        /// <summary>
        /// Resets all the text on this cursor.
        /// </summary>
        public void Reset()
        {
            for (int i = 0; i < textBoxes.Length; i++)
            {
                textBoxes[i].Set(string.Empty, TextColor, false);
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
        public void UpdateCursor(bool modeEnabled)
        {
            // If we should show cursor and we aren't over a ui element (unless we are UI mode) then show cursor.
            CursorGameObject.SetActive(modeEnabled && shouldShowCursor && (UIMode || EventSystem.current.IsPointerOverGameObject() == false));
            cachedTransform.position = Input.mousePosition;
        }

        /// <summary>
        /// Build builds the cursor.
        /// </summary>
        /// <returns> The cursor object. </returns>
        private void BuildCursor()
        {
            CursorCanvasGameObject = new GameObject("Cursor_Canvas");
            Canvas cursorCanvas = CursorCanvasGameObject.AddComponent<Canvas>();
            cursorCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cursorCanvas.worldCamera = Camera.main;

            // TOOD: do we want this to be so damn high
            cursorCanvas.sortingOrder = 32767; // This is the maximum sorting layer value one can have
            cursorCanvas.referencePixelsPerUnit = 100f;
            cursorCanvas.pixelPerfect = true;

            RectTransform rt = CursorCanvasGameObject.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 200);

            CanvasScaler cs = CursorCanvasGameObject.gameObject.AddComponent<CanvasScaler>();
            cs.scaleFactor = 1;
            cs.referencePixelsPerUnit = 100f;

            CursorGameObject = new GameObject("Cursor_Parent");
            CanvasGroup cg = CursorGameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;

            cachedTransform = CursorGameObject.AddComponent<RectTransform>();
            cachedTransform.sizeDelta = new Vector2(64, 64);
            cachedTransform.SetParent(CursorCanvasGameObject.transform);

            CursorCanvasGameObject.gameObject.AddComponent<UIRescaler>();

            Cursor.SetCursor(cursorTexture, new Vector2(0, 0), CursorMode.Auto);

            textBoxes = new CursorTextBox[]
            {
            new CursorTextBox(CursorGameObject, TextAnchor.LowerRight, style, TextAnchor.UpperLeft),      // UpperLeft
            new CursorTextBox(CursorGameObject, TextAnchor.LowerCenter, style, TextAnchor.UpperCenter),   // UpperCenter
            new CursorTextBox(CursorGameObject, TextAnchor.LowerLeft, style, TextAnchor.UpperRight),      // UpperRight

            new CursorTextBox(CursorGameObject, TextAnchor.MiddleRight, style, TextAnchor.MiddleLeft),     // MiddleLeft,
            new CursorTextBox(CursorGameObject, TextAnchor.MiddleCenter, style, TextAnchor.MiddleCenter),  // MiddleCenter
            new CursorTextBox(CursorGameObject, TextAnchor.MiddleLeft, style, TextAnchor.MiddleRight),     // MiddleRight

            new CursorTextBox(CursorGameObject, TextAnchor.UpperRight, style,  TextAnchor.LowerLeft),     // LowerLeft
            new CursorTextBox(CursorGameObject, TextAnchor.UpperCenter, style, TextAnchor.LowerCenter),   // LowerCenter
            new CursorTextBox(CursorGameObject, TextAnchor.UpperLeft, style,  TextAnchor.LowerRight),     // LowerRight
            };
        }

        /// <summary>
        /// Handles the text boxes for this cursor.
        /// </summary>
        private class CursorTextBox
        {
            private Image background;
            private Text textComponent;

            public CursorTextBox(GameObject parentObject, TextAnchor textAlignment, GUIStyle style, TextAnchor location)
            {
                Vector3 localPosition;
                switch (location)
                {
                case TextAnchor.UpperLeft:
                    localPosition = new Vector3(-32f, 16f, 0f);
                    break;
                case TextAnchor.UpperCenter:
                    localPosition = new Vector3(0f, 32f, 0f);
                    break;
                case TextAnchor.UpperRight:
                    localPosition = new Vector3(64f, 16f, 0f);
                    break;

                case TextAnchor.MiddleLeft:
                    localPosition = new Vector3(-32f, 0f, 0f);
                    break;
                case TextAnchor.MiddleCenter:
                    localPosition = new Vector3(0f, 0f, 0f);
                    break;
                case TextAnchor.MiddleRight:
                    localPosition = new Vector3(64, 0f, 0f);
                    break;

                case TextAnchor.LowerLeft:
                    localPosition = new Vector3(-40f, -32f, 0f);
                    break;
                case TextAnchor.LowerCenter:
                    localPosition = new Vector3(0f, -32f, 0f);
                    break;
                case TextAnchor.LowerRight:
                    localPosition = new Vector3(80, -32f, 0f);
                    break;

                default:
                    localPosition = Vector3.zero;
                    break;
                }

                GameObject rootMaster = new GameObject("Root: " + location);
                ContentSizeFitter fitter = rootMaster.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                HorizontalLayoutGroup horizontalLayout = rootMaster.AddComponent<HorizontalLayoutGroup>();
                horizontalLayout.childForceExpandHeight = false;
                horizontalLayout.childForceExpandWidth = false;
                horizontalLayout.childAlignment = textAlignment;
                rootMaster.transform.SetParent(parentObject.transform);
                rootMaster.transform.localPosition = localPosition;

                background = new GameObject("Background: " + location).AddComponent<Image>();
                background.color = MouseCursor.BackgroundColor;
                background.enabled = false;
                Outline outline = background.gameObject.AddComponent<Outline>();
                outline.effectColor = MouseCursor.BackgroundOutlineColor;
                outline.effectDistance = new Vector2(1.5f, 1.5f);

                VerticalLayoutGroup group = background.gameObject.AddComponent<VerticalLayoutGroup>();
                group.childForceExpandHeight = false;
                group.childForceExpandWidth = false;
                group.padding = new RectOffset(5, 5, 5, 5);

                background.transform.SetParent(rootMaster.transform);
                background.transform.localPosition = Vector3.zero;

                GameObject textObject = new GameObject("Cursor-Text: " + location);
                textObject.transform.SetParent(background.transform);
                textObject.transform.localPosition = Vector3.zero;

                textComponent = textObject.AddComponent<Text>();
                textComponent.alignment = textAlignment;
                textComponent.font = style.font;
                textComponent.fontSize = style.fontSize;
                textComponent.verticalOverflow = VerticalWrapMode.Overflow;
                textObject.transform.localScale = Vector3.one;

                outline = textObject.AddComponent<Outline>();
                outline.effectColor = MouseCursor.TextOutlineColor;
                outline.effectDistance = new Vector2(1.5f, 1.5f);
            }

            /// <summary>
            /// Sets the text component values.
            /// </summary>
            /// <param name="text"> The text to set. </param>
            /// <param name="textColor"> The text color to set. </param>
            /// <param name="withBackground"> Enable background. </param>
            public void Set(string text, Color textColor, bool withBackground)
            {
                textComponent.text = text;
                textComponent.color = textColor;
                background.enabled = withBackground;
            }
        }
    }
}
