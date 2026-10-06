using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// TEMPORARY (sandbox test page, to be removed).
/// Builds the Sandbox scene through code. It runs once automatically when the scene
/// file is missing, and can be run again from the menu: KyubPon > Rebuild Sandbox Scene.
/// </summary>
[InitializeOnLoad]
public static class SandboxSceneBuilder
{
    private const string ScenePath = SceneBuilderTools.SandboxScenePath;

    // Press Start 2P is drawn on an 8-pixel grid, so sizes that are multiples of 8 stay sharp.
    private const int HeadingFontSize = 64;
    private const int ButtonFontSize = 48;
    private const int ControlFontSize = 32;
    private const int NoteFontSize = 24;
    private const int PanelFontSize = 16;

    private const float HeadingOffsetFromTop = 50f;
    private const float NoteOffsetFromTop = 140f;
    private const float ControlsOffsetFromTop = 190f;
    private const float StatusOffsetFromTop = 258f;
    private const float GridOffsetFromCenter = -30f;
    private const float PanelMarginFromSide = 40f;
    private const float PaletteOffsetFromBottom = 130f;
    private const float ButtonsOffsetFromBottom = 36f;
    private const float SpaceBetweenBottomButtons = 400f;
    private const float RowSpacing = 32f;
    private const float SizeValueWidth = 96f;
    private const float GapBetweenControlGroups = 96f;

    // The pixel font's lines sit very close together, so wrapped text needs extra room.
    private const float PanelLineSpacing = 1.6f;

    private static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
    private static readonly Vector2 ScreenCenter = new Vector2(0.5f, 0.5f);
    private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
    private static readonly Vector2 LeftCenter = new Vector2(0f, 0.5f);
    private static readonly Vector2 RightCenter = new Vector2(1f, 0.5f);
    private static readonly Vector2 HeadingSize = new Vector2(1600f, 72f);
    private static readonly Vector2 NoteSize = new Vector2(1800f, 32f);
    private static readonly Vector2 ControlsSize = new Vector2(1800f, 56f);
    private static readonly Vector2 StatusSize = new Vector2(1000f, 32f);
    private static readonly Vector2 GridAreaSize = new Vector2(900f, 520f);
    private static readonly Vector2 PanelSize = new Vector2(440f, 620f);
    private static readonly Vector2 PaletteSize = new Vector2(1600f, 100f);
    private static readonly Vector2 BottomButtonSize = new Vector2(360f, 56f);

    static SandboxSceneBuilder()
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
    /// Creates the scene with the size controls, the grid area, the side notes,
    /// the block palette, and the buttons, saves it, opens it, and updates the
    /// build scene list.
    /// </summary>
    [MenuItem("KyubPon/Rebuild Sandbox Scene")]
    public static void BuildScene()
    {
        Font font = SceneBuilderTools.LoadPixelFont();
        MenuUiBuilder ui = new MenuUiBuilder(font);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        SceneBuilderTools.CreateCamera();
        SceneBuilderTools.CreateEventSystem();

        Transform canvas = ui.CreateCanvas("SandboxCanvas");
        SandboxScreen sandboxScreen = canvas.gameObject.AddComponent<SandboxScreen>();
        SerializedObject data = new SerializedObject(sandboxScreen);

        BuildHeading(ui, canvas, data);
        BuildSizeControls(ui, canvas, data);
        BuildSidePanels(ui, canvas, data);
        BuildGridArea(canvas, data);
        BuildPalette(ui, canvas, data);
        BuildBottomButtons(ui, canvas, data);

        // The HP numbers and X marks are created while the game runs, so the screen needs the font.
        SceneBuilderTools.SetReference(data, "_font", font);

        // Dragged blocks are moved under the canvas itself so they draw on top of everything.
        SceneBuilderTools.SetReference(data, "_dragLayer", canvas);
        data.ApplyModifiedPropertiesWithoutUndo();

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        SceneBuilderTools.ApplyBuildScenes();
        Debug.Log($"[Info] Created {ScenePath} and updated the build scene list.");
    }

    /// <summary>The title, the "temporary" note, and the status line that says what is happening.</summary>
    private static void BuildHeading(MenuUiBuilder ui, Transform canvas, SerializedObject data)
    {
        UnityEngine.UI.Text heading = ui.CreateLabel(canvas, "Heading", "SANDBOX", HeadingFontSize);
        SceneBuilderTools.PlaceAtEdge(heading.rectTransform, TopCenter, new Vector2(0f, -HeadingOffsetFromTop), HeadingSize);

        UnityEngine.UI.Text note = ui.CreateLabel(canvas, "Note", "TEMPORARY TEST PAGE", NoteFontSize);
        SceneBuilderTools.PlaceAtEdge(note.rectTransform, TopCenter, new Vector2(0f, -NoteOffsetFromTop), NoteSize);

        UnityEngine.UI.Text status = ui.CreateLabel(canvas, "StatusLabel", "YOUR TURN", NoteFontSize);
        SceneBuilderTools.PlaceAtEdge(status.rectTransform, TopCenter, new Vector2(0f, -StatusOffsetFromTop), StatusSize);
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
    /// The rules note on the left and the battle numbers on the right.
    /// Their text is filled in by SandboxScreen when the game runs.
    /// </summary>
    private static void BuildSidePanels(MenuUiBuilder ui, Transform canvas, SerializedObject data)
    {
        UnityEngine.UI.Text rules = CreatePanelText(ui, canvas, "RulesLabel");
        SceneBuilderTools.PlaceAtEdge(rules.rectTransform, LeftCenter, new Vector2(PanelMarginFromSide, GridOffsetFromCenter), PanelSize);
        SceneBuilderTools.SetReference(data, "_rulesLabel", rules);

        UnityEngine.UI.Text stats = CreatePanelText(ui, canvas, "StatsLabel");
        SceneBuilderTools.PlaceAtEdge(stats.rectTransform, RightCenter, new Vector2(-PanelMarginFromSide, GridOffsetFromCenter), PanelSize);
        SceneBuilderTools.SetReference(data, "_statsLabel", stats);
    }

    /// <summary>Side panels hold several lines, so their text wraps and starts at the top-left.</summary>
    private static UnityEngine.UI.Text CreatePanelText(MenuUiBuilder ui, Transform canvas, string name)
    {
        UnityEngine.UI.Text text = ui.CreateLabel(canvas, name, string.Empty, PanelFontSize);
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.lineSpacing = PanelLineSpacing;
        return text;
    }

    /// <summary>An empty box in the middle of the screen. The grid squares are created inside it when the game runs.</summary>
    private static void BuildGridArea(Transform canvas, SerializedObject data)
    {
        GameObject gridArea = new GameObject("GridArea", typeof(RectTransform));
        gridArea.transform.SetParent(canvas, false);
        SceneBuilderTools.PlaceAtEdge((RectTransform)gridArea.transform, ScreenCenter, new Vector2(0f, GridOffsetFromCenter), GridAreaSize);
        SceneBuilderTools.SetReference(data, "_gridArea", gridArea.transform);
    }

    /// <summary>A row under the grid. The blocks to drag from are added to it when the game runs.</summary>
    private static void BuildPalette(MenuUiBuilder ui, Transform canvas, SerializedObject data)
    {
        Transform palette = ui.CreateRow(canvas, "Palette", RowSpacing);
        SceneBuilderTools.PlaceAtEdge((RectTransform)palette, BottomCenter, new Vector2(0f, PaletteOffsetFromBottom), PaletteSize);
        ui.CreateLabel(palette, "PaletteLabel", "DRAG A BLOCK:", ControlFontSize);
        SceneBuilderTools.SetReference(data, "_palette", palette);
    }

    private static void BuildBottomButtons(MenuUiBuilder ui, Transform canvas, SerializedObject data)
    {
        BuildBottomButton(ui, canvas, data, "BackButton", "BACK", "_backButton", -SpaceBetweenBottomButtons);
        BuildBottomButton(ui, canvas, data, "PassButton", "PASS", "_passButton", 0f);
        BuildBottomButton(ui, canvas, data, "ResetButton", "RESET", "_resetButton", SpaceBetweenBottomButtons);
    }

    private static void BuildBottomButton(MenuUiBuilder ui, Transform canvas, SerializedObject data,
        string objectName, string caption, string fieldName, float offsetFromCenter)
    {
        UnityEngine.UI.Button button = ui.CreateTextButton(canvas, objectName, caption, ButtonFontSize);
        SceneBuilderTools.PlaceAtEdge((RectTransform)button.transform, BottomCenter, new Vector2(offsetFromCenter, ButtonsOffsetFromBottom), BottomButtonSize);
        SceneBuilderTools.SetReference(data, fieldName, button);
    }
}
