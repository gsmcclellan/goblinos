using System.Collections.Generic;
using System.Linq;
using GoblinCardGame.Scripts.Cards;
using Godot;

namespace GoblinCardGame.Scripts.CardContainers;

public interface ICardContainer
{
    /* Signals */
    [Signal]
    public delegate void CardListChangedEventHandler();
    
    /* Properties */
    public IEnumerable<CardNode> Cards { get; set; }
    public int CardCount => Cards.Count();
    public bool CanAddCard { get; }
    public bool IsEmpty => CardCount == 0;
    
    /* Methods */
    public bool AddCard(CardNode cardNode);

    public bool AddCards(IEnumerable<CardNode> cardNodes)
    {
        var success = true;
        foreach (var cardNode in cardNodes)
        {
            success = success && AddCard(cardNode);
        }

        return success;
    }
    
    public bool HasCard(CardNode cardNode) => Cards.Contains(cardNode);
    
    public IEnumerable<CardNode> RemoveAllCards(bool destroy = false);
    public CardNode RemoveRandomCard(int number = 1)
        {
            var i = GD.RandRange(0, CardCount - 1);
            var card = Cards.ElementAt(i);
            RemoveCard(card);
            return card;
        }
    public void RemoveCard(CardNode cardNode);
    
    public CardNode SelectRandomCard(int number = 1)
    {
        var i = GD.RandRange(0, CardCount - 1);
        var card = Cards.ElementAt(i);
        return card;
    }
}