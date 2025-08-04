
#nullable enable
using System;
using System.Collections.Generic;
using GoblinCardGame.Scripts.Cards;
using GoblinCardGame.Scripts.Cards.Classes;

namespace GoblinCardGame.Scripts.Actions;

public class CardAction
{
    public event Action<int, int> AmountChanged;

    private int _amount;
    
    public CardNode? CardNode { get; set; }
    public CardActionType Type { get; set; } = CardActionType.Attack;
    public string Icon { get; set; } // TODO - not implemented
    public string Text { get; set; } = "Enter Scuffle";
    public StatName? Stat { get; set; }
    public bool TargetsAlly { get; set; }
    public CardAction Copy()
    {
        return new CardAction
        {
            CardNode = CardNode,
            Type = Type,
            Icon = Icon,
            Text = Text,
            Stat = Stat,
            Amount = Amount,
            TargetsAlly = TargetsAlly
        };
    }

    public int Amount
    {
        get => _amount;
        set
        {
            var oldAmount = _amount;
            _amount = value;
            if (oldAmount != _amount)
                AmountChanged?.Invoke(_amount, oldAmount);
        }
    }
    // public int Amount => CardNode != null ? (int)CardNode.GetStat(StatName) : 0;
    
}

public class CardActionEventDetails
{
    public CardAction Action;
    public CardActionType ActionType;
    public CardNode CardNode;
    public CardNode? Target;
    public bool TargetsAlly;

    public bool DiscardAfterAction => ActionType != CardActionType.Attack;
}

public enum CardActionType
{
    Attack,
    Shield,
    Sneak,
    Snipe,
    Assist,
    Confuse
}