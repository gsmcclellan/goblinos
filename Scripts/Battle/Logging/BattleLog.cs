using GoblinCardGame.Scripts.Actions;
using GoblinCardGame.Scripts.Cards;

namespace GoblinCardGame.Scripts.Battle;

public class BattleLog: IBattleLog
{
    public CardNode? Subject { get; init; }
    public CardNode[] Targets { get; init; } = [];
    public CardAction? Action { get; init; }
    public bool IsCombat { get; init; }
    public bool IsDeath { get; init; }
    public string Message { get; init; }
    public string Details { get; init; }

    public BattleLogDetailsType LogType { get; init; } = BattleLogDetailsType.Info;

    public BattleLog()
    {
        
    }

    public BattleLog(CardActionEventDetails eventDetails)
    {
        Subject = eventDetails.CardNode;
        Targets = [eventDetails.Target];
        Action = eventDetails.Action;
        IsCombat = false;
        IsDeath = false;
        Message = BuildMessage(eventDetails);
    }

    public BattleLog(CardAttackDetails eventDetails)
    {
        Subject = eventDetails.Subject;
        Targets = [eventDetails.Target];
        IsCombat = false;
        IsDeath = false;
        Message = BuildMessage(eventDetails);
    }
    
    public string BuildMessage(CardActionEventDetails eventDetails)
    {
        if (eventDetails.ActionType == CardActionType.Attack)
            return $"{eventDetails.CardNode.CardName} entered the scuffle";

        return
            $"{eventDetails.CardNode.CardName} committed {eventDetails.ActionType} against {(eventDetails.Target != null ? eventDetails.Target.CardName : "nobody")}";
    }

    public string BuildMessage(CardAttackDetails eventDetails)
    {
        return
            $"{eventDetails.Subject.CardName} attacked {eventDetails.Subject.CardName} for {eventDetails.Damage} damage ({eventDetails.ShieldDamage}s {eventDetails.HealthDamage}h {eventDetails.OverkillDamage}ok)";
    }
}

public enum BattleLogDetailsType
{
    Info,
    CardAction,
    ScuffleAction,
    CardDeath,
    CardStatus
}