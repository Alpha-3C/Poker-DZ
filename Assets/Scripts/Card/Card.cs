using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum CardSuit
{
    Diamond, Club, Heart, Spade, BlackJoker, RedJoker
}

public enum CardRank
{
    Joker=0, Ace, Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King
}

[Serializable]
public class Card
{
    public CardSuit cardSuit;
    public CardRank cardRank;
    public bool IsJoker;
    public int cardIndex;
}
