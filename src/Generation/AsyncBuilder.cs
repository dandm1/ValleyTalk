using System;
using System.Threading.Tasks;
using StardewModdingAPI.Events;
using StardewValley;

namespace ValleyTalk;
public class AsyncBuilder
{
    private static AsyncBuilder _instance = new AsyncBuilder();
    public static AsyncBuilder Instance => _instance;
    private AsyncBuilder()
    { 
        ModEntry.SHelper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }

    private bool _awaitingGeneration = false;
    private GenerationType _awaitedType = GenerationType.None;
    private NPC _speakingNpc = null;
    private string _currentDialogueKey = "";
    private string _originalLine = null;
    private string[] _currentConversation = null;
    private StardewValley.Object _currentGift = null;
    private int _currentTaste = 0;

    public bool AwaitingGeneration => _awaitingGeneration;
    public NPC SpeakingNpc => _speakingNpc;
    
    private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
    {
        ThinkingWindow thinkingWindow = null;
        // Only perform generation if we are awaiting it
        if (_awaitingGeneration && Game1.activeClickableMenu == null)
        {
            _awaitingGeneration = false;
            // Show "Thinking..." window
            thinkingWindow = new ThinkingWindow($"{_speakingNpc.displayName} is thinking");
            Game1.activeClickableMenu = thinkingWindow;

            _ = PerformGeneration(thinkingWindow);
        }
    }

    private async Task PerformGeneration(ThinkingWindow thinkingWindow)
    {
        try
        {
            var npc = _speakingNpc;

            Task<Dialogue> dialogueTask = null;
            _awaitingGeneration = false;
            switch (_awaitedType)
            {
                case GenerationType.Basic:
                    dialogueTask = GenerateNpc();
                    break;
                case GenerationType.conversation:
                    dialogueTask = GenerateNpcResponse();
                    break;
                case GenerationType.Gift:
                    dialogueTask = GenerateNpcGift();
                    break;
                default:
                    ModEntry.SMonitor?.Log("No valid generation type specified.", StardewModdingAPI.LogLevel.Error);
                    return; // Should not happen, but just in case
            }

            var newDialogue = await dialogueTask;
            // Hide thinking window
            if (Game1.activeClickableMenu == thinkingWindow)
            {
                Game1.exitActiveMenu();
            }

            if (newDialogue != null && newDialogue.dialogues.Count > 0)
            {
                npc.CurrentDialogue.Push(newDialogue);

                Game1.DrawDialogue(newDialogue);
            }
        }
        catch (Exception ex)
        {
            ModEntry.SMonitor?.Log($"Error generating NPC response: {ex.Message}", StardewModdingAPI.LogLevel.Error);

            // Make sure to hide thinking window even if there's an error
            if (thinkingWindow != null && Game1.activeClickableMenu == thinkingWindow)
            {
                Game1.exitActiveMenu();
            }
        }
        finally
        {
            // Reset state
            _awaitingGeneration = false;
            _speakingNpc = null;
            _currentDialogueKey = "";
            _originalLine = null;
            _currentConversation = null;
            _currentGift = null;
            _currentTaste = 0;
            _awaitedType = GenerationType.None;
        }
    }

    internal void RequestNpcResponse(NPC currentNpc, string[] currentConversation)
    {
        if (_awaitingGeneration)
        {
            ModEntry.SMonitor?.Log("Already awaiting NPC response generation. Ignoring new request.", StardewModdingAPI.LogLevel.Warn);
            return;
        }

        _speakingNpc = currentNpc;
        _currentConversation = currentConversation;
        _awaitedType = GenerationType.conversation;
        _awaitingGeneration = true;
    }

    internal void RequestNpcGiftResponse(NPC currentNpc, StardewValley.Object gift, int taste)
    {
        if (_awaitingGeneration)
        {
            ModEntry.SMonitor?.Log("Already awaiting NPC response generation. Ignoring new request.", StardewModdingAPI.LogLevel.Warn);
            return;
        }

        _speakingNpc = currentNpc;
        _currentGift = gift;
        _currentTaste = taste;
        _awaitedType = GenerationType.Gift;
        _awaitingGeneration = true;
    }

    internal void RequestNpcBasic(NPC currentNpc, string dialogueKey, string originalLine)
    {
        if (_awaitingGeneration)
        {
            ModEntry.SMonitor?.Log("Already awaiting NPC response generation. Ignoring new request.", StardewModdingAPI.LogLevel.Warn);
            return;
        }

        _speakingNpc = currentNpc;
        _currentDialogueKey = dialogueKey;
        _originalLine = originalLine;
        _awaitedType = GenerationType.Basic;
        _awaitingGeneration = true;
    }

    private async Task<Dialogue> GenerateNpcGift()
    {
        if (_currentGift == null)
        {
            ModEntry.SMonitor?.Log("No gift object available for NPC gift generation.", StardewModdingAPI.LogLevel.Warn);
            return null;
        }

        var newDialogueTask = DialogueBuilder.Instance.GenerateGift(_speakingNpc, _currentGift, _currentTaste);
        return await newDialogueTask;
    }

    private async Task<Dialogue> GenerateNpc()
    {
        var newDialogueTask = DialogueBuilder.Instance.Generate(_speakingNpc, _currentDialogueKey, _originalLine);
        return await newDialogueTask;
    }

    private async Task<Dialogue> GenerateNpcResponse()
    {
        var npc = _speakingNpc;
        var newDialogueTask = DialogueBuilder.Instance.GenerateResponse(npc, _currentConversation, true);
        var newDialogue = await newDialogueTask;
        if (newDialogue == null)
        {
            ModEntry.SMonitor?.Log("Generated dialogue is null. Returning empty dialogue.", StardewModdingAPI.LogLevel.Warn);
            return null;
        }
        DialogueBuilder.Instance.AddConversation(npc, newDialogue);

        // Create a new dialogue with the response and add it to the NPC's dialogue stack
        var dialogue = new Dialogue(npc, _currentDialogueKey, newDialogue);
        return dialogue;
    }
}

internal enum GenerationType
{
    None,
    Basic,
    conversation,
    Gift
}