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

    public List<CardNode> CardList = [];

    public IEnumerable<CardNode> Cards
    {
        get => CardList;
        set => CardList = value.ToList();
    }

    public int CardCount => CardList.Count;
    public bool CanAddCard => CardCount < MaxCards;

    public bool HasCard(CardNode cardNode)
    {
        return CardCount > 0;
    }

    public CardNode RemoveRandomCard(int number = 1)
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
        
        Array<CardNode> cards = new Array<CardNode>(
            GetChildren().OfType<CardNode>()
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
    public bool AddCard(CardNode cardNode)
    {
        if (!CanAddCard) return false;
        
        CardList.Add(cardNode);
        AddChild(cardNode);
        _UpdateCardPositions();
        return true;
    }

    public void RemoveCard(CardNode cardNode)
    {
        if (!CardList.Contains(cardNode)) 
            throw new Exception("Card already removed");
        CardList.Remove(cardNode);
        RemoveChild(cardNode);
        _UpdateCardPositions();
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
}