using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardManagement : MonoBehaviour
{
    public static CardManagement Instance;
    public PlayerManagement playerManager;
    public GameFlowController gameFlowController;
    //public List<Player> players;


    [Header("Card Settings")]
    public GameObject cardPrefab;
    public List<Card> cards = new List<Card>();

    public List<Card> KittyCards;
    public List<Card> Discards;

    public Sprite[] allCardSprites;
    public Sprite backcardSprite;
    public Sprite emptySprite;

    [Header("Suit Sprites")]
    public Sprite spadeSprite;
    public Sprite heartSprite;
    public Sprite diamondSprite;
    public Sprite clubSprite;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadCardSprites();
            LoadSuitSprites();
            InitializeDeck();
        }
    }
    private void Start()
    {
        playerManager = PlayerManagement.Instance;
        gameFlowController = GameFlowController.Instance;
        //players = PlayerManagement.Instance.players;
    }
    private void LoadCardSprites()
    {
        allCardSprites = Resources.LoadAll<Sprite>("Poker");
        backcardSprite = allCardSprites[54];
        emptySprite = allCardSprites[55];

        Debug.Log($"Successfully loaded {allCardSprites.Length} card sprites");
    }

    // CardManagement.cs 中添加

    private void LoadSuitSprites()
    {
        // 从 Resources/Suits 文件夹按文件名加载
        spadeSprite = Resources.Load<Sprite>("Suits/Spade");
        heartSprite = Resources.Load<Sprite>("Suits/Heart");
        diamondSprite = Resources.Load<Sprite>("Suits/Diamond");
        clubSprite = Resources.Load<Sprite>("Suits/Club");

        if (spadeSprite == null || heartSprite == null || diamondSprite == null || clubSprite == null)
        {
            Debug.LogError("Failed to load one or more suit sprites from Resources/Suits!");
        }
        else
        {
            Debug.Log("Suit sprites loaded successfully by name.");
        }
    }

    public Sprite GetSuitSprite(CardSuit suit)
    {
        switch (suit)
        {
            case CardSuit.Spade: return spadeSprite;
            case CardSuit.Heart: return heartSprite;
            case CardSuit.Diamond: return diamondSprite;
            case CardSuit.Club: return clubSprite;
            default: return null;
        }
    }

    private void InitializeDeck()
    {
        cards.Clear();

        CardSuit[] suits = { CardSuit.Diamond, CardSuit.Club, CardSuit.Heart, CardSuit.Spade };

        foreach (CardSuit suit in suits)
        {
            for (int rank = 1; rank <= 13; rank++)
            {
                cards.Add(new Card
                {
                    cardSuit = suit,
                    cardRank = (CardRank)rank,
                    IsJoker = false,
                    cardIndex = cards.Count,
                });
            }
        }

        //Handle Two Jokers
        cards.Add(new Card
        {
            cardSuit = CardSuit.BlackJoker,
            cardRank = (CardRank)0,
            IsJoker = true,
            cardIndex = cards.Count
        });

        cards.Add(new Card
        {
            cardSuit = CardSuit.RedJoker,
            cardRank = (CardRank)0,
            IsJoker = true,
            cardIndex = cards.Count
        });

        if (cards.Count == 54)
        {
            Debug.Log($"Deck initialized with {cards.Count} cards");
        }
        else
        {
            Debug.LogError($"Error occurred while initializing the deck: {cards.Count} cards initialized, 54 cards expected!");
        }
    }

    public Sprite GetCardSprite(Card card)
    {
        if (card == null)
        {
            if (emptySprite == null)
                Debug.LogWarning("GetCardSprite called with null card and emptySprite is not assigned!");
            return emptySprite;   // 返回空白图片
        }
        if (allCardSprites == null) LoadCardSprites();
        if (card.cardIndex >= 0 && card.cardIndex < allCardSprites.Length)
            return allCardSprites[card.cardIndex];

        Debug.LogWarning($"Invalid sprite index: {card.cardIndex}");
        return emptySprite;
    }

    // 在 CardManagement.cs 中添加
    public string GetCardName(Card card)
    {
        if (card == null) return "Null";

        if (card.IsJoker)
            return card.cardSuit == CardSuit.RedJoker ? "Red Joker" : "Black Joker";

        string rankStr = card.cardRank switch
        {
            CardRank.Ace => "Ace",
            CardRank.Two => "Two",
            CardRank.Three => "Three",
            CardRank.Four => "Four",
            CardRank.Five => "Five",
            CardRank.Six => "Six",
            CardRank.Seven => "Seven",
            CardRank.Eight => "Eight",
            CardRank.Nine => "Nine",
            CardRank.Ten => "Ten",
            CardRank.Jack => "Jack",
            CardRank.Queen => "Queen",
            CardRank.King => "King",
            _ => "Unknown"
        };

        string suitStr = card.cardSuit switch
        {
            CardSuit.Club => "Club",
            CardSuit.Diamond => "Diamond",
            CardSuit.Heart => "Heart",
            CardSuit.Spade => "Spade",
            _ => "Unknown"
        };

        return $"{suitStr} {rankStr}";
    }
    public void ResetKittyCards()
    {
        KittyCards.Clear();
        KittyCards = new List<Card>();
    }

    //洗牌
    public void ShuffleDeck()
    {
        //Fisher-Yates' Algorithm
        for (int i = cards.Count - 1; i > 0; --i)
        {
            int randomIndex = Random.Range(0, i + 1);
            (cards[i], cards[randomIndex]) = (cards[randomIndex], cards[i]);
        }
    }

    //发牌
    public void DealCardsToPlayers(int cardsPerPlayer,int kittyCardsNumber)
    {
        // 清空玩家手牌
        foreach (Player player in playerManager.players)
        {
            player.HandCards.Clear();
        }

        // 发牌
        int cardIndex = 0;
        for (int i = 0; i < cardsPerPlayer; i++)
        {
            for (int p = 0; p < playerManager.playerCount; p++)
            {
                if (cardIndex < cards.Count)
                {
                    playerManager.players[p].AddCard(cards[cardIndex]);
                    cardIndex++;
                }
            }
        }

        for(int i = 0; i < kittyCardsNumber; i++)
        {
            KittyCards.Add(cards[cardIndex]);
            cardIndex++;
        }
        
        if(cardIndex == cards.Count)
        {
            Debug.Log($"Dealing cards finished with {kittyCardsNumber} kitty cards");
        }

        else
        {
            Debug.LogError($"Error occurred while dealing cards: totally {cardIndex} cards, not {cards.Count}");
        }

    }
    public static void SortHands(List<Card> Hands, CardSuit? trumpSuit = null)
    {
        if (Hands == null || Hands.Count <= 1)
            return;

        Hands.Sort((a, b) => CardComparer.SuitCompare(b, a, trumpSuit));
    }

    public static (Card winningCard, int winningIndex) GetMaxCardInTrick(
    List<Card> trickCards,
    CardSuit leadSuit,
    CardSuit? trumpSuit = null)
    {
        if (trickCards == null || trickCards.Count == 0)
            return (null, -1);

        Card maxCard = trickCards[0];
        int maxIndex = 0;

        for (int i = 1; i < trickCards.Count; i++)
        {
            if (CardComparer.TrickCompare(trickCards[i], maxCard, leadSuit, trumpSuit) > 0)
            {
                maxCard = trickCards[i];
                maxIndex = i;
            }
        }

        return (maxCard, maxIndex);
    }

    /// 判断玩家是否能打出指定的牌
    public static bool CanDealCard(List<Card> handCards, CardSuit trickSuit, Card dealCard)
    {
        if (handCards == null || dealCard == null) return false;
        if (!handCards.Contains(dealCard)) return false; // 手牌中没有这张牌

        // 获取全局主花色（假设 GameFlowController 有 TrumpSuit 字段，且可能未设置时默认 CardSuit.Club 或其他）
        CardSuit? trumpSuit = GameFlowController.Instance.TrumpSuit;

        // 定义“需要跟的花色集合”：即玩家如果手中有这些花色的牌，就必须出它们
        HashSet<CardSuit> requiredSuits = new HashSet<CardSuit>();

        // 情况1：trickSuit 是主花色
        if (trumpSuit.HasValue && trickSuit == trumpSuit.Value)
        {
            // 主花色包括：实际的主花色 + 2 + Joker
            requiredSuits.Add(trumpSuit.Value); // 主花色的普通牌
        }
        else
        {
            // 情况2：trickSuit 不是主花色（或没有主花色）
            requiredSuits.Add(trickSuit);
        }

        // 检查玩家手牌中是否有“必须跟”的牌
        bool hasRequiredCard = false;

        foreach (Card card in handCards)
        {
            bool isRequired = false;

            if (trumpSuit.HasValue && trickSuit == trumpSuit.Value)
            {
                if (card.cardSuit == trumpSuit.Value || card.cardRank == CardRank.Two || card.IsJoker)
                {
                    isRequired = true;
                }
            }
            else
            {
                // 非主花色情况：只有相同花色的普通牌才是必须跟的（2和Joker不算）
                if (card.cardSuit == trickSuit && !card.IsJoker && card.cardRank != CardRank.Two)
                {
                    isRequired = true;
                }
            }

            if (isRequired)
            {
                hasRequiredCard = true;
                break;
            }
        }

        // 如果没有必须跟的牌，则玩家可以出任意牌
        if (!hasRequiredCard) return true;

        // 有必须跟的牌，则检查 dealCard 是否属于必须跟的牌
        bool dealCardIsRequired = false;

        if (trumpSuit.HasValue && trickSuit == trumpSuit.Value)
        {
            if (dealCard.cardSuit == trumpSuit.Value || dealCard.cardRank == CardRank.Two || dealCard.IsJoker)
                dealCardIsRequired = true;
        }
        else
        {
            if (dealCard.cardSuit == trickSuit && !dealCard.IsJoker && dealCard.cardRank != CardRank.Two)
                dealCardIsRequired = true;
        }

        return dealCardIsRequired;
    }
}
