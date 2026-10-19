using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KittySlotsUI : MonoBehaviour
{
    [Header("Slots")]
    public List<Slot> slots;            // 底牌卡槽（按顺序，长度应等于底牌数量）
    public int kittyCardsCount = 6;     // 应与 GameFlowController.kittyCardsCount 一致

    private CardManagement cardManager;

    private void Start()
    {
        cardManager = CardManagement.Instance;
    }

    /// <summary>
    /// 初始化底牌区域：清空所有槽位，并激活正确数量的槽位。
    /// </summary>
    public void Initialize()
    {
        int slotCount = slots.Count;
        for (int i = 0; i < slotCount; i++)
        {
            if (i < kittyCardsCount)
            {
                slots[i].gameObject.SetActive(true);
                slots[i].UpdateSlot(null, false);   // 清空显示（可显示空白）
            }
            else
            {
                slots[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 将底牌区域显示为背面（隐藏点数/花色）。
    /// 通常在发牌后、尚未亮牌时调用。
    /// </summary>
    public void ShowBacks()
    {
        if (cardManager.KittyCards.Count != kittyCardsCount)
        {
            Debug.LogWarning($"底牌数量不符：期望 {kittyCardsCount}，实际 {cardManager.KittyCards.Count}");
            return;
        }
        for (int i = 0; i < kittyCardsCount; i++)
        {
            slots[i].UpdateSlot(cardManager.KittyCards[i], false);
        }
    }

    /// <summary>
    /// 将底牌区域显示为正面（亮牌）。
    /// 通常在叫分结束、进入底牌选择阶段时调用，让庄家看到底牌。
    /// </summary>
    public void ShowFaces()
    {
        if (cardManager.KittyCards.Count != kittyCardsCount)
        {
            Debug.LogWarning($"底牌数量不符：期望 {kittyCardsCount}，实际 {cardManager.KittyCards.Count}");
            return;
        }
        for (int i = 0; i < kittyCardsCount; i++)
        {
            slots[i].UpdateSlot(cardManager.KittyCards[i], true);
        }
    }

    /// <summary>
    /// 清空底牌区域（例如一轮结束后重置）。
    /// </summary>
    public void Clear()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].UpdateSlot(null, false);
        }
    }

    /// <summary>
    /// 翻牌特效（逐个翻开底牌，带延迟）
    /// 未来可扩展动画，目前仅按顺序显示正面。
    /// </summary>
    public IEnumerator RevealWithDelay(float delayPerCard = 0.15f)
    {
        if (cardManager.KittyCards.Count != kittyCardsCount)
        {
            Debug.LogWarning($"底牌数量不符，无法播放翻牌特效");
            yield break;
        }
        for (int i = 0; i < kittyCardsCount; i++)
        {
            slots[i].UpdateSlot(cardManager.KittyCards[i], true);
            yield return new WaitForSeconds(delayPerCard);
        }
    }
}