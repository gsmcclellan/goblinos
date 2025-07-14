using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Card = GoblinCardGame.scripts.cards.Card;

namespace GoblinCardGame.Scripts.CardContainers;

public partial class CardRow : HBoxContainer, ICardContainer
{
    [Export] public int RowSize = 5;
    [Export] private int _overlap = 0;

    private PackedScene _cardScene = GD.Load<PackedScene>("res://Nodes/Card.tscn");
    private PackedScene _cardSlotScene = GD.Load<PackedScene>("res://Nodes/CardSlot.tscn");

    private List<CardSlot> _cardSlots = new List<CardSlot>();

    public IEnumerable<Card> Cards
    {
        get
        {
            var cards = new List<Card>();
            
            _cardSlots.ForEach((slot) =>
            {
                if (slot.HasCard)
                    cards.Add(slot.Card);
            });
            
            return cards;
        }

        set
        {
            RemoveAllCards();
            var i = 0;
            foreach (var card in value)
            {
                _cardSlots[i].AttachCard(card);
                i++;
            }
        }
    }

    public int CardCount => _cardSlots.Count(slot => slot.HasCard);
    public bool CanAddCard => _cardSlots.Any(cardSlot => !cardSlot.HasCard);
    
    public override void _Ready()
    {
        GD.Print("CardRow Ready");
        _initializeCardSlots();
    }

    private void _initializeCardSlots() {
        for (int i = 0; i < RowSize; i++)
        {
            var slot = (CardSlot)_cardSlotScene.Instantiate();
            AddChild(slot);
            _cardSlots.Add(slot);
        }
    }
    
    public bool AddCard(Card card)
    {
        GD.Print("AddCard: ", card);
        
        foreach (var cardSlot in _cardSlots.Where(cardSlot => !cardSlot.HasCard))
        {
            cardSlot.AttachCard(card);
            return true;
        }

        GD.Print("Cannot add card, card slots full");
        return false;
    }

    

    public IEnumerable<Card> RemoveAllCards(bool destroy = false)
    {
        List<Card> cards = [];
        _cardSlots.ForEach((slot) =>
        {
            var card = slot.RemoveCard(destroy);
            if (card != null)
                cards.Add(card);
        });

        return cards;
    }

    public bool HasCard(Card card)
    {
        var hasCard = _cardSlots.Any(cardSlot => cardSlot.Card == card);
        return hasCard;
    }

    public void RemoveCard(Card card)
    {
        CardSlot cardSlot = _cardSlots.FirstOrDefault(cardSlot => cardSlot.Card == card);
        if (cardSlot == null)
            throw new Exception("Cannot remove card, not found");

        cardSlot.RemoveCard();
    }
}