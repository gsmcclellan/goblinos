using System.Threading.Tasks;
using GoblinCardGame.Scripts;
using Godot;

namespace GoblinCardGame.Scripts.Battle;

public partial class BattleUserInterface : Control
{
    private BattleManager _battleManager;

    [Export] private Label _playerActionsRemainingValueLabel;
    [Export] private Button _playerPassTurnButton;

    private bool _CanPlayerPassTurn => _battleManager != null && _battleManager.IsPlayerTurn;

    public void _Init()
    {
        _InitializeBattleComponents();
        _SetupSubscriptions();
    }
    
    private void _InitializeBattleComponents()
    {
        _battleManager = GetNode<BattleManager>(GlobalSettings.BattleManagerPath);

        _playerActionsRemainingValueLabel = GetNode<Label>("ActionsRemainingValue");
        _playerPassTurnButton = GetNode<Button>("PassPlayerTurnButton");
        
        SetPlayerActionsRemainingValueLabel(_battleManager.PlayerActionsRemaining);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        _RemoveSubscriptions();
    }

    private void _SetupSubscriptions()
    {
        _battleManager.PlayerActionsRemainingChanged += OnPlayerActionsRemainingChanged;
        _battleManager.IsPlayerTurnChanged += OnIsPlayerTurnChanged;
    }

    private void _RemoveSubscriptions()
    {
        _battleManager.PlayerActionsRemainingChanged -= OnPlayerActionsRemainingChanged;
        _battleManager.IsPlayerTurnChanged -= OnIsPlayerTurnChanged;
    }

    private void _UpdatePlayerPassTurnButton()
    {
        _playerPassTurnButton.Disabled = !_CanPlayerPassTurn;
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

    public void OnIsPlayerTurnChanged(bool isPlayerTurn)
    {
        _UpdatePlayerPassTurnButton();
    }

    public void OnPlayerPassTurnButtonPressed()
    {
        GD.Print("OnPlayerPassTurnButton pressed");

        _ = _battleManager.HandlePlayerPassTurn();
    }
}