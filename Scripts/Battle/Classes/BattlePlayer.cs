using System;
using Godot;

namespace GoblinCardGame.Scripts.Battle;

public class BattlePlayer
{
    private int _actionsRemaining;
    private int _cardsPlayedThisTurn;
    private bool _isTurn;

    public event Action<bool> IsPlayerTurnChanged;
    public event Action<int, int> PlayerActionsRemainingChanged;
    public event Action PlayerTurnStart;
    public event Action PlayerTurnEnd;

    public int ActionsRemaining
    {
        get => _actionsRemaining;
        set
        {
            var oldValue = _actionsRemaining;
            GD.Print("Actions remaining: ", value);
            _actionsRemaining = value;
            if (value != oldValue)
                PlayerActionsRemainingChanged?.Invoke(value, oldValue);
        }
    }

    public bool IsTurn
    {
        get => _isTurn;
        set
        {
            var oldValue = _isTurn;
            GD.Print("IsPlayerTurn: ", value);
            if (!value)
            {
                _actionsRemaining = 0;
                PlayerTurnEnd?.Invoke();
            }
            else
            {
                PlayerTurnStart?.Invoke();
            }
                
            _isTurn = value;
            if (value != oldValue)
                IsPlayerTurnChanged?.Invoke(value);
        }
    }
}