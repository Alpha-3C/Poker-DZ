/*using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BiddingUI : MonoBehaviour
{
    public Button passButton;
    public Button confirmButton;
    public Scrollbar biddingScrollbar;
    public TMP_Text biddingPointText;
    public TMP_Text finalBiddingPointText;

    private GameFlowController gameFlowController;
    private BiddingManagement biddingManager;

    private int humanPlayerIndex;
    private int startBid;
    private int totalSteps;
    private int currentBid;
    public bool RealPlayerHaveBidded;

    private void Start()
    {
        gameFlowController = GameFlowController.Instance;
        biddingManager = GetComponent<BiddingManagement>();

        InitializeBiddingUI();
    }

    public void InitializeBiddingUI()
    {
        DeactivateBiddingUI();

        humanPlayerIndex = -1;

        biddingPointText.text = gameFlowController.minimumBid.ToString();
        finalBiddingPointText.text = "BiddingPoint:" + gameFlowController.biddingPoint.ToString();
    }

    public void ActivateBiddingUI(int currentHighestBid, int currentPlayerIndex, Action<int> onConfirm, Action onPass)
    {
        // 保存回调
        this.onConfirmCallback = onConfirm;
        this.onPassCallback = onPass;


        passButton.gameObject.SetActive(true);
        confirmButton.gameObject.SetActive(true);
        biddingScrollbar.gameObject.SetActive(true);
        biddingPointText.gameObject.SetActive(true);

        humanPlayerIndex = currentPlayerIndex;

        startBid = (currentHighestBid == 0) ? gameFlowController.minimumBid : currentHighestBid + gameFlowController.bidIncrease;
        currentBid = startBid;
        totalSteps = startBid < gameFlowController.maximumBid?
            (gameFlowController.maximumBid - startBid)/gameFlowController.bidIncrease + 1 : 0;
        Debug.Log(startBid);
        Debug.Log(totalSteps);

        biddingPointText.text = startBid.ToString();
        biddingScrollbar.numberOfSteps = totalSteps;
    }

    public void DeactivateBiddingUI()
    {
        passButton.gameObject.SetActive(false);
        confirmButton.gameObject.SetActive(false);
        biddingScrollbar.gameObject.SetActive(false);
        biddingPointText.gameObject.SetActive(false);
    }

    public void OnBiddingScrollbarValueChanged()
    {
        int currentStepIndex = Mathf.RoundToInt(biddingScrollbar.value * (totalSteps - 1));

        currentBid = startBid + currentStepIndex * gameFlowController.bidIncrease;
        biddingPointText.text = currentBid.ToString();
    }

    public void OnPassButtonClicked()
    {
        RealPlayerHaveBidded = true;
        DeactivateBiddingUI();
        biddingManager.PlayerPass(humanPlayerIndex);
    }
    public void OnConfirmButtonClicked()
    {
        RealPlayerHaveBidded = true;
        DeactivateBiddingUI();
        Debug.Log($"currentBid: {currentBid}");
        biddingManager.PlayerBiddingFor(humanPlayerIndex, currentBid);
    }

}*/
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BiddingUI : MonoBehaviour
{
    [Header("UI Components")]
    public Button passButton;
    public Button confirmButton;
    public Scrollbar biddingScrollbar;
    public TMP_Text biddingPointText;
    public TMP_Text finalBiddingPointText;

    // 回调委托（由 BiddingManagement 注入）
    private Action<int> onConfirmCallback;
    private Action onPassCallback;

    // 内部状态
    private GameFlowController gameFlowController;
    private BiddingManagement biddingManager;
    private int currentHighestBid;      // 当前最高叫分
    private int startBid;               // 起始叫分（第一个可选叫分）
    private int totalSteps;             // Scrollbar 步数
    private int currentSelectedBid;     // 当前选中的叫分
    private int humanPlayerIndex;       // 人类玩家座位索引（用于调试）

    private void Start()
    {
        gameFlowController = GameFlowController.Instance;
        biddingManager = GetComponent<BiddingManagement>();

        // 绑定 UI 按钮事件
        passButton.onClick.AddListener(OnPassButtonClicked);
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        biddingScrollbar.onValueChanged.AddListener(OnBiddingScrollbarValueChanged);

        InitializeBiddingUI();
    }

    private void InitializeBiddingUI()
    {
        DeactivateBiddingUI();
        finalBiddingPointText.text = "BiddingPoint:" + gameFlowController.biddingPoint.ToString();
    }

    /// <summary>
    /// 激活叫分 UI，并注入回调
    /// </summary>
    public void ActivateBiddingUI(int currentHighestBid, int playerIndex,
        Action<int> onConfirm, Action onPass)
    {
        // 保存回调
        this.onConfirmCallback = onConfirm;
        this.onPassCallback = onPass;
        this.currentHighestBid = currentHighestBid;
        this.humanPlayerIndex = playerIndex;

        // 计算有效叫分范围
        startBid = (currentHighestBid == 0)
            ? gameFlowController.minimumBid
            : currentHighestBid + gameFlowController.bidIncrease;

        if (startBid > gameFlowController.maximumBid)
        {
            // 没有可叫的分，只能 Pass
            totalSteps = 0;
            biddingScrollbar.numberOfSteps = 1;   // 避免 Scrollbar 报错
            currentSelectedBid = 0;
            biddingPointText.text = "无有效叫分";
            confirmButton.interactable = false;
        }
        else
        {
            totalSteps = (gameFlowController.maximumBid - startBid) / gameFlowController.bidIncrease + 1;
            biddingScrollbar.numberOfSteps = totalSteps;
            biddingScrollbar.value = 0;   // 默认第一个选项
            OnBiddingScrollbarValueChanged(0);
            confirmButton.interactable = true;
        }

        // 显示 UI 元素
        passButton.gameObject.SetActive(true);
        confirmButton.gameObject.SetActive(true);
        biddingScrollbar.gameObject.SetActive(true);
        biddingPointText.gameObject.SetActive(true);
    }

    public void DeactivateBiddingUI()
    {
        passButton.gameObject.SetActive(false);
        confirmButton.gameObject.SetActive(false);
        biddingScrollbar.gameObject.SetActive(false);
        biddingPointText.gameObject.SetActive(false);

        // 清空回调，防止残留
        onConfirmCallback = null;
        onPassCallback = null;
    }

    private void OnBiddingScrollbarValueChanged(float value)
    {
        if (totalSteps <= 0)
        {
            currentSelectedBid = 0;
            biddingPointText.text = "无有效叫分";
            return;
        }

        int stepIndex = Mathf.RoundToInt(value * (totalSteps - 1));
        stepIndex = Mathf.Clamp(stepIndex, 0, totalSteps - 1);
        currentSelectedBid = startBid + stepIndex * gameFlowController.bidIncrease;
        biddingPointText.text = currentSelectedBid.ToString();
    }

    private void OnPassButtonClicked()
    {
        onPassCallback?.Invoke();
        DeactivateBiddingUI();
    }

    private void OnConfirmButtonClicked()
    {
        if (currentSelectedBid <= 0 && totalSteps > 0)
        {
            Debug.LogWarning("无效叫分，请重新选择");
            return;
        }
        onConfirmCallback?.Invoke(currentSelectedBid);
        DeactivateBiddingUI();
    }

}