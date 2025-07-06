using Godot;
using System;
using System.Linq;
using GoblinCardGame.scripts.cards;
using Card = GoblinCardGame.scripts.cards.Card;

public partial class CardSlot : Panel
{
    private PackedScene _cardScene = GD.Load<PackedScene>("res://nodes/Card.tscn");

    public bool HasCard => Card != null;
    public Card Card { get; private set; } = null;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(GlobalSettings.CardWidth, GlobalSettings.CardHeight);
    }

    public Card AttachCard(Card card, bool destroyExistingCard = false)
    {
        Card existingCard = RemoveCard(destroyExistingCard);
        Card = card;
        AddChild(card);
        return existingCard;
    }

    public Card RemoveCard(bool destroy = false)
    {
        Card existingCard = GetChildren()
            .OfType<Card>()
            .FirstOrDefault();

        if (existingCard != null)
        {
            RemoveChild(existingCard);
            this.Card = null;
            if (destroy)
            {
                existingCard.QueueFree();
                return null;
            }
            else
            {
                return existingCard;
            }
        }

        return null;
    }
}
