using System.Collections.Generic;
using System.Linq;
using GoblinCardGame.scripts.cards;
using Godot;

namespace GoblinCardGame.Scripts.CardContainers;

public interface ICardContainer
{
    /* Properties */
    public IEnumerable<Card> Cards { get; set; }
    public int CardCount => Cards.Count();
    public bool CanAddCard { get; }
    public bool IsEmpty => CardCount == 0;
    
    /* Methods */
    public bool AddCard(Card card);
    
    public bool HasCard(Card card) => Cards.Contains(card);
    
    public IEnumerable<Card> RemoveAllCards(bool destroy = false);
    public Card RemoveRandomCard(int number = 1)
        {
            var i = GD.RandRange(0, CardCount - 1);
            var card = Cards.ElementAt(i);
            RemoveCard(card);
            return card;
        }
    public void RemoveCard(Card card);
}