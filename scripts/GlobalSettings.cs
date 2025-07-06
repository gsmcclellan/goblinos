using Godot;
using System;

public partial class GlobalSettings : Node
{
    public static int CardWidth = 200;
    public static int CardHeight = 280;

    public static string BattlePath = "/root/Main/Battle";
    public static string BattleManagerPath = "/root/Main/Battle/BattleManager";
    
    public static readonly Random Random = new Random();
}
