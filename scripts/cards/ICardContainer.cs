using System.Collections.Generic;

namespace GoblinCardGame.scripts.cards;

public interface ICardContainer
{
    IEnumerable<Card> Cards { get; set; }
    int CardCount { get; }
    bool AddCard(Card card);
    bool CanAddCard();

    void ClearCards(bool destroy = false);

    bool HasCard(Card card);
    Card RemoveRandomCard(int number = 1);

    Card RemoveCard(Card card);
}