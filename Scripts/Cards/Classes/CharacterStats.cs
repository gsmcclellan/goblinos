#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GoblinCardGame.Scripts.Cards.Classes;

public class CharacterStats
{
    /** Actions */
    public event Action<StatChangedEventDetails>? StatChanged;

    private ModifiablePoolStat Health { get; }
    private ModifiablePoolStat Shield { get; }
    private ModifiableStat Power { get;  }

    private readonly Dictionary<StatName, ModifiableStat> _statByName;

    public CharacterStats()
    {
        Health = new ModifiablePoolStat(StatName.Health, 0);
        Shield = new ModifiablePoolStat(StatName.Shield, 0);
        Power = new ModifiableStat(StatName.Power, 0);
        
        _statByName = new Dictionary<StatName, ModifiableStat>
        {
            [StatName.Health] = Health,
            [StatName.Shield] = Shield,
            [StatName.Power]  = Power
        };
    }

    public CharacterStats(int startingHealth, int startingShield, int startingPower)
    {
        Health = new ModifiablePoolStat(StatName.Health, startingHealth);
        Shield = new ModifiablePoolStat(StatName.Shield, startingShield);
        Power = new ModifiableStat(StatName.Power, startingPower);
        
        _statByName = new Dictionary<StatName, ModifiableStat>
        {
            [StatName.Health] = Health,
            [StatName.Shield] = Shield,
            [StatName.Power]  = Power
        };
        
    }
    
    public CharacterStats(int startingHealth, int startingShield, int startingPower, List<StatModifier> statModifiers)
    {
        Health = new ModifiablePoolStat(StatName.Health, startingHealth);
        Shield = new ModifiablePoolStat(StatName.Shield, startingShield);
        Power = new ModifiableStat(StatName.Power, startingPower);
        
        _statByName = new Dictionary<StatName, ModifiableStat>
        {
            [StatName.Health] = Health,
            [StatName.Shield] = Shield,
            [StatName.Power]  = Power
        };

        if (statModifiers.Count == 0)
            return;
        
        foreach (var mod in statModifiers)
        {
            if (_statByName.TryGetValue(mod.StatName, out var stat))
            {
                stat.AddModifier(mod);
            }
            else
            {
                // Unknown stat target; log or ignore safely
                // e.g., Console.WriteLine($"No stat found for {mod.StatName}");
                GD.PrintErr($"Unable to apply stat modifier, unknown target {mod.StatName}");
            }
        }
    }

    public void AddModifier(StatModifier mod)
    {
        var stat = _statByName[mod.StatName];

        if (stat == null)
            throw new Exception($"{mod.StatName} stat not found.");

        stat.AddModifier(mod);
    }
    
    public void ExpireStatModifiers(StatModifierExpiration expiresAt = StatModifierExpiration.EndOfBattle)
    {
        // Remove expiring mods; PoolStats auto-clamp Current via OnTotalChanged.
        Health.RemoveWhere(mod => mod.ExpiresAt == expiresAt);
        Power.RemoveWhere(mod => mod.ExpiresAt == expiresAt);
        Shield.RemoveWhere(mod => mod.ExpiresAt == expiresAt);
        
        // Optional: if you ever add temp shield via Gain(...) without a modifier,
        // and you want it to vanish at EndOfRound, you could do:
        // if (phase == StatModifierExpiration.EndOfRound)
        //     Shield.SetCurrent(Math.Min(Shield.Current, Shield.Total));
        // (OnTotalChanged already enforces this when Total drops; this line is only
        // for temp shield NOT tied to a modifier.)
    }

    public IReadOnlyStat Get(StatName name)
    {
        return _statByName[name];
    }

    public IReadOnlyPoolStat GetPoolStat(StatName name)
    {
        var stat = _statByName[name];
        if (stat is ModifiablePoolStat poolStat)
            return poolStat;

        throw new Exception($"Unable to find pooled stat {name}");
    }

    public List<IReadOnlyStat> List()
    {
        return _statByName.Values.Select(stat => (IReadOnlyStat)stat).ToList();
    }

    public DamageReport TakeDamage(int damage)
    {
        if (damage == 0)
            return new DamageReport(0, 0, 0);
        // Shield Damage
        if (Shield.TrySpend(damage, out var shieldDamage))
            return new DamageReport(0, shieldDamage, 0);
        var remainingDamage = damage - shieldDamage;
        
        // Health Damage
        if (Health.TrySpend(remainingDamage, out var healthDamage))
            return new DamageReport(healthDamage, shieldDamage, 0);

        // Remainder
        var overkillDamage = remainingDamage - healthDamage;
        return new DamageReport(healthDamage, shieldDamage, overkillDamage);
    }
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
public sealed class ModifiablePoolStat: ModifiableStat, IReadOnlyPoolStat
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
    /// <returns>The amount actually spent (0 | amount), never negative.</returns>
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
public class ModifiableStat: IReadOnlyStat
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
            
            // TODO - Decide order for when multiply becomes relevant
            foreach (var m in _mods)
            {
                var stacks = Math.Max(1, m.Stacks);
                switch (m.Operation)
                {
                    case StatModifierOperation.Add: 
                        amountToAdd += m.Value * stacks; 
                        break;
                    case StatModifierOperation.Multiply: // TODO implement multiply
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

public readonly struct DamageReport(int h, int s, int ok)
{
    public int Health { get; } = h;
    public int Shield { get; } = s;
    public int Overkill { get; } = ok;
}

public interface IReadOnlyStat
{
    public StatName Name { get; }
    public int Base { get; }
    public int Total { get; }
    public IReadOnlyList<StatModifier> Mods { get; }
}

public interface IReadOnlyPoolStat: IReadOnlyStat
{
    public int Current { get; }
}