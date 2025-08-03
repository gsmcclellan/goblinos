using System;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using GoblinCardGame.Scripts.Actions;
using GoblinCardGame.Scripts.Cards;
using GoblinCardGame.Scripts.Cards.Classes;
using Godot;

namespace GoblinCardGame.Scripts.Battle;

public partial class BattleManager
{
    public CardNode GetTargetForCardAction(CardActionEventDetails details)
    {
        return Battle.Scuffle.GetCardActionTarget(details);
    }
    
    private async Task AddCardToScuffle(CardNode cardNode)
    {
        if (!Battle.Scuffle.CanAddCard) return;
        
        CardSlot cardSlot = cardNode.GetParent() as CardSlot;
        cardSlot?.RemoveCard();
        Battle.Scuffle.AddCard(cardNode);
        
        var cardEnterDetails = new CardEnterScuffleDetails
        {
            CardNode = cardNode,
            BattleRound = _battleRound,
            PreviousCardsPlayed = _cardsPlayedThisTurn,
            PreviousCardsAddedToScuffle = _cardsAddedToScuffleThisTurn
        };

        _cardsAddedToScuffleThisTurn++;
        
        // Resolve things that trigger on card play - 
        // TODO - scuffle cards do things
        // TODO - hand cards do things?
        // TODO - enemy cards do things?
        
        // Move this to be triggered by signal?
        await cardNode.OnEnterScuffle(cardEnterDetails);
    }
    
    public async Task PlayCardAction(CardActionEventDetails details)
    {
        if (PlayerActionsRemaining < 1) return;

        try
        {
            GD.Print("Resolve card action: ", details);
            switch (details.ActionType)
            {
                case CardActionType.Attack:
                    await AddCardToScuffle(details.CardNode);
                    break;
                case CardActionType.Shield:
                    // TODO - make own function
                    GD.Print("shielding");
                    // get target
                    var target = GetTargetForCardAction(details);
                    // carry out action
                    target.AddStat(StatName.Shield, details.CardNode.Shield); // TODO - change to modifier
                    // discard card
                    break;
                case CardActionType.Sneak:
                    await SneakAction(details);
                    break;
                case CardActionType.Snipe:
                    await SnipeAction(details);
                    break;
                case CardActionType.Confuse:
                    await ConfuseAction(details);
                    break;
                case CardActionType.Assist:
                    await AssistAction(details);
                    break;
                default:
                    throw new NotImplementedException($"Card action type {details.ActionType} not implemented");
            }

            if (details.DiscardAfterAction)
                DiscardCard(details.CardNode);

            PlayerActionsRemaining -= 1;
            _cardsPlayedThisTurn++;
        }
        catch (Exception ex)
        {
            GD.PrintErr("Error playing card action: ", ex);
        }
    }

    private CardNode GetScuffleTarget(CardActionEventDetails details)
    {
        // Get target TODO - bind target during mouseover, then include in details
        return Battle.Scuffle.CardList.LastOrDefault(card =>
            card.IsEnemy == (details.CardNode.IsEnemy == details.TargetsAlly));
    }
    
    private CardNode GetScuffleTarget(Func<CardNode, bool> compareFunction)
    {
        // Get target TODO - bind target during mouseover, then include in details
        return Battle.Scuffle.CardList.LastOrDefault(compareFunction);
    }

    public async Task SneakAction(CardActionEventDetails details)
    {
        var target = GetScuffleTarget(details);

        if (target == null)
            throw new Exception("Missing target for action");
        // Take target, move to front of scuffle
        Battle.Scuffle.MoveCardToIndex(target, 0);
    }

    public async Task SnipeAction(CardActionEventDetails details)
    {
        var target = GetScuffleTarget(details);

        await Battle.Scuffle.CardAttack(details.CardNode, target);
    }

    public async Task ConfuseAction(CardActionEventDetails details)
    {
        var target = GetScuffleTarget((CardNode card) =>
        {
            return card.IsEnemy == (details.CardNode.IsEnemy == details.TargetsAlly) &&
                   card.HasSummoningSickness == false;
        });
        target.HasSummoningSickness = true;
    }

    public async Task AssistAction(CardActionEventDetails details)
    {
        var target = GetScuffleTarget(details);

        target.AddStat(StatName.Power, details.CardNode.Power); // TODO - change to modifier
    }
}