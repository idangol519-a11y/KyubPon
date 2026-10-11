using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// One block on sale in the shop: its picture, its name, and a price button.
/// Pressing the price buys the block. Resting the mouse on the slot shows the
/// block's rules, the same as for a block in the hand.
/// </summary>
public class BattleShopSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private const string SoldText = "SOLD";

    // How see-through a sold block's picture is.
    private const float SoldAlpha = 0.25f;

    [SerializeField] private UnityEngine.UI.Image _picture;
    [SerializeField] private UnityEngine.UI.Text _nameLabel;
    [SerializeField] private UnityEngine.UI.Button _priceButton;
    [SerializeField] private UnityEngine.UI.Text _priceLabel;

    private BattleScreen _screen;
    private BattleCubeKind _kind;
    private int _index;

    /// <summary>Connects the slot to the screen. "index" is which shop slot this is, counting from 0.</summary>
    public void Setup(BattleScreen screen, int index)
    {
        _screen = screen;
        _index = index;
        _priceButton.onClick.AddListener(Buy);
    }

    /// <summary>
    /// Draws the block on sale. A sold block is faded and its button reads "SOLD".
    /// The button cannot be pressed when the block is sold or the player cannot pay for it.
    /// </summary>
    public void Show(BattleCubeKind kind, int price, bool isSold, bool canAfford)
    {
        _kind = kind;
        kind.ApplyLook(_picture, 0f);
        _picture.canvasRenderer.SetAlpha(isSold ? SoldAlpha : 1f);
        _nameLabel.text = kind.Name;
        _priceLabel.text = isSold ? SoldText : $"${price}";
        _priceButton.interactable = !isSold && canAfford;
    }

    /// <summary>Called by Unity when the mouse moves onto the slot. Starts the wait for the rules tooltip.</summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_kind != null)
        {
            _screen.StartHover(_kind, null);
        }
    }

    /// <summary>Called by Unity when the mouse leaves the slot.</summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        _screen.EndHover(_kind, null);
    }

    private void Buy()
    {
        _screen.BuyShopOffer(_index);
    }
}
