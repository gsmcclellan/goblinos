using GoblinCardGame.scripts;
using GoblinCardGame.Scripts.Battle;
using Godot;

namespace GoblinCardGame.Scripts.CardContainers;

public partial class Discard: CardPile
{
    /* Node references */
    private BattleManager _battleManager;
    [Export] private Label _cardCountLabel;
    
    public override void _Ready()
    {
        _battleManager = GetNode<BattleManager>(GlobalSettings.BattleManagerPath);
        _cardCountLabel = GetNode<Label>("CardCountLabel");

        Connect(nameof(CardListChanged), new Callable(this, nameof(OnCardListChanged)));

        UpdateCardCountLabel();
    }
    
    /** Called by subscription when base CardPile class emits CardListChanged signal */
    private void OnCardListChanged ()
    {
        UpdateCardCountLabel();
    }

    /** Handles updating remaining cards label from _shuffledCardCount */
    private void UpdateCardCountLabel()
    {
        if (_cardCountLabel != null)
            _cardCountLabel.Text = $"{CardCount}";
    }
}