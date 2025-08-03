using Godot;
using System;
using System.Collections.Generic;

namespace GoblinCardGame.Scripts.Utilities.Actions;

public class SubscriptionManager
{
    private readonly List<Action> _eventUnsubscribers = new();

    // Subscribe with subscribe/unsubscribe lambdas and handler
    public void Subscribe<T>(Action<T> subscribe, Action<T> unsubscribe, T handler) where T : Delegate
    {
        subscribe(handler);
        _eventUnsubscribers.Add(() => unsubscribe(handler));
    }

    public void Clear()
    {
        foreach (var unsub in _eventUnsubscribers)
            unsub();

        _eventUnsubscribers.Clear();
    }
}