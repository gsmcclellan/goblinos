using System.Threading.Tasks;
using GoblinCardGame.Scripts.Battle;
using GoblinCardGame.Scripts.CardContainers;
using Godot;
using GoblinCardGame.scripts.cards;

namespace GoblinCardGame.scripts.Battle;

public partial class Battle : Node2D
{
    // Signals
    [Signal] public delegate void PlayerActionsRemainingChangedEventHandler(int oldValue, int newValue);
    [Signal] public delegate void IsPlayerTurnChangedEventHandler(int isPlayerTurn);
    
    // Properties
    [Export] private NodePath _battleManagerPath = "BattleManager";
    [Export] private NodePath _playerDeckPath = "PlayerDeck";
    [Export] private NodePath _playerHandPath = "PlayerCards";
    [Export] private NodePath _enemyHandPath = "EnemyCards";
    [Export] private NodePath _meleeCardsPath = "MeleeCards";
    

    public BattleManager BattleManager;
    public BattleUserInterface UserInterface;
    
    public ICardContainer PlayerHand;
    public ICardContainer EnemyHand;
    
    public Scripts.CardContainers.Deck PlayerDeck;
    public Squabble Squabble;

    private bool _isPlayerTurn;
    private int _playerActionsRemaining;

    public bool IsPlayerTurn
    {
        get => _isPlayerTurn;
        set
        {
            bool oldValue = _isPlayerTurn;
            _isPlayerTurn = value;
            if (!value)
                _playerActionsRemaining = 0;
            if (value != oldValue)
                EmitSignal(nameof(IsPlayerTurnChanged), value);
        }
    }
    public int PlayerActionsRemaining
    {
        get => _playerActionsRemaining;
        set
        {
            int oldValue = _playerActionsRemaining;
            _playerActionsRemaining = value;
            if (value != oldValue)
                EmitSignal(nameof(PlayerActionsRemainingChanged), value, oldValue);
        }
    }
    
    public override void _Ready()
    {
        BattleManager = GetNode<BattleManager>(GlobalSettings.BattleManagerPath);
        UserInterface = GetNode<BattleUserInterface>(GlobalSettings.BattleUserInterfacePath);
        PlayerHand = GetNode<Scripts.CardContainers.CardRow>(_playerHandPath);
        EnemyHand = GetNode<Scripts.CardContainers.CardRow>(_enemyHandPath);
        PlayerDeck = GetNode<Scripts.CardContainers.Deck>(_playerDeckPath);
        Squabble = GetNode<Squabble>(_meleeCardsPath);
    }

    public void _Init()
    {
        
    }

    public void OnPlayerDrawButtonPressed()
    {
        BattleManager.DrawCard();
    }
    
    public void OnAddEnemyButtonPressed()
    {
        GD.Print("Add Soldier");
        if (!Squabble.CanAddCard) return;
        Squabble.AddCard(BattleManager.Card("soldier"));
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
    public void AddPlayerCard(Card card)
    {
        if (PlayerHand.CanAddCard)
            PlayerHand.AddCard(card);
    }
}