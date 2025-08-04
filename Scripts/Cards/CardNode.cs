using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GoblinCardGame.Scripts.Actions;
using Godot;

using GoblinCardGame.Scripts.Battle;
using GoblinCardGame.Scripts.Cards.Classes;
using BattleManager = GoblinCardGame.Scripts.Battle.BattleManager;

namespace GoblinCardGame.Scripts.Cards;

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
    [Export] private Button _attackButton;
    [Export] private Button _shieldButton;
    [Export] private Sprite2D _summoningSicknessIcon;
    [Export] private AnimationPlayer _animationPlayer;
    [Export] private Node _actionButtonContainer;
    [Export] private Sprite2D _cardImageSprite;

    /* Signals */
    // [Signal]
    // public delegate void TriggerAddCardToScuffleEventHandler(CardNode cardNode);

    [Signal]
    public delegate void TriggerUpdateCardActionButtonsEventHandler();
    [Signal]
    public delegate void CardNodeUpdateUiEventHandler(CardNode cardNode);

    [Signal]
    public delegate void  CardEnterScuffleEventHandler(CardNode cardNode); // TODO - maybe this should be on scuffle element

    /* Subscriptions */
    private Callable _playerActionsChangedSubscription;

    /* Private properties */
    private string _cardName = "Card Name";
    private CharacterStats _stats;
    private Vector2 _spriteRegion;

    /* Battle properties */
    private bool _hasSummoningSickness;
    private bool _hasActed;

    public List<CardAction> Actions = [];

    // TODO - create status class

    private BattleManager _battleManager;

    public bool IsEnemy { get; set; }

    public IEnumerable<ActionButton> ActionButtons => _actionButtonContainer?.GetChildren().OfType<ActionButton>() ?? [];
    
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

    public int Health => (int)GetStat(StatName.Health);
    public int Shield => (int)GetStat(StatName.Shield);
    public int Power => (int)GetStat(StatName.Power);
    
    public bool HasSummoningSickness
    {
        get => _hasSummoningSickness;
        set
        {
            _hasSummoningSickness = value;
            UpdateSummoningSicknessLabel();
        }
    }

    public Vector2 SpriteRegion
    {
        get => _spriteRegion;
        set
        {
            
            _spriteRegion = value;
            UpdateSpriteRegion();
        }
    }

    /** Determines if card can do action in scuffle */
    public bool CanDoScuffleAction => !_hasActed && !_hasSummoningSickness;
    public bool IsInPlayerHand => _battleManager != null && _battleManager.Battle.PlayerHand.HasCard(this);
    public bool IsPlayable => _battleManager != null && IsInPlayerHand && _battleManager.CanPlayCard;

    /* Lifecycle methods */
    public override void _Ready()
    {
        _InitializeUI();
        _UpdateUI();
    }

    public override void _EnterTree()
    {
        _InitializeBattleManager();
        _SetupSubscriptions();
        GD.Print("Node added to scene tree");
        foreach (Node child in GetChildren())
            GD.Print(child.Name, " - ", child.GetType().Name);
        _UpdateUI();
        // Fire your event here
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        // Remove status effects tied to battle

        // Disconnect signals, stop timers, cleanup
        _RemoveSubscriptions();
    }

    public void _InitializeBattleManager()
    {
        _battleManager = GetNode<BattleManager>(GlobalSettings.BattleManagerPath);
    }

    private void _InitializeUI()
    {
        _healthLabel = GetNode<Label>("CardArea/Stats/Health/Label");
        _shieldLabel = GetNode<Label>("CardArea/Stats/Shield/Label");
        _powerLabel = GetNode<Label>("CardArea/Stats/Power/Label");
        _attackButton = GetNode<Button>("CardArea/Stats/Power");
        _shieldButton = GetNode<Button>("CardArea/Stats/Shield");
        _cardNameLabel = GetNode<Label>("CardArea/NamePanel/Name");
        _summoningSicknessIcon = GetNode<Sprite2D>("CardArea/SummoningSicknessIcon");
        _animationPlayer = GetNode<AnimationPlayer>("CardArea/AnimationPlayer");
        _actionButtonContainer = GetNode("CardArea/Actions");
        _cardImageSprite = GetNode<Sprite2D>("CardArea/Image/Sprite2D");
        if (_actionButtonContainer == null)
            throw new Exception("Action button container not found");
        UpdateStatLabels();
        UpdateStatusIcons();
        UpdateSpriteRegion();
        CreateAndRemoveActionButtons();

        var button = GetNode<Button>("CardArea/Stats/Shield");
        button.Connect("mouse_entered", new Callable(this, nameof(OnButtonMouseEntered)));
        button.Connect("mouse_exited", new Callable(this, nameof(OnButtonMouseExited)));
    }
    
    public int GetStat(StatName statName)
    {
        var prop = _stats.GetType().GetProperty(statName.ToString());
        if (prop == null)
            throw new Exception($"Stat {statName} not found");
        
        return (int)prop.GetValue(_stats);
    }

    public void SetStat(string statName, int value)
    {
        var prop = _stats.GetType().GetProperty(statName);
        if (prop == null)
            throw new Exception($"Stat {statName} not found");
        prop.SetValue(_stats, value);
        
        UpdateStatLabels(); // TODO - normalize names so I can call UpdateStatLabel(statName)
    }
    public void AddStat(StatName statName, int value)
    {
        _stats.AddTempStat(statName, value);
        
        UpdateStatLabels(); // TODO - normalize names so I can call UpdateStatLabel(statName)
    }

    public void SubtractStat(StatName statName, int value)
    {
        AddStat(statName, -value);
    }

    private void OnButtonMouseEntered()
    {
        GD.Print("Mouse entered button");
    }

    private void OnButtonMouseExited()
    {
        GD.Print("Mouse exited button");
    }

    private void _UpdateUI()
    {
        _UpdateActionButtonsUI();
    }
    /** Sets up listeners for signals coming from BattleManager to update UI / status */
    private void _SetupSubscriptions()
    {
        // Update UI when PlayerActionsRemainingChanged fires - if no more actions, cards become unplayable
        _battleManager.PlayerActionsRemainingChanged += OnPlayerActionsRemainingChanged;
        _battleManager.ScuffleEnd += OnScuffleEndEvent;
        
        _stats.StatChanged += OnStatChanged;
    }

    private void _RemoveSubscriptions()
    {
        _battleManager.PlayerActionsRemainingChanged -= OnPlayerActionsRemainingChanged;
        _battleManager.ScuffleEnd -= OnScuffleEndEvent;
        _stats.StatChanged -= OnStatChanged;
    }

    private void OnPlayerActionsRemainingChanged(int newValue, int oldValue)
    {
        _UpdateUI();
    }

    private void OnScuffleEndEvent()
    {
        GD.Print($"scuffle end - reset temp stats for {CardName}");
        _stats.ResetTempStats();
        _UpdateUI();
    }

    private void OnStatChanged(StatChangedEventDetails details)
    {
        // Update action amounts
        var actions = Actions.Where(a => a.Stat == details.Stat);
        foreach (var action in actions)
        {
            action.Amount = details.NewValue;
        }
        GD.Print($"Card {CardName} stat {details.Stat} changed from {details.OldValue} to {details.NewValue}");
    }

    public void InitializeFromCardData(CardData data)
    {
        CardName = data.CardName;
        _stats = new CharacterStats
        {
            BaseMaxHealth = data.MaxHealth,
            Health = data.MaxHealth,
            BaseShield = data.Shield,
            BasePower = data.Power
        };
        IsEnemy = data.IsEnemy;

        SpriteRegion = data.SpriteRegion;

        var attackAction = CardManager.GetCardAction(CardActionType.Attack);
        var shieldAction = CardManager.GetCardAction(CardActionType.Shield);
        shieldAction.Amount = GetStat(shieldAction.Stat.Value);
        Actions.Add(attackAction);
        Actions.Add(shieldAction);
        if (data.Actions != null)
        {
            
            foreach (var actionKey in data.Actions)
            {
                var action = CardManager.GetCardAction(actionKey);
                if (action.Stat != null)
                    action.Amount = GetStat(action.Stat.Value);
                GD.Print($"{action.Type} amount {action.Amount}");
                Actions.Add(action);
            }
        }
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

    public async Task OnScuffleRoundStart(ScuffleRoundEventDetails details)
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
        await ToSignal(_animationPlayer, AnimationMixer.SignalName.AnimationFinished);
    }

    private void AddActionButton(ActionButton actionButton)
    {
        _actionButtonContainer.AddChild(actionButton);
    }
    
    private void UpdateCardNameLabel()
    {
        if (_cardNameLabel != null)
            _cardNameLabel.Text = _cardName;
    }

    private void UpdateShieldLabel()
    {
        if (_shieldLabel != null)
            _shieldLabel.Text = Shield.ToString();
    }

    private void UpdateSummoningSicknessLabel()
    {
        if (_summoningSicknessIcon != null)
            _summoningSicknessIcon.Visible = _hasSummoningSickness;
    }

    private void UpdateHealthLabel()
    {
        if (_healthLabel != null)
            _healthLabel.Text = Health.ToString();
    }

    private void UpdatePowerLabel()
    {
        if (_powerLabel != null)
            _powerLabel.Text = Power.ToString();
    }

    private void UpdateStatLabels()
    {
        UpdateShieldLabel();
        UpdateHealthLabel();
        UpdatePowerLabel();
        UpdateCardNameLabel();
    }

    private void UpdateSpriteRegion()
    {
        if (_cardImageSprite == null)
            return;

        _cardImageSprite.RegionEnabled = true;
        _cardImageSprite.RegionRect = new Rect2(SpriteRegion.X * GlobalSettings.CardSpriteWidth, SpriteRegion.Y * GlobalSettings.CardSpriteHeight, GlobalSettings.CardSpriteWidth, GlobalSettings.CardSpriteHeight);
    }

    private void CreateAndRemoveActionButtons()
    {
        if (_actionButtonContainer == null)
            return;
        
        var actionButtonsList = ActionButtons.ToList();
        var actionButtonsToRemove = actionButtonsList.Where(actionButton => !Actions.Exists(action => action.Type == actionButton.ActionType));
        var actionsToAdd = Actions.Where(action =>
            !actionButtonsList.Exists(actionButton => actionButton.ActionType == action.Type));

        foreach (var actionButton in actionButtonsToRemove)
        {
            RemoveActionButton(actionButton);
        }

        foreach (var action in actionsToAdd)
        {
            var actionButton = CardManager.CreateActionButton(this, action);
            AddActionButton(actionButton);
        }

        EmitSignal(nameof(TriggerUpdateCardActionButtons));
    }

    public void RemoveActionButton(ActionButton button)
    {
        button.RemoveSubscriptions();
        _actionButtonContainer.RemoveChild(button);
        button.QueueFree();
    }

    private void _UpdateActionButtonsUI()
    {
        // Check if playable
        Button playButton = GetNode<Button>("PlayButton");
         if (playButton != null) playButton.Visible = IsPlayable;
                // TODO - update shield button too
        if (_attackButton != null)
            _attackButton.Disabled = !IsPlayable;
        if (_shieldButton != null)
            _shieldButton.Disabled = !IsPlayable;
        
        EmitSignal(nameof(TriggerUpdateCardActionButtons));
    }

    private void UpdateStatusIcons()
    {
        UpdateSummoningSicknessLabel();
    }
    
    /* Event callbacks */
    public void OnPlayButtonPressed()
    {
        // TODO - remove this button
        if (!IsPlayable) return;
        GD.Print("Play this card: ", this);
        // EmitSignal(SignalName.TriggerAddCardToScuffle, this);
    }

    public void TriggerAddToScuffle()
    {
        if (!IsPlayable) return;
        GD.Print("Trigger add card to scuffle ", CardName);
        // EmitSignal(SignalName.TriggerAddCardToScuffle, this);
    }

    public void TriggerShieldAction()
    {
        if (!IsPlayable) return;
        // TODO - make general card action function instead of hardcoding each one.
        GD.Print("Trigger shield action"); 
        _battleManager.PlayCardAction(new CardActionEventDetails
        {
            CardNode = this,
            ActionType = CardActionType.Shield,
            TargetsAlly = true
        });
    }

    public void TriggerAction(CardActionType actionType)
    {
        // Get action
        CardAction action = Actions.FirstOrDefault(cardAction => cardAction.Type == actionType);
        if (action == null)
            throw new Exception("Card does not contain action of triggered type");
        
        _battleManager.PlayCardAction(new CardActionEventDetails
        {
            Action = action,
            ActionType = actionType,
            CardNode = this,
            TargetsAlly = action.TargetsAlly
            // TODO - add target
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
        var damage = (int) GetStat(StatName.Power);
        // Assign damage to shield first then health
        // var remainingHealth = card.Health - damage;
        
        await cardNode.TakeDamage(damage);
        _hasActed = true;
        
        // TODO - battle logging
        GD.Print($"{CardName} attacks {cardNode.CardName} for {damage} damage. {cardNode.GetStat(StatName.Health)} health remaining");
    }

    public async Task TakeDamage(int damage)
    {
        if (GetParent() == null) // TODO - replace this when card is made visible, otherwise animation can't play so awaits forever
        {
            _stats.TakeDamage(damage);
            UpdateHealthLabel();
            UpdateShieldLabel();
            return;
        }
        // Play animation
        var animationTask = PlayAnimationAsync("IsAttacked");
        
        _stats.TakeDamage(damage);
        UpdateHealthLabel();
        UpdateShieldLabel();
        
        await animationTask;
    }
}

public class CardData
{
    public string CardName { get; init; }
    public int MaxHealth { get; init; }
    public int Shield { get; init; }
    public int Power { get; init; }
    public bool IsEnemy { get; init; }
    public CardActionType[] Actions { get; init; }
    
    public Vector2 SpriteRegion { get; init; }
}

public class CardSpriteDetails
{
    public string File { get; init; }
    public Vector2 RegionIndex { get; init; }
    public bool RegionEnabled { get; init; } = true;
}

public class CardEnterScuffleDetails
{
    public CardNode CardNode;
    public int BattleRound;
    public int PreviousCardsPlayed;
    public int PreviousCardsAddedToScuffle;
}

