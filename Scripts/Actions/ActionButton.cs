using Godot;
using System;
using GoblinCardGame.Scripts.Cards;

namespace GoblinCardGame.Scripts.Actions;

public partial class ActionButton : Button
{
    /** Signals */
    [Signal]
    public delegate void ActionButtonPressedEventHandler(CardActionType actionType);
    
    /** Exported properties */
    
    /** properties */
    private Label _label;
    private Sprite2D _icon;
    private CardNode _cardNode;
    public CardAction CardAction;

    public CardActionType? ActionType
    {
        get => CardAction != null ? CardAction.Type: null;
        set
        {
            if (value == null)
                return;
            CardAction = CardManager.GetCardAction(value.Value);
        }
    }

    public bool IsAttack => ActionType == CardActionType.Attack;

    public bool IsButtonVisible => _cardNode != null &&  _cardNode.IsInPlayerHand;
    public bool IsEnabled => _cardNode != null && _cardNode.IsPlayable;
    public override void _Ready()
    {
        _label = GetNode<Label>("Label");
        _icon = GetNode<Sprite2D>("Icon");
        
        CustomMinimumSize = new Vector2(GoblinCardGame.Scripts.GlobalSettings.ActionButtonWidth, GoblinCardGame.Scripts.GlobalSettings.ActionButtonHeight);
        
        Connect("pressed", new Callable(this, nameof(OnPressed)));

        _UpdateUI();
    }

    public void Initialize(CardNode cardNode, CardAction action)
    {
        _cardNode = cardNode;
        CardAction = action;

        _UpdateUI();
        ActionButtonPressed += _cardNode.TriggerAction;
        _cardNode.TriggerUpdateCardActionButtons += _UpdateUI;
    }

    public void _UpdateUI()
    {
        if (_cardNode == null)
            return;
        GD.Print($"Action button {ActionType} update UI - CardNode = {_cardNode.CardName}");
        _UpdateLabel();
        Visible = IsButtonVisible;
        Disabled = !IsEnabled;
        GD.Print($"Visible: {Visible} Enabled: ${IsEnabled} Disabled: {Disabled}");
    }

    private void _UpdateLabel()
    {
        if (_label != null && CardAction != null)
            _label.Text = CardAction.Text;
    }

    private void _UpdateIcon()
    {
        // TODO - set sprite path
    }

    public void OnPressed()
    {
        GD.Print("Button pressed, trigger action");
        if (ActionType != null)
            EmitSignal(nameof(ActionButtonPressed), (int) ActionType.Value);
    }

    public void RemoveSubscriptions()
    {
        if (_cardNode != null)
        {
            _cardNode.TriggerUpdateCardActionButtons -= _UpdateUI;
            ActionButtonPressed -= _cardNode.TriggerAction;
        }
            
    }
}
