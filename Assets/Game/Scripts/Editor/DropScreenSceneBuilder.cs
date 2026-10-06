using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the DropScreen (title screen) scene through code instead of hand-editing
/// scene files. It runs once automatically when the scene file is missing, and can
/// be run again from the menu: KyubPon > Rebuild Drop Screen Scene.
/// Rebuilding replaces the scene, so change the layout here rather than in the Editor.
/// </summary>
[InitializeOnLoad]
public static class DropScreenSceneBuilder
{
    private const string ScenePath = SceneBuilderTools.DropScreenScenePath;

    // Press Start 2P is drawn on an 8-pixel grid, so sizes that are multiples of 8 stay sharp.
    private const int TitleFontSize = 128;
    private const int PromptFontSize = 32;
    private const float PromptOffsetFromBottom = 260f;

    private static readonly Vector2 ScreenCenter = new Vector2(0.5f, 0.5f);
    private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
    private static readonly Vector2 TitleSize = new Vector2(1800f, 160f);
    private static readonly Vector2 PromptSize = new Vector2(1600f, 48f);

    static DropScreenSceneBuilder()
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
    /// Creates the scene with a camera, the big title, the "press any key" prompt,
    /// and the version label, saves it, opens it, and updates the build scene list.
    /// </summary>
    [MenuItem("KyubPon/Rebuild Drop Screen Scene")]
    public static void BuildScene()
    {
        MenuUiBuilder ui = new MenuUiBuilder(SceneBuilderTools.LoadPixelFont());
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        SceneBuilderTools.CreateCamera();

        Transform canvas = ui.CreateCanvas("DropCanvas");
        DropScreen dropScreen = canvas.gameObject.AddComponent<DropScreen>();
        SerializedObject dropScreenData = new SerializedObject(dropScreen);

        UnityEngine.UI.Text title = ui.CreateLabel(canvas, "Title", "KYUBPON", TitleFontSize);
        SceneBuilderTools.PlaceAtEdge(title.rectTransform, ScreenCenter, Vector2.zero, TitleSize);

        UnityEngine.UI.Text prompt = ui.CreateLabel(canvas, "Prompt", "PRESS ANY KEY", PromptFontSize);
        SceneBuilderTools.PlaceAtEdge(prompt.rectTransform, BottomCenter, new Vector2(0f, PromptOffsetFromBottom), PromptSize);

        SceneBuilderTools.SetReference(dropScreenData, "_prompt", prompt);
        SceneBuilderTools.SetReference(dropScreenData, "_versionLabel", SceneBuilderTools.CreateVersionLabel(ui, canvas));
        dropScreenData.ApplyModifiedPropertiesWithoutUndo();

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        SceneBuilderTools.ApplyBuildScenes();
        Debug.Log($"[Info] Created {ScenePath} and updated the build scene list.");
    }
}
