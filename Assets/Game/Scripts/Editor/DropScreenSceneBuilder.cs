using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the DropScreen scene through code instead of hand-editing scene files.
/// It runs once automatically the first time the project is opened, and can be
/// run again from the menu: KyubPon > Rebuild Drop Screen Scene.
/// </summary>
[InitializeOnLoad]
public static class DropScreenSceneBuilder
{
    private const string ScenePath = "Assets/Game/Scenes/DropScreen.unity";
    private static readonly Color BackgroundColor = new Color(0.08f, 0.08f, 0.12f);

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
    /// Creates the scene with a 2D camera and the DropScreen object,
    /// saves it, opens it, and makes it the first scene in the build.
    /// </summary>
    [MenuItem("KyubPon/Rebuild Drop Screen Scene")]
    public static void BuildScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = BackgroundColor;
        cameraObject.AddComponent<AudioListener>();

        new GameObject("DropScreen").AddComponent<DropScreen>();

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        Debug.Log($"[Info] Created {ScenePath} and set it as the first build scene.");
    }
}
