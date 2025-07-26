using System;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

using GoblinCardGame.Scripts.Battle;
using BattleManager = GoblinCardGame.Scripts.Battle.BattleManager;

namespace GoblinCardGame.scripts.Cards;
public partial class CardNode : Control
{
    /* Export properties */
    [Export] public int BaseMaxArmor;
    [Export] public int BaseMaxHealth;
    [Export] public int BasePower;

    [Export] private Label _cardNameLabel;
    [Export] private Label _healthLabel;
    [Export] private Label _shieldLabel;
    [Export] private Label _powerLabel;
    [Export] private Sprite2D _summoningSicknessIcon;
    [Export] private AnimationPlayer _animationPlayer;
    
    /* Signals */
    [Signal]
    public delegate void TriggerAddCardToScuffleEventHandler(CardNode cardNode);

    [Signal]
    public delegate void CardEnterScuffleEventHandler(CardNode cardNode); // TODO - maybe this should be on scuffle element
    
    /* Subscriptions */
    private Callable _playerActionsChangedSubscription;
    
    /* Private properties */
    private string _cardName = "Card Name";
    private int _shield;
    private int _health;
    private int _maxArmor;
    private int _maxHealth;
    private int _power;

    /* Battle properties */
    private bool _hasSummoningSickness;
    private bool _hasActed;
    
    // TODO - create status class

    private BattleManager _battleManager;

    public bool IsEnemy { get; set; }

    /* getters / setters */
    public string CardName
    {
        get => _cardName;
        set
        {
            _cardName = value;
            UpdateCardNameLabel();
        }
    }

    public int Shield
    {
        get => _shield;
        set
        {
            _shield = value;
            UpdateShieldLabel();
        }
    }
    public int Health
    {
        get => _health;
        set
        {
            _health = value;
            UpdateHealthLabel();
        }
    }

    public int MaxArmor
    {
        get { return _maxArmor; }
        set { _maxArmor = value; }
    }

    public int MaxHealth
    {
        get => _maxHealth;
        set => _maxHealth = value;
    }

    public int Power
    {
        get { return _power; }
        set
        {
            _power = value;
            UpdatePowerLabel();
        }
    }

    public bool HasSummoningSickness
    {
        get => _hasSummoningSickness;
        set
        {
            _hasSummoningSickness = value;
            UpdateSummoningSicknessLabel();
        }
    }

    /** Determines if card can do action in scuffle */
    public bool CanDoScuffleAction => !_hasActed && !_hasSummoningSickness;

    public bool IsPlayable => _battleManager != null && _battleManager.CanPlayCard && _battleManager.Battle.PlayerHand.HasCard(this);

    /* Lifecycle methods */
    public override void _Ready()
    {
        _InitializeBattleManager();
        _SetupSubscriptions();
        _InitializeUI();
        _UpdateUI();
    }
    
    public override void _EnterTree()
    {
        GD.Print("Node added to scene tree");
        foreach (Node child in GetChildren())
            GD.Print(child.Name, " - ", child.GetType().Name);
        _UpdateUI();
        // Fire your event here
    }
    
    public override void _ExitTree()
    {
        // Remove status effects tied to battle
        
        // Disconnect signals, stop timers, cleanup
        _RemoveSubscriptions();
    }

    public void _InitializeBattleManager()
    {
        _battleManager = GetNode<BattleManager>(GlobalSettings.BattleManagerPath);
        
        // Add listener for Battle Manager to listen to this card.
        Connect(SignalName.TriggerAddCardToScuffle, new Callable(_battleManager, "PlayCard"));
    }
    
    private void _InitializeUI()
    {
        _healthLabel = GetNode<Label>("CardArea/Stats/Health/Label");
        _shieldLabel = GetNode<Label>("CardArea/Stats/Shield/Label");
        _powerLabel = GetNode<Label>("CardArea/Stats/Power/Label");
        _cardNameLabel = GetNode<Label>("CardArea/NamePanel/Name");
        _summoningSicknessIcon = GetNode<Sprite2D>("CardArea/SummoningSicknessIcon");
        _animationPlayer = GetNode<AnimationPlayer>("CardArea/AnimationPlayer");
        UpdateStatLabels();
        UpdateStatusIcons();
    }
    private void _UpdateUI()
    {
        // Check if playable
        Button button = GetNode<Button>("PlayButton");
        GD.Print($"{CardName} is playable: {IsPlayable}");
        if (button != null) button.Visible = IsPlayable;
    }
    /** Sets up listeners for signals coming from BattleManager to update UI / status */
    private void _SetupSubscriptions()
    {
        // Update UI when PlayerActionsRemainingChanged fires - if no more actions, cards become unplayable
        _playerActionsChangedSubscription = Callable.From((int _, int _) => _UpdateUI());
        _battleManager.Connect("PlayerActionsRemainingChanged", _playerActionsChangedSubscription);
        
    }

    private void _RemoveSubscriptions()
    {
        if (_battleManager?.IsConnected("PlayerActionsRemainingChanged", _playerActionsChangedSubscription) == true)
        {
            _battleManager.Disconnect("PlayerActionsRemainingChanged", _playerActionsChangedSubscription);
        }
            
    }
    

    public void InitializeFromCardData(CardData data)
    {
        CardName = data.CardName;
        Health = MaxHealth = data.MaxHealth;
        Shield = MaxArmor = data.MaxArmor;
        Power = data.Power;
        IsEnemy = data.IsEnemy;
    }

    public void InitializeFromJson(string json)
    {
        CardData cardData = JsonSerializer.Deserialize<CardData>(json);
        InitializeFromCardData(cardData);
    }

    /** Handles interactions when self enters scuffle */
    public async Task OnEnterScuffle(CardEnterScuffleDetails details)
    {
        if (details.CardNode != this)
            return;
        
        // summoning sickness
        if (details.PreviousCardsAddedToScuffle > 0)
            HasSummoningSickness = true;
    }

    /** Handles interactions with other cards entering scuffle */
    public void OnCardEnterScuffle(CardEnterScuffleDetails details)
    {
        throw new NotImplementedException("Card entered function not implemented");
    }

    public async Task OnScuffleRoundStart(ScuffleRoundStartDetails details)
    {
        _hasActed = false;
        if (details.RoundNumber + 1 > GlobalSettings.NumberOfSummoningSicknessRounds)
            HasSummoningSickness = false;
    }

    public void OnScuffleRoundEnd(int roundNumber)
    {
        
    }

    public async Task PlayAnimationAsync(string animationName)
    {
        if (_animationPlayer == null)
            throw new Exception("Unable to access animation player");
        _animationPlayer.Play(animationName);
        await ToSignal(_animationPlayer, AnimationPlayer.SignalName.AnimationFinished);
    }
    
    private void UpdateCardNameLabel()
    {
        if (_cardNameLabel != null)
            _cardNameLabel.Text = _cardName;
    }

    private void UpdateShieldLabel()
    {
        if (_shieldLabel != null)
            _shieldLabel.Text = _shield.ToString();
    }

    private void UpdateSummoningSicknessLabel()
    {
        if (_summoningSicknessIcon != null)
            _summoningSicknessIcon.Visible = _hasSummoningSickness;
    }

    private void UpdateHealthLabel()
    {
        if (_healthLabel != null)
            _healthLabel.Text = _health.ToString();
    }

    private void UpdatePowerLabel()
    {
        if (_powerLabel != null)
            _powerLabel.Text = _power.ToString();
    }

    private void UpdateStatLabels()
    {
        UpdateShieldLabel();
        UpdateHealthLabel();
        UpdatePowerLabel();
        UpdateCardNameLabel();
    }

    private void UpdateStatusIcons()
    {
        UpdateSummoningSicknessLabel();
    }
    
    /* Event callbacks */
    public void OnPlayButtonPressed()
    {
        if (!IsPlayable) return;
        GD.Print("Play this card: ", this);
        EmitSignal(SignalName.TriggerAddCardToScuffle, this);
    }

    public void TriggerAddToScuffle()
    {
        if (!IsPlayable) return;
        GD.Print("Trigger add card to scuffle", this);
        EmitSignal(SignalName.TriggerAddCardToScuffle, this);
    }

    public void TriggerShieldAction()
    {
        if (!IsPlayable) return;
        // TODO - make general card action function instead of hardcoding each one.
        GD.Print("Trigger shield action"); 
        GD.Print(SignalName.TriggerAddCardToScuffle);
        _battleManager.PlayCardAction(new CardActionDetails
        {
            CardNode = this,
            ActionType = CardActionType.Shield,
            TargetsFriend = true
        });
    }
    
    public string TestData = """
                              
                                  {
                                      "cardName": "Goblin",
                                      "maxHealth": 10,
                                      "maxArmor": 0, 
                                      "power": 3
                                  }
                              """;

    public async Task Attack(CardNode cardNode)
    {
        await PlayAnimationAsync("Attacks");
        // Get damage
        var damage = Power;
        // Assign damage to shield first then health
        // var remainingHealth = card.Health - damage;
        
        await cardNode.TakeDamage(damage);
        _hasActed = true;
        
        // TODO - battle logging
        GD.Print($"{CardName} attacks {cardNode.CardName} for {damage} damage. {cardNode.Health} health remaining");
    }

    public async Task TakeDamage(int damage)
    {
        // Play animation
        var animationTask = PlayAnimationAsync("IsAttacked");
        // Assign damage to shield first then health
        if (Shield >= damage)
            Shield -= damage;
        else if (Shield > 0)
        {
            Health -= damage - Shield;
            Shield = 0;
        }
        else
            Health -= damage;

        await animationTask;
    }
}

public class CardData
{
    public string CardName { get; set; }
    public int MaxHealth { get; set; }
    public int MaxArmor { get; set; }
    public int Power { get; set; }
    public bool IsEnemy { get; set; }
}

public class CardEnterScuffleDetails
{
    public CardNode CardNode;
    public int BattleRound;
    public int PreviousCardsPlayed;
    public int PreviousCardsAddedToScuffle;
}

public class CardActionDetails
{
    public CardActionType ActionType;
    public CardNode CardNode;
    public bool TargetsFriend;
}

public enum CardActionType
{
    Shield
}

