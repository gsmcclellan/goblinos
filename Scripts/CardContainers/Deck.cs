using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using GoblinCardGame.scripts;
using GoblinCardGame.scripts.Cards;
using Godot;
using static GoblinCardGame.Scripts.CardContainers.CardPile;
using BattleManager = GoblinCardGame.Scripts.Battle.BattleManager;

namespace GoblinCardGame.Scripts.CardContainers;

public partial class Deck: CardPile
{
    /* Utility stuff */
    
    /* Node references */
    private BattleManager _battleManager;
    [Export] private Label _cardCountLabel;
    
    /* Properties */
    
    public override void _Ready()
    {
        _battleManager = GetNode<BattleManager>(GlobalSettings.BattleManagerPath);
        _cardCountLabel = GetNode<Label>("CardCountLabel");

        Connect(nameof(CardListChanged), new Callable(this, nameof(OnCardListChanged)));

        UpdateCardCountLabel();
    }

    /** Creates cards from array of card data objects */
    public void InitializeFromCardData(CardData[] cardDataArray)
    {
        CardList = [];
        foreach (CardData cardData in cardDataArray)
        {
            CardNode cardNode = new CardNode();
            cardNode.InitializeFromCardData(cardData);
        }
    }

    /** Creates cards from json serialized card data objects */
    public void InitializeFromJson(string json)
    {
        CardData[] cardDataArray = JsonSerializer.Deserialize<CardData[]>(json);
        InitializeFromCardData(cardDataArray);
    }
    /** Shuffles cards & enables them to be drawn */

    public void CreateStartingTestDeck()
    {
        for (var i = 0; i < 5; i++)
        {
            CardNode goblinShielderCardNode = _battleManager.Card("goblin_shielder");
            CardNode goblinStabberCardNode = _battleManager.Card("goblin_stabber");
            CardList.Add(goblinShielderCardNode);
            CardList.Add(goblinStabberCardNode);
        }
        ShuffleCards();
    }

    /** Called by subscription when base CardPile class emits CardListChanged signal */
    private void OnCardListChanged ()
    {
        UpdateCardCountLabel();
    }

    /** Handles updating remaining cards label from _shuffledCardCount */
    private void UpdateCardCountLabel()
    {
        if (_cardCountLabel != null)
            _cardCountLabel.Text = $"{CardCount}";
    }
}