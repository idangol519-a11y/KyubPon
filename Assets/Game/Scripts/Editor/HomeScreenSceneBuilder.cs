using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
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
    private const string ScenePath = "Assets/Game/Scenes/HomeScreen.unity";
    private const string FontPath = "Assets/Game/Fonts/PressStart2P/PressStart2P-Regular.ttf";
    private const string FallbackFontName = "LegacyRuntime.ttf";

    // Press Start 2P is drawn on an 8-pixel grid, so sizes that are multiples of 8 stay sharp.
    private const int TitleFontSize = 96;
    private const int HeadingFontSize = 64;
    private const int ButtonFontSize = 48;
    private const int SettingFontSize = 32;
    private const int VersionFontSize = 24;

    private const float MenuSpacing = 40f;
    private const float SettingsSpacing = 56f;
    private const float RowSpacing = 32f;
    private const float TitleOffsetFromTop = 150f;
    private const float VersionMargin = 24f;
    private const float SliderWidth = 480f;
    private const float SliderHeight = 40f;
    private const float VolumeValueWidth = 128f;
    private const float ResolutionValueWidth = 352f;

    private static readonly Color BackgroundColor = new Color(0.08f, 0.08f, 0.12f);
    private static readonly Vector2 TitleSize = new Vector2(1600f, 120f);
    private static readonly Vector2 VersionSize = new Vector2(600f, 40f);

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
    /// saves it, opens it, and makes it the first scene in the build.
    /// </summary>
    [MenuItem("KyubPon/Rebuild Home Screen Scene")]
    public static void BuildScene()
    {
        MenuUiBuilder ui = new MenuUiBuilder(LoadPixelFont());
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera();
        CreateEventSystem();

        Transform canvas = ui.CreateCanvas("HomeCanvas");
        HomeScreen homeScreen = canvas.gameObject.AddComponent<HomeScreen>();
        SerializedObject homeScreenData = new SerializedObject(homeScreen);

        BuildMainMenu(ui, canvas, homeScreenData);
        SettingsPanel settingsPanel = BuildSettingsPanel(ui, canvas);
        SetReference(homeScreenData, "_settingsPanel", settingsPanel);
        SetReference(homeScreenData, "_versionLabel", CreateVersionLabel(ui, canvas));
        homeScreenData.ApplyModifiedPropertiesWithoutUndo();

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log($"[Info] Created {ScenePath} and set it as the first build scene.");
    }

    /// <summary>
    /// Loads the pixel font and turns off smoothing so its pixels stay sharp.
    /// Falls back to Unity's built-in font if the file is missing, so the menu still works.
    /// </summary>
    private static Font LoadPixelFont()
    {
        TrueTypeFontImporter importer = AssetImporter.GetAtPath(FontPath) as TrueTypeFontImporter;
        if (importer != null && importer.fontRenderingMode != FontRenderingMode.HintedRaster)
        {
            importer.fontRenderingMode = FontRenderingMode.HintedRaster;
            importer.SaveAndReimport();
        }

        Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        if (font == null)
        {
            Debug.LogWarning($"[Warn] Font not found at {FontPath}. Using Unity's built-in font instead.");
            font = Resources.GetBuiltinResource<Font>(FallbackFontName);
        }

        return font;
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = BackgroundColor;
        cameraObject.AddComponent<AudioListener>();
    }

    /// <summary>
    /// The EventSystem passes mouse, keyboard, and controller input to the UI.
    /// Without it, buttons are drawn but cannot be pressed.
    /// </summary>
    private static void CreateEventSystem()
    {
        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        // This module reads input through Unity's Input System package. With no custom
        // setup it uses Unity's default UI actions: mouse, keyboard, and controller.
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }

    private static void BuildMainMenu(MenuUiBuilder ui, Transform canvas, SerializedObject homeScreenData)
    {
        UnityEngine.UI.Text title = ui.CreateLabel(canvas, "Title", "KYUBPON", TitleFontSize);
        PlaceAtEdge(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -TitleOffsetFromTop), TitleSize);

        Transform menu = ui.CreateColumn(canvas, "MainMenu", MenuSpacing);
        SetReference(homeScreenData, "_mainMenu", menu.gameObject);
        SetReference(homeScreenData, "_newGameButton", ui.CreateTextButton(menu, "NewGameButton", "NEW GAME", ButtonFontSize));
        SetReference(homeScreenData, "_continueButton", ui.CreateTextButton(menu, "ContinueButton", "CONTINUE", ButtonFontSize));
        SetReference(homeScreenData, "_settingsButton", ui.CreateTextButton(menu, "SettingsButton", "SETTINGS", ButtonFontSize));
        SetReference(homeScreenData, "_quitButton", ui.CreateTextButton(menu, "QuitButton", "QUIT", ButtonFontSize));
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
        BuildVolumeRow(ui, panel, panelData);

        UnityEngine.UI.Button fullscreenButton = ui.CreateTextButton(panel, "FullscreenButton", "FULLSCREEN: OFF", SettingFontSize);
        SetReference(panelData, "_fullscreenButton", fullscreenButton);
        SetReference(panelData, "_fullscreenLabel", fullscreenButton.GetComponent<UnityEngine.UI.Text>());

        BuildResolutionRow(ui, panel, panelData);
        SetReference(panelData, "_backButton", ui.CreateTextButton(panel, "BackButton", "BACK", ButtonFontSize));

        panelData.ApplyModifiedPropertiesWithoutUndo();
        panel.gameObject.SetActive(false);
        return settingsPanel;
    }

    private static void BuildVolumeRow(MenuUiBuilder ui, Transform panel, SerializedObject panelData)
    {
        Transform row = ui.CreateRow(panel, "VolumeRow", RowSpacing);
        ui.CreateLabel(row, "VolumeLabel", "VOLUME", SettingFontSize);
        SetReference(panelData, "_volumeSlider", ui.CreateSlider(row, "VolumeSlider", SliderWidth, SliderHeight));

        // A fixed width stops the row from shifting when the number changes length.
        UnityEngine.UI.Text value = ui.CreateLabel(row, "VolumeValue", "100%", SettingFontSize);
        ui.SetFixedSize(value.gameObject, VolumeValueWidth, SettingFontSize);
        SetReference(panelData, "_volumeValueLabel", value);
    }

    private static void BuildResolutionRow(MenuUiBuilder ui, Transform panel, SerializedObject panelData)
    {
        Transform row = ui.CreateRow(panel, "ResolutionRow", RowSpacing);
        SetReference(panelData, "_previousResolutionButton", ui.CreateTextButton(row, "PreviousResolutionButton", "<", SettingFontSize));

        UnityEngine.UI.Text value = ui.CreateLabel(row, "ResolutionValue", "1920x1080", SettingFontSize);
        ui.SetFixedSize(value.gameObject, ResolutionValueWidth, SettingFontSize);
        SetReference(panelData, "_resolutionLabel", value);

        SetReference(panelData, "_nextResolutionButton", ui.CreateTextButton(row, "NextResolutionButton", ">", SettingFontSize));
    }

    private static UnityEngine.UI.Text CreateVersionLabel(MenuUiBuilder ui, Transform canvas)
    {
        // Version in the bottom-right corner, as the project guidelines ask.
        UnityEngine.UI.Text version = ui.CreateLabel(canvas, "VersionLabel", "v0.0.0", VersionFontSize);
        version.alignment = TextAnchor.LowerRight;
        PlaceAtEdge(version.rectTransform, new Vector2(1f, 0f), new Vector2(-VersionMargin, VersionMargin), VersionSize);
        return version;
    }

    /// <summary>
    /// Pins an object to a point on the screen edge (for example top-center or
    /// bottom-right), so it stays there at every screen size.
    /// </summary>
    private static void PlaceAtEdge(RectTransform rect, Vector2 edgePoint, Vector2 offset, Vector2 size)
    {
        rect.anchorMin = edgePoint;
        rect.anchorMax = edgePoint;
        rect.pivot = edgePoint;
        rect.sizeDelta = size;
        rect.anchoredPosition = offset;
    }

    /// <summary>
    /// Fills in a [SerializeField] slot on a script, the same as dragging an object
    /// into that slot in the Inspector.
    /// </summary>
    private static void SetReference(SerializedObject target, string fieldName, Object value)
    {
        SerializedProperty property = target.FindProperty(fieldName);
        if (property == null)
        {
            Debug.LogError($"[Error] {target.targetObject.GetType().Name} has no field named {fieldName}.");
            return;
        }

        property.objectReferenceValue = value;
    }
}
