using UnityEngine;

/// <summary>
/// Small helper that creates menu UI objects (labels, text buttons, sliders)
/// in one consistent style. Used by the scene builders, so every menu
/// in the game looks the same and the style is defined in one place.
/// </summary>
public class MenuUiBuilder
{
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;
    private const float MatchWidthAndHeightEqually = 0.5f;
    private const float ButtonFadeSeconds = 0.05f;

    private static readonly Color TextColor = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color HighlightColor = new Color(1f, 0.82f, 0.2f);
    private static readonly Color PressedColor = new Color(1f, 0.55f, 0.15f);
    private static readonly Color DisabledColor = new Color(0.36f, 0.36f, 0.42f);
    private static readonly Color SliderTrackColor = new Color(0.22f, 0.22f, 0.3f);

    private readonly Font _font;

    /// <summary>Creates a builder that writes all text in the given font.</summary>
    public MenuUiBuilder(Font font)
    {
        _font = font;
    }

    /// <summary>
    /// Creates a Canvas (the root object all UI must sit under). It scales with the
    /// screen, so the menu keeps the same proportions at every resolution.
    /// </summary>
    public Transform CreateCanvas(string name)
    {
        GameObject canvasObject = new GameObject(name);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        UnityEngine.UI.CanvasScaler scaler = canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.matchWidthOrHeight = MatchWidthAndHeightEqually;

        // The raycaster is what lets the mouse "hit" buttons on this canvas.
        canvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        return canvasObject.transform;
    }

    /// <summary>
    /// Creates a full-screen container that stacks its children top to bottom,
    /// centered on the screen.
    /// </summary>
    public Transform CreateColumn(Transform parent, string name, float spacing)
    {
        RectTransform rect = CreateUiObject(parent, name);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        UnityEngine.UI.VerticalLayoutGroup layout = rect.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        ConfigureLayout(layout, spacing);
        return rect;
    }

    /// <summary>Creates a container that places its children side by side.</summary>
    public Transform CreateRow(Transform parent, string name, float spacing)
    {
        RectTransform rect = CreateUiObject(parent, name);
        UnityEngine.UI.HorizontalLayoutGroup layout = rect.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        ConfigureLayout(layout, spacing);
        return rect;
    }

    /// <summary>Creates a piece of text that cannot be clicked.</summary>
    public UnityEngine.UI.Text CreateLabel(Transform parent, string name, string content, int fontSize)
    {
        RectTransform rect = CreateUiObject(parent, name);
        UnityEngine.UI.Text text = rect.gameObject.AddComponent<UnityEngine.UI.Text>();
        text.font = _font;
        text.fontSize = fontSize;
        text.text = content;
        text.color = TextColor;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    /// <summary>
    /// Creates a button that is just text. The text changes color when it is
    /// highlighted, pressed, or disabled, in classic retro-menu style.
    /// </summary>
    public UnityEngine.UI.Button CreateTextButton(Transform parent, string name, string content, int fontSize)
    {
        UnityEngine.UI.Text text = CreateLabel(parent, name, content, fontSize);
        text.color = Color.white;
        text.raycastTarget = true;

        UnityEngine.UI.Button button = text.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = text;
        button.colors = CreateColors();
        text.gameObject.AddComponent<ButtonSound>();
        return button;
    }

    /// <summary>Creates a horizontal slider that goes from 0 to 1.</summary>
    public UnityEngine.UI.Slider CreateSlider(Transform parent, string name, float width, float height)
    {
        // DefaultControls builds the same slider as the Editor's GameObject > UI > Slider menu.
        // With no sprites it is made of plain rectangles, which suits the pixel style.
        GameObject sliderObject = UnityEngine.UI.DefaultControls.CreateSlider(new UnityEngine.UI.DefaultControls.Resources());
        sliderObject.name = name;
        sliderObject.transform.SetParent(parent, false);
        SetFixedSize(sliderObject, width, height);

        UnityEngine.UI.Slider slider = sliderObject.GetComponent<UnityEngine.UI.Slider>();
        slider.colors = CreateColors();
        sliderObject.transform.Find("Background").GetComponent<UnityEngine.UI.Image>().color = SliderTrackColor;
        slider.fillRect.GetComponent<UnityEngine.UI.Image>().color = HighlightColor;
        return slider;
    }

    /// <summary>Gives an object a fixed size inside a row or column.</summary>
    public void SetFixedSize(GameObject target, float width, float height)
    {
        UnityEngine.UI.LayoutElement element = target.AddComponent<UnityEngine.UI.LayoutElement>();
        element.preferredWidth = width;
        element.preferredHeight = height;
    }

    private static RectTransform CreateUiObject(Transform parent, string name)
    {
        GameObject uiObject = new GameObject(name, typeof(RectTransform));
        uiObject.transform.SetParent(parent, false);
        return (RectTransform)uiObject.transform;
    }

    /// <summary>
    /// Children keep their natural size and sit in the middle,
    /// instead of being stretched to fill the container.
    /// </summary>
    private static void ConfigureLayout(UnityEngine.UI.HorizontalOrVerticalLayoutGroup layout, float spacing)
    {
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }

    private static UnityEngine.UI.ColorBlock CreateColors()
    {
        UnityEngine.UI.ColorBlock colors = UnityEngine.UI.ColorBlock.defaultColorBlock;
        colors.normalColor = TextColor;
        colors.highlightedColor = HighlightColor;
        colors.selectedColor = HighlightColor;
        colors.pressedColor = PressedColor;
        colors.disabledColor = DisabledColor;
        colors.fadeDuration = ButtonFadeSeconds;
        return colors;
    }
}
