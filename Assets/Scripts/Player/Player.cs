using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using AI;

public enum PlayerPosition
{
    South, East, North, West
}

public enum PlayerTeam  //Íæ¼ÒÕóÓª
{
    Host, Client
}

[Serializable]
public class Player
{
    public PlayerPosition Position;
    public string PlayerName;
    public int? PlayerID;
    public int score;
    public bool IsAI;
    public IAIPlayer aiPlayer;

    public PlayerTeam Team;
    public bool IsKittyMaster;       //µ×ÅÆÈ¨
    public List<Card> HandCards;
    //public List<Card> SelectedCards;

    public void AddCard(Card card)
    {
        HandCards.Add(card);
    }

    public bool RemoveCard(Card card)
    {
        return HandCards.Remove(card);
    }
    
    /*
    public void ToggleCardSelection(Card card)
    {
        if (SelectedCards.Contains(card))
            SelectedCards.Remove(card);
        else SelectedCards.Add(card);
    }

    public void ClearSelectedCards()
    {
        SelectedCards.Clear();
    }*/
}
