using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using GoblinCardGame.scripts.cards;
using Godot;
using BattleManager = GoblinCardGame.scripts.Battle.BattleManager;
using Card = GoblinCardGame.scripts.cards.Card;

namespace GoblinCardGame.scripts;

public partial class Deck : Node2D
{
    /* Utility stuff */
    private static readonly Random Random = new();
    
    /* Node references */
    private BattleManager _battleManager;
    private Label _shuffledCardCountLabel;
    
    /* Properties */
    private readonly List<CardData> _masterCardList = [];
    private List<Card> _shuffledCards = [];
    private List<Card> _cards = [];
    
    private int _shuffledCardCount;
    public int ShuffledCardCount
    {
        get => _shuffledCardCount;
        private set
        {
            _shuffledCardCount = value;
            UpdateShuffledCardCountLabel();
        }
    }
    public IEnumerable<Card> Cards
    {
        get => _cards;
        set => _cards = value.ToList();
    }
    public bool HasShuffledCards => ShuffledCardCount > 0;

    public override void _Ready()
    {
        _battleManager = GetNode<BattleManager>(GlobalSettings.BattleManagerPath);
        _shuffledCardCountLabel = GetNode<Label>("ShuffledCardCountLabel");
        //CreateStartingTestDeck();
        //ShuffleCards();
    }

    /** Creates cards from array of card data objects */
    public void InitializeFromCardData(CardData[] cardDataArray)
    {
        _cards = [];
        foreach (CardData cardData in cardDataArray)
        {
            Card card = new Card();
            card.InitializeFromCardData(cardData);
        }
        
    }

    /** Creates cards from json serialized card data objects */
    public void InitializeFromJson(string json)
    {
        CardData[] cardDataArray = JsonSerializer.Deserialize<CardData[]>(json);
        InitializeFromCardData(cardDataArray);
    }
    /** Shuffles cards & enables them to be drawn */
    public void ShuffleCards()
    {
        // var shuffledCardData = _masterCardList.OrderBy(_ => Random.Next()).ToList();
        var shuffledCards = new List<Card>(_cards);
        var n = shuffledCards.Count;
        while (n > 1)
        {
            n--;
            var randomIndex = GD.RandRange(0, n);
            (shuffledCards[randomIndex], shuffledCards[n]) = (shuffledCards[n], shuffledCards[randomIndex]);
        }
        SetShuffledCards(shuffledCards);
    }

    public void CreateStartingTestDeck()
    {
        for (var i = 0; i < 5; i++)
        {
            CardData goblinShielder = _battleManager.CardData("goblin_shielder");
            CardData goblinStabber = _battleManager.CardData("goblin_stabber");
            Card goblinShielderCard = _battleManager.Card("goblin_shielder");
            Card goblinStabberCard = _battleManager.Card("goblin_stabber");
            _masterCardList.Add(goblinShielder);
            _masterCardList.Add(goblinStabber);
            _cards.Add(goblinShielderCard);
            _cards.Add(goblinStabberCard);
        }
        ShuffleCards();
    }

    /** if any shuffled cards remain, return top card */
    public Card DrawCard()
    {
        if (HasShuffledCards)
        {
            return Pop();
        }
        else
            throw new Exception("No shuffled cards");
    }
    /** Sets shuffled cards from array of cards, assumed are already shuffled */
    private void SetShuffledCards(List<Card> cards)
    {
        _shuffledCards = cards;
        ShuffledCardCount = _shuffledCards.Count;
    }
    /** Removes & returns card at given index, defaults to zero */
    private Card RemoveShuffledCardAt(int index = 0)
    {
        var card = _shuffledCards[index];
        _shuffledCards.RemoveAt(index);
        ShuffledCardCount = _shuffledCards.Count;
        return card;
    }
    /** Removes & returns top card in shuffled cards list */
    private Card Pop()
    {
        return RemoveShuffledCardAt(_shuffledCards.Count - 1);
    }
    /** Handles updating remaining cards label from _shuffledCardCount */
    private void UpdateShuffledCardCountLabel()
    {
        if (_shuffledCardCountLabel != null)
            _shuffledCardCountLabel.Text = $"{_shuffledCardCount}";
    }

    public IEnumerable<Card> RemoveCards(bool destroy = false)
    {
        if (destroy)
            Cleanup();
        var cards = new List<Card>(_shuffledCards);
        _shuffledCards.Clear();
        return cards;
    }

    public void Cleanup()
    {
        foreach (var card in _shuffledCards)
            card.QueueFree();
    }
}