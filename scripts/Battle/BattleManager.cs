using System.Collections.Generic;
using System.Text.Json;
using GoblinCardGame.scripts.cards;
using Godot;

namespace GoblinCardGame.scripts.Battle;

public partial class BattleManager : Node
{
    // Signals
    [Signal] public delegate void PlayerActionsRemainingChangedEventHandler(int newValue, int oldValue);
    
    
    [Export] public Battle Battle;
    [Export] private string _cardDataJsonPath = "res://data/test_cards.json";
    
    private PackedScene _cardScene = GD.Load<PackedScene>("res://nodes/card.tscn");
    
    private Dictionary<string, CardData> _cardDataDict;
    
    public bool CanDrawCard => Battle.PlayerDeck.HasShuffledCards && Battle.PlayerHand.CanAddCard();
    public bool CanPlayCard => PlayerActionsRemaining > 0;
    
    public int PlayerActionsRemaining
    {
        get => Battle.PlayerActionsRemaining;
        set => Battle.PlayerActionsRemaining = value;
    }

    public override void _Ready()
    {
        _InitializeBattleComponents();
        _InitializeCardDataDict();
        _SetupSubscriptions();
        HandleStartOfBattle();
    }

    private void _InitializeBattleComponents()
    {
        Battle = GetParent() as Battle;
    }
    
    private void _InitializeCardDataDict()
    {
        // Open JSON file
        using var file = FileAccess.Open(_cardDataJsonPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr("Could not open test_data.json");
            return;
        }

        string json = file.GetAsText();

        // Deserialize dictionary<string, CardData>
        _cardDataDict = JsonSerializer.Deserialize<Dictionary<string, CardData>>(json);
        
        if (_cardDataDict != null) return;
        
        GD.PrintErr("Failed to parse card dictionary");
    }

    private void _SetupSubscriptions()
    {
        Battle.Connect(
            "PlayerActionsRemainingChanged",
            Callable.From<int, int>((newValue, oldValue) =>
                EmitSignal(nameof(PlayerActionsRemainingChanged), newValue, oldValue)
            )
        );
    }

    public CardData CardData(string cardId)
    {
        return _cardDataDict[cardId];
    }

    public Card Card(CardData cardData)
    {
        Card card = _cardScene.Instantiate<Card>();
        card.InitializeFromCardData(cardData);
        return card;
    }

    public Card Card(string cardId)
    {
        return Card(_cardDataDict[cardId]);
    }

    public void DrawCard()
    {
        if (!CanDrawCard) return;
        Battle.PlayerHand.AddCard(Battle.PlayerDeck.DrawCard());
    }
    
    public void PlayCard(Card card)
    {
        if (!Battle.MeleeCards.CanAddCard) return;
        
        CardSlot cardSlot = card.GetParent() as CardSlot;
        cardSlot?.RemoveCard();
        Battle.MeleeCards.AddCard(card);
        PlayerActionsRemaining -= 1;
    }

    public void DrawUntil(int num)
    {
        while (Battle.PlayerHand.CardCount < num)
        {
            if (!CanDrawCard) return;
            
            
        }
    }

    public void HandleStartOfBattle()
    {
        var playerGoesFirst = true; // TODO GD.Randf() < 0.5f;
        GD.Print("Player goes first: " + playerGoesFirst);
        PlayerActionsRemaining = playerGoesFirst ? 1: 0;
        
        // Draw initial player cards
        
    }
}