
using System.Text.Json;
using Godot;

using GoblinCardGame.scripts.Battle;

namespace GoblinCardGame.scripts.cards;
public partial class Card : Control
{
    /* Export properties */
    [Export] public int BaseMaxArmor = 0;
    [Export] public int BaseMaxHealth = 0;
    [Export] public int BasePower = 0;

    [Export] private Label _cardNameLabel;
    [Export] private Label _healthLabel;
    [Export] private Label _shieldLabel;
    [Export] private Label _powerLabel;
    
    /* Signals */
    [Signal]
    public delegate void CardTriggerPlayEventHandler(Card card);
    
    /* Subscriptions */
    private Callable _playerActionsChangedSubscription;
    
    /* Private properties */
    private string _cardName = "Card Name";

    private int _shield;
    private int _health;
    private int _maxArmor;
    private int _maxHealth;
    private int _power;

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
        GD.Print("Node removed from scene tree");
        _UpdateUI();
        _RemoveSubscriptions();
    }

    public void _InitializeBattleManager()
    {
        _battleManager = GetNode<BattleManager>(GlobalSettings.BattleManagerPath);
        
        // Add listener for Battle Manager to listen to this card.
        Connect(SignalName.CardTriggerPlay, new Callable(_battleManager, "PlayCard"));
    }
    
    private void _InitializeUI()
    {
        _healthLabel = GetNode<Label>("CardArea/Stats/Health/Label");
        _shieldLabel = GetNode<Label>("CardArea/Stats/Armor/Label");
        _powerLabel = GetNode<Label>("CardArea/Stats/Power/Label");
        _cardNameLabel = GetNode<Label>("CardArea/NamePanel/Name");
        UpdateStatLabels();
    }
    private void _UpdateUI()
    {
        // Check if playable
        Button button = GetNode<Button>("PlayButton");
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
            _battleManager.Disconnect("PlayerActionsRemainingChanged", _playerActionsChangedSubscription);
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
    
    /* Event callbacks */
    public void OnPlayButtonPressed()
    {
        GD.Print("Play this card: ", this);
        EmitSignal(SignalName.CardTriggerPlay, this);
    }
    
    public string TestData = """
                              
                                  {
                                      "cardName": "Goblin",
                                      "maxHealth": 10,
                                      "maxArmor": 0, 
                                      "power": 3
                                  }
                              """;

    public void Attack(Card card)
    {
        // Get damage
        var damage = Power;
        
        // Assign damage to shield first then health
        // var remainingHealth = card.Health - damage;
        card.TakeDamage(damage);
        GD.Print($"{CardName} attacks {card.CardName} for {damage} damage. {card.Health} health remaining");
    }

    public void TakeDamage(int damage)
    {
        // Assign damage to shield first then health
        if (Shield >= damage)
        {
            Shield -= damage;
            return;
        }

        if (Shield > 0)
        {
            Health -= damage - Shield;
            Shield = 0;
            return;
        }

        Health -= damage;

        // var remainingHealth = card.Health - damage;
        // GD.Print($"{Name} attacks {card.Name} for {damage} damage. ${remainingHealth} health remaining");
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


