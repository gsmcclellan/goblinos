using System;
using System.Threading.Tasks;
using GoblinCardGame.Scripts.CardContainers;
using Godot;
using GoblinCardGame.Scripts.Cards;

namespace GoblinCardGame.Scripts.Battle;

public partial class Battle : Node2D
{
    // Signals
    
    // Properties
    [Export] private NodePath _battleManagerPath = "BattleManager";
    [Export] private NodePath _playerDeckPath = "PlayerDeck";
    [Export] private NodePath _playerHandPath = "PlayerCards";
    [Export] private NodePath _enemyHandPath = "EnemyCards";
    [Export] private NodePath _scufflePath = "Scuffle";
    [Export] private NodePath _discardPath = "Discard";
    
    /** Component nodes */
    public BattleManager BattleManager;
    public BattleUserInterface UserInterface;
    public ICardContainer PlayerHand;
    public ICardContainer EnemyHand;
    public Deck PlayerDeck;
    public Scuffle Scuffle;
    public CardPile Discard;
    // public BattlePlayer Player;
    
    public override void _Ready()
    {
        var button = GetNode<Button>(_playerDeckPath + "/PlayerDrawButton");

        // Optional: connect signal manually to verify
        // button.Connect("pressed", new Callable(this, nameof(OnPlayerDrawButtonPressed)));
        
        BattleManager = GetNode<BattleManager>("BattleManager");
        UserInterface = GetNode<BattleUserInterface>("BattleUI");
        if (BattleManager == null)
            throw new Exception("Battle Manager not loaded");
        if (UserInterface == null)
            throw new Exception("Battle User Interface not loaded");
        
        PlayerHand = GetNode<CardRow>(_playerHandPath);
        EnemyHand = GetNode<CardRow>(_enemyHandPath);
        PlayerDeck = GetNode<Deck>(_playerDeckPath);
        Scuffle = GetNode<Scuffle>(_scufflePath);
        Discard = GetNode<Discard>(_discardPath);

        if (PlayerHand == null || EnemyHand == null || PlayerDeck == null || Scuffle == null || Discard == null)
            throw new Exception("Battle components not loaded");
        
        _SetupSubscriptions();
    }

    public void _Init()
    {
        
    }

    private void _SetupSubscriptions()
    {
        
    }
    
    public void OnAddEnemyButtonPressed()
    {
        GD.Print("Add Soldier");
        if (!Scuffle.CanAddCard) return;
        Scuffle.AddCard(BattleManager.Card("soldier"));
    }

    public void OnAddGoblinButtonPressed()
    {
        GD.Print("Add Goblin");
        AddPlayerCard(BattleManager.Card("goblin"));
    }

    public async Task OnDoBattleButtonPressed()
    {
        await BattleManager.ResolveCombatPhase();
    }
    
    /** Adds card to player hand if able */
    public void AddPlayerCard(CardNode cardNode)
    {
        if (PlayerHand.CanAddCard)
            PlayerHand.AddCard(cardNode);
    }

    public void OnPlayerDrawButtonPressed()
    {
        GD.Print("PlayerDrawButton pressed");
        BattleManager.DrawCard();
    }
}