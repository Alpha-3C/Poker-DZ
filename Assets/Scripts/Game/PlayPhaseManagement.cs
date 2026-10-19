using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AI;

public class PlayPhaseManagement : MonoBehaviour
{
    private GameFlowController gameFlowController;
    private GameStateMachine gameStateMachine;
    private PlayerManagement playerManager;
    private CardManagement cardManager;
    private PlayPhaseUI playPhaseUI;

    public CardSuit? trumpSuit;               // 全局主花色（从gameFlow获取）

    // 本地游戏流变量（原存储在 GameFlowController 中的临时状态）
    public int currentTrickNumber = 0;
    public CardSuit? currentLeadSuit = null;
    public int trickLeadIndex = -1;           // 当前墩的首出玩家索引
    public int currentPlayerIndex = -1;       // 当前正在出牌的玩家索引
    public List<Card> currentTrickCards = new List<Card>();
    public int currentEarnedPoint = 0;        // 本轮闲家累计得分（待结算）

    // 等待人类玩家出牌的控制变量
    private bool waitingForHuman = false;
    private Card selectedCardByHuman = null;
    private Coroutine playingCoroutine;

    private void Start()
    {
        gameFlowController = GameFlowController.Instance;
        gameStateMachine = GameStateMachine.Instance;
        playerManager = PlayerManagement.Instance;
        cardManager = CardManagement.Instance;
        playPhaseUI = GetComponent<PlayPhaseUI>();

    }


    public IEnumerator PlayAllTricksCoroutine()
    {
        trumpSuit = gameFlowController.TrumpSuit;

        // 重置本地状态
        currentTrickNumber = 0;
        currentLeadSuit = null;
        trickLeadIndex = gameFlowController.HostPlayerIndex;
        currentPlayerIndex = trickLeadIndex;
        currentTrickCards.Clear();
        currentEarnedPoint = 0;

        // 清理 UI（出牌区）
        playPhaseUI.ClearAllDealAreas();
        playPhaseUI.ActivateArrow(true);

        Debug.Log("=== 开始出牌阶段 ===");

        for (currentTrickNumber = 1; currentTrickNumber <= 12; currentTrickNumber++)
        {
            Debug.Log($"---------- 第 {currentTrickNumber} 墩开始 ----------");
            yield return StartCoroutine(PlayOneTrickCoroutine());

            // 墩结束后短暂停顿，让玩家看清结果
            yield return new WaitForSeconds(0.8f);
        }

        Debug.Log("=== 全部12墩结束 ===");
        playPhaseUI.DeactivatePlayPhaseUI();

        // 出牌结束，将本轮闲家得分写回 GameFlowController（供结算使用）
        gameFlowController.currentEarnedPoint = currentEarnedPoint;
        gameFlowController.FinalTrickWinnerIndex = trickLeadIndex;
        gameStateMachine.ChangeGameState(GameState.RoundEnd);
    }

    private IEnumerator PlayOneTrickCoroutine()
    {

        currentPlayerIndex = trickLeadIndex;
        List<int> playOrder = new List<int>(); // 记录出牌顺序

        // 所有玩家依次出牌
        for (int i = 0; i < playerManager.playerCount; i++)
        {
            playOrder.Add(currentPlayerIndex);
            Player currentPlayer = playerManager.GetPlayer(currentPlayerIndex);
            Debug.Log($"轮到玩家 {currentPlayer.PlayerName} 出牌");

            playPhaseUI.SetArrowPos(currentPlayerIndex);

            Card playedCard = null;

            if (!currentPlayer.IsAI)
            {
                yield return StartCoroutine(WaitForHumanPlayCoroutine(currentLeadSuit));
                playedCard = selectedCardByHuman;
                selectedCardByHuman = null;
            }
            else
            {
                yield return StartCoroutine(AIThinkAndPlayCoroutine(currentPlayer, currentLeadSuit));
                //playedCard = GetAIPlayCard(currentPlayer, currentLeadSuit);
                playedCard = currentPlayer.aiPlayer.DecidePlayCard(
                            handCards: currentPlayer.HandCards,
                            leadSuit: currentLeadSuit,
                            trumpSuit: trumpSuit,
                            mySeatIndex: currentPlayerIndex,
                            currentTrickCards: currentTrickCards,
                            trickPlayOrder: playOrder
                            );
            }

            // 执行出牌
            ExecutePlay(currentPlayerIndex, playedCard, ref currentLeadSuit);
            currentTrickCards.Add(playedCard);

            // 切换到下一个玩家
            currentPlayerIndex = (currentPlayerIndex + 1) % playerManager.playerCount;
        }

        // 一墩结束，结算赢家
        int winnerOrderIndex = DetermineTrickWinnerIndex(currentTrickCards, currentLeadSuit.Value);
        int winnerPlayerIndex = playOrder[winnerOrderIndex];
        Debug.Log($"第 {currentTrickNumber} 墩赢家：玩家 {winnerPlayerIndex}");

        // 设置下一墩的首出玩家为当前墩赢家
        trickLeadIndex = winnerPlayerIndex;

        // 累计闲家得分
        currentEarnedPoint += DetermineTrickEarnedPoint(currentTrickCards, winnerPlayerIndex);
        playPhaseUI.UpdateEarnPointText(currentEarnedPoint);

        yield return new WaitForSeconds(gameFlowController.trickEndWaitingTime);

        // 重置当前墩数据
        currentTrickCards.Clear();
        currentLeadSuit = null;
        playPhaseUI.ClearAllDealAreas();

    }

    // ---------- 人类玩家等待协程 ----------
    private IEnumerator WaitForHumanPlayCoroutine(CardSuit? leadSuit)
    {
        playPhaseUI.StartHumanTurn(leadSuit, trumpSuit);
        waitingForHuman = true;
        selectedCardByHuman = null;
        yield return new WaitUntil(() => !waitingForHuman && selectedCardByHuman != null);
        playPhaseUI.EndHumanTurn();
    }

    public void OnHumanCardSelected(Card card)
    {
        if (!waitingForHuman) return;

        int humanIndex = playerManager.GetHumanPlayerIndex().Value;
        if (IsValidPlay(playerManager.players[humanIndex], card, currentLeadSuit, trumpSuit))
        {
            selectedCardByHuman = card;
            waitingForHuman = false;
        }
        else
        {
            Debug.LogWarning("选择的牌不合法，请重新选择");
        }
    }

    // ---------- AI 思考协程 ----------
    private IEnumerator AIThinkAndPlayCoroutine(Player aiPlayer, CardSuit? leadSuit)
    {
        float thinkTime = Random.Range(0.5f, 1.5f);
        yield return new WaitForSeconds(thinkTime);
    }

    private Card GetAIPlayCard(Player aiPlayer, CardSuit? leadSuit)
    {
        List<Card> validCards = GetValidCardsForPlayer(aiPlayer, leadSuit, trumpSuit);
        if (validCards.Count == 0)
        {
            Debug.LogError($"AI {aiPlayer.PlayerName} 没有合法牌！");
            return aiPlayer.HandCards[0];
        }

        int strategy = Random.Range(0, 3);
        validCards.Sort((a, b) => CardComparer.SuitCompare(b, a, trumpSuit));

        switch (strategy)
        {
            case 0: return validCards[0];
            case 1: return validCards[Random.Range(0, validCards.Count)];
            case 2: return validCards[validCards.Count - 1];
            default: return validCards[0];
        }
    }

    // ---------- 辅助方法 ----------
    private bool IsValidPlay(Player player, Card card, CardSuit? leadSuit, CardSuit? trumpSuit)
    {
        if (!player.HandCards.Contains(card)) return false;
        if (leadSuit == null) return true;
        return CardManagement.CanDealCard(player.HandCards, leadSuit.Value, card);
    }

    private List<Card> GetValidCardsForPlayer(Player player, CardSuit? leadSuit, CardSuit? trumpSuit)
    {
        if (leadSuit == null)
            return new List<Card>(player.HandCards);

        List<Card> valid = new List<Card>();
        foreach (Card card in player.HandCards)
        {
            if (CardManagement.CanDealCard(player.HandCards, leadSuit.Value, card))
                valid.Add(card);
        }
        return valid;
    }

    private void ExecutePlay(int playerIndex, Card card, ref CardSuit? leadSuit)
    {
        playerManager.players[playerIndex].HandCards.Remove(card);
        GameEvents.TriggerSlotCardsChanged(playerIndex);

        if (leadSuit == null)
        {
            leadSuit = GetRealSuitForLead(card);
        }

        playPhaseUI.UpdateDealArea(playerIndex, card);
    }

    private CardSuit GetRealSuitForLead(Card card)
    {
        if (card.IsJoker || card.cardRank == CardRank.Two)
            return trumpSuit ?? CardSuit.RedJoker;
        return card.cardSuit;
    }

    private int DetermineTrickWinnerIndex(List<Card> trickCards, CardSuit leadSuit)
    {
        int winnerIndex = 0;
        Card winnerCard = trickCards[0];
        for (int i = 1; i < trickCards.Count; i++)
        {
            if (CardComparer.TrickCompare(trickCards[i], winnerCard, leadSuit, trumpSuit) > 0)
            {
                winnerCard = trickCards[i];
                winnerIndex = i;
            }
        }
        return winnerIndex;
    }

    private int DetermineTrickEarnedPoint(List<Card> trickCards, int winningPlayerIndex)
    {
        if (playerManager.players[winningPlayerIndex].Team == PlayerTeam.Host) return 0;
        int point = 0;
        foreach (Card card in trickCards)
        {
            if (card.cardRank == CardRank.Five) point += 5;
            else if (card.cardRank == CardRank.Ten || card.cardRank == CardRank.King) point += 10;
        }
        return point;
    }

    public void StopPlayingPhase()
    {
        if (playingCoroutine != null)
            StopCoroutine(playingCoroutine);
        playingCoroutine = null;
        waitingForHuman = false;
        selectedCardByHuman = null;
    }
}