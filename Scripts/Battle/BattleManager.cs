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
    [Signal] public delegate void IsPlayerTurnChangedEventHandler(bool isPlayerTurn);
    [Signal] public delegate void PlayerTurnStartEventHandler();
    [Signal] public delegate void PlayerTurnEndEventHandler();
    [Signal] public delegate void ScuffleStartEventHandler();
    [Signal] public delegate void ScuffleRoundStartEventHandler(int roundNumber);
    [Signal] public delegate void ScuffleRoundEndEventHandler(int roundNumber);
    [Signal] public delegate void ScuffleEndEventHandler();

    // event handlers
    private readonly SubscriptionManager _subscriptionManager = new();
    
    // Export properties
    [Export] public Battle Battle;
    [Export] private string _cardDataJsonPath = "res://data/test_cards.json";
    public BattlePlayer Player;
    
    private PackedScene _cardScene = GD.Load<PackedScene>(GlobalSettings.CardNodeScenePath);
    private PackedScene _battleResultsScene = GD.Load<PackedScene>(GlobalSettings.BattleResultsScreenScenePath);

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
        Battle.Scuffle.ScuffleEnd += OnScuffleEnd;
        Battle.Scuffle.ScuffleStart += OnScuffleStart;
        Battle.Scuffle.ScuffleRoundEnd += OnScuffleRoundEnd;
        Battle.Scuffle.ScuffleRoundStart += OnScuffleRoundStart;
        
        // Player events
        Player.IsPlayerTurnChanged += OnIsPlayerTurnChanged;
        Player.PlayerActionsRemainingChanged += OnPlayerActionsRemainingChanged;
        Player.PlayerTurnEnd += OnPlayerTurnEnd;
        Player.PlayerTurnStart += OnPlayerTurnStart;
    }

    private void _RemoveSubscriptions()
    {
        // Scuffle Events
        Battle.Scuffle.ScuffleEnd -= OnScuffleEnd;
        Battle.Scuffle.ScuffleStart -= OnScuffleStart;
        Battle.Scuffle.ScuffleRoundEnd -= OnScuffleRoundEnd;
        Battle.Scuffle.ScuffleRoundStart -= OnScuffleRoundStart;
        
        // Player events
        Player.IsPlayerTurnChanged -= OnIsPlayerTurnChanged;
        Player.PlayerActionsRemainingChanged -= OnPlayerActionsRemainingChanged;
        Player.PlayerTurnEnd -= OnPlayerTurnEnd;
        Player.PlayerTurnStart -= OnPlayerTurnStart;
        
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
        if (!IsPlayerTurn)
            throw new Exception("Cannot pass player turn, already not player turn");
        
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
        HandleResetBattleAfterScuffle();
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

    public void HandleEndOfBattle(bool isVictory)
    {
        GD.Print("Game Over!");

        // instantiate
        var resultsScreen = _battleResultsScene.Instantiate<BattleResultsScreen>();
        resultsScreen.IsVictory = isVictory;

        var root = GetTree().Root;

        // capture current scene BEFORE adding the new one
        var oldScene = GetTree().CurrentScene;

        // add new scene and set it current immediately
        root.AddChild(resultsScreen);
        GetTree().CurrentScene = resultsScreen;
        GD.Print("After set: CurrentScene=", GetTree().CurrentScene, " path=", GetTree().CurrentScene?.GetPath());

        // now remove & free the old scene (if it exists and isn't the same node)
        if (oldScene != null && oldScene != resultsScreen)
        {
            // disconnect any signals on oldScene from elsewhere before freeing it
            root.RemoveChild(oldScene);
            oldScene.Free(); // immediate
            GD.Print("Freed old scene: ", oldScene);
        }
    }

    public void HandleResetBattleAfterScuffle()
    {
        // Get cards from discard - put them back in player deck or enemy hand
        var allCards = Battle.Scuffle.RemoveAllCards().Concat(Battle.Discard.RemoveAllCards()).ToList();
        var enemyCards = allCards.Where(card => card.IsEnemy).ToList();
        var playerCards = allCards.Where(card => !card.IsEnemy).ToList();

        if (enemyCards.Count == 0 || playerCards.Count == 0)
        {
            var isVictory = playerCards.Count != 0;
            HandleEndOfBattle(isVictory);
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
    
    /** Signal handlers */
    private void OnIsPlayerTurnChanged(bool isPlayerTurn)
    {
        EmitSignal(nameof(IsPlayerTurnChanged), isPlayerTurn);
    }
    private void OnPlayerActionsRemainingChanged(int newValue, int oldValue)
    {
        EmitSignal(nameof(PlayerActionsRemainingChanged), newValue, oldValue);
    }
    private void OnPlayerTurnEnd()
    {
        EmitSignal(nameof(PlayerTurnEnd));
    }
    private void OnPlayerTurnStart()
    {
        EmitSignal(nameof(PlayerTurnStart));
    }

    private void OnScuffleEnd()
    {
        EmitSignal(nameof(ScuffleEnd));
    }

    private void OnScuffleRoundEnd(int roundNumber)
    {
        EmitSignal(nameof(ScuffleRoundEnd), roundNumber);
    }

    private void OnScuffleRoundStart(int roundNumber)
    {
        EmitSignal(nameof(ScuffleRoundStart), roundNumber);
    }

    private void OnScuffleStart()
    {
        EmitSignal(nameof(ScuffleStart));
    }
}