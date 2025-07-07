using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace GoblinCardGame.scripts.cards;

public partial class MeleeCards : CardContainer
{
    public async Task DoBattle()
    {
        for (int i = 0; i < GlobalSettings.NumberOfCombatRounds; i++)
        {
            GD.Print($"Round {i + 1} of {GlobalSettings.NumberOfCombatRounds}");
            await DoBattleRoundAsync();
        }
    }
    
    public async Task DoBattleRoundAsync (float delaySeconds = 0.5f)
    {
        var actedCards = new HashSet<Card>(); // if current card dies, get next card by iterating list until you find one that hasn't acted
        Card currentCard = null;
        do
        {
            currentCard = GetNext(currentCard);
            
            if (currentCard == null)
                throw new Exception("No current card - something went wrong");
            
            // Pick target
            var target = GetNearestTarget(currentCard);

            // Do damage
            if (target != null)
            {
                currentCard.Attack(target);
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
            if (currentCard.Health < 0 || !Cards.Contains(currentCard))
                currentCard = Cards.FirstOrDefault(c => !actedCards.Contains(c));
            else 
                actedCards.Add(currentCard);
        } while (HasNext(currentCard));
    }

    private bool HasNext(Card card)
    {
        return Cards.IndexOf(card) < Cards.Count - 1;
    }

    private Card GetNext(Card card)
    {
        if (card == null)
            return Cards[0];
        return Cards[Cards.IndexOf(card) + 1];
    }

    /**
     * Returns target for melee, either closest left or right card, if they exist (randomly selected if both) or null
     */
    private Card GetNearestTarget(Card card)
    {
        // get index of card
        var index = Cards.IndexOf(card);
        // get card to left
        Card cardOnLeft = null;
        for (int i = index - 1; i >= 0; i--)
        {
            if (Cards[i].IsEnemy != card.IsEnemy)
            {
                cardOnLeft = Cards[i];
                break;
            }
                
        }
        // get card to right
        Card cardOnRight = null;
        for (int i = index + 1; i < Cards.Count; i++)
        {
            if (Cards[i].IsEnemy != card.IsEnemy)
            {
                cardOnRight = Cards[i];
                break;
            }
        }
        
        if (cardOnLeft != null && cardOnRight != null) // randomly pick one
            return GD.Randf() < 0.5f ? cardOnLeft: cardOnRight;
        if (cardOnLeft != null) // Hits left
            return cardOnLeft;
        return cardOnRight; // Hits right or null
    }

    private void KillCard(Card card)
    {
        // Do animations
        RemoveCard(card);
        card.QueueFree();
    }

    public new void AddCard(Card card)
    {
        if (!CanAddCard) return;
        card.CardName = $"({Cards.Count.ToString()}) {card.CardName}";
        
        
        Cards.Add(card);
        AddChild(card);
        _UpdateCardPositions();
    }
}