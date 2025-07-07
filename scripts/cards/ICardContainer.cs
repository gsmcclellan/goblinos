using System.Collections.Generic;

namespace GoblinCardGame.scripts.cards;

public interface ICardContainer
{
    /* Properties */
    public IEnumerable<Card> Cards { get; set; }
    public int CardCount { get; }
    public bool CanAddCard { get; }
    
    /* Methods */
    public bool AddCard(Card card);
    public void ClearCards(bool destroy = false);
    public bool HasCard(Card card);
    public bool IsEmpty();
    public Card RemoveRandomCard(int number = 1);
    public void RemoveCard(Card card);
}