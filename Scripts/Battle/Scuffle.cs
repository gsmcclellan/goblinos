using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GoblinCardGame.Scripts.Actions;
using GoblinCardGame.Scripts.CardContainers;
using GoblinCardGame.Scripts.Cards;
using GoblinCardGame.Scripts.Utilities.Actions;
using Godot;

namespace GoblinCardGame.Scripts.Battle;

public partial class Scuffle : CardContainers.CardContainer
{
    /** Signals */
    [Signal] public delegate void ScuffleStartEventHandler(); // TODO - hook this event up 
    [Signal] public delegate void ScuffleRoundStartEventHandler(int roundNumber); // TODO - hook this event up 
    [Signal] public delegate void ScuffleRoundEndEventHandler(int roundNumber); // TODO - hook this event up 
    [Signal] public delegate void ScuffleEndEventHandler(); // TODO - hook this event up 
    
    private BattleManager _battleManager;
    
    public override void _Ready()
    {
        _battleManager = GetNode<BattleManager>(GlobalSettings.BattleManagerPath);
    }
    public async Task DoBattle()
    {
        EmitSignal(nameof(ScuffleStart));
        var numCombatRounds = GlobalSettings.NumberOfCombatRounds;
        for (var i = 0; i < numCombatRounds; i++)
        {
            GD.Print($"Round {i + 1} of {GlobalSettings.NumberOfCombatRounds}");
            await DoBattleRoundAsync(i, numCombatRounds);
        }
        EmitSignal(nameof(ScuffleEnd));
    }
    
    private async Task DoBattleRoundAsync (int roundNumber, int numberOfRounds, float delaySeconds = 0.5f)
    {
        var scuffleEventDetails = new ScuffleRoundEventDetails
        {
            RoundNumber = roundNumber,
            NumberOfRounds = numberOfRounds
        };
        await OnScuffleRoundStart(scuffleEventDetails);
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
            var target = GetNearestAttackTarget(currentCardNode);

            // Do damage
            if (target != null)
            {
                await CardAttack(currentCardNode, target);
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

        await OnScuffleRoundEnd(scuffleEventDetails);
    }

    public async Task CardAttack(CardNode attacker, CardNode target)
    {
        await attacker.Attack(target);
        // TODO - attack animation
                    
        // If killed, remove card
        if (target.Health <= 0)
            KillCard(target);
    }

    /** Do things that happen on scuffle round start. Including callback for each card*/
    public async Task OnScuffleRoundStart(ScuffleRoundEventDetails details)
    {
        EmitSignal(nameof(ScuffleRoundStart), details.RoundNumber);
        
        // TODO - round start animation
        foreach (var cardNode in Cards)
        {
            await cardNode.OnScuffleRoundStart(details);
        }
    }

    /** Do things that happen on scuffle round end. Including callback for each card*/
    public async Task OnScuffleRoundEnd(ScuffleRoundEventDetails details)
    {
        EmitSignal(nameof(ScuffleRoundEnd), details.RoundNumber);
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
    private CardNode GetNearestAttackTarget(CardNode cardNode)
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

    public CardNode GetCardActionTarget(CardActionEventDetails details)
    {
        // enemy / friend
        var targetsEnemy = details.CardNode.IsEnemy && details.TargetsAlly ||
                           !details.CardNode.IsEnemy && !details.TargetsAlly;
        // get last index matching -
        return Cards.LastOrDefault(potentialTarget => potentialTarget.IsEnemy == targetsEnemy);

        // factor in level
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

public class ScuffleRoundEventDetails
{
    public int RoundNumber; // starting with 0
    public int NumberOfRounds; // total rounds to be done in scuffle
    public bool IsLastRound => RoundNumber + 1 == NumberOfRounds;
}
