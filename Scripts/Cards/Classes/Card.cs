using System;

namespace GoblinCardGame.Scripts.Cards.Classes;

public class Card
{
    private Guid _id;
    private string _cardName = "Card Name";
    private int _shield;
    private int _health;
    private int _maxArmor;
    private int _maxHealth;
    private int _power;
    
    public string CardName => _cardName;

    public int Shield
    {
        get => _shield;
        set => _shield = Math.Clamp(value, 0, MaxArmor);
    }

    public int Health
    {
        get => _health;
        set => _health = Math.Clamp(value, 0, MaxHealth);
    }

    public int MaxArmor => _maxArmor;
    public int MaxHealth => _maxHealth;
    public int Power => _power;
}