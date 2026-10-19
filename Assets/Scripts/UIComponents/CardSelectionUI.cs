/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardSelectionUI : MonoBehaviour
{
    public Button ConfirmButton;
    public Button CancelButton;
    public GameObject SuitsButtonController;

    private GameFlowController gameFlowController;
    private CardManagement cardManager;
    private KittySelectionManagement kittyManager;

    void Start()
    {
        kittyManager = GetComponent<KittySelectionManagement>();
        gameFlowController = GameFlowController.Instance;
        cardManager = CardManagement.Instance;
        InitializeSelectionUI();
    }


    private void InitializeSelectionUI()
    {
        DeactivateSelectionUI();
        DeactivateSuitsController();
    }

    public void ActivateSuitsController()
    {
        SuitsButtonController.SetActive(true);
    }
    public void DeactivateSuitsController()
    {
        SuitsButtonController.SetActive(false);
    }
    public void ActivateSelectionUI()
    {
        ConfirmButton.gameObject.SetActive(true);
        CancelButton.gameObject.SetActive(true);
    }

    public void DeactivateSelectionUI()
    {
        ConfirmButton.gameObject.SetActive(false);
        CancelButton.gameObject.SetActive(false);
    }

    public void OnSuitSelectionButtonClicked(CardSuit TrumpSuit)
    {
        kittyManager.SetTrumpSuit(TrumpSuit);
        kittyManager.EndTrumpSuitSelection();
    }


    public void OnConfirmButtonClicked()
    {
        if(kittyManager.Discards.Count == gameFlowController.kittyCardsCount)
        {
            Debug.Log($"Valid number of discards!");
            cardManager.Discards = new List<Card>(kittyManager.Discards);
            kittyManager.EndKittySelection();
        }
        else
        {
            Debug.Log($"Invalid number of discards!");
            kittyManager.ClearDiscards();
        }
    }

    public void OnCancelButtonClicked()
    {
        Debug.Log("Cancel discards selection.");
        kittyManager.ClearDiscards();
    }
}*/
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardSelectionUI : MonoBehaviour
{
    public Button ConfirmButton;
    public Button CancelButton;
    public GameObject SuitsButtonController; // 花色按钮容器
    public GameObject suitButtonPrefab;       // 花色按钮预制体（需拖拽）
    public Image trumpSuitIndicator;          // 主花色指示器（拖拽）
    // 动态生成的按钮列表
    private List<Button> dynamicSuitButtons = new List<Button>();

    public float buttonSpacing = 100f;   // 按钮之间的水平间距（Inspector中调整）
    public Vector2 buttonSize = new Vector2(98, 130); // 按钮尺寸（可选）


    // 回调
    private Action<CardSuit> onTrumpSuitSelected;
    private Action onDiscardConfirm;
    private Action onDiscardCancel;

    private GameFlowController gameFlowController;
    private KittySelectionManagement kittyManager;
    private CardManagement cardManager;

    void Start()
    {
        kittyManager = GetComponent<KittySelectionManagement>();
        gameFlowController = GameFlowController.Instance;
        cardManager = CardManagement.Instance;

        ConfirmButton.onClick.AddListener(OnConfirmButtonClicked);
        CancelButton.onClick.AddListener(OnCancelButtonClicked);

        InitializeSelectionUI();
    }

    private void InitializeSelectionUI()
    {
        DeactivateSelectionUI();
        DeactivateSuitsController();
        HideTrumpSuitIndicator();
        ClearSuitButtons();
    }

    private void ClearSuitButtons()
    {
        foreach (var btn in dynamicSuitButtons)
        {
            if (btn != null) Destroy(btn.gameObject);
        }
        dynamicSuitButtons.Clear();
    }

    public void ActivateSuitsController(Action<CardSuit> onSelected)
    {
        onTrumpSuitSelected = onSelected;
        ClearSuitButtons();
        CreateSuitButtons();
        SuitsButtonController.SetActive(true);
    }

    private void CreateSuitButtons()
    {
        if (suitButtonPrefab == null)
        {
            Debug.LogError("Suit button prefab not assigned!");
            return;
        }

        // 花色顺序：黑桃、红桃、方块、梅花
        CardSuit[] suits = { CardSuit.Spade, CardSuit.Heart, CardSuit.Diamond, CardSuit.Club };
        RectTransform parentRect = SuitsButtonController.GetComponent<RectTransform>();

        // 计算起始X（使整体居中）
        float totalWidth = buttonSpacing * (suits.Length - 1);
        float startX = -totalWidth / 2f;

        for (int i = 0; i < suits.Length; i++)
        {
            GameObject btnObj = Instantiate(suitButtonPrefab, SuitsButtonController.transform);
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();

            // 设置位置和大小
            btnRect.anchoredPosition = new Vector2(startX + i * buttonSpacing, 0);
            btnRect.sizeDelta = buttonSize;

            // 设置花色图标
            Button btn = btnObj.GetComponent<Button>();
            Image img = btnObj.GetComponentInChildren<Image>();
            if (img != null)
                img.sprite = cardManager.GetSuitSprite(suits[i]);

            // 绑定点击事件
            btn.onClick.RemoveAllListeners();
            CardSuit capturedSuit = suits[i]; // 捕获循环变量
            btn.onClick.AddListener(() => OnSuitButtonClicked(capturedSuit));

            dynamicSuitButtons.Add(btn);
        }
    }

    private void OnSuitButtonClicked(CardSuit suit)
    {
        onTrumpSuitSelected?.Invoke(suit);
        DeactivateSuitsController();
    }

    public void DeactivateSuitsController()
    {
        SuitsButtonController.SetActive(false);
    }

    // 主花色指示器
    public void SetTrumpSuitIndicator(CardSuit suit)
    {
        if (trumpSuitIndicator == null) return;
        trumpSuitIndicator.sprite = cardManager.GetSuitSprite(suit);
        trumpSuitIndicator.gameObject.SetActive(true);
    }

    public void HideTrumpSuitIndicator()
    {
        if (trumpSuitIndicator != null)
            trumpSuitIndicator.gameObject.SetActive(false);
    }

    // ----- 弃牌阶段 UI -----
    public void ActivateDiscardSelection(int requiredCount, Action onConfirm, Action onCancel)
    {
        this.onDiscardConfirm = onConfirm;
        this.onDiscardCancel = onCancel;
        ConfirmButton.gameObject.SetActive(true);
        CancelButton.gameObject.SetActive(true);
        Debug.Log($"请选择 {requiredCount} 张牌作为弃牌，当前已选 0 张");
    }

    public void DeactivateSelectionUI()
    {
        ConfirmButton.gameObject.SetActive(false);
        CancelButton.gameObject.SetActive(false);
    }

    public void ReactivateDiscardSelection()
    {
        ConfirmButton.gameObject.SetActive(true);
        CancelButton.gameObject.SetActive(true);
    }

    private void OnConfirmButtonClicked()
    {
        onDiscardConfirm?.Invoke();
    }

    private void OnCancelButtonClicked()
    {
        onDiscardCancel?.Invoke();
    }
}
