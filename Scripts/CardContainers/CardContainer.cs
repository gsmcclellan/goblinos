using System;
using System.Collections.Generic;
using System.Linq;
using GoblinCardGame.scripts;
using GoblinCardGame.scripts.cards;
using Godot;
using Godot.Collections;

namespace GoblinCardGame.Scripts.CardContainers;

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

    public bool HasCard(Card card)
    {
        return CardCount > 0;
    }

    public Card RemoveRandomCard(int number = 1)
    {
        return CardList[GD.RandRange(0, CardCount)];
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
}