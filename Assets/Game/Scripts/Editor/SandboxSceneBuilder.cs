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

    private const float HeadingOffsetFromTop = 50f;
    private const float NoteOffsetFromTop = 140f;
    private const float ControlsOffsetFromTop = 190f;
    private const float GridOffsetFromCenter = -20f;
    private const float PaletteOffsetFromBottom = 130f;
    private const float BackOffsetFromBottom = 36f;
    private const float RowSpacing = 32f;
    private const float SizeValueWidth = 96f;
    private const float GapBetweenControlGroups = 96f;

    private static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
    private static readonly Vector2 ScreenCenter = new Vector2(0.5f, 0.5f);
    private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
    private static readonly Vector2 HeadingSize = new Vector2(1600f, 72f);
    private static readonly Vector2 NoteSize = new Vector2(1800f, 32f);
    private static readonly Vector2 ControlsSize = new Vector2(1800f, 56f);
    private static readonly Vector2 GridAreaSize = new Vector2(1400f, 560f);
    private static readonly Vector2 PaletteSize = new Vector2(1600f, 100f);
    private static readonly Vector2 BackSize = new Vector2(400f, 56f);

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
    /// Creates the scene with the size controls, the grid area, the cube palette,
    /// and the Back button, saves it, opens it, and updates the build scene list.
    /// </summary>
    [MenuItem("KyubPon/Rebuild Sandbox Scene")]
    public static void BuildScene()
    {
        MenuUiBuilder ui = new MenuUiBuilder(SceneBuilderTools.LoadPixelFont());
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        SceneBuilderTools.CreateCamera();
        SceneBuilderTools.CreateEventSystem();

        Transform canvas = ui.CreateCanvas("SandboxCanvas");
        SandboxScreen sandboxScreen = canvas.gameObject.AddComponent<SandboxScreen>();
        SerializedObject data = new SerializedObject(sandboxScreen);

        BuildHeading(ui, canvas);
        BuildSizeControls(ui, canvas, data);
        BuildGridArea(canvas, data);
        BuildPalette(ui, canvas, data);

        UnityEngine.UI.Button back = ui.CreateTextButton(canvas, "BackButton", "BACK", ButtonFontSize);
        SceneBuilderTools.PlaceAtEdge((RectTransform)back.transform, BottomCenter, new Vector2(0f, BackOffsetFromBottom), BackSize);
        SceneBuilderTools.SetReference(data, "_backButton", back);

        // Dragged cubes are moved under the canvas itself so they draw on top of everything.
        SceneBuilderTools.SetReference(data, "_dragLayer", canvas);
        data.ApplyModifiedPropertiesWithoutUndo();

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        SceneBuilderTools.ApplyBuildScenes();
        Debug.Log($"[Info] Created {ScenePath} and updated the build scene list.");
    }

    private static void BuildHeading(MenuUiBuilder ui, Transform canvas)
    {
        UnityEngine.UI.Text heading = ui.CreateLabel(canvas, "Heading", "SANDBOX", HeadingFontSize);
        SceneBuilderTools.PlaceAtEdge(heading.rectTransform, TopCenter, new Vector2(0f, -HeadingOffsetFromTop), HeadingSize);

        UnityEngine.UI.Text note = ui.CreateLabel(canvas, "Note", "TEMPORARY TEST PAGE - DROP A CUBE OUTSIDE THE GRID TO REMOVE IT", NoteFontSize);
        SceneBuilderTools.PlaceAtEdge(note.rectTransform, TopCenter, new Vector2(0f, -NoteOffsetFromTop), NoteSize);
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

    /// <summary>An empty box in the middle of the screen. The grid cells are created inside it when the game runs.</summary>
    private static void BuildGridArea(Transform canvas, SerializedObject data)
    {
        GameObject gridArea = new GameObject("GridArea", typeof(RectTransform));
        gridArea.transform.SetParent(canvas, false);
        SceneBuilderTools.PlaceAtEdge((RectTransform)gridArea.transform, ScreenCenter, new Vector2(0f, GridOffsetFromCenter), GridAreaSize);
        SceneBuilderTools.SetReference(data, "_gridArea", gridArea.transform);
    }

    /// <summary>A row under the grid. The cubes to drag from are added to it when the game runs.</summary>
    private static void BuildPalette(MenuUiBuilder ui, Transform canvas, SerializedObject data)
    {
        Transform palette = ui.CreateRow(canvas, "Palette", RowSpacing);
        SceneBuilderTools.PlaceAtEdge((RectTransform)palette, BottomCenter, new Vector2(0f, PaletteOffsetFromBottom), PaletteSize);
        ui.CreateLabel(palette, "PaletteLabel", "DRAG A CUBE:", ControlFontSize);
        SceneBuilderTools.SetReference(data, "_palette", palette);
    }
}
