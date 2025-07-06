using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using GoblinCardGame.scripts.cards;
using Card = GoblinCardGame.scripts.cards.Card;

public partial class CardRow : HBoxContainer, ICardContainer
{
    [Export] public int RowSize = 5;
    [Export] private int _overlap = 0;

    private PackedScene _cardScene = GD.Load<PackedScene>("res://nodes/card.tscn");
    private PackedScene _cardSlotScene = GD.Load<PackedScene>("res://nodes/card_slot.tscn");

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
            ClearCards();
            var i = 0;
            foreach (var card in value)
            {
                _cardSlots[i].AttachCard(card);
                i++;
            }
        }
    }

    public int CardCount => _cardSlots.Count(slot => slot.HasCard);

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

    public bool CanAddCard()
    {
        return _cardSlots.Any(cardSlot => !cardSlot.HasCard);
    }

    public void ClearCards(bool destroy = false)
    {
        _cardSlots.ForEach(slot => slot.RemoveCard(destroy));
    }

    public bool HasCard(Card card)
    {
        var hasCard = _cardSlots.Any(cardSlot => cardSlot.Card == card);
        return hasCard;
    }

    public Card RemoveCard(Card card)
    {
        throw new NotImplementedException();
    }

    public Card RemoveRandomCard(int number = 1)
    {
        var i = GD.RandRange(0, CardCount - 1);
        return Cards.ElementAt(i);
    }
}

