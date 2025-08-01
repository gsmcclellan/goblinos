using System;
using Godot;

namespace GoblinCardGame.Scripts;

public partial class GlobalSettings : Node
{
    public const int CardWidth = 200;
    public const int CardHeight = 280;

    public const int CardSpriteHeight = 16;
    public const int CardSpriteWidth = 16;

    public const int ActionButtonHeight = 24;
    public const int ActionButtonWidth = 190;

    public const string BattlePath = "/root/Main/Battle";
    public const string BattleManagerPath = "/root/Main/Battle/BattleManager";
    public const string BattleUserInterfacePath = "/root/Main/Battle/BattleUI";

    public static readonly Random Random = new Random();

    public const int NumberOfCombatRounds = 2;
    public const int NumberOfSummoningSicknessRounds = 1;

    public const int EnemyActionsPerTurn = 2;
    public const int EnemyCombatActionsBeforeSummoningSickness = 1;

    public const int PlayerActionsPerTurn = 2;
    public const int PlayerCombatActionsBeforeSummoningSickness = 1;
    public const int PlayerStartingCards = 5;
    public const int PlayerDrawCardsPerTurn = 2;
}