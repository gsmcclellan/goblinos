using System;
using System.Collections.Generic;
using System.Linq;
using GoblinCardGame.Scripts.Cards;
using Godot;

namespace GoblinCardGame.Scripts.CardContainers;

public partial class CardPile : Node2D, ICardContainer
{
    [Signal]
    public delegate void CardListChangedEventHandler();

    public List<CardNode> CardList = [];

    public IEnumerable<CardNode> Cards
    {
        get => CardList;
        set => CardList = value.ToList();
    }

    public bool CanAddCard => true;
    public int CardCount => CardList.Count;
    public bool IsEmpty => CardCount == 0;
    
    private bool _addCard(CardNode cardNode)
    {
        if (!CanAddCard) return false;
        CardList.Add(cardNode);
        return true;
    }

    public bool AddCard(CardNode cardNode)
    {
        bool cardAdded = _addCard(cardNode);
        if (cardAdded)
            EmitSignal(nameof(CardListChanged));
        return cardAdded;
    }

    public bool AddCards(IEnumerable<CardNode> cards)
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
    public CardNode DrawCard()
    {
        if (!IsEmpty)
        {
            return Pop();
        }
        else
            throw new Exception("No shuffled cards");
    }
    public bool HasCard(CardNode cardNode)
    {
        return CardList.Contains(cardNode);
    }
    /** Removes & returns top card in shuffled cards list */
    private CardNode Pop()
    {
        var card = RemoveCardAt(CardList.Count - 1);
        return card;
    }
    public IEnumerable<CardNode> RemoveAllCards(bool destroy = false)
    {
        var cards = new List<CardNode>();
        while (CardList.Count > 0)
        {
            CardNode cardNode = CardList[0];
            RemoveCard(cardNode);
            if (destroy)
                cardNode.QueueFree();
            else
            {
                cards.Add(cardNode);
                cardNode.Position = Vector2.Zero;
            }
                
        }

        return cards;
    }

    public CardNode RemoveRandomCard(int number = 1)
    {
        if (IsEmpty)
            throw new Exception("No cards to remove");

        int index = GD.RandRange(0, CardCount);
        CardNode cardNode = CardList[index];
        RemoveCardAt(index);
        return cardNode;
    }

    public void RemoveCard(CardNode cardNode)
    {
        if (!CardList.Contains(cardNode)) 
            throw new Exception("Card already removed");
        CardList.Remove(cardNode);
        EmitSignal(nameof(CardListChanged));
    }

    public CardNode RemoveCardAt(int index)
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