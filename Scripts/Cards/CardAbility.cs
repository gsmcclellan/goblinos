namespace GoblinCardGame.Scripts.Cards;

public class CardAbility
{
    public string Name;
    public string Description;
    public string Key;
    public CardAbilityType Type;

    public int Amount;
}

public enum CardAbilityType
{
    Action,
    Passive,
    OnEnterScuffle,
    OnEnemyEnterScuffle,
    OnFriendEnterScuffle,
    OnCardEnterScuffle,
    OnTurnStart,
    OnTurnEnd
}