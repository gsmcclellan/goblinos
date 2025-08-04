using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GoblinCardGame.Scripts;
using GoblinCardGame.Scripts.Actions;
using GoblinCardGame.Scripts.Cards;
using GoblinCardGame.Scripts.Cards.Classes;
using GoblinCardGame.Scripts.Utilities.Actions;
using Godot;

namespace GoblinCardGame.Scripts.Battle;

public partial class BattleManager : Node
{
    // Signals
    [Signal] public delegate void PlayerActionsRemainingChangedEventHandler(int newValue, int oldValue);
    [Signal] public delegate void PlayerTurnStartEventHandler(); // TODO - hook this event up 
    [Signal] public delegate void PlayerTurnEndEventHandler(); // TODO - hook this event up 
    [Signal] public delegate void ScuffleStartEventHandler(); // TODO - hook this event up 
    [Signal] public delegate void ScuffleRoundStartEventHandler(int roundNumber); // TODO - hook this event up 
    [Signal] public delegate void ScuffleRoundEndEventHandler(int roundNumber); // TODO - hook this event up 
    [Signal] public delegate void ScuffleEndEventHandler(); // TODO - hook this event up 

    // event handlers
    private Scuffle.ScuffleStartEventHandler _scuffleStartHandler;
    private Scuffle.ScuffleRoundStartEventHandler _scuffleRoundStartHandler;
    private Scuffle.ScuffleRoundEndEventHandler _scuffleRoundEndHandler;
    private Scuffle.ScuffleEndEventHandler _scuffleEndHandler;
    private readonly SubscriptionManager _subscriptionManager = new();
    
    // Export properties
    [Export] public Battle Battle;
    [Export] private string _cardDataJsonPath = "res://data/test_cards.json";
    
    public BattlePlayer Player;
    private PackedScene _cardScene = GD.Load<PackedScene>("res://Nodes/Card.tscn");

    private int _cardsPlayedThisTurn = 0;
    private int _cardsAddedToScuffleThisTurn;
    private int _battleRound = 0;
    
    public bool CanDrawCard => !Battle.PlayerDeck.IsEmpty && Battle.PlayerHand.CanAddCard;
    public bool CanPlayCard => PlayerActionsRemaining > 0;
    public bool EnemyHasCardsInHand => !Battle.EnemyHand.IsEmpty;

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

    public IEnumerable<CardNode> AllCardsInActiveBattle =>
        // get cards in player hand, enemy hand, melee, deck, & discard
        Battle.PlayerHand.Cards.Concat(Battle.PlayerDeck.Cards).Concat(Battle.EnemyHand.Cards).Concat(Battle.Scuffle.Cards).Concat(Battle.Discard.Cards);

    public override void _Ready()
    {
        CallDeferred(nameof(_DeferredInit));
        CardManager.LoadData();
    }

    public override void _ExitTree()
    {
        Battle.Scuffle.RemoveAllCards(true);
        Battle.PlayerHand.RemoveAllCards(true);
        Battle.EnemyHand.RemoveAllCards(true);
        Battle.PlayerDeck.Cleanup();
        _RemoveSubscriptions();
        base._ExitTree();
    }

    private void _DeferredInit()
    {
        _InitializeBattleComponents();
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
        
        Player = Battle.Player;
    }


    private void _SetupSubscriptions()
    {
        // Scuffle events
        _scuffleStartHandler = () => { EmitSignal(nameof(ScuffleStart)); };
        Battle.Scuffle.ScuffleStart += _scuffleStartHandler;
        _scuffleRoundStartHandler = (int roundNumber) => { EmitSignal(nameof(ScuffleRoundStart), roundNumber); };
        Battle.Scuffle.ScuffleRoundStart += _scuffleRoundStartHandler;
        _scuffleRoundEndHandler = (int roundNumber) => { EmitSignal(nameof(ScuffleRoundEnd), roundNumber); };
        Battle.Scuffle.ScuffleRoundEnd += _scuffleRoundEndHandler;
        _scuffleEndHandler = () => { EmitSignal(nameof(ScuffleEnd)); };
        Battle.Scuffle.ScuffleEnd += _scuffleEndHandler;
        
        // Player events
        _subscriptionManager.Subscribe(
            h => Player.PlayerActionsRemainingChanged += h,
            h => Player.PlayerActionsRemainingChanged -= h,
            (int newVal, int oldVal) => { EmitSignal(nameof(PlayerActionsRemainingChanged), newVal, oldVal); }
        );

        _subscriptionManager.Subscribe(
            h => Player.PlayerTurnStart += h,
            h => Player.PlayerTurnStart -= h,
            () => { EmitSignal(nameof(PlayerTurnStart)); }
        );

        _subscriptionManager.Subscribe(
            h => Player.PlayerTurnEnd += h,
            h => Player.PlayerTurnEnd -= h,
            () => { EmitSignal(nameof(PlayerTurnEnd)); }
        );
    }

    private void _RemoveSubscriptions()
    {
        // Scuffle Events
        Battle.Scuffle.ScuffleStart -= _scuffleStartHandler;
        Battle.Scuffle.ScuffleRoundStart -= _scuffleRoundStartHandler;
        Battle.Scuffle.ScuffleRoundEnd -= _scuffleRoundEndHandler;
        Battle.Scuffle.ScuffleEnd -= _scuffleEndHandler;
        
        _subscriptionManager.Clear();
    }

    public CardData CardData(string cardId)
    {
        return CardManager.CardDataDatabase.Get(cardId);
    }

    public CardNode Card(CardData cardData)
    {
        CardNode cardNode = _cardScene.Instantiate<CardNode>();
        cardNode.InitializeFromCardData(cardData);
        return cardNode;
    }

    public CardNode Card(string cardId)
    {
        return Card(CardData(cardId));
    }
    private void DoEnemyTurn(bool isFirstTurn = false)
    {
        _cardsPlayedThisTurn = 0;
        _cardsAddedToScuffleThisTurn = 0;
        var numCardsToPlay = Math.Min(isFirstTurn ? 1 : GlobalSettings.EnemyActionsPerTurn, Battle.EnemyHand.CardCount);

        for (var i = 0; i < numCardsToPlay; i++)
        {
            if (Battle.EnemyHand.IsEmpty)
                return;
            
            // Select card to play
            var card = Battle.EnemyHand.RemoveRandomCard();
            Battle.Scuffle.AddCard(card);
        }
    }

    public async Task HandlePlayerPassTurn()
    {
        // TODO - if actions remaining, do confirmation
        IsPlayerTurn = false;
        
        if (EnemyHasCardsInHand)
        {
            DoEnemyTurn();
            HandleStartOfPlayerTurn();
            
            // TODO - if player has no actions remaining, go to combat phase
        }
        else
        {
            await ResolveCombatPhase();
        }    
        // TODO - implement way for player to skip straight to combat resolution phase
    }
    
    
    

    public void DiscardCard(CardNode cardNode)
    {
        CardSlot cardSlot = cardNode.GetParent() as CardSlot;
        cardSlot?.RemoveCard();
        Battle.Discard.AddCard(cardNode);
    }

    public void DiscardNonCombatCards()
    {
        IEnumerable<CardNode> cards = [];
        cards = cards.Concat(Battle.PlayerHand.RemoveAllCards());
        cards = cards.Concat(Battle.EnemyHand.RemoveAllCards());
        cards = cards.Concat(Battle.PlayerDeck.RemoveAllCards());

        Battle.Discard.AddCards(cards);
    }

    public async Task ResolveCombatPhase()
    {
        // Discard unused cards
        DiscardNonCombatCards();
        
        // Resolve scuffle
        await Battle.Scuffle.DoBattle();
        
        // Check if battle over, out of all cards, if one side has none battle is over
        HandleResetBattle();
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
        CreatePlayerTestDeck(); // TODO - replace with loading deck from player's characters / party
        DrawUntil(GlobalSettings.PlayerStartingCards);
        CreateEnemyCards();
        
        var playerGoesFirst = GD.Randf() < 0.5f;
        GD.Print("Player goes first: " + playerGoesFirst);
        if (!playerGoesFirst)
            DoEnemyTurn(true);
        HandleStartOfPlayerTurn(playerGoesFirst);
    }

    public void HandleEndOfBattle()
    {
        // TODO - end of battle
        GD.Print("Game Over!");
    }

    public void HandleResetBattle()
    {
        // Get cards from discard - put them back in player deck or enemy hand
        var allCards = Battle.Scuffle.RemoveAllCards().Concat(Battle.Discard.RemoveAllCards()).ToList();
        var enemyCards = allCards.Where(card => card.IsEnemy).ToList();
        var playerCards = allCards.Where(card => !card.IsEnemy).ToList();

        if (enemyCards.Count == 0 || playerCards.Count == 0)
        {
            HandleEndOfBattle();
            return;
        }
        
        Battle.PlayerDeck.Cards = playerCards;
        Battle.PlayerDeck.ShuffleCards();
        Battle.EnemyHand.Cards = enemyCards;
        DrawUntil(GlobalSettings.PlayerStartingCards);
        
        var playerGoesFirst = GD.Randf() < 0.5f;
        GD.Print("Player goes first: " + playerGoesFirst);
        if (!playerGoesFirst)
            DoEnemyTurn(true);
        HandleStartOfPlayerTurn(playerGoesFirst);
    }

    /** Starts player turn, sets IsPlayerTurn to true & sets PlayerActionsRemaining */
    public void HandleStartOfPlayerTurn(bool isFirstTurn = false)
    {
        GD.Print("HandleStartOfPlayerTurn");
        // Reset per turn variables
        IsPlayerTurn = true;
        _cardsPlayedThisTurn = 0;
        _cardsAddedToScuffleThisTurn = 0;
        
        DrawCards(GlobalSettings.PlayerDrawCardsPerTurn);
        
        if (EnemyHasCardsInHand) // Set player actions remaining based on settings
            PlayerActionsRemaining = isFirstTurn ? 1: GlobalSettings.PlayerActionsPerTurn;
        else // Allow player to play rest of cards in hand once enemy has played all
            PlayerActionsRemaining = Battle.PlayerHand.CardCount;
        GD.Print("PlayerActionsRemaining: ", PlayerActionsRemaining);
    }

    public IEnumerable<CardNode> RemoveAllCardsInActiveBattle(bool destroy = false)
    {
        IEnumerable<CardNode> cards = [];

        cards = cards.Concat(Battle.PlayerHand.RemoveAllCards());
        cards = cards.Concat(Battle.EnemyHand.RemoveAllCards());
        cards = cards.Concat(Battle.PlayerDeck.RemoveAllCards());
        cards = cards.Concat(Battle.Scuffle.RemoveAllCards());
        cards = cards.Concat(Battle.Discard.RemoveAllCards());
        // TODO - add discard, maybe put all in discard before melee starts so no concatenating necessary
        
        return cards;
    }

    private void CreateEnemyCards()
    {
        CardData[] cardDataList = CardManager.ReadCardDataFromFileLocation("res://data/demo_enemy_starting_cards.txt");
        CardNode[] cardNodeList = cardDataList.Select(Card).ToArray();

        Battle.EnemyHand.AddCards(cardNodeList);
    }

    private void CreatePlayerTestDeck()
    {
        CardData[] cardDataList = CardManager.ReadCardDataFromFileLocation("res://data/demo_player_starting_cards.txt");
        CardNode[] cardNodeList = cardDataList.Select(Card).ToArray();

        Battle.PlayerDeck.AddCards(cardNodeList);
        Battle.PlayerDeck.ShuffleCards();
    }

    private void OnBreakButtonPressed()
    {
        GD.Print("Break here");
        foreach (CardNode card in Battle.PlayerHand.Cards)
        {
            card.SubtractStat(StatName.Shield, 1);
        }
    }
}