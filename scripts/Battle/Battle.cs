using System.Threading.Tasks;
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
    
    public Deck PlayerDeck;
    public MeleeCards MeleeCards;

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
        PlayerHand = GetNode<CardRow>(_playerHandPath);
        EnemyHand = GetNode<CardRow>(_enemyHandPath);
        PlayerDeck = GetNode<Deck>(_playerDeckPath);
        MeleeCards = GetNode<MeleeCards>(_meleeCardsPath);
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
        if (!MeleeCards.CanAddCard) return;
        MeleeCards.AddCard(BattleManager.Card("soldier"));
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