using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

namespace GoblinCardGame.scripts.cards;

public partial class CardContainer : Control, ICardContainer
{
    [Export] public int MaxCards = 20;

    public List<Card> CardList = [];

    public IEnumerable<Card> Cards
    {
        get => CardList;
        set => CardList = value.ToList();
    }

    public int CardCount => CardList.Count;
    public bool CanAddCard => CardCount < MaxCards;

    public IEnumerable<Card> RemoveCards(bool destroy = false)
    {
        throw new NotImplementedException();
    }

    public bool HasCard(Card card)
    {
        throw new NotImplementedException();
    }

    public bool IsEmpty()
    {
        throw new NotImplementedException();
    }

    public Card RemoveRandomCard(int number = 1)
    {
        throw new NotImplementedException();
    }

    public override void _Ready()
    {
        _UpdateCardPositions();
    }
    
    /**
     * Recalculates position of cards in row so they overlap if necessary to all fit
     */
    protected void _UpdateCardPositions()
    {
        
        Array<Card> cards = new Array<Card>(
            GetChildren().OfType<Card>()
        );
        int count = cards.Count;

        if (count == 0)
            return;

        float containerWidth = Size.X;
        
        float cardWidth = GlobalSettings.CardWidth;
        float cardHeight = GlobalSettings.CardHeight;

        float spacing;
        if (cardWidth * count <= containerWidth)
        {
            // No overlap needed
            spacing = cardWidth;
        }
        else
        {
            // Calculate overlap spacing
            spacing = (containerWidth - cardWidth) / (count - 1);
            if (spacing < 0) spacing = 0;
        }

        for (int i = 0; i < count; i++)
        {
            float verticalOffset = cardHeight / 4;
            if (cards[i].IsEnemy)
                verticalOffset *= -1;
            cards[i].Position = new Vector2(i * spacing, verticalOffset);
            cards[i].ZIndex = count - i;
        }
    }

    
    /** Adds card if able */
    public bool AddCard(Card card)
    {
        if (!CanAddCard) return false;
        
        CardList.Add(card);
        AddChild(card);
        _UpdateCardPositions();
        return true;
    }

    public void RemoveCard(Card card)
    {
        if (!CardList.Contains(card)) 
            throw new Exception("Card already removed");
        CardList.Remove(card);
        RemoveChild(card);
        _UpdateCardPositions();
    }
    
    public void ClearCards(bool destroy = false)
    {
        foreach (var card in CardList)
        {
            RemoveChild(card);
            card.QueueFree();
        }
    }
}