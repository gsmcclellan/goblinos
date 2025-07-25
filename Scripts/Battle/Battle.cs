using System.Threading.Tasks;
using GoblinCardGame.Scripts.Battle;
using GoblinCardGame.Scripts.CardContainers;
using Godot;
using GoblinCardGame.scripts.Cards;

namespace GoblinCardGame.scripts.Battle;

public partial class Battle : Node2D
{
    // Signals
    [Signal] public delegate void PlayerActionsRemainingChangedEventHandler(int newValue, int oldValue);
    [Signal] public delegate void IsPlayerTurnChangedEventHandler(int isPlayerTurn);
    
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
    public BattlePlayer Player;

    public bool IsPlayerTurn
    {
        get => Player.IsTurn;
        set
        {
            Player.IsTurn = value;
        }
    }
    public int PlayerActionsRemaining
    {
        get => Player.ActionsRemaining;
        set
        {
            int oldValue = Player.ActionsRemaining;
            Player.ActionsRemaining = value;
            if (value != oldValue)
                EmitSignal(nameof(PlayerActionsRemainingChanged), value, oldValue);
        }
    }
    
    public override void _Ready()
    {
        BattleManager = GetNode<BattleManager>(GlobalSettings.BattleManagerPath);
        UserInterface = GetNode<BattleUserInterface>(GlobalSettings.BattleUserInterfacePath);
        PlayerHand = GetNode<CardRow>(_playerHandPath);
        EnemyHand = GetNode<CardRow>(_enemyHandPath);
        PlayerDeck = GetNode<Deck>(_playerDeckPath);
        Scuffle = GetNode<Scuffle>(_scufflePath);
        Discard = GetNode<Discard>(_discardPath);
        
        Player = new BattlePlayer();

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
}