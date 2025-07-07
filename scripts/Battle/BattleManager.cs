using System;
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
    
    public bool CanDrawCard => Battle.PlayerDeck.HasShuffledCards && Battle.PlayerHand.CanAddCard;
    public bool CanPlayCard => PlayerActionsRemaining > 0;
    public bool EnemyHasCards => !Battle.EnemyHand.IsEmpty();

    public bool IsPlayerTurn
    {
        get => Battle.IsPlayerTurn;
        private set => Battle.IsPlayerTurn = value;
    }
    
    public int PlayerActionsRemaining
    {
        get => Battle.PlayerActionsRemaining;
        private set => Battle.PlayerActionsRemaining = value;
    }
    
    public override void _Ready()
    {
        CallDeferred(nameof(_DeferredInit));
    }

    private void _DeferredInit()
    {
        _InitializeBattleComponents();
        _InitializeCardDataDict();
        _SetupSubscriptions();
        
        HandleStartOfBattle();
    }

    private void _InitializeBattleComponents()
    {
        Battle = GetParent() as Battle;
        if (Battle == null)
            throw new Exception("Battle component not found");
        
        Battle._Init();
        Battle.UserInterface._Init();
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
    
    public override void _Notification(int what)
    {
        if (what == NotificationExitTree)
        {
            Battle.MeleeCards.ClearCards(true);
            Battle.PlayerHand.ClearCards(true);
            Battle.EnemyHand.ClearCards(true);
            Battle.PlayerDeck.Cleanup();
        }
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

    public void DoEnemyTurn(bool isFirstTurn = false)
    {
        var numCardsToPlay = Math.Min(isFirstTurn ? 1 : GlobalSettings.EnemyActionsPerTurn, Battle.EnemyHand.CardCount);

        for (int i = 0; i < numCardsToPlay; i++)
        {
            if (Battle.EnemyHand.IsEmpty())
                return;
            
            // Select card to play
            var card = Battle.EnemyHand.RemoveRandomCard();
            Battle.MeleeCards.AddCard(card);
        }
    }

    public void HandlePlayerPassTurn()
    {
        // TODO - if actions remaining, do confirmation
        IsPlayerTurn = false;

        if (EnemyHasCards)
        {
            DoEnemyTurn();
            HandleStartOfPlayerTurn();
        }
        else
        {
            ResolveCombatPhase();
        }

        
    }
    
    private void PlayCard(Card card)
    {
        if (!Battle.MeleeCards.CanAddCard) return;
        
        CardSlot cardSlot = card.GetParent() as CardSlot;
        cardSlot?.RemoveCard();
        Battle.MeleeCards.AddCard(card);
        PlayerActionsRemaining -= 1;
    }

    private void ResolveCombatPhase()
    {
        Battle.MeleeCards.DoBattle();
    }

    public void DrawCard()
    {
        if (!CanDrawCard) return;
        Battle.PlayerHand.AddCard(Battle.PlayerDeck.DrawCard());
    }

    public void DrawCards(int numCardsToDraw = 1)
    {
        for (int i = 0; i < numCardsToDraw; i++)
            DrawCard();
    }
    /** Draws card until hand contains specified number, num not provided, draws until hand is full */
    public void DrawUntil(int num = 0)
    {
        while (CanDrawCard && (Battle.PlayerHand.CardCount < num  || num == 0))
        {
            DrawCard();
        }
    }
    public void HandleStartOfBattle()
    {
        Battle.PlayerDeck.CreateStartingTestDeck(); // TODO - replace with loading deck from player's characters / party
        DrawUntil(GlobalSettings.PlayerStartingCards);
        CreateEnemyCards();
        
        var playerGoesFirst = GD.Randf() < 0.5f;
        GD.Print("Player goes first: " + playerGoesFirst);
        if (!playerGoesFirst)
            DoEnemyTurn();
        HandleStartOfPlayerTurn(playerGoesFirst);
    }

    public void HandleStartOfPlayerTurn(bool isFirstTurn = false)
    {
        IsPlayerTurn = true;
        DrawCards(GlobalSettings.PlayerDrawCardsPerTurn);
        if (EnemyHasCards)
            PlayerActionsRemaining = isFirstTurn ? 1: GlobalSettings.PlayerActionsPerTurn;
        else
            PlayerActionsRemaining = Battle.PlayerHand.CardCount;

        
    }

    private void CreateEnemyCards()
    {
        while (Battle.EnemyHand.CanAddCard)
        {
            Battle.EnemyHand.AddCard(Card("soldier"));
        }
    }
}