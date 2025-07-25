using Godot;
using System;
using System.Linq;
using GoblinCardGame.scripts.Cards;

public partial class CardSlot : Panel
{
    private PackedScene _cardScene = GD.Load<PackedScene>("res://nodes/Card.tscn");

    public bool HasCard => CardNode != null;
    public CardNode CardNode { get; private set; } = null;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(GoblinCardGame.scripts.GlobalSettings.CardWidth, GoblinCardGame.scripts.GlobalSettings.CardHeight);
    }

    public CardNode AttachCard(CardNode cardNode, bool destroyExistingCard = false)
    {
        CardNode existingCardNode = RemoveCard(destroyExistingCard);
        CardNode = cardNode;
        AddChild(cardNode);
        return existingCardNode;
    }

    public CardNode RemoveCard(bool destroy = false)
    {
        CardNode existingCardNode = GetChildren()
            .OfType<CardNode>()
            .FirstOrDefault();

        if (existingCardNode != null)
        {
            RemoveChild(existingCardNode);
            this.CardNode = null;
            if (destroy)
            {
                existingCardNode.QueueFree();
                return null;
            }
            else
            {
                return existingCardNode;
            }
        }

        return null;
    }
}
