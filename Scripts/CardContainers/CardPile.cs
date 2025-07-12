using System;
using System.Collections.Generic;
using System.Linq;
using GoblinCardGame.scripts.cards;
using Godot;

namespace GoblinCardGame.Scripts.CardContainers;

public partial class CardPile: Node2D, ICardContainer
{
    public List<Card> CardList = [];
    public IEnumerable<Card> Cards
    {
        get => CardList;
        set => CardList = value.ToList();
    }
    public bool CanAddCard => true;
    public int CardCount => CardList.Count;
    public bool IsEmpty => CardCount == 0;
    
    public bool AddCard(Card card)
    {
        if (!CanAddCard) return false;
        CardList.Add(card);
        return true;
    }
    /** if any shuffled cards remain, return top card */
    public Card DrawCard()
    {
        if (!IsEmpty)
        {
            return Pop();
        }
        else
            throw new Exception("No shuffled cards");
    }
    public bool HasCard(Card card)
    {
        return CardList.Contains(card);
    }
    /** Removes & returns top card in shuffled cards list */
    private Card Pop()
    {
        return RemoveCardAt(CardList.Count - 1);
    }
    public IEnumerable<Card> RemoveAllCards(bool destroy = false)
    {
        var cards = new List<Card>();
        while (CardList.Count > 0)
        {
            Card card = CardList[0];
            RemoveCard(card);
            if (destroy)
                card.QueueFree();
            else
            {
                cards.Add(card);
                card.Position = Vector2.Zero;
            }
                
        }

        return cards;
    }

    public Card RemoveRandomCard(int number = 1)
    {
        return CardList[GD.RandRange(0, CardCount)];
    }

    public void RemoveCard(Card card)
    {
        if (!CardList.Contains(card)) 
            throw new Exception("Card already removed");
        CardList.Remove(card);
    }

    public Card RemoveCardAt(int index)
    {
        var card = CardList[index];
        CardList.RemoveAt(index);
        return card;
    }
    public void ShuffleCards()
    {
        // var shuffledCardData = _masterCardList.OrderBy(_ => Random.Next()).ToList();
        // var shuffledCards = new List<Card>(CardList);
        var n = CardCount;
        while (n > 1)
        {
            n--;
            var randomIndex = GD.RandRange(0, n);
            (CardList[randomIndex], CardList[n]) = (CardList[n], CardList[randomIndex]);
        }
    }
    public void Cleanup()
    {
        foreach (var card in CardList)
            card.QueueFree();
    }
}