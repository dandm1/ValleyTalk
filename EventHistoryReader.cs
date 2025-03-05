using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using ValleyTalk;

namespace StardewDialogue
{

public class EventHistoryReader
{
    public static EventHistoryReader Instance { get; } = new EventHistoryReader();

    private EventHistoryReader() 
    { 
        if (!Context.IsMainPlayer)
        {
            _multiplayerFilename = $"multiplayer/{Constants.SaveFolderName},json";
            _fileEventHistories = ModEntry.SHelper.Data.ReadJsonFile<Dictionary<string, StardewEventHistory>>(_multiplayerFilename) ?? new Dictionary<string, StardewEventHistory>();
            ModEntry.SHelper.Events.GameLoop.Saving += OnSaving;
        }
    }

    private void OnSaving(object sender, SavingEventArgs e)
    {
        ModEntry.SHelper.Data.WriteJsonFile(_multiplayerFilename, _fileEventHistories);
    }

    private Dictionary<string, StardewEventHistory> _fileEventHistories = new Dictionary<string, StardewEventHistory>();
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
            var eventKey = $"EventHistory_{name}";
            try
            {
                var history = ModEntry.SHelper.Data.ReadSaveData<StardewEventHistory>(eventKey);
                if (history != null)
                {
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
            ModEntry.SHelper.Data.WriteSaveData($"EventHistory_{name}", eventHistory);
        }
        else
        {
            _fileEventHistories[name] = eventHistory;
        }
    }
}
}