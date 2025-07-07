using System;
using Godot;

namespace GoblinCardGame.scripts;

public partial class GlobalSettings : Node
{
    public static readonly int CardWidth = 200;
    public static readonly int CardHeight = 280;

    public static string BattlePath = "/root/Main/Battle";
    public static readonly string BattleManagerPath = "/root/Main/Battle/BattleManager";
    public static readonly string BattleUserInterfacePath = "/root/Main/Battle/BattleUI";
    
    public static readonly Random Random = new Random();

    public static readonly int NumberOfCombatRounds = 2;
    
    public static readonly int EnemyActionsPerTurn = 1;
    
    public static readonly int PlayerActionsPerTurn = 2;
    public static readonly int PlayerStartingCards = 5;
    public static readonly int PlayerDrawCardsPerTurn = 2;
}