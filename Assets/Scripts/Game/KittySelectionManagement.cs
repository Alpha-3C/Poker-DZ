/*using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class KittySelectionManagement : MonoBehaviour
{
    private PlayerManagement playerManager;
    private GameFlowController gameFlowController;
    private GameStateMachine gameStateMachine;
    private CardManagement cardManager;
    private CardSelectionUI cardSelectionUI;

    public List<Card> Kittys;
    public List<Card> Discards;
    public int KittyOwnerIndex;

    private void OnEnable()
    {
        GameStateMachine.OnGameStateChanged += HandleGameStateChanged;
        GameStateMachine.OnDiscardSelected += HandleDiscardSelected;
    }

    private void OnDisable()
    {
        GameStateMachine.OnGameStateChanged -= HandleGameStateChanged;
        GameStateMachine.OnDiscardSelected -= HandleDiscardSelected;
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (newState != GameState.KittySelection) return;

        StartKittySelectionPhase();
    }

    private void HandleDiscardSelected(Card card, bool IsSelected)
    {
        if (IsSelected)
        {
            Discards.Add(card);
        }
        else
        {
            Discards.Remove(card);
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        playerManager = PlayerManagement.Instance;
        gameFlowController = GameFlowController.Instance;
        gameStateMachine = GameStateMachine.Instance;
        cardManager = CardManagement.Instance;
        cardSelectionUI = GetComponent<CardSelectionUI>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void StartKittySelectionPhase()
    {
        Kittys = cardManager.KittyCards;
        Discards = new List<Card>();
        gameFlowController.TrumpSuit = CardSuit.RedJoker;
        KittyOwnerIndex = gameFlowController.HostPlayerIndex;

        AddKittycardsToHand();
        playerManager.SortPlayerHandCards(KittyOwnerIndex, null);

        OnTrumpSuitSelection();
        //OnKittySelection();

    }

    private void OnTrumpSuitSelection()
    {
        if (playerManager.players[KittyOwnerIndex].IsAI)
        {
            //随机选花色等，暂不考虑。
        }
        else
        {
            cardSelectionUI.ActivateSuitsController();
        }
    }

    public  void EndTrumpSuitSelection()
    {
        cardSelectionUI?.DeactivateSuitsController();
        Debug.Log("End trump suit selection, start kitty selection.");
        //Waitforseconds
        OnKittySelection();

    }

    private void OnKittySelection()
    {
        if (playerManager.players[KittyOwnerIndex].IsAI)
        {
              //随机选牌等，暂不考虑。
        }
        else
        {
            gameStateMachine.SlotButtonsUpdate(KittyOwnerIndex, true);
            cardSelectionUI.ActivateSelectionUI();
        }


    }

    public void EndKittySelection()
    {
        RemoveDiscardsFromHand();
        cardSelectionUI?.DeactivateSelectionUI();
        gameStateMachine.SlotButtonsUpdate(KittyOwnerIndex, false);

        gameFlowController.EndKittySelection();
    }

    public void ClearDiscards()
    {
        Discards.Clear();
        gameStateMachine.ClearDiscard(KittyOwnerIndex);
    }


    private void AddKittycardsToHand()
    {
        foreach (Card kittycard in Kittys)
        {
            playerManager.players[KittyOwnerIndex].AddCard(kittycard);
        }

        gameStateMachine.SlotCardsUpdate(KittyOwnerIndex);
    }

    private void RemoveDiscardsFromHand()
    {
        foreach (Card discard in Discards)
        {
            playerManager.players[KittyOwnerIndex].RemoveCard(discard);
        }
        playerManager.SortPlayerHandCards(KittyOwnerIndex, gameFlowController.TrumpSuit);
        gameStateMachine.SlotCardsUpdate(KittyOwnerIndex);
    }

    public void SetTrumpSuit(CardSuit TrumpSuit)
    {
        gameFlowController.TrumpSuit = TrumpSuit;
        Debug.Log($"Trump suit is selected as {TrumpSuit}.");
        playerManager.SortPlayerHandCards(gameFlowController.allPlayerIndexSign, TrumpSuit);
        gameStateMachine.SlotCardsUpdate(gameFlowController.allPlayerIndexSign);
    }


}*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AI;

public class KittySelectionManagement : MonoBehaviour
{
    private GameFlowController gameFlowController;
    private GameStateMachine gameStateMachine;
    private PlayerManagement playerManager;
    private CardManagement cardManager;
    private CardSelectionUI cardSelectionUI;
    private KittySlotsUI kittySlotsUI;

    public List<Card> Kittys;      // 底牌
    public List<Card> Discards;    // 庄家要弃的牌
    public int KittyOwnerIndex;

    private bool isSelectingDiscards = false;   // 新增标志

    // AI 配置
    public float aiThinkTime = 1.2f;

    private void OnEnable()
    {
        GameEvents.OnDiscardSelected += HandleDiscardSelected;
    }

    private void OnDisable()
    {
        GameEvents.OnDiscardSelected -= HandleDiscardSelected;
    }

    private void HandleDiscardSelected(Card card, bool isSelected)
    {
        // 仅在弃牌选择阶段处理
        if (!isSelectingDiscards) return;
        // 可选：检查当前玩家是否为庄家（防止其他玩家的选择干扰）
        // 由于只有庄家手牌被激活，实际上只有庄家能点击，直接处理即可
        if (isSelected)
        {
            if (!Discards.Contains(card))
                Discards.Add(card);
        }
        else
        {
            Discards.Remove(card);
        }
        Debug.Log($"当前已选择 {Discards.Count} 张弃牌");
    }
    private void Start()
    {
        gameFlowController = GameFlowController.Instance;
        gameStateMachine = GameStateMachine.Instance;
        playerManager = PlayerManagement.Instance;
        cardManager = CardManagement.Instance;
        cardSelectionUI = GetComponent<CardSelectionUI>();
        kittySlotsUI = GetComponent<KittySlotsUI>();

    }

    /// <summary>
    /// 外部调用：开始底牌选择协程
    /// </summary>
    public IEnumerator RunKittySelectionCoroutine()
    {
        Debug.Log("=== 底牌选择阶段开始 ===");

        // 隐藏主花色指示器（防止上一轮残留）
        cardSelectionUI.HideTrumpSuitIndicator();

        // 初始化数据
        Kittys = cardManager.KittyCards;
        Discards = new List<Card>();
        KittyOwnerIndex = gameFlowController.HostPlayerIndex;

        gameStateMachine.ChangeGameState(GameState.KittySelection);

        // 亮出底牌（正面）
        kittySlotsUI.ShowFaces();
        // 或者带特效： yield return StartCoroutine(kittySlotsUI.RevealWithDelay());

        // 将底牌加入庄家手牌
        AddKittycardsToHand();
        playerManager.SortPlayerHandCards(KittyOwnerIndex, null);
        GameEvents.TriggerSlotCardsChanged(KittyOwnerIndex);

        // 1. 选择主花色
        yield return StartCoroutine(SelectTrumpSuit());

        // 2. 选择要弃的六张牌
        yield return StartCoroutine(SelectDiscards());

        // 3. 结束底牌选择
        RemoveDiscardsFromHand();
        
        gameStateMachine.ChangeGameState(GameState.Playing);
        Debug.Log("=== 底牌选择阶段结束 ===");
    }

    private IEnumerator SelectTrumpSuit()
    {
        Debug.Log("等待选择主花色...");
        Player owner = playerManager.GetPlayer(KittyOwnerIndex);
        CardSuit selectedSuit = CardSuit.Club; // 默认

        if (owner.IsAI)
        {
            // AI 随机选择花色
            yield return new WaitForSeconds(aiThinkTime);
            //selectedSuit = GetRandomSuit();
            selectedSuit = owner.aiPlayer.DecideTrumpSuit(owner.HandCards);
            Debug.Log($"AI 庄家 {owner.PlayerName} 选择主花色：{selectedSuit}");
        }
        else
        {
            bool suitSelected = false;
            cardSelectionUI.ActivateSuitsController((suit) =>
            {
                selectedSuit = suit;
                suitSelected = true;
            });

            while (!suitSelected)
                yield return null;
        }

        // 设置主花色并重新排序所有手牌
        SetTrumpSuit(selectedSuit);
        Debug.Log($"主花色设置为 {selectedSuit}");
    }

    private IEnumerator SelectDiscards()
    {
        Debug.Log("等待选择弃牌（需要 6 张）...");
        Player owner = playerManager.GetPlayer(KittyOwnerIndex);
        List<Card> finalDiscards = null;

        if (owner.IsAI)
        {
            yield return new WaitForSeconds(aiThinkTime);
            //finalDiscards = GetRandomDiscards(owner.HandCards, gameFlowController.kittyCardsCount);
            finalDiscards = owner.aiPlayer.DecideDiscards(owner.HandCards, gameFlowController.kittyCardsCount);
            Discards = finalDiscards;
            Debug.Log("AI 选择弃牌：");
            LogDiscards(finalDiscards);
        }
        else
        {
            // 清空之前的选择
            Discards.Clear();
            GameEvents.TriggerDiscardsCleared(KittyOwnerIndex);
            GameEvents.TriggerSlotButtonsActivated(KittyOwnerIndex, true);

            isSelectingDiscards = true;

            bool discardConfirmed = false;

            // 定义回调（只绑定一次）
            void OnConfirm()
            {
                if (Discards.Count == gameFlowController.kittyCardsCount)
                {
                    finalDiscards = new List<Card>(Discards);
                    discardConfirmed = true;
                    isSelectingDiscards = false;
                    cardSelectionUI.DeactivateSelectionUI(); // 正确时关闭 UI
                }
                else
                {
                    Debug.LogWarning($"弃牌数量不正确：需要 {gameFlowController.kittyCardsCount} 张，实际 {Discards.Count} 张，已清空弃牌");
                    Discards.Clear();
                    GameEvents.TriggerDiscardsCleared(KittyOwnerIndex);
                    // 重新显示按钮（不清空回调，UI 保持激活）
                    cardSelectionUI.ReactivateDiscardSelection();
                }
            }

            void OnCancel()
            {
                Debug.Log("玩家取消弃牌选择，清空弃牌");
                Discards.Clear();
                GameEvents.TriggerDiscardsCleared(KittyOwnerIndex);
                // 重新显示按钮
                cardSelectionUI.ReactivateDiscardSelection();
            }

            // 首次激活 UI，绑定回调
            cardSelectionUI.ActivateDiscardSelection(gameFlowController.kittyCardsCount, OnConfirm, OnCancel);

            // 等待确认
            while (!discardConfirmed)
                yield return null;

            // 弃牌完成：禁用按钮，保存弃牌列表
            GameEvents.TriggerSlotButtonsActivated(KittyOwnerIndex, false);
            Discards = finalDiscards;
            Debug.Log("玩家选择弃牌：");
            LogDiscards(finalDiscards);
        }
    }

    // ---------- 核心逻辑方法（基本保持原样） ----------
    private void AddKittycardsToHand()
    {
        foreach (Card kittycard in Kittys)
        {
            playerManager.players[KittyOwnerIndex].AddCard(kittycard);
        }
        GameEvents.TriggerSlotCardsChanged(KittyOwnerIndex);
    }

    private void RemoveDiscardsFromHand()
    {
        foreach (Card discard in Discards)
        {
            playerManager.players[KittyOwnerIndex].RemoveCard(discard);
        }
        playerManager.SortPlayerHandCards(KittyOwnerIndex, gameFlowController.TrumpSuit);
        GameEvents.TriggerSlotCardsChanged(KittyOwnerIndex);
    }

    public void SetTrumpSuit(CardSuit trumpSuit)
    {
        gameFlowController.TrumpSuit = trumpSuit;
        Debug.Log($"Trump suit set to {trumpSuit}");
        // 更新 UI 指示器
        if (cardSelectionUI != null)
            cardSelectionUI.SetTrumpSuitIndicator(trumpSuit);
        // 重新排序所有玩家的手牌
        playerManager.SortPlayerHandCards(gameFlowController.allPlayerIndexSign, trumpSuit);
        GameEvents.TriggerSlotCardsChanged(gameFlowController.allSeatIndexSign);
    }

    public void ClearDiscards()
    {
        Discards.Clear();
        GameEvents.TriggerDiscardsCleared(KittyOwnerIndex);
    }

    // ---------- AI 辅助方法 ----------
    private CardSuit GetRandomSuit()
    {
        CardSuit[] suits = { CardSuit.Club, CardSuit.Diamond, CardSuit.Heart, CardSuit.Spade };
        return suits[Random.Range(0, suits.Length)];
    }

    private List<Card> GetRandomDiscards(List<Card> hand, int count)
    {
        if (hand.Count <= count) return new List<Card>(hand);
        List<Card> shuffled = new List<Card>(hand);
        // Fisher-Yates 洗牌取前 count 张
        for (int i = 0; i < count; i++)
        {
            int r = Random.Range(i, shuffled.Count);
            (shuffled[i], shuffled[r]) = (shuffled[r], shuffled[i]);
        }
        Debug.Log($"AI 随机选牌：手牌数 {hand.Count}，需要弃 {count} 张");
        return shuffled.GetRange(0, count);
    }

    private void LogDiscards(List<Card> discards)
    {
        foreach (Card card in discards)
        {
            Debug.Log($"  弃牌：{cardManager.GetCardName(card)}");
        }
    }
}
