using Godot;
using System;
using GoblinCardGame.Scripts.Cards;

namespace GoblinCardGame.Scripts.Actions;

public partial class ActionButton : Button
{
    /** Signals */
    [Signal]
    public delegate void TriggerActionButtonPressedEventHandler(CardActionType actionType);
    
    /** Exported properties */
    
    
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
    public override void _Ready()
    {
        _label = GetNode<Label>("Label");
        _icon = GetNode<Sprite2D>("Icon");
        _setParentCardNode();
        
        CustomMinimumSize = new Vector2(GoblinCardGame.Scripts.GlobalSettings.ActionButtonWidth, GoblinCardGame.Scripts.GlobalSettings.ActionButtonHeight);
        
        Connect("pressed", new Callable(this, nameof(OnPressed)));

        _UpdateUI();
    }

    public void Initialize(CardAction action)
    {
        CardAction = action;
    }
    
    private void _setParentCardNode()
    {
        Node current = GetParent();
        while (current != null && _cardNode == null)
        {
            _cardNode = current as CardNode;
            current = current.GetParent();
        }
    }

    private void _UpdateUI()
    {
        _UpdateLabel();
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
    }
}
