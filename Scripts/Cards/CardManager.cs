using System;
using System.Text.Json;
using Godot;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using GoblinCardGame.Scripts.Actions;
using GoblinCardGame.Scripts.Utilities.Json;

namespace GoblinCardGame.Scripts.Cards;

public class CardManager
{
    private const string CardDataJsonPath = "res://data/test_cards.json";
    private const string CardActionsJsonPath = "res://data/card_actions.json";
    private static readonly Regex LineRegex = new(@"^\s*(\d+)\s*x?\s*(\w+)", RegexOptions.Compiled);
    
    private static PackedScene _actionButtonScene = GD.Load<PackedScene>("res://Nodes/ActionButton.tscn");

    public static void LoadData()
    {
        CardActionDatabase.Load();
        CardDataDatabase.Load();
    }
    public static class CardActionDatabase
    {
        private static Dictionary<CardActionType, CardAction> _cardActions;
        private static bool _isLoaded;
        public static void Load()
        {
            var file = FileAccess.Open(CardActionsJsonPath, FileAccess.ModeFlags.Read);
            if (file == null)
                throw new Exception($"Failed to open {CardActionsJsonPath}");

            var jsonText = file.GetAsText();
            var options = new JsonSerializerOptions
            {
                Converters = {
                    new JsonStringEnumConverter(), 
                    new Vector2Converter()
                }
            };
            _cardActions = JsonSerializer.Deserialize<Dictionary<CardActionType, CardAction>>(jsonText, options);
            _isLoaded = true;
        }

        public static CardAction Get(CardActionType key)
        {
            if (!_isLoaded)
                Load();

            if (_cardActions.TryGetValue(key, out var value))
            {
                value.Type = key;
                return value.Copy();
            }
            else
            {
                throw new Exception($"Card key not found: {key}");
            }
        }
            
    }
    
    public static class CardDataDatabase
    {
        private static Dictionary<string, CardData> _cardDataDict;
        private static bool _isLoaded;
        public static void Load()
        {
            using var file = FileAccess.Open(CardDataJsonPath, FileAccess.ModeFlags.Read);
            if (file == null)
                throw new Exception("Could not open test_cards.json");

            string json = file.GetAsText();
            var options = new JsonSerializerOptions
            {
                Converters = {
                    new JsonStringEnumConverter(), 
                    new Vector2Converter()
                }
            };
            _cardDataDict = JsonSerializer.Deserialize<Dictionary<string, CardData>>(json, options);
            _isLoaded = true;
        }

        public static CardData Get(string key)
        {
            if (!_isLoaded)
                Load();

            return _cardDataDict.TryGetValue(key, out var value)
                ? value
                : throw new Exception($"Card key not found: {key}");
        }

        public static bool Contains(string key) => _cardDataDict.ContainsKey(key);

        public static IEnumerable<CardData> GetAll() => _cardDataDict.Values;
    }

    public static CardAction GetCardAction(CardActionType actionKey)
    {
        return CardActionDatabase.Get(actionKey);
    }
    
    public static ActionButton CreateActionButton(CardNode cardNode, CardActionType actionType)
    {
        return CreateActionButton(cardNode, GetCardAction(actionType));
    }

    public static ActionButton CreateActionButton(CardNode cardNode, CardAction action)
    {
        var button = _actionButtonScene.Instantiate<ActionButton>();
        button.Initialize(cardNode, action);
        return button;
    }

    /**
     * expects format -
     * 1 card_key
     * 2 other_card_key
     */
    public static CardData[] ReadCardDataFromTextList(string cardTextList)
    {
        var result = new List<CardData>();

        foreach (var line in cardTextList.Split('\n'))
        {
            var match = LineRegex.Match(line.Trim());
            if (!match.Success)
                continue;

            int count = int.Parse(match.Groups[1].Value);
            string key = match.Groups[2].Value;
            for (var i = 0; i < count; i++)
                result.Add(CardDataDatabase.Get(key));
        }

        return result.ToArray();
    }

    public static CardData[] ReadCardDataFromFileLocation(string dataLocation)
    {
        using var file = FileAccess.Open(dataLocation, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr("Could not open test_data.json");
            return new CardData[] { };
        }

        string text = file.GetAsText();

        return ReadCardDataFromTextList(text);
    }
}
         