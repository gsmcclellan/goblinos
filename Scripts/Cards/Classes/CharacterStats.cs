using System;
using System.Collections.Generic;

namespace GoblinCardGame.Scripts.Cards.Classes;

public class CharacterStats
{
    /** Actions */
    public event Action<StatChangedEventDetails> StatChanged;
    
    private int _health;
    private int _baseMaxHealth;

    // Base properties - define permanent base stats, can have modifiers & ablities add to increase total, plus temp values
    public int BaseMaxHealth
    {
        get => _baseMaxHealth;
        set => _baseMaxHealth = value;
    }
    public int BasePower { get; init; }
    public int BaseShield { get; init; }
    
    // Temp stats - reset on battle reset (replace with list of modifiers that can have different resets)
    public int TempMaxHealth { get; set; }
    public int TempPower { get; set; }
    public int TempShield { get; set; }
    
    public int Health
    {
        get => _health;
        set
        {
            if (_baseMaxHealth == 0)
                _baseMaxHealth = value;
            _health = Math.Min(value, MaxHealth);
        }
    }

    public int MaxHealth => BaseMaxHealth + TempMaxHealth; // modifiers
    public int Shield => BaseShield + TempShield; // modifiers
    public int Power => BasePower + TempPower;

    public void ResetTempStats()
    {
        TempMaxHealth = 0;
        TempShield = 0;
        TempPower = 0;
    }

    public int TakeDamage(int damage)
    {
        List<StatName> damageTargets = [StatName.TempShield, StatName.BaseShield, StatName.Health];
        var i = 0;
        var remainingDamage = damage;
        while (i < damageTargets.Count && remainingDamage > 0)
        {
            var statName = damageTargets[i];
            // Get current value of damage target
            var statProp = GetType().GetProperty(statName.ToString());
            if (statProp == null)
                throw new Exception($"Stat {statName} not found");
            var existingStatValue = (int)(statProp.GetValue(this) ?? 0);
            
            // calculate damage & remove from damage target, sub from remaining value
            var damageToApply = Math.Min(damage, existingStatValue);
            
            var newStatValue = existingStatValue - damageToApply;
            remainingDamage -= damageToApply;
            
            // Set resulting value, increment i to go to next damage target
            statProp.SetValue(this, newStatValue);

            if (newStatValue != existingStatValue)
            {
                StatChanged?.Invoke(new StatChangedEventDetails
                {
                    StatName = statName,
                    OldValue = existingStatValue,
                    NewValue = newStatValue
                });
            }
            i++;
        }

        return remainingDamage;
    }

    public void AddTempStat(StatName statName, object value)
    {
        var prop = GetType().GetProperty("Temp" + statName);
        if (prop == null)
            throw new Exception($"Stat {statName} not found");
        
        var currentValue = prop.GetValue(this);
        if (currentValue == null)
            throw new Exception($"Stat {statName} value is null");
        
        var propType = prop.PropertyType;
        var convertedValue  = Convert.ChangeType(value, propType);
        
        dynamic currentDynamic = currentValue;
        dynamic addDynamic = convertedValue;
        prop.SetValue(this, currentDynamic + addDynamic);
    }
}

public class StatModifier
{
    public StatName StatName { get; set; }
    public object Value { get; set; }
    public StatModifierDuration Duration;
    public StatModifierType Type;
}

public enum StatName
{
    BaseMaxHealth,
    BasePower,
    BaseShield,
    Health,
    MaxHealth,
    Power,
    Shield,
    TempHealth,
    TempShield,
    TempPower
}

public enum StatModifierDuration
{
    Scuffle,
    Battle
}

public enum StatModifierType
{
    Add,
    Subtract,
    Multiply
}

public class StatChangedEventDetails
{
    public StatName StatName { get; init; }
    public object OldValue { get; init; }
    public object NewValue { get; init; }
}
