using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace StardewDialogue;

internal class StardewEventHistory
{
    private List<Tuple<StardewTime,IHistory>> _eventHistory = new();
    private List<Tuple<StardewTime,IHistory>> _overheardHistory = new();
    private List<Tuple<StardewTime,IHistory>> _dialogueHistory = new();
    private List<Tuple<StardewTime,IHistory>> _conversationHistory = new();

    public List<Tuple<StardewTime, DialogueEventHistory>> EventHistory
    {
        get
        {
            return _eventHistory.Select(x => new Tuple<StardewTime, DialogueEventHistory>(x.Item1, (DialogueEventHistory)x.Item2)).ToList();
        }
        set
        {
            _eventHistory = value.Select(x => new Tuple<StardewTime, IHistory>(x.Item1, x.Item2)).ToList();
        }
    }

    public List<Tuple<StardewTime, DialogueEventOverheard>> OverheardHistory
    {
        get
        {
            return _overheardHistory.Select(x => new Tuple<StardewTime, DialogueEventOverheard>(x.Item1, (DialogueEventOverheard)x.Item2)).ToList();
        }
        set
        {
            _overheardHistory = value.Select(x => new Tuple<StardewTime, IHistory>(x.Item1, x.Item2)).ToList();
        }
    }

    public List<Tuple<StardewTime, DialogueHistory>> DialogueHistory
    {
        get
        {
            return _dialogueHistory.Select(x => new Tuple<StardewTime, DialogueHistory>(x.Item1, (DialogueHistory)x.Item2)).ToList();
        }
        set
        {
            _dialogueHistory = value.Select(x => new Tuple<StardewTime, IHistory>(x.Item1, x.Item2)).ToList();
        }
    }

    public List<Tuple<StardewTime, ConversationHistory>> ConversationHistory
    {
        get
        {
            return _conversationHistory.Select(x => new Tuple<StardewTime, ConversationHistory>(x.Item1, (ConversationHistory)x.Item2)).ToList();
        }
        set
        {
            _conversationHistory = value.Select(x => new Tuple<StardewTime, IHistory>(x.Item1, x.Item2)).ToList();
        }
    }

    [JsonIgnore]
    public IEnumerable<Tuple<StardewTime, IHistory>> AllTypes => 
        _eventHistory.AsEnumerable<Tuple<StardewTime, IHistory>>()
                .Concat(_overheardHistory)
                .Concat(_dialogueHistory)
                .Concat(_conversationHistory);

    internal void Add(StardewTime time, IHistory theEvent)
    {
        switch(theEvent.GetType().Name)
        {
            case "DialogueEventHistory":
                _eventHistory.Add(new(time,(DialogueEventHistory)theEvent));
                break;
            case "DialogueEventOverheard":
                _overheardHistory.Add(new(time,(DialogueEventOverheard)theEvent));
                break;
            case "DialogueHistory":
                _dialogueHistory.Add(new(time,(DialogueHistory)theEvent));
                break;
            case "ConversationHistory":
                var chEvent = theEvent as ConversationHistory;
                if (_conversationHistory.Any(x => ((ConversationHistory)x.Item2).Id == chEvent.Id))
                {
                    // If the conversation already exists, update it
                    _conversationHistory.RemoveAll(x => ((ConversationHistory)x.Item2).Id == chEvent.Id);
                }
                _conversationHistory.Add(new(time,chEvent));
                break;
            default:
                throw new NotImplementedException();
        }
    }

    internal bool Any()
    {
        return _eventHistory.Any() || _overheardHistory.Any() || _dialogueHistory.Any();
    }

    internal Tuple<StardewTime,IHistory> Last()
    {
        var lastEvent = _eventHistory.LastOrDefault();
        var lastOverheard = _overheardHistory.LastOrDefault();
        var lastDialogue = _dialogueHistory.LastOrDefault();
        var lastConversation = _conversationHistory.LastOrDefault();
        // Return the item with the latest time in Item1 of the tuple
        var lastEventTime = lastEvent?.Item1 ?? new StardewTime();
        var lastOverheardTime = lastOverheard?.Item1 ?? new StardewTime();
        var lastDialogueTime = lastDialogue?.Item1 ?? new StardewTime();
        var lastConversationTime = lastConversation?.Item1 ?? new StardewTime();
        var lastDlg = lastDialogueTime.CompareTo(lastConversationTime) > 0 ? (lastDialogue,lastDialogueTime) : (lastConversation,lastConversationTime);
        if (lastEventTime.CompareTo(lastOverheardTime) > 0)
        {
            return lastEventTime.CompareTo(lastDlg.Item2) > 0 ? lastEvent : lastDlg.Item1;
        }
        else
        {
            return lastOverheardTime.CompareTo(lastDlg.Item2) > 0 ? lastOverheard : lastDlg.Item1;
        }
    }
}