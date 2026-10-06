using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// The Home screen: the first screen the player sees.
/// It shows the game title, the main buttons, and the version number.
/// The buttons and labels live in the HomeScreen scene; this script only
/// decides what happens when they are pressed.
/// </summary>
public class HomeScreen : MonoBehaviour
{
    private const string GameTitle = "KyubPon";
    private const string SandboxSceneName = "Sandbox";

    [SerializeField] private UnityEngine.UI.Button _newGameButton;
    [SerializeField] private UnityEngine.UI.Button _continueButton;
    [SerializeField] private UnityEngine.UI.Button _settingsButton;
    [SerializeField] private UnityEngine.UI.Button _quitButton;

    // TEMPORARY: opens the sandbox test page. Remove with the Sandbox scripts.
    [SerializeField] private UnityEngine.UI.Button _sandboxButton;
    [SerializeField] private UnityEngine.UI.Text _versionLabel;
    [SerializeField] private GameObject _mainMenu;
    [SerializeField] private SettingsPanel _settingsPanel;

    private GameSettings _settings;

    private void Awake()
    {
        _settings = GameSettings.Load();
        _settings.Apply();

        DisableUnfinishedButtons();
        _settingsButton.onClick.AddListener(OpenSettings);
        _quitButton.onClick.AddListener(QuitGame);
        _sandboxButton.onClick.AddListener(OpenSandbox);
        _settingsPanel.Closed += ShowMainMenu;

        // Application.version reads "Version" from Project Settings > Player,
        // so the version number only needs to be changed in one place.
        _versionLabel.text = $"v{Application.version}";
    }

    private void Start()
    {
        Debug.Log($"[Info] {GameTitle} v{Application.version} started on {Application.platform}");
        ShowMainMenu();
    }

    /// <summary>
    /// New Game and Continue are shown so the menu already has its final shape,
    /// but they cannot be pressed until runs and saved games exist.
    /// </summary>
    private void DisableUnfinishedButtons()
    {
        _newGameButton.interactable = false;
        _continueButton.interactable = false;
    }

    private void OpenSettings()
    {
        _mainMenu.SetActive(false);
        _settingsPanel.Open(_settings);
    }

    private void OpenSandbox()
    {
        SceneManager.LoadScene(SandboxSceneName);
    }

    private void ShowMainMenu()
    {
        _mainMenu.SetActive(true);

        // Selecting a button lets keyboard and controller players move through the menu.
        EventSystem.current.SetSelectedGameObject(_settingsButton.gameObject);
    }

    private void QuitGame()
    {
        Debug.Log("[Info] Quit pressed. Closing the game.");
#if UNITY_EDITOR
        // Application.Quit does nothing inside the Editor, so stop Play mode instead.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
