using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Gives a button its sounds: a soft one when the mouse moves onto it and a
/// click when it is pressed. A disabled button stays silent.
/// The scene builders add this to every menu button.
/// </summary>
[RequireComponent(typeof(UnityEngine.UI.Button))]
public class ButtonSound : MonoBehaviour, IPointerEnterHandler
{
    private UnityEngine.UI.Button _button;

    private void Awake()
    {
        _button = GetComponent<UnityEngine.UI.Button>();
        _button.onClick.AddListener(PlayClick);
    }

    /// <summary>Called by Unity when the mouse moves onto the button.</summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_button.interactable)
        {
            GameAudio.Play(GameSound.ButtonHover);
        }
    }

    private void PlayClick()
    {
        GameAudio.Play(GameSound.ButtonClick);
    }
}
