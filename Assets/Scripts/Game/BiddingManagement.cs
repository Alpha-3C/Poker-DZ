/*using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Rendering;

public class BiddingManagement : MonoBehaviour
{
    // 叫分阶段状态变量
    int firstBiddingPlayerIndex;      // 起始叫分玩家索引
    int lastBiddingPlayerIndex;       // 最后叫分玩家索引
    int currentBiddingPlayerIndex;    // 当前叫分玩家索引
    int winningBidderIndex = -1;      // 最终赢家索引（-1表示没有赢家/流局）

    // 叫分记录
    int currentHighestBid = 0;        // 当前最高叫分（0表示无人叫分）
    bool[] hasPassed;   // 每个玩家是否已Pass
    int[] playerBids;    // 每个玩家的最终叫分（-1=Pass, 0=未叫, >0=叫分）

    private PlayerManagement playerManager;
    private GameFlowController gameFlowController;
    private BiddingUI biddingUI;

    public float AIPassingTime = 1.5f;
    private void OnEnable()
    {
        GameStateMachine.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameStateMachine.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (newState != GameState.Bidding) return;

        StartBiddingPhase();

    }

    private void Start()
    {
        playerManager = PlayerManagement.Instance;
        gameFlowController = GameFlowController.Instance;
        biddingUI = GetComponent<BiddingUI>();
    }

    private int DetermineFirstBiddingPlayer()
    {
        return (gameFlowController.currentRoundNumber - 1) % playerManager.players.Count;
    }

    private int DetermineWinningBidder()
    {
        Debug.Log($"highest bid: {currentHighestBid}");
        for (int i = 0; i < playerManager.playerCount; i++)
        {
            Debug.Log($"player{i}: {playerBids[i]}");
            if (currentHighestBid !=0 && playerBids[i] == currentHighestBid) return i;
        }

        return -1;
    }

    private int DetermineLastBiddingPlayer()
    {
        return (gameFlowController.currentRoundNumber + playerManager.playerCount - 2) % playerManager.players.Count;
    }
    private void StartBiddingPhase()
    {
        hasPassed = new bool[playerManager.playerCount];
        playerBids = new int[playerManager.playerCount];
        currentHighestBid = 0;
        winningBidderIndex = -1;
        gameFlowController.IsFullBid = false;
        biddingUI.RealPlayerHaveBidded = false;

        for (int i = 0; i < playerManager.playerCount; i++)
        {
            Debug.Log(i);
            hasPassed[i] = false;
            playerBids[i] = 0; // 0表示未叫分
        }

        firstBiddingPlayerIndex = DetermineFirstBiddingPlayer();
        lastBiddingPlayerIndex = DetermineLastBiddingPlayer();

        //开始第一个玩家的叫分
        StartPlayerBiddingTurn();
    }


    private void StartPlayerBiddingTurn()
    {
        currentBiddingPlayerIndex = firstBiddingPlayerIndex;

        PlayerBidding(currentBiddingPlayerIndex);

    }

    public void PlayerBidding(int playerIndex)
    {


        if (playerManager.players[playerIndex].IsAI)
        {
            PlayerPass(playerIndex); //////////////////
        }
        else
        {
            biddingUI.ActivateBiddingUI(currentHighestBid, playerIndex);

        }
    }

    public void PlayerPass(int playerIndex)
    {
        hasPassed[playerIndex] = true;
        playerBids[playerIndex] = -1;

        if (playerManager.players[playerIndex].IsAI) StartCoroutine(WaitForTime(AIPassingTime));

        if(playerIndex == lastBiddingPlayerIndex)  //最后一个玩家
        {
            EndPlayerBiddingTurn();
        }
        else
        {
            int nextPlayerIndex = playerManager.DetermineNextPlayer(playerIndex);
            PlayerBidding(nextPlayerIndex);
        }

    }

    public void PlayerBiddingFor(int playerIndex, int bid)
    {
        if (currentHighestBid != 0 && bid <= currentHighestBid) Debug.LogError("Invalid Bid!");

        currentHighestBid = bid;
        hasPassed[playerIndex] = false;
        playerBids[playerIndex] = bid;

        if (playerManager.players[playerIndex].IsAI) StartCoroutine(WaitForTime(AIPassingTime));

        if (playerIndex == lastBiddingPlayerIndex || bid == gameFlowController.maximumBid)  //最后一个玩家，或者有Full Bid提前结束Bidding Phase.
        {
            EndPlayerBiddingTurn();
        }
        else
        {
            int nextPlayerIndex = playerManager.DetermineNextPlayer(playerIndex);
            PlayerBidding(nextPlayerIndex);
        }

    }

    private void EndPlayerBiddingTurn()
    {
        if (IsEveryonePassed()) gameFlowController.EndBidding(true);
        else
        {
            winningBidderIndex = DetermineWinningBidder();

            gameFlowController.biddingPoint = currentHighestBid;
            gameFlowController.IsFullBid = IsSomeoneFullBid();
            gameFlowController.HostPlayerIndex = winningBidderIndex;
            gameFlowController.HostPlayer = playerManager.players[winningBidderIndex];
            foreach (Player player in playerManager.players)
            {
                if (player == playerManager.players[winningBidderIndex])
                {
                    player.IsKittyMaster = true;
                    player.Team = PlayerTeam.Host;
                }

                else
                {
                    player.IsKittyMaster = false;
                    player.Team = PlayerTeam.Client;
                }
            }

            biddingUI.finalBiddingPointText.text = "BiddingPoint:" + gameFlowController.biddingPoint.ToString();
            gameFlowController.EndBidding(false);
        }
    }

    private bool IsEveryonePassed()
    {
        foreach(bool b in hasPassed)
        {
            if(!b) return false;
        }
        Debug.Log("All passed.");
        return true;
    }

    private bool IsSomeoneFullBid()
    {
        return currentHighestBid == gameFlowController.maximumBid;
    }

    IEnumerator WaitForTime(float time)
    {
        Debug.Log($"Waiting for {time} seconds...");
        yield return new WaitForSeconds(time);
    }


}*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AI;

public class BiddingManagement : MonoBehaviour
{
    // 叫分参数（从 GameFlowController 读取）
    private int minimumBid;
    private int maximumBid;
    private int bidIncrease;
    private float biddingWaitingTime;
    private int playerCount;

    // 叫分阶段状态变量
    private int firstBiddingPlayerIndex;
    private int lastBiddingPlayerIndex;
    private int currentBiddingPlayerIndex;
    private int winningBidderIndex = -1;
    private int currentHighestBid = 0;
    private bool[] hasPassed;
    private int[] playerBids;
    public bool IsAllPass { get; private set; }

    // 引用
    private GameFlowController gameFlowController;
    private GameStateMachine gameStateMachine;
    private PlayerManagement playerManager;
    private BiddingUI biddingUI;
    private List<DeskUI> deskUIs;

    // AI 思考时间（可配置）
    public float aiThinkTime = 1.2f;

    private void Start()
    {
        gameFlowController = GameFlowController.Instance;
        playerManager = PlayerManagement.Instance;
        gameStateMachine = GameStateMachine.Instance;
        biddingUI = GetComponent<BiddingUI>();
        deskUIs = playerManager.deskUIs;

        // 从 GameFlowController 读取配置
        minimumBid = gameFlowController.minimumBid;
        maximumBid = gameFlowController.maximumBid;
        bidIncrease = gameFlowController.bidIncrease;
        biddingWaitingTime = gameFlowController.biddingWaitingTime;
        playerCount = playerManager.playerCount;
    }

    /// <summary>
    /// 外部调用：开始叫分协程
    /// </summary>
    public IEnumerator RunBiddingCoroutine()
    {
        Debug.Log("=== 叫分阶段开始 ===");

        // 初始化叫分数据
        ResetBiddingData();

        // 确定起始和最后玩家（每轮顺时针轮转）
        firstBiddingPlayerIndex = (gameFlowController.currentRoundNumber - 1) % playerCount;
        lastBiddingPlayerIndex = (gameFlowController.currentRoundNumber + playerCount - 2) % playerCount;

        currentBiddingPlayerIndex = firstBiddingPlayerIndex;

        gameStateMachine.ChangeGameState(GameState.Bidding);

        bool biddingFinished = false;
        int passCount = 0;

        while (!biddingFinished)
        {
            Player currentPlayer = playerManager.GetPlayer(currentBiddingPlayerIndex);
            Debug.Log($"轮到 {currentPlayer.PlayerName} 叫分，当前最高叫分：{currentHighestBid}");

            int selectedBid = -1; // -1 表示 Pass

            if (currentPlayer.IsAI)
            {
                // AI 叫分策略（简单版：直接 Pass）
                // 后续可扩展为调用 IAIPlayer.SelectBid
                yield return new WaitForSeconds(aiThinkTime);
                //selectedBid = GetAIBid(currentHighestBid, currentPlayer.HandCards);
                selectedBid = currentPlayer.aiPlayer.DecideBid(currentHighestBid, currentPlayer.HandCards, minimumBid, maximumBid, bidIncrease);
                Debug.Log($"AI {currentPlayer.PlayerName} 选择 {(selectedBid == -1 ? "Pass" : selectedBid.ToString())}");
            }
            else
            {
                // 人类玩家：显示 UI，等待玩家操作或超时
                bool hasConfirmed = false;
                int humanBid = -1;

                biddingUI.ActivateBiddingUI(currentHighestBid, currentBiddingPlayerIndex,
                    onConfirm: (bid) => { humanBid = bid; hasConfirmed = true; },
                    onPass: () => { humanBid = -1; hasConfirmed = true; }
                );

                float timer = 0f;
                while (!hasConfirmed && timer < biddingWaitingTime)
                {
                    timer += Time.deltaTime;
                    yield return null;
                }

                biddingUI.DeactivateBiddingUI();

                if (!hasConfirmed)
                {
                    Debug.Log($"人类玩家 {currentPlayer.PlayerName} 超时未响应，自动 Pass");
                    humanBid = -1;
                }

                selectedBid = humanBid;
            }

            // 处理叫分结果
            if (selectedBid == -1)
            {
                // Pass
                hasPassed[currentBiddingPlayerIndex] = true;
                playerBids[currentBiddingPlayerIndex] = -1;
                passCount++;
                Debug.Log($"{currentPlayer.PlayerName} Pass");
            }
            else
            {
                // 叫分
                currentHighestBid = selectedBid;
                winningBidderIndex = currentBiddingPlayerIndex;
                hasPassed[currentBiddingPlayerIndex] = false;
                playerBids[currentBiddingPlayerIndex] = selectedBid;
                Debug.Log($"{currentPlayer.PlayerName} 叫分 {selectedBid}");
            }

            // 检查叫分结束条件
            if (passCount == playerCount)
            {
                // 全 Pass → 流局
                Debug.Log("所有玩家 Pass，流局");
                biddingFinished = true;
                winningBidderIndex = -1;
                break;
            }

            if (selectedBid == maximumBid)
            {
                // 有人叫满分 → 立即结束
                Debug.Log("叫满分，叫分结束");
                biddingFinished = true;
                break;
            }

            // 检查是否是最后一个玩家且已有叫分
            if (currentBiddingPlayerIndex == lastBiddingPlayerIndex && currentHighestBid > 0)
            {
                Debug.Log("最后一轮叫分结束，已有叫分");
                biddingFinished = true;
                break;
            }

            // 移动到下一个未 Pass 的玩家
            currentBiddingPlayerIndex = GetNextNonPassPlayer(currentBiddingPlayerIndex);
        }

        // 叫分结束后的处理
        if (winningBidderIndex == -1)
        {
            Debug.Log("叫分流局，重新开始本局");
            IsAllPass = true;
            //gameFlowController.EndBidding(true);
        }
        else
        {
            Player winner = playerManager.GetPlayer(winningBidderIndex);
            Debug.Log($"叫分结束！赢家：{winner.PlayerName}，叫分：{currentHighestBid}");

            // 更新 GameFlowController 的数据（座位索引）
            gameFlowController.biddingPoint = currentHighestBid;
            //gameFlowController.IsFullBid = (currentHighestBid == maximumBid);
            gameFlowController.HostPlayerIndex = winningBidderIndex;           // 座位索引
            gameFlowController.HostPlayer = winner;

            // 设置玩家阵营
            for (int i = 0; i < playerManager.players.Count; i++)
            {
                Player player = playerManager.players[i];
                bool isWinner = (i == winningBidderIndex);
                player.IsKittyMaster = isWinner;
                player.Team = isWinner ? PlayerTeam.Host : PlayerTeam.Client;

                deskUIs[i].SetTeamIcon(isWinner);  //更新Host/Client标识
            }

            // 更新 UI
            if (biddingUI != null && biddingUI.finalBiddingPointText != null)
                biddingUI.finalBiddingPointText.text = "BiddingPoint:" + currentHighestBid.ToString();

            //gameFlowController.EndBidding(false);
            IsAllPass = false;
        }
    }

    // ---------- 私有辅助方法 ----------
    private void ResetBiddingData()
    {
        hasPassed = new bool[playerCount];
        playerBids = new int[playerCount];
        currentHighestBid = 0;
        winningBidderIndex = -1;
        // gameFlowController.IsFullBid = false;

        for (int i = 0; i < playerCount; i++)
        {
            hasPassed[i] = false;
            playerBids[i] = 0; // 0 表示未叫分
        }
    }

    private int GetNextNonPassPlayer(int current)
    {
        int next = (current + 1) % playerCount;
        while (hasPassed[next])
        {
            next = (next + 1) % playerCount;
            if (next == current) break; // 防止无限循环
        }
        return next;
    }

    /// <summary>
    /// AI 叫分策略（简单版：永远 Pass）
    /// 后续可替换为更智能的实现
    /// </summary>
    /// <summary>
    /// AI 叫分策略（随机叫分或 Pass）
    /// </summary>
    /// <param name="currentHighest">当前最高叫分</param>
    /// <param name="handCards">AI 手牌</param>
    /// <returns>叫分（-1 表示 Pass）</returns>
    private int GetAIBid(int currentHighest, List<Card> handCards)
    {
        // 计算手牌强度（0~1，基于大牌数量简单估算）
        float handStrength = EvaluateHandStrength(handCards);

        // 基础叫分倾向：手牌强度越高，越愿意叫分
        int maxPossibleBid = maximumBid;
        int startBid = (currentHighest == 0) ? minimumBid : currentHighest + bidIncrease;

        // 如果没有可叫的分，只能 Pass
        if (startBid > maximumBid)
            return -1;

        // 随机决定是否叫分：概率随手牌强度增加
        float bidProbability = 0.3f + handStrength * 0.5f; // 30%~80% 概率叫分
        if (Random.value > bidProbability)
            return -1; // Pass

        // 随机选择一个叫分（从 startBid 到 maximumBid，步长 bidIncrease）
        int steps = (maximumBid - startBid) / bidIncrease + 1;
        int stepIndex = Random.Range(0, steps);
        int selectedBid = startBid + stepIndex * bidIncrease;

        // 如果当前最高叫分已很高且手牌不强，有概率降低叫分（实际已经随机选择）
        return selectedBid;
    }

    /// <summary>
    /// 评估手牌强度（简单估算）
    /// </summary>
    private float EvaluateHandStrength(List<Card> hand)
    {
        if (hand.Count == 0) return 0;
        int strongCount = 0;
        foreach (Card card in hand)
        {
            // 强牌：Joker、2、Ace、K、Q、J
            if (card.IsJoker || card.cardRank == CardRank.Two || card.cardRank >= CardRank.Jack)
                strongCount++;
        }
        return (float)strongCount / hand.Count;
    }
}
