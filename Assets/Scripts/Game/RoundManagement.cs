using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoundManagement : MonoBehaviour
{
    private GameFlowController gameFlowController;
    private GameStateMachine gameStateMachine;
    private PlayerManagement playerManager;
    private CardManagement cardManager;
    public CardSelectionUI cardSelectionUI;   // 用于隐藏主花色指示器
    public KittySlotsUI kittySlotsUI;
    public List<DeskUI> deskUIs;

    public Button readyButton;

    public float dealDisplayTime = 1.5f;      // 发牌动画占位时间
    public float roundEndDisplayTime = 1.5f;   // 结算显示时间

    private void Start()
    {
        gameFlowController = GameFlowController.Instance;
        gameStateMachine = GameStateMachine.Instance;
        playerManager = PlayerManagement.Instance;
        cardManager = CardManagement.Instance;
        deskUIs = playerManager.deskUIs;
    }
    /*
    /// <summary>
    /// 一轮游戏的完整协程（仅包含发牌和最终结算，中间流程由状态机驱动）
    /// </summary>
    public IEnumerator RunRoundCoroutine()
    {
        Debug.Log("=== RoundManagement: 开始新的一轮 ===");

        // 1. 发牌阶段
        yield return StartCoroutine(DealPhase());

        // 注意：发牌后，游戏流程将进入叫分、底牌、出牌等阶段（由状态机/其他管理器驱动）
        // 本协程在此处暂停（yield break 或等待），直到游戏进入 RoundEnd 状态时再继续？
        // 实际上，我们需要一个“等待本轮结束”的机制。改用以下方式：
        // 启动发牌后，等待状态机变为 RoundEnd（或由其他方式触发结算）
        // 更简单的方案：本协程只执行发牌，结算由外部单独调用。但用户要求整合结算。
        // 因此我们将结算协程单独暴露，由外部在状态机进入 RoundEnd 时调用？但用户希望整合在同一个协程中。
        // 为了满足“整合发牌和结算”，同时中间阶段由状态机驱动，可以这样设计：
        // 发牌后启动一个等待，直到游戏状态变为 RoundEnd，然后执行结算。

        // 等待直到游戏状态变为 RoundEnd（由出牌阶段结束后通过状态机切换触发）
        yield return new WaitUntil(() => gameStateMachine.gameState == GameState.RoundEnd);

        // 2. 结算阶段
        yield return StartCoroutine(RoundEndCoroutine());

        Debug.Log("=== RoundManagement: 本轮结束 ===");
    }*/
    public IEnumerator ReadyPhase()
    {
        cardManager.ResetKittyCards();
        // 初始化底牌槽位
        kittySlotsUI.Initialize();
        foreach (DeskUI deskUI in deskUIs)
        {
            deskUI.ClearTeamIcon();
        }
        readyButton.gameObject.SetActive(false);

        gameStateMachine.ChangeGameState(GameState.Setup);
        Debug.Log("初始化结束，等待开始：...");
        yield return WaitForAllReady();
    }


    public IEnumerator DealPhase()
    {
        Debug.Log("开始发牌...");

        cardManager.ShuffleDeck();
        cardManager.DealCardsToPlayers(gameFlowController.cardsPerPlayer, gameFlowController.kittyCardsCount);
        playerManager.SortPlayerHandCards(gameFlowController.allPlayerIndexSign, null);
        GameEvents.TriggerSlotCardsChanged(gameFlowController.allSeatIndexSign);

        // 显示背面（底牌尚未亮开）
        kittySlotsUI.ShowBacks();

        gameStateMachine.ChangeGameState(GameState.Dealing);
        yield return new WaitForSeconds(1.5f);
        Debug.Log("发牌完成");
    }

    public IEnumerator RoundEndCoroutine()
    {
        Debug.Log("=== 进入结算阶段 ===");

        // 此处放置原 RoundEndManagement 的所有逻辑
        PlayerTeam winningTeam = DetermineWinningTeam();
        Debug.Log($"Winning Team: {winningTeam}");

        // 隐藏主花色指示器
        if (cardSelectionUI != null)
            cardSelectionUI.HideTrumpSuitIndicator();

        kittySlotsUI.Clear();

        foreach(DeskUI deskUI in deskUIs)
        {
            deskUI.ClearTeamIcon();
        }

        // 让玩家看清结果
        yield return new WaitForSeconds(roundEndDisplayTime);

        // 切换到 Setup 状态（准备下一轮）
        gameStateMachine.ChangeGameState(GameState.Setup);

        Debug.Log("结算完成");
    }

    private PlayerTeam DetermineWinningTeam()
    {
        bool isPointExceed = (gameFlowController.biddingPoint + gameFlowController.currentEarnedPoint > gameFlowController.pointLimit);
        bool isClientFinalWinner = (playerManager.players[gameFlowController.FinalTrickWinnerIndex.Value].Team == PlayerTeam.Client);
        return (isPointExceed || isClientFinalWinner) ? PlayerTeam.Client : PlayerTeam.Host;
    }

    // 可选：外部强制结束当前轮（例如游戏结束）
    public void StopRound()
    {
        StopAllCoroutines();
    }

    /// <summary>
    /// 等待所有玩家准备就绪
    /// </summary>
    private IEnumerator WaitForAllReady()
    {
        // 准备状态数组：索引为座位索引（0~3）
        bool[] isReady = new bool[4];
        int playerCount = playerManager.players.Count;

        // 重置所有玩家的准备指示器（UI 部分）
        for (int i = 0; i < playerCount; i++)
        {
            isReady[i] = false;
            // 隐藏准备指示器（需要你在 DeskUI 中实现 SetReadyIndicator 方法）
            deskUIs[i]?.SetReadyIndicator(false);
        }

        // 显示人类玩家的准备按钮（需要你在 UI 中实现）
        // TODO: 显示准备面板和按钮，例如：readyPanel.SetActive(true);
        readyButton.gameObject.SetActive(true);

        // AI 玩家自动准备（可添加短暂延迟模拟思考）
        for (int i = 0; i < playerCount; i++)
        {
            if (playerManager.players[i].IsAI)
            {
                // 模拟 AI 准备延迟
                yield return new WaitForSeconds(0.3f);
                isReady[i] = true;
                deskUIs[i]?.SetReadyIndicator(true);
                Debug.Log($"AI 玩家 {i} 已准备");
            }
        }

        // 挂载在准备按钮上的方法：
        UnityEngine.Events.UnityAction OnHumanReady = null;
        OnHumanReady = () =>
        {
            int humanIndex = playerManager.GetHumanPlayerIndex().Value;
            if (!isReady[humanIndex])
            {
                isReady[humanIndex] = true;
                deskUIs[humanIndex]?.SetReadyIndicator(true);
                Debug.Log("人类玩家已准备");
            }
            // 移除监听
            readyButton.onClick.RemoveListener(OnHumanReady);
        };
        // 假设 readyButton 是你 UI 上的按钮
        readyButton.onClick.AddListener(OnHumanReady);

        // 等待所有玩家准备
        while (true)
        {
            bool allReady = true;
            for (int i = 0; i < playerCount; i++)
                if (!isReady[i]) { allReady = false; break; }
            if (allReady) break;
            yield return null;
        }

        // 所有玩家准备完毕，隐藏准备 UI
        // TODO: 隐藏准备面板和按钮，例如：readyPanel.SetActive(false);
        readyButton.gameObject.SetActive(false);
        // 可选：播放开始特效、倒计时等
        yield return new WaitForSeconds(1f); // 短暂停顿，增加仪式感

        for (int i = 0; i < playerCount; i++)
        {
            // 隐藏准备指示器（需要你在 DeskUI 中实现 SetReadyIndicator 方法）
            deskUIs[i]?.SetReadyIndicator(false);
        }
        Debug.Log("所有玩家准备完毕，开始发牌");
    }
}