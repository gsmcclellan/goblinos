using System.Collections.Generic;
using GoblinCardGame.Scripts.Actions;
using GoblinCardGame.Scripts.Cards;
using Godot;


namespace GoblinCardGame.Scripts.Battle;
public partial class BattleLogger: Node
{
    private const bool DEBUG_MODE = true;
    
    private BattleManager _battleManager;
    private VBoxContainer _logContainer;
    
    private PackedScene _battleLogScene = GD.Load<PackedScene>(GlobalSettings.BattleLogScenePath);

    private List<IBattleLog> _logDetails = [];
    
    public override void _Ready()
    {
        _battleManager = GetNode<BattleManager>(GlobalSettings.BattleManagerPath);
        _logContainer = GetNode<VBoxContainer>("ScrollContainer/LogContainer");
        _SetupSubscriptions();
    }

    public override void _ExitTree()
    {
        _RemoveSubscriptions();
    }

    private void _RemoveSubscriptions()
    {
        _battleManager.BattleStart -= OnBattleStart;
        _battleManager.CardActionOccurred -= OnCardAction;
        _battleManager.CardAttackOccurred -= OnCardAttack;
        _battleManager.CardDeathOccurred -= OnCardDeath;
        _battleManager.IsPlayerTurnChanged -= OnIsPlayerTurnChanged;
    }

    private void _SetupSubscriptions()
    {
        _battleManager.BattleStart += OnBattleStart;
        _battleManager.CardActionOccurred += OnCardAction;
        _battleManager.CardAttackOccurred += OnCardAttack;
        _battleManager.CardDeathOccurred += OnCardDeath;
        _battleManager.IsPlayerTurnChanged += OnIsPlayerTurnChanged;


        // ScuffleStart
        // ScuffleRoundStart
        // ScuffleRoundEndEvent
        // ScuffleEnd
    }

    private void OnBattleStart()
    {
        Log("Battle started.");
    }

    private void OnCardAction(CardActionEventDetails details)
    {
        Log(details);
    }

    private void OnCardAttack(CardAttackDetails details)
    {
        Log(details);
    }

    private void OnCardDeath(CardNode cardNode)
    {
        var details = new CardDeathBattleLog()
        {
            Message = $"{cardNode.CardName} died."
        };
        Log(details);
    }

    private void OnIsPlayerTurnChanged(bool isPlayerTurn)
    {
        var details = new BattleLog()
        {
            Message = $"{(isPlayerTurn ? "Player":"Enemy")} turn started."
        };
        Log(details);
    }
    
    public void Log(CardActionEventDetails details)
    {
        var battleLogDetails = new BattleLog(details);
        Log(battleLogDetails);
    }

    public void Log(CardAttackDetails details)
    {
        var battleLogDetails = new BattleLog(details);
        Log(battleLogDetails);
    }
    
    public void Log(string str)
     {
         if (DEBUG_MODE)
            GD.Print(str);
         _logContainer.AddChild(CreateBattleLog(str));
     }

    public void Log(BattleLog details)
    {
        if (DEBUG_MODE)
            GD.Print(details.Message);
        _logDetails.Add(details);
        _logContainer.AddChild(CreateBattleLog(details.Message));
    }

    public Label CreateBattleLog(string message)
    {
        var log = _battleLogScene.Instantiate<Label>();
        log.Text = message;
        return log;
    }
}