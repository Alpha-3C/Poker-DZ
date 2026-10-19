using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class CardData : MonoBehaviour
{
    public Image VisualImage;
    public Sprite cardFrontImage;
    public Sprite cardBackImage;
    public Card card;
    public Button cardButton;

    public bool IsFaceUp = true;

    private void Start()
    {
        VisualImage.sprite = CardManagement.Instance.emptySprite;
        cardBackImage = CardManagement.Instance.backcardSprite;
        UpdateButtonState(false);
    }

    public void UpdateCard(Card card, bool isFaceUp)
    {
        this.card = card;

        cardFrontImage = CardManagement.Instance.GetCardSprite(card);

        IsFaceUp = isFaceUp;

        UpdateCardVisual();

    }

    public void UpdateCardVisual()
    {
        if (CardManagement.Instance == null)
        {
            Debug.LogError("CardManager instance is null!");
            return;
        }

        VisualImage.sprite = IsFaceUp? cardFrontImage : cardBackImage;

    }
    public void UpdateButtonState(bool isActive)
    {
        cardButton.interactable = isActive;
    }
}