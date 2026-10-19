using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AI;

public class PlayerManagement : MonoBehaviour
{
    public static PlayerManagement Instance;
    public GameFlowController gameFlowController;

    [Header("Players")]
    public List<Player> players;
    //public List<DealAreaBinder> dealAreas;
    public List<DeskUI> deskUIs;

    [Header("Game Settings")]
    public int playerCount = 4;

    public string[] playerNames = { "You", "East", "North", "West" };

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        InitializePlayers();
    }

    private void Start()
    {
        gameFlowController = GameFlowController.Instance;

    }
    public void InitializePlayers()
    {
        players = new List<Player>();
        for (int i = 0; i < playerCount; i++)
        {
            bool isAI = !(i == 0);
            Player player = new Player
            {
                PlayerName = playerNames[i],
                PlayerID = i,
                IsAI = isAI, // 第一个玩家是人类
                Position = (PlayerPosition)i,
                HandCards = new List<Card>(),
                //SelectedCards = new List<Card>(),
                score = 0
            };
            if(isAI) player.aiPlayer = i % 2 == 0 ? new BasicAIPlayer() : new AdvancedAIPlayer();  
             Debug.Log($"{i}");
            players.Add(player);

            //UpdatePlayerDesk(i);
            deskUIs[i].playerIndex = i;
        }

        Debug.Log($"Initialized {players.Count} players");
    }

    public Player GetPlayer(int playerId)
    {
        if (playerId >= 0 && playerId < players.Count)
            return players[playerId];
        return null;
    }

    /// <summary>
    /// 根据座位获取玩家在 players 列表中的索引
    /// </summary>
    public int GetPlayerIndexAtPosition(PlayerPosition pos)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].Position == pos)
                return i;
        }
        Debug.LogWarning($"未找到座位 {pos} 对应的玩家，返回 -1");
        return -1;
    }


    public int? GetHumanPlayerIndex()
    {
        for (int i = 0; i < playerCount; i++)
        {
            if(!players[i].IsAI)
                return i;
        }
        
        return null;
    }
    public int DetermineNextPlayer(int playerIndex)
    {
        return (playerIndex + 1) % players.Count;
    }

    public void SortPlayerHandCards(int playerIndex, CardSuit? TrumpSuit)
    {
        if(playerIndex == gameFlowController.allPlayerIndexSign)
        {
            foreach(Player player in players)
            {
                CardManagement.SortHands(player.HandCards, TrumpSuit);
            }
        }
        else
        {
            CardManagement.SortHands(players[playerIndex].HandCards, TrumpSuit);
        }
    }
}
