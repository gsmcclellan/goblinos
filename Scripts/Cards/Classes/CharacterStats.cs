#nullable enable
using System;
using System.Collections.Generic;

namespace GoblinCardGame.Scripts.Cards.Classes;

public class CharacterStats
{
    /** Actions */
    public event Action<StatChangedEventDetails> StatChanged;

    private ModifiableStat MaxHealth { get; }
    private ModifiableStat Power { get;  }
    private ModifiableStat ShieldCapacity { get; }
    
    private int _health;
    
    
    
    private int _baseMaxHealth;
    private int _basePower;
    private int _baseShield;
    private int _tempHealth;
    private int _tempShield;
    private int _tempPower;
    
    public int Health
    {
        get => _health;
        set
        {
            var oldAmount = _health;
            if (BaseMaxHealth == 0)
                BaseMaxHealth = value;
            Math.Clamp(value, 0, MaxHealth);
            if (oldAmount != _health)
                StatChanged?.Invoke(new StatChangedEventDetails(StatName.Health, oldAmount, _health));
        }
    }
    
    // Base properties - define permanent base stats, can have modifiers & ablities add to increase total, plus temp values
    public int BaseMaxHealth
    {
        get => _baseMaxHealth;
        set
        {
            var oldAmount = _baseMaxHealth;
            _baseMaxHealth = Math.Max(value, 1);
            if (oldAmount != _baseMaxHealth)
                StatChanged?.Invoke(new StatChangedEventDetails(StatName.BaseMaxHealth, oldAmount, _baseMaxHealth));
        }
    }

    public int BasePower
    {
        get => _basePower;
        set
        {
            var oldAmount = _basePower;
            _basePower = Math.Max(value, 0);
            if (oldAmount != _basePower)
                StatChanged?.Invoke(new StatChangedEventDetails(StatName.BasePower, oldAmount, _basePower));
        }
    }
    public int BaseShield { get; init; }
    
    // Temp stats - reset on battle reset (replace with list of modifiers that can have different resets)
    public int TempMaxHealth { get; set; }
    public int TempPower { get; set; }
    public int TempShield { get; set; }
    
    

    public int MaxHealth => BaseMaxHealth + TempMaxHealth; // modifiers
    public int Shield => BaseShield + TempShield; // modifiers
    public int Power => BasePower + TempPower;

    public void ResetTempStats()
    {
        TempMaxHealth = 0;
        TempShield = 0;
        TempPower = 0;
    }

    public (int HealthDamage, int ShieldDamage, int RemainingDamage) TakeDamage(int damage)
    {
        var startingHealth = Health;
        var startingShield = Shield;
        
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

            // if (newStatValue != existingStatValue)
            // {
            //     StatChanged?.Invoke(new StatChangedEventDetails
            //     {
            //         Stat = statName,
            //         OldValue = existingStatValue,
            //         NewValue = newStatValue
            //     });
            // }
            i++;
        }

        return (startingHealth - Health, startingShield - Shield, remainingDamage);
    }

    public void AddTempStat(StatName statName, int value)
    {
        var statProperty = GetType().GetProperty(statName.ToString());
        var tempStatProperty = GetType().GetProperty("Temp" + statName);
        
        if (tempStatProperty == null || statProperty == null)
            throw new Exception($"Stat {statName} not found");
        
        
        var currentStatValue = (int)(statProperty.GetValue(this) ?? 0);
        var currentTempStatValue = (int)(tempStatProperty.GetValue(this) ?? 0);

        int newValue = currentTempStatValue + value;
        tempStatProperty.SetValue(this, newValue);
        var newFullValue = (int)(statProperty.GetValue(this) ?? 0);

        // StatChanged?.Invoke(new StatChangedEventDetails(statName, currentStatValue, newFullValue)); // TODO - remove, redundant if setters invoke action
    }
}

public enum StatName
{
    Health,
    Shield,
    Power
}

public enum StatModifierExpiration
{
    EndOfScuffle,
    EndOfBattle
}

public enum StatModifierOperation
{
    Add,
    Multiply
}



public class StatModifier
{
    public string? Source { get;  }
    public StatName StatName { get; }
    public float Value { get; }
    public StatModifierExpiration ExpiresAt { get; } = StatModifierExpiration.EndOfScuffle;
    public StatModifierOperation Operation { get; } = StatModifierOperation.Add;
    public int Stacks { get; private set; } = 1;

    public StatModifier()
    {
        
    }

    public StatModifier(string source, 
                        StatName statName, 
                        float value, 
                        StatModifierExpiration expiresAt, 
                        StatModifierOperation op,
                        int stacks)
    {
        Source = source;
        StatName = statName;
        Value = value;
        ExpiresAt = expiresAt;
        Operation = op;
        Stacks = stacks;
    }
    
    public StatModifier(StatName statName, float value, StatModifierExpiration expiresAt, StatModifierOperation op)
    {
        StatName = statName;
        Value = value;
        ExpiresAt = expiresAt;
        Operation = op;
    }
}

/// <summary>
/// A modifiable stat that also tracks a consumable pool (e.g., Health, Shield, Mana).
/// The pool's <see cref="Current"/> value is clamped to the computed <see cref="Total"/> whenever the total changes.
/// </summary>
/// <remarks>
/// Typical use: create a <see cref="ModifiablePoolStat"/> for Health or Mana. Add modifiers on the fly,
/// and the pool auto-adjusts (gains when max rises; clamps when max falls).
/// </remarks>
/// <seealso cref="ModifiableStat"/>
public sealed class ModifiablePoolStat: ModifiableStat
{
    /// <summary>The current (consumable) amount in the pool. Always clamped to the range [0, <see cref="Total"/>].</summary>
    public int Current { get; private set; }
    
    /// <summary>
    /// Creates a pool stat whose current amount starts at the (clamped) base maximum.
    /// </summary>
    /// <param name="name">Identifier for the stat (e.g., <c>StatName.Health</c>).</param>
    /// <param name="baseValue">The base maximum value before modifiers.</param>
    public ModifiablePoolStat(StatName name, int baseValue)
        : base(name, baseValue)
    {
        Current = Math.Clamp(baseValue, 0, Total);
        StatChanged += OnTotalChanged;
    }
    
    /// <summary>
    /// Creates a pool stat with explicit starting current amount.
    /// </summary>
    /// <param name="name">Identifier for the stat (e.g., <c>StatName.Mana</c>).</param>
    /// <param name="baseValue">The base maximum value before modifiers.</param>
    /// <param name="currentValue">The initial current amount, clamped to [0, <see cref="Total"/>].</param>
    public ModifiablePoolStat(StatName name, int baseValue, int currentValue)
        : base(name, baseValue)
    {
        Current = Math.Clamp(baseValue, 0, Total);
        StatChanged += OnTotalChanged;
    }

    /// <summary>
    /// Handles changes to the computed <see cref="Total"/> by adjusting <see cref="Current"/>.
    /// If the max increases, the pool gains the delta (capped at new max). If the max decreases, the pool clamps down.
    /// </summary>
    /// <param name="changeDetails">Details about the total change.</param>
    private void OnTotalChanged(StatChangedEventDetails changeDetails)
    {
        // If stat was raised, add to current pool
        var delta = changeDetails.NewValue - changeDetails.OldValue;
        if (delta > 0)
            Current = Math.Clamp(Current + delta, 0, Total);
        // If stat was lowered, clamp pool to new max
        else if (delta < 0)
            Current = Math.Min(Current, changeDetails.NewValue);
    }

    /// <summary>
    /// Sets the current pool amount directly, clamped to [0, <see cref="Total"/>].
    /// </summary>
    /// <param name="value">The desired current amount.</pa
    public void SetCurrent(int value)
    {
        Current = Math.Clamp(value, 0, Total);
    }
    
    /// <summary>
    /// Spends up to <paramref name="amount"/> from the pool.
    /// </summary>
    /// <param name="amount">Requested spend amount.</param>
    /// <returns>The amount actually spent (0..amount), never negative.</returns>
    public int Spend(int amount)
    {
        if (amount <= 0) return 0;
        var spent = Math.Min(amount, Current);
        Current -= spent;
        return spent;
    }
    
    /// <summary>
    /// Attempts to spend <paramref name="amount"/> from the pool.
    /// </summary>
    /// <param name="amount">Requested spend amount.</param>
    /// <param name="spent">Outputs how much was actually deducted (≥ 0).</param>
    /// <returns>
    /// <see langword="true"/> if the full amount was available and spent;
    /// <see langword="false"/> if only a partial amount could be spent.
    /// </returns>
    public bool TrySpend(int amount, out int spent)
    {
        if (amount <= 0)
        {
            spent = 0;
            return true; // trivially satisfied
        }

        spent = Math.Min(amount, Current);
        Current -= spent;
        return spent == amount;
    }

    /// <summary>
    /// Increases the current pool by <paramref name="amount"/>, capped at <see cref="Total"/>.
    /// </summary>
    /// <param name="amount">Amount to add. Non-positive values are ignored.</param>
    public void Gain(int amount)
    {
        if (amount <= 0) return;
        Current = Math.Min(Current + amount, Total);
    }
    
    /// <summary>
    /// Sets <see cref="Current"/> to the computed <see cref="Total"/>.
    /// </summary>
    public void RefillToFull() => Current = Total;
}

/// <summary>
/// Represents a base stat with a computed total derived from a base value and a set of modifiers.
/// </summary>
/// <remarks>
/// The computed <see cref="Total"/> is emitted via <see cref="StatChanged"/> when it changes
/// through <see cref="SetBase"/> or modifier list updates (<see cref="AddModifier"/> / <see cref="RemoveWhere"/>).
/// </remarks>
public class ModifiableStat
{
    /// <summary>Logical identifier for the stat (e.g., Health, Power, Shield).</summary>
    public StatName Name { get; }
    /// <summary>The permanent base value before modifiers.</summary>
    public int Base { get; private set; } = 0;
    /// <summary>List of modifiers adjusting the stat.</summary>
    private readonly List<StatModifier> _mods = new();
    
    /// <summary>Raised whenever the computed <see cref="Total"/> changes due to base or modifier adjustments.</summary>
    public event Action<StatChangedEventDetails>? StatChanged;
    
    /// <summary>
    /// Constructs a new modifiable stat.
    /// </summary>
    /// <param name="name">Identifier for the stat.</param>
    /// <param name="baseValue">Initial base value before modifiers.</param>
    public ModifiableStat(StatName name, int baseValue)
    {
        Name = name;
        Base = baseValue;
    }
    
    /// <summary>Read-only view of the applied modifiers.</summary>
    public IReadOnlyList<StatModifier> Mods => _mods;
    
    /// <summary>
    /// The computed total value = Base + additive modifiers (rounded, non-negative).
    /// </summary>
    /// <remarks>
    /// Multiplicative modifiers are not implemented here yet (see TODO). When you add them,
    /// document the stacking model (e.g., compounding vs. summing) and the evaluation order.
    /// </remarks>
    public int Total
    {
        get
        {
            var amountToAdd = 0f;
            
            // TODO - Decide order for when multiply becomes relevent
            foreach (var m in _mods)
            {
                var stacks = Math.Max(1, m.Stacks);
                switch (m.Operation)
                {
                    case StatModifierOperation.Add: 
                        amountToAdd += m.Value * stacks; 
                        break;
                    case StatModifierOperation.Multiply: // TODO implement mult
                    default:
                        throw new NotImplementedException($"Modifier operation {m.Operation} not implemented.");
                }
            }

            var afterAdd = Base + amountToAdd;
            return Math.Max(0, (int) MathF.Round(afterAdd, MidpointRounding.AwayFromZero));
        }
    }
    
    /// <summary>
    /// Sets the base value (pre-modifiers) and raises <see cref="StatChanged"/> if the computed total changes.
    /// </summary>
    /// <param name="value">New base value.</param>
    public void SetBase(int value)
    {
        var oldValue = Total;
        Base = value;
        var newValue = Total;
        if (oldValue != newValue)
            StatChanged?.Invoke(new StatChangedEventDetails(Name, oldValue, newValue));
    }

    /// <summary>
    /// Adds a modifier and raises <see cref="StatChanged"/> if the computed total changes.
    /// </summary>
    /// <param name="mod">The modifier to add.</param>
    public void AddModifier(StatModifier mod)
    {
        var oldValue = Total;
        _mods.Add(mod);
        var newValue = Total;
        if (oldValue != newValue)
            StatChanged?.Invoke(new StatChangedEventDetails(Name, oldValue, newValue));
    } 
    
    /// <summary>
    /// Removes modifiers that satisfy the given predicate and raises <see cref="StatChanged"/> if the total changes.
    /// </summary>
    /// <param name="p">Predicate that determines which modifiers to remove.</param>
    /// <returns>The number of removed modifiers.</returns>
    public int RemoveWhere(Predicate<StatModifier> p)
    {
        var oldValue = Total;
        var returnVal = _mods.RemoveAll(p);
        var newValue = Total;
        if (oldValue != newValue)
            StatChanged?.Invoke(new StatChangedEventDetails(Name, oldValue, newValue));

        return returnVal;
    }
}

/// <summary>
/// Immutable payload describing a change in a computed stat value.
/// </summary>
/// <param name="stat">The stat that changed.</param>
/// <param name="oldValue">The previous computed value.</param>
/// <param name="newValue">The new computed value.</param>
public readonly struct StatChangedEventDetails(StatName stat, int oldValue, int newValue)
{
    /// <summary>The stat that changed.</summary>
    public StatName Stat { get; init; } = stat;
    /// <summary>The previous computed value.</summary>
    public int OldValue { get; init; } = oldValue;
    /// <summary>The new computed value.</summary>
    public int NewValue { get; init; } = newValue;
}
