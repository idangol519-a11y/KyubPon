using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// The title screen: the first screen shown when the game starts.
/// It shows the game name and waits for the player to press any key,
/// mouse button, or controller button, then opens the Home screen.
/// </summary>
public class DropScreen : MonoBehaviour
{
    private const string GameTitle = "KyubPon";
    private const string HomeScreenSceneName = "HomeScreen";
    private const float PromptBlinkSeconds = 0.6f;

    [SerializeField] private UnityEngine.UI.Text _prompt;
    [SerializeField] private UnityEngine.UI.Text _versionLabel;

    private bool _isLeaving;

    private void Start()
    {
        // Application.version reads "Version" from Project Settings > Player,
        // so the version number only needs to be changed in one place.
        _versionLabel.text = $"v{Application.version}";
        Debug.Log($"[Info] {GameTitle} v{Application.version} started on {Application.platform}");
    }

    private void Update()
    {
        BlinkPrompt();

        if (!_isLeaving && AnyButtonPressed())
        {
            // Loading takes a moment; this stops a second press from loading twice.
            _isLeaving = true;
            SceneManager.LoadScene(HomeScreenSceneName);
        }
    }

    /// <summary>Shows and hides the prompt in a steady rhythm so it catches the eye.</summary>
    private void BlinkPrompt()
    {
        bool visible = Mathf.FloorToInt(Time.unscaledTime / PromptBlinkSeconds) % 2 == 0;
        if (_prompt.enabled != visible)
        {
            _prompt.enabled = visible;
        }
    }

    /// <summary>
    /// True on the frame the player presses a keyboard key, a mouse button,
    /// a controller button, or touches the screen. A device is null when it
    /// is not connected, so each one is checked first.
    /// </summary>
    private static bool AnyButtonPressed()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
        {
            return true;
        }

        Mouse mouse = Mouse.current;
        if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame))
        {
            return true;
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null && (gamepad.buttonSouth.wasPressedThisFrame || gamepad.startButton.wasPressedThisFrame))
        {
            return true;
        }

        Touchscreen touchscreen = Touchscreen.current;
        return touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame;
    }
}
