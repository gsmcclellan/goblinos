using System;
using System.Collections.Generic;
using System.Linq;
using GoblinCardGame.scripts.cards;
using Godot;

namespace GoblinCardGame.Scripts.CardContainers;

public partial class CardPile : Node2D, ICardContainer
{
    [Signal]
    public delegate void CardListChangedEventHandler();

    public List<Card> CardList = [];

    public IEnumerable<Card> Cards
    {
        get => CardList;
        set => CardList = value.ToList();
    }

    public bool CanAddCard => true;
    public int CardCount => CardList.Count;
    public bool IsEmpty => CardCount == 0;
    
    private bool _addCard(Card card)
    {
        if (!CanAddCard) return false;
        CardList.Add(card);
        return true;
    }

    public bool AddCard(Card card)
    {
        bool cardAdded = _addCard(card);
        if (cardAdded)
            EmitSignal(nameof(CardListChanged));
        return cardAdded;
    }

    public bool AddCards(IEnumerable<Card> cards)
    {
        // TODO - figure out what to do if some cards can be added but not all
        
        bool anyCardAdded = false;
        foreach (var card in cards)
        {
            
            anyCardAdded = _addCard(card) || anyCardAdded;
        }
        
        if (anyCardAdded)
            EmitSignal(nameof(CardListChanged));

        return anyCardAdded;
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
        if (IsEmpty)
            throw new Exception("No cards to remove");

        int index = (int)GD.RandRange(0, CardCount); // RandRange returns float
        Card card = CardList[index];
        RemoveCardAt(index);
        return card;
    }

    public void RemoveCard(Card card)
    {
        if (!CardList.Contains(card)) 
            throw new Exception("Card already removed");
        CardList.Remove(card);
        EmitSignal(nameof(CardListChanged));
    }

    public Card RemoveCardAt(int index)
    {
        var card = CardList[index];
        CardList.RemoveAt(index);
        EmitSignal(nameof(CardListChanged));
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
        EmitSignal(nameof(CardListChanged));
    }
    public void Cleanup()
    {
        foreach (var card in CardList)
            card.QueueFree();
    }
}