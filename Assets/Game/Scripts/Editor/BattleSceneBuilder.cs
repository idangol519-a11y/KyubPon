using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the Battle scene through code. It runs once automatically when the scene
/// file is missing, and can be run again from the menu: KyubPon > Rebuild Battle Scene.
/// </summary>
[InitializeOnLoad]
public static class BattleSceneBuilder
{
    private const string ScenePath = SceneBuilderTools.BattleScenePath;

    // Press Start 2P is drawn on an 8-pixel grid, so sizes that are multiples of 8 stay sharp.
    private const int HeadingFontSize = 48;
    private const int ControlFontSize = 32;
    private const int NoteFontSize = 24;
    private const int TooltipFontSize = 24;

    // The top of the page is kept thin so the grid gets as much height as possible.
    private const float HeadingOffsetFromTop = 24f;
    private const float CornerMargin = 32f;
    private const float ControlsOffsetFromTop = 88f;
    private const float StatsOffsetFromTop = 158f;
    private const float StatusOffsetFromTop = 202f;
    private const float GridOffsetFromCenter = -50f;

    // The grid sits a little right of the middle, to leave the left edge for the item library.
    private const float GridOffsetFromMiddle = 200f;

    // The item library: a heading, then one entry per item. Each entry is a picture
    // with the item's name, what it does, and its state underneath.
    private const int ItemTextFontSize = 16;
    private const float ItemTextLineSpacing = 1.4f;
    private const float FirstItemOffsetFromTop = 48f;
    private const float SecondItemOffsetFromTop = 350f;
    private const float ItemNameOffset = 124f;
    private const float ItemDescriptionOffset = 164f;
    private const float ItemStateOffset = 264f;
    private const float ItemDeleteButtonOffset = 296f;
    private const float BottomRowOffsetFromBottom = 30f;
    private const float PaletteOffsetFromCenter = -350f;
    private const float PaletteSpacing = 12f;
    private const float RowSpacing = 32f;
    private const float SizeValueWidth = 96f;
    private const float GapBetweenControlGroups = 96f;

    private const float TooltipWidth = 760f;
    private const int TooltipPadding = 28;
    private const float TooltipBorderWidth = 4f;

    // The pixel font's lines sit very close together, so wrapped text needs extra room.
    private const float TooltipLineSpacing = 1.6f;

    private static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
    private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
    private static readonly Vector2 TopRight = new Vector2(1f, 1f);
    private static readonly Vector2 AutosaveLabelSize = new Vector2(520f, 48f);
    private static readonly Vector2 OrderButtonSize = new Vector2(520f, 48f);
    private static readonly Vector2 ScreenCenter = new Vector2(0.5f, 0.5f);
    private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
    private static readonly Vector2 HeadingSize = new Vector2(1800f, 56f);
    private static readonly Vector2 ControlsSize = new Vector2(1800f, 56f);
    private static readonly Vector2 LineSize = new Vector2(1800f, 32f);
    private static readonly Vector2 GridAreaSize = new Vector2(1400f, 680f);
    private static readonly Vector2 LeftCenter = new Vector2(0f, 0.5f);
    private static readonly Vector2 ItemLibrarySize = new Vector2(380f, 680f);
    private static readonly Vector2 ItemHeadingSize = new Vector2(380f, 32f);
    private static readonly Vector2 ItemPictureSize = new Vector2(112f, 112f);
    private static readonly Vector2 ItemNameSize = new Vector2(380f, 32f);
    private static readonly Vector2 ItemDescriptionSize = new Vector2(380f, 96f);
    private static readonly Vector2 ItemStateSize = new Vector2(380f, 24f);
    private static readonly Vector2 ItemDeleteButtonSize = new Vector2(160f, 24f);

    // Shown only when an item's PNG is missing: a plain square in the item's color.
    private static readonly Color PotionFallbackColor = new Color(0.90f, 0.30f, 0.25f);
    private static readonly Color GemFallbackColor = new Color(0.25f, 0.60f, 0.95f);
    private static readonly Color ItemStateColor = new Color(1f, 0.82f, 0.2f);
    private static readonly Vector2 PaletteSize = new Vector2(1100f, 100f);
    private static readonly Vector2 BottomButtonSize = new Vector2(200f, 100f);
    private static readonly Color TooltipBackgroundColor = new Color(0.02f, 0.02f, 0.04f, 1f);
    private static readonly Color TooltipBorderColor = new Color(0.92f, 0.92f, 0.92f);

    // Where the three bottom buttons sit, measured sideways from the middle of the screen.
    private const float BackButtonOffset = 300f;
    private const float PassButtonOffset = 520f;
    private const float ResetButtonOffset = 760f;

    static BattleSceneBuilder()
    {
        // Wait until Unity has finished loading before touching scenes.
        EditorApplication.delayCall += BuildSceneIfMissing;
    }

    private static void BuildSceneIfMissing()
    {
        if (File.Exists(ScenePath) || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        BuildScene();
    }

    /// <summary>
    /// Creates the scene with the size controls, the grid area, the item library, the block palette,
    /// the buttons, and the rules tooltip, saves it, opens it, and updates the
    /// build scene list.
    /// </summary>
    [MenuItem("KyubPon/Rebuild Battle Scene")]
    public static void BuildScene()
    {
        Font font = SceneBuilderTools.LoadPixelFont();
        MenuUiBuilder ui = new MenuUiBuilder(font);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        SceneBuilderTools.CreateCamera();
        SceneBuilderTools.CreateEventSystem();

        Transform canvas = ui.CreateCanvas("BattleCanvas");
        BattleScreen battleScreen = canvas.gameObject.AddComponent<BattleScreen>();
        SerializedObject data = new SerializedObject(battleScreen);

        BuildTopLines(ui, canvas, data);
        BuildSizeControls(ui, canvas, data);
        BuildGridArea(canvas, data);
        BuildItemLibrary(ui, canvas, data);
        BuildPalette(ui, canvas, data);
        BuildBottomButtons(ui, canvas, data);

        // The tooltip is created last so it is drawn on top of everything else.
        BuildTooltip(ui, canvas, data);

        // The HP numbers and X marks are created while the game runs, so the screen needs the font.
        SceneBuilderTools.SetReference(data, "_font", font);

        // Dragged blocks are moved under the canvas itself so they draw on top of the grid.
        SceneBuilderTools.SetReference(data, "_dragLayer", canvas);
        data.ApplyModifiedPropertiesWithoutUndo();

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        SceneBuilderTools.ApplyBuildScenes();
        Debug.Log($"[Info] Created {ScenePath} and updated the build scene list.");
    }

    /// <summary>The title, the battle numbers, and the status line that says what is happening.</summary>
    private static void BuildTopLines(MenuUiBuilder ui, Transform canvas, SerializedObject data)
    {
        UnityEngine.UI.Text heading = ui.CreateLabel(canvas, "Heading", "BATTLE", HeadingFontSize);
        SceneBuilderTools.PlaceAtEdge(heading.rectTransform, TopCenter, new Vector2(0f, -HeadingOffsetFromTop), HeadingSize);

        // In the top-left corner: appears for a moment each time the battle is saved.
        UnityEngine.UI.Text autosave = ui.CreateLabel(canvas, "AutosaveLabel", "AUTOSAVING...", NoteFontSize);
        autosave.alignment = TextAnchor.UpperLeft;
        autosave.raycastTarget = false;
        SceneBuilderTools.PlaceAtEdge(autosave.rectTransform, TopLeft, new Vector2(CornerMargin, -CornerMargin), AutosaveLabelSize);
        SceneBuilderTools.SetReference(data, "_autosaveLabel", autosave);
        autosave.gameObject.SetActive(false);

        // In the top-right corner: switches the order in which squares are activated.
        UnityEngine.UI.Button order = ui.CreateTextButton(canvas, "OrderButton", "ORDER: LEFT TO RIGHT", NoteFontSize);
        SceneBuilderTools.PlaceAtEdge((RectTransform)order.transform, TopRight, new Vector2(-CornerMargin, -CornerMargin), OrderButtonSize);
        SceneBuilderTools.SetReference(data, "_orderButton", order);
        SceneBuilderTools.SetReference(data, "_orderLabel", order.GetComponent<UnityEngine.UI.Text>());

        UnityEngine.UI.Text stats = ui.CreateLabel(canvas, "StatsLabel", "TURN 1", NoteFontSize);
        SceneBuilderTools.PlaceAtEdge(stats.rectTransform, TopCenter, new Vector2(0f, -StatsOffsetFromTop), LineSize);
        SceneBuilderTools.SetReference(data, "_statsLabel", stats);

        UnityEngine.UI.Text status = ui.CreateLabel(canvas, "StatusLabel", "YOUR TURN", NoteFontSize);
        SceneBuilderTools.PlaceAtEdge(status.rectTransform, TopCenter, new Vector2(0f, -StatusOffsetFromTop), LineSize);
        SceneBuilderTools.SetReference(data, "_statusLabel", status);
    }

    /// <summary>Builds the "COLUMNS &lt; 5 &gt;   ROWS &lt; 4 &gt;" line above the grid.</summary>
    private static void BuildSizeControls(MenuUiBuilder ui, Transform canvas, SerializedObject data)
    {
        Transform row = ui.CreateRow(canvas, "SizeControls", RowSpacing);
        SceneBuilderTools.PlaceAtEdge((RectTransform)row, TopCenter, new Vector2(0f, -ControlsOffsetFromTop), ControlsSize);

        BuildSizeControl(ui, row, data, "Columns", "COLUMNS", "_fewerColumnsButton", "_columnsLabel", "_moreColumnsButton");

        // An empty label used as a gap between the two groups.
        UnityEngine.UI.Text gap = ui.CreateLabel(row, "Gap", string.Empty, ControlFontSize);
        ui.SetFixedSize(gap.gameObject, GapBetweenControlGroups, ControlFontSize);

        BuildSizeControl(ui, row, data, "Rows", "ROWS", "_fewerRowsButton", "_rowsLabel", "_moreRowsButton");
    }

    private static void BuildSizeControl(MenuUiBuilder ui, Transform row, SerializedObject data,
        string objectName, string caption, string fewerField, string valueField, string moreField)
    {
        ui.CreateLabel(row, objectName + "Label", caption, ControlFontSize);
        SceneBuilderTools.SetReference(data, fewerField, ui.CreateTextButton(row, "Fewer" + objectName + "Button", "<", ControlFontSize));

        // A fixed width stops the row from shifting when the number changes length.
        UnityEngine.UI.Text value = ui.CreateLabel(row, objectName + "Value", "0", ControlFontSize);
        ui.SetFixedSize(value.gameObject, SizeValueWidth, ControlFontSize);
        SceneBuilderTools.SetReference(data, valueField, value);

        SceneBuilderTools.SetReference(data, moreField, ui.CreateTextButton(row, "More" + objectName + "Button", ">", ControlFontSize));
    }

    /// <summary>
    /// An empty box that fills the middle of the screen, to the right of the item library.
    /// The grid squares are created inside it when the game runs.
    /// </summary>
    private static void BuildGridArea(Transform canvas, SerializedObject data)
    {
        GameObject gridArea = new GameObject("GridArea", typeof(RectTransform));
        gridArea.transform.SetParent(canvas, false);
        SceneBuilderTools.PlaceAtEdge((RectTransform)gridArea.transform, ScreenCenter, new Vector2(GridOffsetFromMiddle, GridOffsetFromCenter), GridAreaSize);
        SceneBuilderTools.SetReference(data, "_gridArea", gridArea.transform);
    }

    /// <summary>
    /// The column on the left edge that holds the player's items: the potion, which is
    /// clicked to use it, and the gem, which works by itself until the player deletes it.
    /// </summary>
    private static void BuildItemLibrary(MenuUiBuilder ui, Transform canvas, SerializedObject data)
    {
        GameObject library = new GameObject("ItemLibrary", typeof(RectTransform));
        library.transform.SetParent(canvas, false);
        SceneBuilderTools.PlaceAtEdge((RectTransform)library.transform, LeftCenter, new Vector2(CornerMargin, GridOffsetFromCenter), ItemLibrarySize);

        UnityEngine.UI.Text heading = ui.CreateLabel(library.transform, "Heading", "ITEMS", NoteFontSize);
        SceneBuilderTools.PlaceAtEdge(heading.rectTransform, TopCenter, Vector2.zero, ItemHeadingSize);

        UnityEngine.UI.Image potion = BuildItem(ui, library.transform, "Potion", "POTION", PotionFallbackColor, FirstItemOffsetFromTop,
            $"ONE USE. YOUR RED BLOCKS HIT +{BattleItems.PotionStrikeBonus} HARDER FOR ONE TURN", "CLICK TO USE", out UnityEngine.UI.Text potionState);

        // The potion's picture is also its button. A faded picture means it is used up.
        UnityEngine.UI.Button potionButton = potion.gameObject.AddComponent<UnityEngine.UI.Button>();
        potionButton.targetGraphic = potion;
        potion.raycastTarget = true;
        potion.gameObject.AddComponent<ButtonSound>();
        SceneBuilderTools.SetReference(data, "_potionButton", potionButton);
        SceneBuilderTools.SetReference(data, "_potionIcon", potion);
        SceneBuilderTools.SetReference(data, "_potionStateLabel", potionState);

        UnityEngine.UI.Image gem = BuildItem(ui, library.transform, "Gem", "GEM", GemFallbackColor, SecondItemOffsetFromTop,
            "TRINKET. BEFORE YOU PLACE, DRAG ANY BLUE BLOCK TO AN EMPTY SQUARE", "ALWAYS ON", out UnityEngine.UI.Text gemState);
        SceneBuilderTools.SetReference(data, "_gemIcon", gem);
        SceneBuilderTools.SetReference(data, "_gemStateLabel", gemState);

        // Throws the gem away for the rest of the battle.
        UnityEngine.UI.Button delete = ui.CreateTextButton(library.transform, "GemDeleteButton", "DELETE", ItemTextFontSize);
        SceneBuilderTools.PlaceAtEdge((RectTransform)delete.transform, TopCenter,
            new Vector2(0f, -SecondItemOffsetFromTop - ItemDeleteButtonOffset), ItemDeleteButtonSize);
        SceneBuilderTools.SetReference(data, "_gemDeleteButton", delete);
    }

    /// <summary>
    /// Builds one entry of the item library and returns its picture. "offsetFromTop" is
    /// how far below the top of the library the entry starts.
    /// </summary>
    private static UnityEngine.UI.Image BuildItem(MenuUiBuilder ui, Transform library, string objectName, string itemName,
        Color fallbackColor, float offsetFromTop, string description, string state, out UnityEngine.UI.Text stateLabel)
    {
        GameObject picture = new GameObject(objectName + "Picture", typeof(RectTransform));
        picture.transform.SetParent(library, false);
        SceneBuilderTools.PlaceAtEdge((RectTransform)picture.transform, TopCenter, new Vector2(0f, -offsetFromTop), ItemPictureSize);
        UnityEngine.UI.Image image = picture.AddComponent<UnityEngine.UI.Image>();
        image.color = fallbackColor;
        image.raycastTarget = false;

        UnityEngine.UI.Text name = ui.CreateLabel(library, objectName + "Name", itemName, NoteFontSize);
        SceneBuilderTools.PlaceAtEdge(name.rectTransform, TopCenter, new Vector2(0f, -offsetFromTop - ItemNameOffset), ItemNameSize);

        UnityEngine.UI.Text text = ui.CreateLabel(library, objectName + "Description", description, ItemTextFontSize);
        text.alignment = TextAnchor.UpperCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.lineSpacing = ItemTextLineSpacing;
        SceneBuilderTools.PlaceAtEdge(text.rectTransform, TopCenter, new Vector2(0f, -offsetFromTop - ItemDescriptionOffset), ItemDescriptionSize);

        stateLabel = ui.CreateLabel(library, objectName + "State", state, ItemTextFontSize);
        stateLabel.color = ItemStateColor;
        SceneBuilderTools.PlaceAtEdge(stateLabel.rectTransform, TopCenter, new Vector2(0f, -offsetFromTop - ItemStateOffset), ItemStateSize);
        return image;
    }

    /// <summary>A row at the bottom-left. The blocks of the player's hand are added to it when the game runs.</summary>
    private static void BuildPalette(MenuUiBuilder ui, Transform canvas, SerializedObject data)
    {
        Transform palette = ui.CreateRow(canvas, "Palette", PaletteSpacing);
        SceneBuilderTools.PlaceAtEdge((RectTransform)palette, BottomCenter, new Vector2(PaletteOffsetFromCenter, BottomRowOffsetFromBottom), PaletteSize);
        ui.CreateLabel(palette, "PaletteLabel", "YOUR BLOCKS:", NoteFontSize);
        SceneBuilderTools.SetReference(data, "_palette", palette);
    }

    /// <summary>The buttons share the bottom row with the palette, so the grid can be taller.</summary>
    private static void BuildBottomButtons(MenuUiBuilder ui, Transform canvas, SerializedObject data)
    {
        BuildBottomButton(ui, canvas, data, "BackButton", "BACK", "_backButton", BackButtonOffset);
        BuildBottomButton(ui, canvas, data, "PassButton", "PASS", "_passButton", PassButtonOffset);
        BuildBottomButton(ui, canvas, data, "ResetButton", "RESET", "_resetButton", ResetButtonOffset);
    }

    private static void BuildBottomButton(MenuUiBuilder ui, Transform canvas, SerializedObject data,
        string objectName, string caption, string fieldName, float offsetFromCenter)
    {
        UnityEngine.UI.Button button = ui.CreateTextButton(canvas, objectName, caption, ControlFontSize);
        SceneBuilderTools.PlaceAtEdge((RectTransform)button.transform, BottomCenter, new Vector2(offsetFromCenter, BottomRowOffsetFromBottom), BottomButtonSize);
        SceneBuilderTools.SetReference(data, fieldName, button);
    }

    /// <summary>
    /// The box that shows a block's rules when the mouse rests on it. It has a fixed
    /// width and grows taller to fit its text. It starts hidden, and it ignores the
    /// mouse so it can never get in the way of the block underneath it.
    /// </summary>
    private static void BuildTooltip(MenuUiBuilder ui, Transform canvas, SerializedObject data)
    {
        GameObject tooltip = new GameObject("Tooltip", typeof(RectTransform));
        tooltip.transform.SetParent(canvas, false);
        RectTransform rect = (RectTransform)tooltip.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.sizeDelta = new Vector2(TooltipWidth, 0f);

        UnityEngine.UI.Image background = tooltip.AddComponent<UnityEngine.UI.Image>();
        background.color = TooltipBackgroundColor;
        background.raycastTarget = false;
        UnityEngine.UI.Outline border = tooltip.AddComponent<UnityEngine.UI.Outline>();
        border.effectColor = TooltipBorderColor;
        border.effectDistance = new Vector2(TooltipBorderWidth, -TooltipBorderWidth);

        // The layout group gives the text the box's width; the fitter makes the box as tall as the text.
        UnityEngine.UI.VerticalLayoutGroup layout = tooltip.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.padding = new RectOffset(TooltipPadding, TooltipPadding, TooltipPadding, TooltipPadding);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        tooltip.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

        UnityEngine.UI.Text text = ui.CreateLabel(tooltip.transform, "TooltipText", string.Empty, TooltipFontSize);
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.lineSpacing = TooltipLineSpacing;
        text.fontStyle = FontStyle.Bold;
        text.color = Color.white;

        SceneBuilderTools.SetReference(data, "_tooltip", rect);
        SceneBuilderTools.SetReference(data, "_tooltipText", text);
        tooltip.SetActive(false);
    }
}
