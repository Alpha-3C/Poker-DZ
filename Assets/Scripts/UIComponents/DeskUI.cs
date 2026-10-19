using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeskUI : MonoBehaviour
{
    [Header("玩家信息")]
    public int playerIndex;      // 对应的玩家索引
    public PlayerPosition position;     //预设位置索引
    public PlayerData playerData;

    [Header("UI组件")]
    public Image DealCardImage;      // 用于显示卡牌图片的 Image
    public Image TeamIcon;      // 用于指示玩家阵营
    public List<Slot> slots;
    public Image ReadyIndicator;

    public Sprite HostSprite;
    public Sprite ClientSprite;

    private PlayerManagement playerManager;
    private GameFlowController gameFlowController;

    private void OnEnable()
    {
        GameEvents.OnSlotCardsChanged += HandleSlotCardsChanged;
        GameEvents.OnSlotButtonsActivated += HandleSlotButtonsActivated;
        GameEvents.OnSlotButtonsEnabled += HandleSlotButtonsEnabled;
        GameEvents.OnDiscardsCleared += HandleDiscardsCleared;
    }

    private void OnDisable()
    {
        GameEvents.OnSlotCardsChanged -= HandleSlotCardsChanged;
        GameEvents.OnSlotButtonsActivated -= HandleSlotButtonsActivated;
        GameEvents.OnSlotButtonsEnabled -= HandleSlotButtonsEnabled;
        GameEvents.OnDiscardsCleared -= HandleDiscardsCleared;
    }

    private void Start()
    {
        playerManager = PlayerManagement.Instance;
        gameFlowController = GameFlowController.Instance;
        playerData = GetComponent<PlayerData>();
    }

    private void HandleSlotCardsChanged(int targetIndex)
    {
        if (!ShouldUpdateForSeat(targetIndex)) return;
        UpdateAllSlots(ShouldShowFaceUp());
    }

    private void HandleSlotButtonsActivated(int targetSeat, bool active)
    {
        if (!ShouldUpdateForSeat(targetSeat)) return;
        UpdateAllSlotButtons(active);
    }

    private void HandleSlotButtonsEnabled(int targetSeat, List<bool> enables)
    {
        if (!ShouldUpdateForSeat(targetSeat)) return;
        UpdateAllSlotButtons(enables);
    }

    private void HandleDiscardsCleared(int targetSeat)
    {
        if (!ShouldUpdateForSeat(targetSeat)) return;
        ClearAllSlotSelections();
    }

    // ---------- UI 更新 ----------

    public void SetDealCard(Card card, Sprite cardSprite)
    {
        if (DealCardImage != null)
        {
            DealCardImage.sprite = cardSprite;
            DealCardImage.gameObject.SetActive(true);
        }
    }

    public void ClearDealCard()
    {
        if (DealCardImage != null)
        {
            DealCardImage.sprite = null;
            DealCardImage.gameObject.SetActive(false);
        }
    }

    public void SetTeamIcon(bool IsHost)
    {
        if (TeamIcon != null)
        {
            TeamIcon.sprite = IsHost ? HostSprite : ClientSprite;
            TeamIcon.gameObject.SetActive(true);
        }

    }

    public void ClearTeamIcon()
    {
        if (TeamIcon != null)
        {
            TeamIcon.sprite = null;
            TeamIcon.gameObject.SetActive(false);
        }
    }

    public void SetReadyIndicator(bool IsReady)
    {
        if(ReadyIndicator != null)
        {
            ReadyIndicator.gameObject.SetActive(IsReady);
        }
    }
    public void UpdatePlayerDesk()
    {
        playerIndex = GetCurrentPlayerIndex();
        playerData.UpdatePlayer(playerIndex);
    }

    private void UpdateAllSlots(bool isFaceUp)
    {
        List<Card> hand = GetHandCards();
        int cardCount = hand.Count;
        Debug.Log($"更新座位 {position} 的手牌，数量: {cardCount}");
        for (int i = 0; i < slots.Count; i++)
        {
            if (i < cardCount)
            {
                slots[i].UpdateSlot(hand[i], isFaceUp);
                slots[i].SetSelectedState(false);
                slots[i].gameObject.SetActive(true);
            }
            else
            {
                slots[i].gameObject.SetActive(false);
            }
        }
    }

    private void UpdateAllSlotButtons(bool isActive)
    {
        List<Card> hand = GetHandCards();
        for (int i = 0; i < slots.Count; i++)
        {
            if (i < hand.Count)
                slots[i].UpdateButtonState(isActive);
            else
                slots[i].UpdateButtonState(false);
        }
    }

    private void UpdateAllSlotButtons(List<bool> enables)
    {
        List<Card> hand = GetHandCards();
        if (hand.Count != enables.Count)
        {
            Debug.LogError($"SlotsManagement 座位 {position} 收到 Enable 列表长度 ({enables.Count}) 与手牌数量 ({hand.Count}) 不符");
            return;
        }
        for (int i = 0; i < slots.Count; i++)
        {
            if (i < hand.Count)
                slots[i].UpdateButtonState(enables[i]);
            else
                slots[i].UpdateButtonState(false);
        }
    }

    private void ClearAllSlotSelections()
    {
        int idx = GetCurrentPlayerIndex();
        if (idx < 0 || idx >= playerManager.players.Count) return;
        List<Card> hand = playerManager.players[idx].HandCards;
        for (int i = 0; i < slots.Count && i < hand.Count; i++)
        {
            slots[i].SetSelectedState(false);
        }
    }


    private bool ShouldUpdateForSeat(int targetIndex)
    {
        if (targetIndex == gameFlowController.allSeatIndexSign) return true;          // 表示所有座位

        return GetCurrentPlayerIndex() == targetIndex;
    }

    // 获取当前座位的玩家索引（不直接返回 Player 对象）
    private int GetCurrentPlayerIndex()
    {
        return playerManager.GetPlayerIndexAtPosition(position);
    }

    private bool ShouldShowFaceUp()
    {
        int idx = GetCurrentPlayerIndex();
        if (idx < 0 || idx >= playerManager.players.Count) return false;
        return !playerManager.players[idx].IsAI;
    }

    private List<Card> GetHandCards()
    {
        int idx = GetCurrentPlayerIndex();
        if (idx < 0 || idx >= playerManager.players.Count)
            return new List<Card>();
        return playerManager.players[idx].HandCards;
    }
}
