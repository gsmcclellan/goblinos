using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GoblinCardGame.scripts;
using GoblinCardGame.Scripts.CardContainers;
using GoblinCardGame.scripts.Cards;
using Godot;

namespace GoblinCardGame.Scripts.Battle;

public partial class Scuffle : CardContainers.CardContainer
{
    private BattleManager _battleManager;
    
    public override void _Ready()
    {
        _battleManager = GetNode<BattleManager>(GlobalSettings.BattleManagerPath);
    }
    public async Task DoBattle()
    {
        var numCombatRounds = GlobalSettings.NumberOfCombatRounds;
        for (var i = 0; i < numCombatRounds; i++)
        {
            GD.Print($"Round {i + 1} of {GlobalSettings.NumberOfCombatRounds}");
            await DoBattleRoundAsync(i, numCombatRounds);
        }
    }
    
    private async Task DoBattleRoundAsync (int roundNumber, int numberOfRounds, float delaySeconds = 0.5f)
    {
        await OnScuffleRoundStart(new ScuffleRoundStartDetails
        {
            RoundNumber = roundNumber,
            NumberOfRounds = numberOfRounds
        });
        var actedCards = new HashSet<CardNode>(); // if current card dies, get next card by iterating list until you find one that hasn't acted
        CardNode currentCardNode = null;
        do
        {
            currentCardNode = GetNext(currentCardNode);
            
            if (currentCardNode == null)
                throw new Exception("No current card - something went wrong");

            if (!currentCardNode.CanDoScuffleAction)
            {
                // TODO - log that a thing didn't occur & why
                GD.Print(currentCardNode.CardName, " is unable to act");
                continue;
            }
                
            
            // Pick target
            var target = GetNearestTarget(currentCardNode);

            // Do damage
            if (target != null)
            {
                currentCardNode.Attack(target);
                // TODO - attack animation
                await ToSignal(GetTree().CreateTimer(delaySeconds), "timeout");
                    
                // If killed, remove card
                if (target.Health < 0)
                    KillCard(target);
            }
            else
            {
                // Combat is over
                GD.Print("No targets, combat over");
                return;
            }
                

            // If current card has died, get first card in Cards which has not acted
            if (currentCardNode.Health < 0 || !CardList.Contains(currentCardNode))
                currentCardNode = CardList.FirstOrDefault(c => !actedCards.Contains(c));
            else 
                actedCards.Add(currentCardNode);
        } while (HasNext(currentCardNode));
    }

    /** Do things that happen on scuffle round start. Including callback for each card*/
    public async Task OnScuffleRoundStart(ScuffleRoundStartDetails details)
    {
        // TODO - round start animation
        foreach (var cardNode in Cards)
        {
            await cardNode.OnScuffleRoundStart(details);
        }
    }

    /** Do things that happen on scuffle round end. Including callback for each card*/
    public async Task OnScuffleRoundEnd()
    {
        
    }

    private bool HasNext(CardNode cardNode)
    {
        return CardList.IndexOf(cardNode) < CardCount - 1;
    }

    private CardNode GetNext(CardNode cardNode)
    {
        if (cardNode == null)
            return CardList[0];
        return CardList[CardList.IndexOf(cardNode) + 1];
    }

    /**
     * Returns target for melee, either closest left or right card, if they exist (randomly selected if both) or null
     */
    private CardNode GetNearestTarget(CardNode cardNode)
    {
        // TODO - if no target in scuffle, target something in discard (random?)
        // get index of card
        var index = CardList.IndexOf(cardNode);
        // get card to left
        CardNode cardNodeOnLeft = null;
        for (int i = index - 1; i >= 0; i--)
        {
            if (CardList[i].IsEnemy != cardNode.IsEnemy)
            {
                cardNodeOnLeft = CardList[i];
                break;
            }
                
        }
        // get card to right
        CardNode cardNodeOnRight = null;
        for (int i = index + 1; i < CardCount; i++)
        {
            if (CardList[i].IsEnemy != cardNode.IsEnemy)
            {
                cardNodeOnRight = CardList[i];
                break;
            }
        }
        
        if (cardNodeOnLeft != null && cardNodeOnRight != null) // randomly pick one
            return GD.Randf() < 0.5f ? cardNodeOnLeft: cardNodeOnRight;
        if (cardNodeOnLeft != null) // Hits left
            return cardNodeOnLeft;
        if (cardNodeOnRight != null) // Hits right
            return cardNodeOnRight;
        
        // Check in discard for available targets
        var discardedCards = _battleManager.Battle.Discard.Cards;
        var discardTarget = discardedCards.FirstOrDefault(dc => dc.IsEnemy != cardNode.IsEnemy); // TODO - decide if last, or random
        
        if (discardTarget != null)
        {
            GD.Print("Attack discarded card: ", discardTarget.CardName);
            return discardTarget;
        }
            

        return null; // No targets found
    }

    private void KillCard(CardNode cardNode)
    {
        // TODO - Do animations
        if (IsAncestorOf(cardNode))
            cardNode.GetParent<ICardContainer>().RemoveCard(cardNode);
        else
            // Must be attacking discarded card
            _battleManager.Battle.Discard.RemoveCard(cardNode);
        cardNode.QueueFree();
    }
    public new void AddCard(CardNode cardNode)
    {
        if (!CanAddCard) return;
        cardNode.CardName = $"({CardCount.ToString()}) {cardNode.CardName}";
        
        CardList.Add(cardNode);
        AddChild(cardNode);
        _UpdateCardPositions();
    }
}

public class ScuffleRoundStartDetails
{
    public int RoundNumber; // starting with 0
    public int NumberOfRounds; // total rounds to be done in scuffle
    public bool IsLastRound => RoundNumber + 1 == NumberOfRounds;
}
