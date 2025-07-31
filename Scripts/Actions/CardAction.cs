
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
    public CardActionType ActionType;
    public CardNode CardNode;
    public bool TargetsAlly;
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