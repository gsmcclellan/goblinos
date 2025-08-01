
#nullable enable
using System.Collections.Generic;
using GoblinCardGame.Scripts.Cards;

namespace GoblinCardGame.Scripts.Actions;

public class CardAction
{
    public CardActionType Type { get; set; } = CardActionType.Attack;
    public string Text { get; set; } = "Enter Scuffle";
    public string Icon { get; set; } // TODO - not implemented

    public bool TargetsAlly { get; set; }
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