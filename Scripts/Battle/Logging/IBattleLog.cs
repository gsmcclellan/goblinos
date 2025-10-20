using GoblinCardGame.Scripts.Actions;
using GoblinCardGame.Scripts.Cards;

namespace GoblinCardGame.Scripts.Battle;

public interface IBattleLog
{
    public BattleLogDetailsType LogType { get; init; }
    public string Message { get; init; }
    public string Details { get; init; }

    public string PlainTextMessage => Message; // TODO - once message has formatting & whatnot
    public string PlainTextDetails => Details; // TODO - once details has formatting & whatnot
}