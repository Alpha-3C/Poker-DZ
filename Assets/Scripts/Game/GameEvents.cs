using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局事件总线，管理所有与游戏逻辑解耦的事件。
/// 用法：订阅者使用 GameEvents.OnXXX += Handler；触发者调用 GameEvents.TriggerXXX()。
/// </summary>
public static class GameEvents
{
    // ---------------------- 插槽（手牌）相关事件 ----------------------
    /// <summary> 手牌更新（参数：座位索引 或 -1 表示所有座位） </summary>
    public static event Action<int> OnSlotCardsChanged;

    /// <summary> 激活/禁用插槽按钮（参数：座位索引，是否激活） </summary>
    public static event Action<int, bool> OnSlotButtonsActivated;

    /// <summary> 精细控制每个插槽按钮的启用状态（参数：座位索引，每个按钮的启用列表） </summary>
    public static event Action<int, List<bool>> OnSlotButtonsEnabled;

    /// <summary> 清除指定座位的插槽选中状态（参数：座位索引） </summary>
    public static event Action<int> OnDiscardsCleared;

    /// <summary> 底牌选择阶段，玩家选中/取消选中某张手牌（参数：卡牌，是否选中） </summary>
    public static event Action<Card, bool> OnDiscardSelected;

    /// <summary> 出牌阶段，玩家选中/取消选中某张手牌（参数：卡牌，对应Slot的GameObject，是否选中） </summary>
    public static event Action<Card, GameObject, bool> OnCardSelectedInPlaying;

    // ---------------------- 触发方法 ----------------------
    #region 触发方法
    public static void TriggerSlotCardsChanged(int playerIndex)
    {
        OnSlotCardsChanged?.Invoke(playerIndex);
    }

    public static void TriggerSlotButtonsActivated(int seatIndex, bool isActive)
    {
        OnSlotButtonsActivated?.Invoke(seatIndex, isActive);
    }

    public static void TriggerSlotButtonsEnabled(int seatIndex, List<bool> enables)
    {
        OnSlotButtonsEnabled?.Invoke(seatIndex, enables);
    }

    public static void TriggerDiscardsCleared(int seatIndex)
    {
        OnDiscardsCleared?.Invoke(seatIndex);
    }

    public static void TriggerDiscardSelected(Card card, bool isSelected)
    {
        OnDiscardSelected?.Invoke(card, isSelected);
    }

    public static void TriggerCardSelectedInPlaying(Card card, GameObject slotObj, bool isSelected)
    {
        OnCardSelectedInPlaying?.Invoke(card, slotObj, isSelected);
    }
    #endregion
}