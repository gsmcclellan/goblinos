using Godot;

namespace GoblinCardGame.scripts.Battle;

public partial class BattleUserInterface : Control
{
    private BattleManager _battleManager;

    [Export] private Label _playerActionsRemainingValueLabel;

    public override void _Ready()
    {
        _InitializeBattleComponents();
        _SetupSubscriptions();
    }
    
    private void _InitializeBattleComponents()
    {
        _battleManager = GetNode<BattleManager>(GlobalSettings.BattleManagerPath);

        _playerActionsRemainingValueLabel = GetNode<Label>("ActionsRemainingValue");
        
        SetPlayerActionsRemainingValueLabel(_battleManager.PlayerActionsRemaining);
    }

    private void _SetupSubscriptions()
    {
        _battleManager.Connect(
            "PlayerActionsRemainingChanged",
            Callable.From<int, int>(OnPlayerActionsRemainingChanged)
        );
    }

    private void OnPlayerActionsRemainingChanged(int newValue, int oldValue)
    {
        GD.Print($"PlayerActionsRemainingChanged from {oldValue} to {newValue}");
        SetPlayerActionsRemainingValueLabel(newValue);
    }

    private void SetPlayerActionsRemainingValueLabel(int value)
    {
        if (_playerActionsRemainingValueLabel != null)
            _playerActionsRemainingValueLabel.Text = value.ToString();
    }
}