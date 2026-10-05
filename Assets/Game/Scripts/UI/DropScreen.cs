using UnityEngine;

/// <summary>
/// The blank landing screen shown when the game starts.
/// For now it only shows the game name and the version number,
/// so we can confirm the build runs and which version it is.
/// </summary>
public class DropScreen : MonoBehaviour
{
    private const string GameTitle = "KyubPon";
    private const int TitleFontSize = 64;
    private const int VersionFontSize = 18;
    private const float VersionMargin = 12f;

    private GUIStyle _titleStyle;
    private GUIStyle _versionStyle;

    private void Start()
    {
        // Application.version reads "Version" from Project Settings > Player,
        // so the version number only needs to be changed in one place.
        Debug.Log($"[Info] {GameTitle} v{Application.version} started on {Application.platform}");
    }

    private void OnGUI()
    {
        CreateStylesIfNeeded();

        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), GameTitle, _titleStyle);

        // Version in the bottom-right corner, as the project guidelines ask.
        Rect versionArea = new Rect(0, 0, Screen.width - VersionMargin, Screen.height - VersionMargin);
        GUI.Label(versionArea, $"v{Application.version}", _versionStyle);
    }

    /// <summary>
    /// GUI styles can only be created inside OnGUI, so we build them once on first use.
    /// </summary>
    private void CreateStylesIfNeeded()
    {
        if (_titleStyle != null)
        {
            return;
        }

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = TitleFontSize,
            fontStyle = FontStyle.Bold
        };
        _titleStyle.normal.textColor = Color.white;

        _versionStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.LowerRight,
            fontSize = VersionFontSize
        };
        _versionStyle.normal.textColor = new Color(1f, 1f, 1f, 0.6f);
    }
}
