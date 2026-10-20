using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Slot : MonoBehaviour
{
    public GameObject card;
    public Image SlotImage;
    public Button cardButton;
    public Animator SlotAnim;

    public GameStateMachine gameStateMachine;

    public bool IsSelected;

    private void Start()
    {
        SlotImage.sprite = CardManagement.Instance.emptySprite;
        SlotAnim = GetComponent<Animator>();
        gameStateMachine = GameStateMachine.Instance;
    }

    public void UpdateSlot(Card newCard, bool isFaceUp = true)
    {
        card.GetComponent<CardData>().UpdateCard(newCard, isFaceUp);
    }

    public void UpdateButtonState(bool IsActive)
    {
        cardButton.interactable = IsActive;
    }
    public void OnCardSlotClicked()
    {
        SetSelectedState(!IsSelected);
        if (gameStateMachine.gameState == GameState.KittySelection)
        {
            GameEvents.TriggerDiscardSelected(card.GetComponent<CardData>().card, IsSelected);
        }
        else if (gameStateMachine.gameState == GameState.Playing)
        {
            // 触发事件，由 PlayPhaseUI 处理
            GameEvents.TriggerCardSelectedInPlaying(card.GetComponent<CardData>().card,gameObject, IsSelected);
        }
    }


    public void SetSelectedState(bool state)
    {
        IsSelected=state;
        SlotAnim.SetBool("IsSelected", IsSelected);
    }


}
