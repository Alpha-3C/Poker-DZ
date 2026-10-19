using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerData : MonoBehaviour
{
    public Image PlayerIcon;
    public TMP_Text NameText;
    public TMP_Text IDText;
    public int playerIndex;

    public PlayerManagement playerManager;

    public void Start()
    {
        playerManager = PlayerManagement.Instance;
    }

    public void UpdatePlayer(int playerIndex)
    {
        if (playerManager == null)  playerManager = PlayerManagement.Instance;
        if (playerManager.players == null)
        {
            Debug.LogError("PlayerManager or players list is null");
            return;
        }

        if (playerIndex < 0 || playerIndex >= playerManager.players.Count)
        {
            Debug.LogError($"Invalid playerIndex: {playerIndex}");
            return;
        }

        var player = playerManager.players[playerIndex];
        if (player == null)
        {
            Debug.LogError($"Player at index {playerIndex} is null");
            return;
        }
        this.playerIndex = playerIndex;
        NameText.text = playerManager.players[playerIndex].PlayerName == null? "": playerManager.players[playerIndex].PlayerName;
        IDText.text = playerManager.players[playerIndex].PlayerID == null ? "NaN" : playerManager.players[playerIndex].PlayerID.ToString();
    }

}
