using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AI
{
    /// <summary>
    /// 基础 AI 策略（简单逻辑）
    /// </summary>
    public class BasicAIPlayer : IAIPlayer
    {
        // ------------------------------------------------------------
        // 公共接口实现
        // ------------------------------------------------------------
        public bool IsReady() => true;

        public int DecideBid(int currentHighestBid, List<Card> handCards, int minBid, int maxBid, int bidStep)
        {
            // 计算手牌评估分数（0~1）
            float strength = EvaluateHandStrength(handCards);

            // 阈值配置
            float callThreshold = 0.5f;      // 叫牌阈值 A
            float fullBidThreshold = 1.1f;   // 满叫阈值 B

            // 低于 A：不叫
            if (strength < callThreshold)
                return -1;

            // 计算可叫范围
            int startBid = (currentHighestBid == 0) ? minBid : currentHighestBid + bidStep;
            if (startBid > maxBid) return -1;

            // 高于 B：满叫
            if (strength >= fullBidThreshold)
                return maxBid;

            // 在 A 和 B 之间：在可叫区间内随机叫分
            int steps = (maxBid - startBid) / bidStep + 1;
            int stepIndex = Random.Range(0, steps);
            return startBid + stepIndex * bidStep;
        }

        public CardSuit DecideTrumpSuit(List<Card> handCards)
        {
            // 统计四种普通花色的数量
            var suitCount = new Dictionary<CardSuit, int>();
            foreach (var suit in new[] { CardSuit.Club, CardSuit.Diamond, CardSuit.Heart, CardSuit.Spade })
                suitCount[suit] = 0;

            foreach (var card in handCards)
            {
                if (!card.IsJoker && card.cardRank != CardRank.Two)
                {
                    suitCount[card.cardSuit]++;
                }
            }

            // 选择数量最多的花色
            int maxCount = suitCount.Values.Max();
            var candidates = suitCount.Where(kv => kv.Value == maxCount).Select(kv => kv.Key).ToList();
            if (candidates.Count == 0) return CardSuit.Club;
            return candidates[Random.Range(0, candidates.Count)];
        }

        public List<Card> DecideDiscards(List<Card> handCards, int discardCount)
        {
            // 获取当前主花色（可能为 null，表示没有主花色）
            CardSuit? trumpSuit = GameFlowController.Instance?.TrumpSuit;

            // 判断一张牌是否属于“主牌”
            System.Func<Card, bool> isTrumpCard = (c) =>
                c.IsJoker ||
                c.cardRank == CardRank.Two ||
                (trumpSuit.HasValue && c.cardSuit == trumpSuit.Value);

            // 判断一张牌是否为大副牌（非主牌且是 J/Q/K/A）
            System.Func<Card, bool> isBigSideCard = (c) =>
                !isTrumpCard(c) && (
                    c.cardRank == CardRank.Jack ||
                    c.cardRank == CardRank.Queen ||
                    c.cardRank == CardRank.King ||
                    c.cardRank == CardRank.Ace
                );

            // 分离三种牌型
            var trumpCards = handCards.Where(isTrumpCard).ToList();
            var bigSideCards = handCards.Where(isBigSideCard).ToList();
            var smallCards = handCards.Except(trumpCards).Except(bigSideCards).ToList();

            var discards = new List<Card>();

            // 1. 优先弃小牌（最小价值）
            discards.AddRange(smallCards);
            if (discards.Count >= discardCount)
                return discards.Take(discardCount).ToList();

            // 2. 其次弃大副牌
            discards.AddRange(bigSideCards);
            if (discards.Count >= discardCount)
                return discards.Take(discardCount).ToList();

            // 3. 最后弃主牌（不得已）
            discards.AddRange(trumpCards);
            return discards.Take(discardCount).ToList();
        }

        public Card DecidePlayCard(
            List<Card> handCards,
            CardSuit? leadSuit,
            CardSuit? trumpSuit,
            int mySeatIndex,
            List<Card> currentTrickCards,
            List<int> trickPlayOrder)
        {
            var valid = GetValidCards(handCards, leadSuit, trumpSuit);
            if (valid.Count == 0) return handCards[0];

            // 默认策略：出最小牌
            valid.Sort((a, b) => CardComparer.SuitCompare(b, a, trumpSuit));
            return valid[valid.Count - 1];
        }

        // ------------------------------------------------------------
        // 高级接口暂不实现
        // ------------------------------------------------------------
        public void UpdateMemory(int seatIndex, Card playedCard) { }
        public float EvaluateSituation() => 0f;

        // ------------------------------------------------------------
        // 私有辅助方法
        // ------------------------------------------------------------
        private float EvaluateHandStrength(List<Card> hand)
        {
            if (hand.Count == 0) return 0;
            float score = 0f;

            foreach (Card c in hand)
            {
                if (c.IsJoker)
                    score += 1.6f;
                else if (c.cardRank == CardRank.Two)
                    score += 1.2f;
                else if (c.cardRank == CardRank.Ace)
                    score += 0.7f;
                else if (c.cardRank == CardRank.King)
                    score += 0.6f;
                else if (c.cardRank == CardRank.Queen)
                    score += 0.4f;
                else if (c.cardRank == CardRank.Jack)
                    score += 0.3f;
                else if (c.cardRank == CardRank.Ten)
                    score += 0.35f;
                else if (c.cardRank == CardRank.Nine || c.cardRank == CardRank.Eight || c.cardRank == CardRank.Seven)
                    score += 0.1f;
                else if (c.cardRank == CardRank.Six || c.cardRank == CardRank.Five)
                    score += 0.05f;
                else if (c.cardRank == CardRank.Four || c.cardRank == CardRank.Three)
                    score += 0.02f;
            }

            return Mathf.Clamp01(score / 5.6f);
        }

        private List<Card> GetValidCards(List<Card> hand, CardSuit? leadSuit, CardSuit? trumpSuit)
        {
            if (leadSuit == null) return new List<Card>(hand);
            List<Card> valid = new List<Card>();
            foreach (Card c in hand)
                if (CardManagement.CanDealCard(hand, leadSuit.Value, c))
                    valid.Add(c);
            return valid;
        }
    }
}