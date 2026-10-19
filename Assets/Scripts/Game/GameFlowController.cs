using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameFlowController : MonoBehaviour
{
    public static GameFlowController Instance;

    // 各阶段管理组件（通过 Inspector 拖拽）
    public BiddingManagement biddingManagement;
    public KittySelectionManagement kittySelectionManagement;
    public PlayPhaseManagement playPhaseManagement;
    public RoundManagement roundManagement;

    [Header("Game Settings")]
    public int cardsPerPlayer = 12;
    public int kittyCardsCount = 6;
    public int minimumBid = 60;
    public int maximumBid = 100;
    public int pointLimit = 100;
    public int bidIncrease = 5;
    public float biddingWaitingTime = 15f;
    public float trickEndWaitingTime = 1.5f;

    [Header("Game Flow")]
    public int currentRoundNumber = 0;
    public int currentEarnedPoint;      // 本轮闲家累计得分
    public int biddingPoint;            // 庄家叫分
    public CardSuit? TrumpSuit;
    public int HostPlayerIndex;
    public Player HostPlayer;
    public int? FinalTrickWinnerIndex;  // 最后一墩赢家索引

    [Header("References")]
    public CardManagement cardManager;
    public PlayerManagement playerManager;
    public GameStateMachine stateMachine;   // 仅用于 UI 状态显示

    [Header("Sign")]
    public int allPlayerIndexSign = -42;    // 特殊标记
    public int allSeatIndexSign = -42;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeManagers();
    }

    private void InitializeManagers()
    {
        if (cardManager == null) cardManager = CardManagement.Instance;
        if (playerManager == null) playerManager = PlayerManagement.Instance;
        if (stateMachine == null) stateMachine = GameStateMachine.Instance;
    }

    /// <summary>
    /// 外部调用：开始一局完整游戏（不断重复轮次直到游戏结束）
    /// </summary>
    public void StartGame()
    {
        StartCoroutine(RunGameLoop());
    }

    private IEnumerator RunGameLoop()
    {
        bool gameEnd = false;
        while (!gameEnd)
        {
            yield return StartCoroutine(RunOneRound());

            // 判断是否达到游戏结束条件（例如某一方达到 pointLimit 分）
            // 这里根据你的计分规则判断，示例：闲家得分累计达到 pointLimit 或庄家叫分 + 闲家得分超过 pointLimit 等
            // 具体逻辑可放入一个方法 CheckGameEndCondition()
            if (CheckGameEndCondition())
            {
                gameEnd = true;
                Debug.Log("游戏结束！");
                stateMachine.ChangeGameState(GameState.GameEnd);
            }
            else
            {
                // 继续下一轮，重置部分状态
                currentRoundNumber++;
                Debug.Log($"准备开始第 {currentRoundNumber + 1} 轮");
                yield return new WaitForSeconds(1f);
            }
        }
    }

    private IEnumerator RunOneRound()
    {
        Debug.Log($"=== 开始第 {currentRoundNumber + 1} 轮 ===");

        // 0. 准备阶段
        yield return StartCoroutine(roundManagement.ReadyPhase());

        // 1. 发牌
        yield return StartCoroutine(roundManagement.DealPhase());

        // 2. 叫分（含流局重试）
        bool isAllPass = false;
        do
        {
            yield return StartCoroutine(biddingManagement.RunBiddingCoroutine());
            isAllPass = biddingManagement.IsAllPass;   
            if (isAllPass)
            {
                Debug.Log("叫分流局，重新发牌");
                yield return StartCoroutine(roundManagement.DealPhase());
            }
        } while (isAllPass);

        // 3. 底牌选择
        yield return StartCoroutine(kittySelectionManagement.RunKittySelectionCoroutine());

        // 4. 出牌阶段（12墩）
        yield return StartCoroutine(playPhaseManagement.PlayAllTricksCoroutine());

        // 5. 结算本轮
        yield return StartCoroutine(roundManagement.RoundEndCoroutine());

        Debug.Log($"=== 第 {currentRoundNumber + 1} 轮结束 ===");
    }

    /// <summary>
    /// 根据你的计分规则判断游戏是否结束
    /// </summary>
    private bool CheckGameEndCondition()
    {
        // 示例：如果闲家累计得分 >= pointLimit，则游戏结束（闲家胜利）
        // 具体规则需要根据你的游戏设计实现
        // 这里简单返回 false，表示永不结束（之后你需完善）
        return false;
    }
}