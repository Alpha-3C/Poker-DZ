using System;

public static class CardComparer
{
    // ==================== 公共比较方法 ====================

    /// <summary>
    /// 用于手牌排序：先按花色分组（主花色 > 副花色），再按点数排序（Joker > 2 > Ace > ... > 3）
    /// </summary>
    public static int SuitCompare(Card a, Card b, CardSuit? trumpSuit = null)
    {
        if (a == b) return 0;
        if (a == null) return -1;
        if (b == null) return 1;

        // 1. Joker 处理（最大）
        bool aJoker = a.IsJoker, bJoker = b.IsJoker;
        if (aJoker && bJoker)
            return GetJokerWeight(a).CompareTo(GetJokerWeight(b));
        if (aJoker) return 1;
        if (bJoker) return -1;

        // 2. 2 的处理（仅次于Joker）
        bool aTwo = a.cardRank == CardRank.Two;
        bool bTwo = b.cardRank == CardRank.Two;
        if (aTwo && bTwo)
            return CompareTwoSuit(a.cardSuit, b.cardSuit, trumpSuit);
        if (aTwo) return 1;
        if (bTwo) return -1;

        // 3. 普通牌：先比花色（主花色最高），再比点数
        int aSuitPriority = GetSuitPriorityForSorting(a.cardSuit, trumpSuit);
        int bSuitPriority = GetSuitPriorityForSorting(b.cardSuit, trumpSuit);
        int suitComp = aSuitPriority.CompareTo(bSuitPriority);
        if (suitComp != 0) return suitComp;

        return GetRankWeight(a.cardRank).CompareTo(GetRankWeight(b.cardRank));
    }

    /// <summary>
    /// 用于决定一墩中的赢家
    /// </summary>
    public static int TrickCompare(Card a, Card b, CardSuit leadSuit, CardSuit? trumpSuit = null)
    {
        if (a == b) return 0;
        if (a == null) return -1;
        if (b == null) return 1;

        // 处理 Joker 和 2（它们优先于所有普通牌）
        bool aIsSpecial = a.IsJoker || a.cardRank == CardRank.Two;
        bool bIsSpecial = b.IsJoker || b.cardRank == CardRank.Two;

        if (aIsSpecial || bIsSpecial)
        {
            // 双方都是特殊牌 -> 直接比较
            if (aIsSpecial && bIsSpecial)
                return ComparePointsInSameSuit(a, b, trumpSuit);
            // 一方特殊，一方普通 -> 特殊牌更大
            return aIsSpecial ? 1 : -1;
        }

        // 普通牌：比较花色等级
        int aLevel = GetSuitLevelForTrick(a.cardSuit, leadSuit, trumpSuit);
        int bLevel = GetSuitLevelForTrick(b.cardSuit, leadSuit, trumpSuit);
        if (aLevel != bLevel)
            return aLevel.CompareTo(bLevel);

        // 同花色（或同为副花色）内比较点数
        return ComparePointsInSameSuit(a, b, trumpSuit);
    }
    // ==================== 私有辅助方法 ====================

    /// <summary>花色基础权重（黑桃>红桃>梅花>方块）</summary>
    private static int GetBaseSuitWeight(CardSuit suit)
    {
        switch (suit)
        {
            case CardSuit.Spade: return 4;
            case CardSuit.Heart: return 3;
            case CardSuit.Club: return 2;
            case CardSuit.Diamond: return 1;
            default: return 0;
        }
    }

    /// <summary>排序时使用的花色优先级（主花色最高）</summary>
    private static int GetSuitPriorityForSorting(CardSuit suit, CardSuit? trumpSuit)
    {
        if (trumpSuit.HasValue && suit == trumpSuit.Value)
            return 100; // 主花色远大于所有副花色
        return GetBaseSuitWeight(suit);
    }

    /// <summary>普通牌的点数权重</summary>
    private static int GetRankWeight(CardRank rank)
    {
        switch (rank)
        {
            case CardRank.Ace: return 14;
            case CardRank.King: return 13;
            case CardRank.Queen: return 12;
            case CardRank.Jack: return 11;
            case CardRank.Ten: return 10;
            case CardRank.Nine: return 9;
            case CardRank.Eight: return 8;
            case CardRank.Seven: return 7;
            case CardRank.Six: return 6;
            case CardRank.Five: return 5;
            case CardRank.Four: return 4;
            case CardRank.Three: return 3;
            default: return 0;
        }
    }

    /// <summary>Joker权重：大王 > 小王</summary>
    private static int GetJokerWeight(Card card)
    {
        return card.cardSuit == CardSuit.RedJoker ? 2 : 1;
    }

    /// <summary>两张2的花色比较（主花色的2 > 其他花色的2，副2之间相等）</summary>
    private static int CompareTwoSuit(CardSuit a, CardSuit b, CardSuit? trumpSuit)
    {
        bool aIsTrump = trumpSuit.HasValue && a == trumpSuit.Value;
        bool bIsTrump = trumpSuit.HasValue && b == trumpSuit.Value;

        if (aIsTrump && bIsTrump) return 0;   // 都是主花色2，相等
        if (aIsTrump) return 1;               // a是主花色2，b不是
        if (bIsTrump) return -1;              // b是主花色2，a不是
        return 0;                             // 都是副2，相等
    }

    /// <summary>墩内花色等级：全局主 > 墩主 > 其他</summary>
    private static int GetSuitLevelForTrick(CardSuit suit, CardSuit leadSuit, CardSuit? trumpSuit)
    {
        if (trumpSuit.HasValue && suit == trumpSuit.Value) return 3;
        if (suit == leadSuit) return 2;
        return 1;
    }

    /// <summary>同花色（或同为副花色）内比较点数</summary>
    private static int ComparePointsInSameSuit(Card a, Card b, CardSuit? trumpSuit)
    {
        bool aJoker = a.IsJoker, bJoker = b.IsJoker;
        if (aJoker && bJoker) return GetJokerWeight(a).CompareTo(GetJokerWeight(b));
        if (aJoker) return 1;
        if (bJoker) return -1;

        bool aTwo = a.cardRank == CardRank.Two;
        bool bTwo = b.cardRank == CardRank.Two;
        if (aTwo && bTwo) return CompareTwoSuit(a.cardSuit, b.cardSuit, trumpSuit);
        if (aTwo) return 1;
        if (bTwo) return -1;

        return GetRankWeight(a.cardRank).CompareTo(GetRankWeight(b.cardRank));
    }
}