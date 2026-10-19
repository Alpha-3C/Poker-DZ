using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class PlayPhaseUI : MonoBehaviour
{
    private GameStateMachine gameStateMachine;
    private GameFlowController gameFlowController;
    private CardManagement cardManager;
    private PlayPhaseManagement playPhaseManager;
    private PlayerManagement playerManager;

    //public List<DealAreaBinder> dealAreas;
    public List<DeskUI> deskUIs;

    public RectTransform arrow;  // currentPlayerIndicator
    public Vector3[] arrowPositions;      // 四个座位对应的箭头位置（需手动设置）

    public Button confirmButton;
    public TMP_Text earningPointText;

    // 当前选中的卡牌（仅用于人类玩家）
    private Card currentSelectedCard;
    private Slot currentSelectedSlot;
  
    private void OnEnable()
    {
        GameEvents.OnCardSelectedInPlaying += HandleCardSelectedInPlaying;
    }
    private void OnDisable()
    {
        GameEvents.OnCardSelectedInPlaying -= HandleCardSelectedInPlaying;
    }

    // 事件回调：当 Playing 阶段卡牌被选中/取消选中时
    private void HandleCardSelectedInPlaying(Card card, GameObject slotObj, bool isSelected)
    {
        if (gameStateMachine.gameState != GameState.Playing) return;

        Slot slot = slotObj.GetComponent<Slot>();
        if (slot == null) return;

        if (isSelected)
        {
            // 清除之前选中的高亮
            if (currentSelectedSlot != null)
            {
                currentSelectedSlot.SetSelectedState(false);
            }
            currentSelectedSlot = slot;
            currentSelectedCard = card;
            if (confirmButton != null) confirmButton.gameObject.SetActive(true);
        }
        else
        {
            if (currentSelectedSlot == slot)
            {
                currentSelectedSlot = null;
                currentSelectedCard = null;
                if (confirmButton != null) confirmButton.gameObject.SetActive(false);
            }
        }
    }
    private void Start()
    {
        gameStateMachine = GameStateMachine.Instance;
        gameFlowController = GameFlowController.Instance;
        playerManager = PlayerManagement.Instance;
        cardManager = CardManagement.Instance;
        playPhaseManager = GetComponent<PlayPhaseManagement>();

        InitializePlayPhaseUI();
    }

    public void InitializePlayPhaseUI()
    {
        if(confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmButtonClick);
            confirmButton.gameObject.SetActive(false);
        }
        //dealAreas = playerManager.dealAreas;
        deskUIs = playerManager.deskUIs;

        currentSelectedCard = null;
        currentSelectedSlot = null;

        ActivateArrow(false);

        UpdateEarnPointText(playPhaseManager.currentEarnedPoint);
    }
    public void DeactivatePlayPhaseUI()
    {
        if (confirmButton != null)
        {
            //confirmButton.onClick.RemoveAllListeners();
            confirmButton.gameObject.SetActive(false);
        }
        
        foreach(DeskUI deskUI in deskUIs)
        {
            deskUI.DealCardImage.sprite = cardManager.emptySprite;
        }

        ActivateArrow(false);
    }

    // ---------- 外部调用接口（供 PlayPhaseManagement 使用） ----------
    public void StartHumanTurn(CardSuit? leadSuit, CardSuit? trumpSuit)
    {
        EnableHandCards(playPhaseManager.currentPlayerIndex ,true, leadSuit, trumpSuit);
        currentSelectedCard = null;
        if (confirmButton != null) confirmButton.gameObject.SetActive(false);
    }

    public void EndHumanTurn()
    {
        EnableHandCards(playPhaseManager.currentPlayerIndex, false, null, null);
        if (confirmButton != null) confirmButton.gameObject.SetActive(false);
        currentSelectedCard = null;
    }

    public void UpdateDealArea(int playerIndex, Card card)
    {
        if (playerIndex < 0 || playerIndex >= deskUIs.Count) return;
        if (deskUIs[playerIndex] != null)
            deskUIs[playerIndex].SetDealCard(card, cardManager.GetCardSprite(card));
    }

    public void UpdateEarnPointText(int point)
    {
        earningPointText.text = "Earning Point:" + point;
    }

    public void ClearAllDealAreas()
    {
        foreach (var deskUI in deskUIs)
            deskUI?.ClearDealCard();
    }

    public void ActivateArrow(bool isActive)
    {
        arrow.gameObject.SetActive(isActive);
    }

    public void SetArrowPos(int playerIndex)
    {
        if(playerIndex >= 0 &&  playerIndex < arrowPositions.Length)
        {
            Debug.Log(arrowPositions[playerIndex]);
            arrow.anchoredPosition = arrowPositions[playerIndex];
        }
    }

    // ---------- 内部辅助方法 ----------
    private void EnableHandCards(int playerIndex, bool enable, CardSuit? leadSuit, CardSuit? trumpSuit)
    {
        // 确定是人类玩家
        if (playerManager.players[playerIndex].IsAI) return;

        List<Card> handCards = playerManager.players[playerIndex].HandCards;
        int cardCount = handCards.Count;
        Debug.Log($"??;cardCount:{cardCount}");

        // 创建长度为 cardCount 的 List<bool>，默认全 false
        List<bool> enables = new List<bool>(new bool[cardCount]);

        if (enable)
        {
            // 根据出牌规则逐张判断是否可出
            for (int i = 0; i < cardCount; i++)
            {
                Card card = handCards[i];
                enables[i] = IsValidCardToPlay(card, leadSuit, trumpSuit);
            }
        }
        // else 保持全 false，即全部禁用

        // 统一更新所有卡牌按钮的启用状态
        GameEvents.TriggerSlotButtonsEnabled(playerIndex, enables);
    }

    private bool IsValidCardToPlay(Card card, CardSuit? leadSuit, CardSuit? trumpSuit)
    {
        if (leadSuit == null) return true;
        int currentPlayerIndex = playPhaseManager.currentPlayerIndex;
        if (playerManager.players[currentPlayerIndex].IsAI) return false;
        return CardManagement.CanDealCard(playerManager.players[currentPlayerIndex].HandCards, leadSuit.Value, card);
    }


    private void OnConfirmButtonClick()
    {
        if (currentSelectedCard != null && gameStateMachine.gameState == GameState.Playing)
        {
            playPhaseManager?.OnHumanCardSelected(currentSelectedCard);
            if (confirmButton != null) confirmButton.gameObject.SetActive(false);
        }
    }

}