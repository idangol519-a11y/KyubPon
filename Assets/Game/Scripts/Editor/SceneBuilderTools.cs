using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Things every scene builder needs: the pixel font, the camera, the version
/// label, and the list of scenes that go into the built game.
/// Keeping them here means all scenes share one look and one scene order.
/// </summary>
public static class SceneBuilderTools
{
    /// <summary>The title screen: the first thing shown when the game starts.</summary>
    public const string DropScreenScenePath = "Assets/Game/Scenes/DropScreen.unity";

    /// <summary>The Home screen with the main menu and settings.</summary>
    public const string HomeScreenScenePath = "Assets/Game/Scenes/HomeScreen.unity";

    /// <summary>TEMPORARY: the sandbox test page. Remove together with the Sandbox scripts.</summary>
    public const string SandboxScenePath = "Assets/Game/Scenes/Sandbox.unity";

    private const string FontPath = "Assets/Game/Fonts/PressStart2P/PressStart2P-Regular.ttf";
    private const string FallbackFontName = "LegacyRuntime.ttf";
    private const int VersionFontSize = 24;
    private const float VersionMargin = 24f;

    private static readonly Color BackgroundColor = new Color(0.08f, 0.08f, 0.12f);
    private static readonly Vector2 VersionSize = new Vector2(600f, 40f);

    /// <summary>
    /// Sets which scenes are included in the built game, in play order.
    /// The first scene in the list is the one the game starts on.
    /// </summary>
    public static void ApplyBuildScenes()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(DropScreenScenePath, true),
            new EditorBuildSettingsScene(HomeScreenScenePath, true),
            new EditorBuildSettingsScene(SandboxScenePath, true)
        };
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Loads the pixel font and turns off smoothing so its pixels stay sharp.
    /// Falls back to Unity's built-in font if the file is missing, so screens still work.
    /// </summary>
    public static Font LoadPixelFont()
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

    /// <summary>Creates the 2D camera with the game's dark background color.</summary>
    public static void CreateCamera()
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
    public static void CreateEventSystem()
    {
        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        // This module reads input through Unity's Input System package. With no custom
        // setup it uses Unity's default UI actions: mouse, keyboard, and controller.
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }

    /// <summary>
    /// Creates the version text in the bottom-right corner, as the project guidelines ask.
    /// The real number is filled in by the screen's script when the game runs.
    /// </summary>
    public static UnityEngine.UI.Text CreateVersionLabel(MenuUiBuilder ui, Transform canvas)
    {
        UnityEngine.UI.Text version = ui.CreateLabel(canvas, "VersionLabel", "v0.0.0", VersionFontSize);
        version.alignment = TextAnchor.LowerRight;
        PlaceAtEdge(version.rectTransform, new Vector2(1f, 0f), new Vector2(-VersionMargin, VersionMargin), VersionSize);
        return version;
    }

    /// <summary>
    /// Pins an object to a point on the screen (for example top-center or
    /// bottom-right), so it stays there at every screen size.
    /// </summary>
    public static void PlaceAtEdge(RectTransform rect, Vector2 edgePoint, Vector2 offset, Vector2 size)
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
    public static void SetReference(SerializedObject target, string fieldName, Object value)
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
