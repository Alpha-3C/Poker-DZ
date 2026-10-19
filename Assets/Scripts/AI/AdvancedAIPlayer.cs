using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AI
{
    /// <summary>
    /// 进阶 AI 策略（包含叫分概率、弃牌优先级、出牌辅助判断等）
    /// </summary>
    public class AdvancedAIPlayer : IAIPlayer
    {
        // ------------------------------------------------------------
        // 配置参数
        // ------------------------------------------------------------
        private const float PLAY_AROUND = 0.1f;           // 乱玩率 Pw
        private const float CAUTION_THRESHOLD = 80f;      // 谨慎阈值 (最低叫分)
        private const float BASE_BID_PROB = 0.5f;         // 基础叫分欲望
        private const float FULL_BID_STRENGTH_MIN = 1.1f; // 允许FullBid的最小手牌强度

        // ------------------------------------------------------------
        // 公共接口实现
        // ------------------------------------------------------------
        public bool IsReady() => true;

        public int DecideBid(int currentHighestBid, List<Card> handCards, int minBid, int maxBid, int bidStep)
        {
            // 如果没有可叫的范围，直接 Pass
            int startBid = (currentHighestBid == 0) ? minBid : currentHighestBid + bidStep;
            if (startBid > maxBid) return -1;

            float strength = EvaluateHandStrength(handCards);
            bool isCaution = (currentHighestBid >= CAUTION_THRESHOLD);
            float p = CalculateBidDesire(strength, currentHighestBid, isCaution);

            if (Random.value > p) return -1;

            int add = DetermineAddBid(strength, isCaution);
            int finalBid = startBid + add;
            if (finalBid > maxBid) finalBid = maxBid;
            if (finalBid <= currentHighestBid) return -1;

            if (finalBid == maxBid && strength < FULL_BID_STRENGTH_MIN) return -1;

            return finalBid;
        }

        public CardSuit DecideTrumpSuit(List<Card> handCards)
        {
            var suitCount = new Dictionary<CardSuit, int>();
            foreach (var suit in new[] { CardSuit.Club, CardSuit.Diamond, CardSuit.Heart, CardSuit.Spade })
                suitCount[suit] = 0;

            foreach (var card in handCards)
            {
                if (!card.IsJoker && card.cardRank != CardRank.Two)
                    suitCount[card.cardSuit]++;
            }
            int maxCount = suitCount.Values.Max();
            var candidates = suitCount.Where(kv => kv.Value == maxCount).Select(kv => kv.Key).ToList();
            if (candidates.Count == 0) return CardSuit.Club;
            return candidates[Random.Range(0, candidates.Count)];
        }

        public List<Card> DecideDiscards(List<Card> handCards, int discardCount)
        {
            CardSuit? trumpSuit = GameFlowController.Instance?.TrumpSuit;

            System.Func<Card, bool> isTrumpCard = (c) =>
                c.IsJoker ||
                c.cardRank == CardRank.Two ||
                (trumpSuit.HasValue && c.cardSuit == trumpSuit.Value);

            System.Func<Card, bool> isBigSideCard = (c) =>
                !isTrumpCard(c) && (
                    c.cardRank == CardRank.Jack ||
                    c.cardRank == CardRank.Queen ||
                    c.cardRank == CardRank.King ||
                    c.cardRank == CardRank.Ace
                );

            var trumps = handCards.Where(isTrumpCard).ToList();
            var bigsides = handCards.Where(isBigSideCard).ToList();
            var smalls = handCards.Except(trumps).Except(bigsides).ToList();

            var discards = new List<Card>();
            discards.AddRange(smalls);
            if (discards.Count >= discardCount)
                return discards.Take(discardCount).ToList();
            discards.AddRange(bigsides);
            if (discards.Count >= discardCount)
                return discards.Take(discardCount).ToList();
            discards.AddRange(trumps);
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

            var gfc = GameFlowController.Instance;
            var playerMgr = PlayerManagement.Instance;
            if (gfc == null || playerMgr == null) return valid[0];

            int hostIndex = gfc.HostPlayerIndex;
            bool isHost = (mySeatIndex == hostIndex);

            // 判断友方是否领先（仅当有首出花色且已有出牌时）
            bool friendlyLeading = false;
            if (leadSuit.HasValue && currentTrickCards.Count > 0)
            {
                Card maxCard = currentTrickCards[0];
                int maxIdx = 0;
                for (int i = 1; i < currentTrickCards.Count; i++)
                {
                    if (CardComparer.TrickCompare(currentTrickCards[i], maxCard, leadSuit.Value, trumpSuit) > 0)
                    {
                        maxCard = currentTrickCards[i];
                        maxIdx = i;
                    }
                }
                int leadingPlayer = trickPlayOrder[maxIdx];
                bool leadingIsHost = (leadingPlayer == hostIndex);
                friendlyLeading = (isHost == leadingIsHost);
            }

            if (!isHost) // 闲家
            {
                if (friendlyLeading)
                {
                    var pointCards = valid.Where(c => c.cardRank == CardRank.Five ||
                                                      c.cardRank == CardRank.Ten ||
                                                      c.cardRank == CardRank.King).ToList();
                    if (pointCards.Count > 0) return pointCards[0];
                }
                else
                {
                    // 只有存在首出花色且有出牌记录时才尝试管上
                    if (leadSuit.HasValue && currentTrickCards.Count > 0)
                    {
                        var beatable = valid.Where(c => CanBeat(c, leadSuit.Value, trumpSuit, currentTrickCards, trickPlayOrder)).ToList();
                        if (beatable.Count > 0) return beatable[0];
                    }
                }
            }
            else // 庄家
            {
                int totalPlayed = currentTrickCards.Count;
                int trickNumber = totalPlayed / 4 + 1;
                if (trickNumber <= 3)
                {
                    var mainCards = valid.Where(c => c.IsJoker || c.cardRank == CardRank.Two ||
                                                     (trumpSuit.HasValue && c.cardSuit == trumpSuit.Value)).ToList();
                    if (mainCards.Count > 0) return mainCards[0];
                }
                if (leadSuit.HasValue && currentTrickCards.Count > 0)
                {
                    var beatable = valid.Where(c => CanBeat(c, leadSuit.Value, trumpSuit, currentTrickCards, trickPlayOrder)).ToList();
                    if (beatable.Count > 0) return beatable[0];
                }
            }

            valid.Sort((a, b) => CardComparer.SuitCompare(b, a, trumpSuit));
            return valid[valid.Count - 1];
        }
        // ------------------------------------------------------------
        // 占位接口
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
            var suitDist = new Dictionary<CardSuit, int>();
            foreach (var suit in new[] { CardSuit.Club, CardSuit.Diamond, CardSuit.Heart, CardSuit.Spade })
                suitDist[suit] = 0;

            int totalPoints = 0;
            foreach (var c in hand)
            {
                if (c.IsJoker) score += 1.6f;
                else if (c.cardRank == CardRank.Two) score += 1.2f;
                else if (c.cardRank == CardRank.Ace) score += 0.7f;
                else if (c.cardRank == CardRank.King) score += 0.6f;
                else if (c.cardRank == CardRank.Queen) score += 0.4f;
                else if (c.cardRank == CardRank.Jack) score += 0.3f;
                else if (c.cardRank == CardRank.Ten) score += 0.35f;
                else if (c.cardRank == CardRank.Nine || c.cardRank == CardRank.Eight || c.cardRank == CardRank.Seven)
                    score += 0.1f;
                else if (c.cardRank == CardRank.Six || c.cardRank == CardRank.Five) score += 0.05f;
                else if (c.cardRank == CardRank.Four || c.cardRank == CardRank.Three) score += 0.02f;

                if (!c.IsJoker && c.cardRank != CardRank.Two)
                {
                    suitDist[c.cardSuit]++;
                    if (c.cardRank == CardRank.Five) totalPoints += 5;
                    else if (c.cardRank == CardRank.Ten) totalPoints += 10;
                    else if (c.cardRank == CardRank.King) totalPoints += 10;
                }
            }

            if (suitDist.Values.Any(v => v >= 7))
                score += 0.4f;
            if (totalPoints < 15)
                score -= 0.3f;

            return Mathf.Clamp01(score / 5.6f);
        }

        private float CalculateBidDesire(float strength, int currentHighest, bool isCaution)
        {
            float p = BASE_BID_PROB;
            if (isCaution)
            {
                if (strength < 0.4f)
                    p = PLAY_AROUND;
                else
                {
                    p = PLAY_AROUND + (1 - 2 * PLAY_AROUND) * strength;
                    float reduce = (currentHighest - CAUTION_THRESHOLD) / (100 - CAUTION_THRESHOLD) * 0.2f;
                    p -= reduce;
                }
            }
            else
            {
                p = PLAY_AROUND + (1 - 2 * PLAY_AROUND) * strength;
            }
            return Mathf.Clamp(p, PLAY_AROUND, 1 - PLAY_AROUND);
        }

        private int DetermineAddBid(float strength, bool isCaution)
        {
            int step = GameFlowController.Instance.bidIncrease;
            if (isCaution)
            {
                float pt = 0.2f;
                return Random.value < pt ? step : step * 2;
            }
            else
            {
                float ps = 0.15f;
                float r = Random.value;
                if (r < 2 * ps) return step;
                else if (r < 1 - ps) return step * 2;
                else return step * 3;
            }
        }

        // 辅助方法：判断一张牌是否能管住当前墩中的最大牌
        private bool CanBeat(Card card, CardSuit leadSuit, CardSuit? trumpSuit,
            List<Card> currentTrickCards, List<int> trickPlayOrder)
        {
            if (currentTrickCards.Count == 0) return true;
            Card maxCard = currentTrickCards[0];
            for (int i = 1; i < currentTrickCards.Count; i++)
            {
                if (CardComparer.TrickCompare(currentTrickCards[i], maxCard, leadSuit, trumpSuit) > 0)
                    maxCard = currentTrickCards[i];
            }
            return CardComparer.TrickCompare(card, maxCard, leadSuit, trumpSuit) > 0;
        }

        private bool IsHost()
        {
            var gfc = GameFlowController.Instance;
            if (gfc == null) return false;
            // 需要知道当前 AI 对应玩家索引，这里只是一个占位
            return false;
        }

        private bool IsFriendlyWinning()
        {
            // 需要外部信息，简化返回 false
            return false;
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