using Godot;
using System;
using GoblinCardGame.Scripts;
using GoblinCardGame.Scripts.Battle;

public partial class BattleResultsScreen : Node2D
{
    private PackedScene _battleScene = GD.Load<PackedScene>(GlobalSettings.BattleScenePath);
    
    private bool _isVictory;
    public bool IsVictory
    {
        get => _isVictory;
        set
        {
            _isVictory = value;
            _UpdateBattleOverLabel();
        }
    }
    
    /** Nodes */
    private Label _battleOverLabel;
    private Button _resetBattleButton;

    public BattleResultsScreen()
    {
        
    }
    
    public BattleResultsScreen(bool isVictory)
    {
        IsVictory = true;
    }
    
    public override void _Ready()
    {
        _battleOverLabel = GetNode<Label>("BattleOverLabel");
        _resetBattleButton = GetNode<Button>("Details/ResetButton");
        GD.Print("button: ", _resetBattleButton);
        _SetupSubscriptions();
        _UpdateBattleOverLabel();
    }

    private void _SetupSubscriptions()
    {
        _resetBattleButton.Pressed += OnResetBattleButtonPressed;
    }

    private void _RemoveSubscriptions()
    {
        _resetBattleButton.Pressed -= OnResetBattleButtonPressed;
    }

    private void _UpdateBattleOverLabel()
    {
        if (_battleOverLabel == null)
            return;
        _battleOverLabel.Text = IsVictory ? "Victory!" : "Defeat!";
    }

    public override void _ExitTree()
    {
        _RemoveSubscriptions();
    }

    private void OnResetBattleButtonPressed()
    {
        GD.Print("Reset battle");
        // instantiate new battle scene
        var newBattle = _battleScene.Instantiate<Battle>();

        var root = GetTree().Root;
        var currentScene = GetTree().CurrentScene;

        // add & make current first so the viewport shows the new scene immediately
        root.AddChild(newBattle);
        GetTree().CurrentScene = newBattle;

        // then remove & free the old scene immediately
        if (currentScene != null && currentScene != newBattle)
        {
            root.RemoveChild(currentScene);
            currentScene.Free(); // immediate free (not deferred)
        }
    }
}
