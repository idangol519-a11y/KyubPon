using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the HomeScreen scene through code instead of hand-editing scene files.
/// It runs once automatically when the scene file is missing, and can be run again
/// from the menu: KyubPon > Rebuild Home Screen Scene.
/// Rebuilding replaces the scene, so change the layout here rather than in the Editor.
/// </summary>
[InitializeOnLoad]
public static class HomeScreenSceneBuilder
{
    private const string ScenePath = SceneBuilderTools.HomeScreenScenePath;

    // Press Start 2P is drawn on an 8-pixel grid, so sizes that are multiples of 8 stay sharp.
    private const int TitleFontSize = 96;
    private const int HeadingFontSize = 64;
    private const int ButtonFontSize = 48;
    private const int SettingFontSize = 32;

    private const float MenuSpacing = 40f;
    private const float SettingsSpacing = 40f;
    private const float RowSpacing = 32f;
    private const float TitleOffsetFromTop = 150f;
    private const float SliderWidth = 480f;
    private const float SliderHeight = 40f;
    private const float VolumeValueWidth = 128f;
    private const float VolumeLabelWidth = 192f;
    private const float ResolutionValueWidth = 352f;

    private static readonly Vector2 TitleSize = new Vector2(1600f, 120f);

    static HomeScreenSceneBuilder()
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
    /// Creates the scene with a camera, the main menu, and the settings panel,
    /// saves it, opens it, and updates the list of scenes in the build.
    /// </summary>
    [MenuItem("KyubPon/Rebuild Home Screen Scene")]
    public static void BuildScene()
    {
        MenuUiBuilder ui = new MenuUiBuilder(SceneBuilderTools.LoadPixelFont());
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        SceneBuilderTools.CreateCamera();
        SceneBuilderTools.CreateEventSystem();

        Transform canvas = ui.CreateCanvas("HomeCanvas");
        HomeScreen homeScreen = canvas.gameObject.AddComponent<HomeScreen>();
        SerializedObject homeScreenData = new SerializedObject(homeScreen);

        BuildMainMenu(ui, canvas, homeScreenData);
        SettingsPanel settingsPanel = BuildSettingsPanel(ui, canvas);
        SceneBuilderTools.SetReference(homeScreenData, "_settingsPanel", settingsPanel);
        SceneBuilderTools.SetReference(homeScreenData, "_versionLabel", SceneBuilderTools.CreateVersionLabel(ui, canvas));
        homeScreenData.ApplyModifiedPropertiesWithoutUndo();

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        SceneBuilderTools.ApplyBuildScenes();
        Debug.Log($"[Info] Created {ScenePath} and updated the build scene list.");
    }


    private static void BuildMainMenu(MenuUiBuilder ui, Transform canvas, SerializedObject homeScreenData)
    {
        UnityEngine.UI.Text title = ui.CreateLabel(canvas, "Title", "KYUBPON", TitleFontSize);
        SceneBuilderTools.PlaceAtEdge(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -TitleOffsetFromTop), TitleSize);

        Transform menu = ui.CreateColumn(canvas, "MainMenu", MenuSpacing);
        SceneBuilderTools.SetReference(homeScreenData, "_mainMenu", menu.gameObject);
        SceneBuilderTools.SetReference(homeScreenData, "_newGameButton", ui.CreateTextButton(menu, "NewGameButton", "NEW GAME", ButtonFontSize));
        SceneBuilderTools.SetReference(homeScreenData, "_continueButton", ui.CreateTextButton(menu, "ContinueButton", "CONTINUE", ButtonFontSize));
        SceneBuilderTools.SetReference(homeScreenData, "_settingsButton", ui.CreateTextButton(menu, "SettingsButton", "SETTINGS", ButtonFontSize));
        // TEMPORARY: the Sandbox button is a test feature and will be removed.
        SceneBuilderTools.SetReference(homeScreenData, "_sandboxButton", ui.CreateTextButton(menu, "SandboxButton", "SANDBOX", ButtonFontSize));
        SceneBuilderTools.SetReference(homeScreenData, "_quitButton", ui.CreateTextButton(menu, "QuitButton", "QUIT", ButtonFontSize));
    }

    /// <summary>
    /// Builds the settings page. It starts hidden; the Home screen shows it
    /// when the Settings button is pressed.
    /// </summary>
    private static SettingsPanel BuildSettingsPanel(MenuUiBuilder ui, Transform canvas)
    {
        Transform panel = ui.CreateColumn(canvas, "SettingsPanel", SettingsSpacing);
        SettingsPanel settingsPanel = panel.gameObject.AddComponent<SettingsPanel>();
        SerializedObject panelData = new SerializedObject(settingsPanel);

        ui.CreateLabel(panel, "Heading", "SETTINGS", HeadingFontSize);
        BuildVolumeRow(ui, panel, panelData, "Master", "MASTER", "_masterSlider", "_masterValueLabel");
        BuildVolumeRow(ui, panel, panelData, "Music", "MUSIC", "_musicSlider", "_musicValueLabel");
        BuildVolumeRow(ui, panel, panelData, "Sound", "SFX", "_soundSlider", "_soundValueLabel");

        UnityEngine.UI.Button fullscreenButton = ui.CreateTextButton(panel, "FullscreenButton", "FULLSCREEN: OFF", SettingFontSize);
        SceneBuilderTools.SetReference(panelData, "_fullscreenButton", fullscreenButton);
        SceneBuilderTools.SetReference(panelData, "_fullscreenLabel", fullscreenButton.GetComponent<UnityEngine.UI.Text>());

        BuildResolutionRow(ui, panel, panelData);
        SceneBuilderTools.SetReference(panelData, "_backButton", ui.CreateTextButton(panel, "BackButton", "BACK", ButtonFontSize));

        panelData.ApplyModifiedPropertiesWithoutUndo();
        panel.gameObject.SetActive(false);
        return settingsPanel;
    }

    /// <summary>Builds one volume line: a name, a slider, and the value as a percentage.</summary>
    private static void BuildVolumeRow(MenuUiBuilder ui, Transform panel, SerializedObject panelData,
        string objectName, string caption, string sliderField, string valueField)
    {
        Transform row = ui.CreateRow(panel, objectName + "VolumeRow", RowSpacing);

        // The three names have different lengths. A fixed width keeps the sliders lined up.
        UnityEngine.UI.Text label = ui.CreateLabel(row, objectName + "VolumeLabel", caption, SettingFontSize);
        ui.SetFixedSize(label.gameObject, VolumeLabelWidth, SettingFontSize);

        SceneBuilderTools.SetReference(panelData, sliderField, ui.CreateSlider(row, objectName + "VolumeSlider", SliderWidth, SliderHeight));

        // A fixed width stops the row from shifting when the number changes length.
        UnityEngine.UI.Text value = ui.CreateLabel(row, objectName + "VolumeValue", "100%", SettingFontSize);
        ui.SetFixedSize(value.gameObject, VolumeValueWidth, SettingFontSize);
        SceneBuilderTools.SetReference(panelData, valueField, value);
    }

    private static void BuildResolutionRow(MenuUiBuilder ui, Transform panel, SerializedObject panelData)
    {
        Transform row = ui.CreateRow(panel, "ResolutionRow", RowSpacing);
        SceneBuilderTools.SetReference(panelData, "_previousResolutionButton", ui.CreateTextButton(row, "PreviousResolutionButton", "<", SettingFontSize));

        UnityEngine.UI.Text value = ui.CreateLabel(row, "ResolutionValue", "1920x1080", SettingFontSize);
        ui.SetFixedSize(value.gameObject, ResolutionValueWidth, SettingFontSize);
        SceneBuilderTools.SetReference(panelData, "_resolutionLabel", value);

        SceneBuilderTools.SetReference(panelData, "_nextResolutionButton", ui.CreateTextButton(row, "NextResolutionButton", ">", SettingFontSize));
    }
}
