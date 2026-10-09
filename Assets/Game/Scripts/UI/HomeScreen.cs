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
    private const string BattleSceneName = "Battle";

    [SerializeField] private UnityEngine.UI.Button _newGameButton;
    [SerializeField] private UnityEngine.UI.Button _continueButton;
    [SerializeField] private UnityEngine.UI.Button _settingsButton;
    [SerializeField] private UnityEngine.UI.Button _quitButton;

    [SerializeField] private UnityEngine.UI.Text _versionLabel;
    [SerializeField] private GameObject _mainMenu;
    [SerializeField] private SettingsPanel _settingsPanel;

    private GameSettings _settings;

    private void Awake()
    {
        _settings = GameSettings.Load();
        _settings.Apply();

        _newGameButton.onClick.AddListener(StartNewGame);
        _continueButton.onClick.AddListener(ContinueGame);

        // Continue only works when there is a saved battle to go back to.
        _continueButton.interactable = BattleSave.Exists;
        _settingsButton.onClick.AddListener(OpenSettings);
        _quitButton.onClick.AddListener(QuitGame);
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
    /// Throws away any saved battle and opens the battle screen, which then
    /// deals a fresh battle because it finds no save.
    /// </summary>
    private void StartNewGame()
    {
        BattleSave.Delete();
        SceneManager.LoadScene(BattleSceneName);
    }

    /// <summary>Opens the battle screen, which loads the saved battle it finds.</summary>
    private void ContinueGame()
    {
        SceneManager.LoadScene(BattleSceneName);
    }

    private void OpenSettings()
    {
        _mainMenu.SetActive(false);
        _settingsPanel.Open(_settings);
    }

    private void ShowMainMenu()
    {
        _mainMenu.SetActive(true);

        // Selecting a button lets keyboard and controller players move through the menu.
        // Start on Continue when there is a battle to continue, otherwise on New Game.
        UnityEngine.UI.Button firstButton = _continueButton.interactable ? _continueButton : _newGameButton;
        EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
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
