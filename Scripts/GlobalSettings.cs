using System;
using Godot;

namespace GoblinCardGame.scripts;

public partial class GlobalSettings : Node
{
    public const int CardWidth = 200;
    public const int CardHeight = 280;

    public const string BattlePath = "/root/Main/Battle";
    public const string BattleManagerPath = "/root/Main/Battle/BattleManager";
    public const string BattleUserInterfacePath = "/root/Main/Battle/BattleUI";

    public static readonly Random Random = new Random();

    public static readonly int NumberOfCombatRounds = 2;
    
    public static readonly int EnemyActionsPerTurn = 1;

    public const int PlayerActionsPerTurn = 2;
    public const int PlayerStartingCards = 5;
    public const int PlayerDrawCardsPerTurn = 2;
}