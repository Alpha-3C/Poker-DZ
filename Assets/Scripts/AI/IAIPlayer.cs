using System.Collections.Generic;
using UnityEngine;

namespace AI
{
    /// <summary>
    /// AI 玩家决策接口
    /// </summary>
    public interface IAIPlayer
    {
        /// <summary>
        /// 准备阶段：是否准备就绪（通常返回 true）
        /// </summary>
        bool IsReady();

        /// <summary>
        /// 叫分阶段决策
        /// </summary>
        /// <param name="currentHighestBid">当前最高叫分（0表示无人叫分）</param>
        /// <param name="handCards">当前手牌</param>
        /// <param name="minBid">最低叫分</param>
        /// <param name="maxBid">最高叫分</param>
        /// <param name="bidStep">叫分步长</param>
        /// <returns>叫分（-1 表示 Pass）</returns>
        int DecideBid(int currentHighestBid, List<Card> handCards, int minBid, int maxBid, int bidStep);

        /// <summary>
        /// 成为庄家后选择主花色
        /// </summary>
        CardSuit DecideTrumpSuit(List<Card> handCards);

        /// <summary>
        /// 底牌阶段选择要弃掉的牌（数量固定）
        /// </summary>
        /// <param name="handCards">当前手牌（已包含底牌）</param>
        /// <param name="discardCount">需要弃掉的数量</param>
        /// <returns>弃牌列表</returns>
        List<Card> DecideDiscards(List<Card> handCards, int discardCount);

        /// <summary>
        /// 出牌阶段决策
        /// </summary>
        /// <param name="handCards">当前手牌</param>
        /// <param name="leadSuit">本墩首出花色（null 表示本轮首出）</param>
        /// <param name="trumpSuit">全局主花色（可能为 null）</param>
        /// <returns>要打出的牌</returns>
        Card DecidePlayCard(
            List<Card> handCards,
            CardSuit? leadSuit,
            CardSuit? trumpSuit,
            int mySeatIndex,
            List<Card> currentTrickCards,      // 当前墩已出的牌（顺序）
            List<int> trickPlayOrder            // 与 currentTrickCards 对应的玩家索引
            );

        // 以下为高级接口，可暂不实现，留待后续扩展

        /// <summary>
        /// 记忆/推断其他玩家手牌（可选）
        /// </summary>
        void UpdateMemory(int seatIndex, Card playedCard);

        /// <summary>
        /// 评估当前局势（可选，用于复杂决策）
        /// </summary>
        float EvaluateSituation();
    }
}