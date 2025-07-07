using System;
using System.Linq;
using Godot;
using Godot.Collections;

namespace GoblinCardGame.scripts.cards;

public partial class CardContainer : Control
{
    [Export] public int MaxCards = 20;

    public Array<Card> Cards { get; set; } = [];

    public bool CanAddCard => Cards.Count < MaxCards;
    
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
    public void AddCard(Card card)
    {
        if (!CanAddCard) return;
        
        Cards.Add(card);
        AddChild(card);
        _UpdateCardPositions();
    }

    public void RemoveCard(Card card)
    {
        if (!Cards.Contains(card)) 
            throw new Exception("Card alredy removed");
        Cards.Remove(card);
        RemoveChild(card);
        _UpdateCardPositions();
    }
    
    public void ClearCards(bool destroy = false)
    {
        foreach (var card in Cards)
        {
            RemoveChild(card);
            card.QueueFree();
        }
    }
}