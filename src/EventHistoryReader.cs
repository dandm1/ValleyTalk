using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using ValleyTalk;

namespace ValleyTalk;

public class EventHistoryReader
{
    public static EventHistoryReader Instance { get; } = new EventHistoryReader();

    private static readonly Dictionary<string, string> _conversionCache = new();

    private EventHistoryReader()
    {
        if (!Context.IsMainPlayer)
        {
            _multiplayerFilename = $"multiplayer/{Constants.SaveFolderName}.json";
            _fileEventHistories = ModEntry.SHelper.Data.ReadJsonFile<Dictionary<string, StardewEventHistory>>(_multiplayerFilename) ?? new();
            ModEntry.SHelper.Events.GameLoop.Saving += OnSavingFile;
        }
        else
        {
            _saveCache = new();
            ModEntry.SHelper.Events.GameLoop.Saving += OnSavingGameData;
        }
    }

    private void OnSavingFile(object sender, SavingEventArgs e)
    {
        ModEntry.SHelper.Data.WriteJsonFile(_multiplayerFilename, _fileEventHistories);
    }

    private void OnSavingGameData(object sender, SavingEventArgs e)
    {
        foreach (var kvp in _saveCache)
        {
            ModEntry.SHelper.Data.WriteSaveData(kvp.Key, kvp.Value);
        }
        _saveCache.Clear();
    }

    private Dictionary<string, StardewEventHistory> _fileEventHistories = new();
    private Dictionary<string, StardewEventHistory> _saveCache;
    private readonly string _multiplayerFilename;

    internal StardewEventHistory GetEventHistory(string name)
    {
        if (Context.IsMainPlayer)
        {
            return LoadFromSaveFile(name);
        }
        else
        {
            if (_fileEventHistories.TryGetValue(name, out var history))
            {
                return history;
            }
            
            return new StardewEventHistory();
        }
    }
            
    private static StardewEventHistory LoadFromSaveFile(string name)
    {
        {
            var saveName = GetSaveName(name);
            var eventKey = $"EventHistory_{saveName}";
            try
            {
                var history = ModEntry.SHelper.Data.ReadSaveData<StardewEventHistory>(eventKey);
                if (history != null)
                {
                    // Remove anything from the history that happens after the current game time
                    history.RemoveAfter(new StardewTime(Game1.Date,Game1.timeOfDay));
                    return history;
                }
            }
            catch (Exception ex)
            {
                ModEntry.SMonitor.Log($"Error loading event history for {name} from save file. {ex}", LogLevel.Error);
            }
        }

        return new StardewEventHistory();
    }

    internal void UpdateEventHistory(string name, StardewEventHistory eventHistory)
    {
        if (Context.IsMainPlayer)
        {
            var saveName = GetSaveName(name);
            var eventKey = $"EventHistory_{saveName}";
            _saveCache[eventKey] = eventHistory;
        }
        else
        {
            _fileEventHistories[name] = eventHistory;
        }
    }

    private static string GetSaveName(string name)
    {
        string saveName;
        if (_conversionCache.TryGetValue(name, out saveName))
        {
            return saveName;
        }
        // Create a list of characters that are valid - letters, numbers, underscores, periods, or hyphens
        const string validCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_-.";
        saveName = name;
        // Strip out any characters that are not valid
        foreach (var ch in name.Distinct())
        {
            if (!validCharacters.Contains(ch))
            {
                saveName = saveName.Replace(ch.ToString(), string.Empty);
            }
        }

        // If the name is empty (no valid characters), take a hexadecimal representation of the string bytes
        if (string.IsNullOrEmpty(saveName))
        {
            saveName = BitConverter.ToString(name.Select(ch => (byte)ch).ToArray()).Replace("-", "");
        }

        // If the name is too long, truncate it to 50 characters
        if (saveName.Length > 50)
        {
            saveName = saveName.Substring(0, 50);
        }
        _conversionCache[name] = saveName;
        return saveName;
    }
}
