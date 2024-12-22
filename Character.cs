using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using LlamaDialogue;
using StardewValley;
using Polly;
using Polly.Retry;
using System.Threading.Tasks;
using Serilog;

namespace StardewDialogue;

public class Character
{
    private BioData _bioData;

    
    private static readonly Dictionary<string,TimeSpan> filterTimes = new() { { "House", TimeSpan.Zero }, { "Action", TimeSpan.Zero }, { "Received Gift", TimeSpan.Zero }, { "Given Gift", TimeSpan.Zero }, { "Editorial", TimeSpan.Zero }, { "Gender", TimeSpan.Zero }, { "Question", TimeSpan.Zero } };
    private StardewEventHistory eventHistory = new();
    internal IEnumerable<Tuple<StardewTime,IHistory>> EventHistory => eventHistory.AllTypes;

    public Character(string name, NPC stardewNpc)
    {
        Name = name;
        BioFilePath = $"assets/bio/{Name}.txt";
        StardewNpc = stardewNpc;

        // Load and process the dialogue file
        LoadDialogue();
        LoadEventHistory();
        ////Log.Information($"Loaded dialogue for {Name}");
    }


    private void LoadDialogue()
    {
        var canonDialogue = StardewNpc.Dialogue;
        DialogueData = new();
        foreach (var dialogue in canonDialogue)
        {
            var context = new DialogueContext(dialogue.Key);
            var value = new DialogueValue(dialogue.Value);
            if (value is DialogueValue)
            {
                DialogueData.Add("Base",context, value);
            }
        }
    }

    private void LoadBio()
    {
        BioData bioData = null;
        
        bioData = ModEntry.SHelper.Data.ReadJsonFile<BioData>(BioFilePath);

        _bioData = bioData ?? new BioData();
    }

    private void LoadEventHistory()
    {
        var eventKey = $"EventHistory_{Name}";
        try
        {
        var history = ModEntry.SHelper.Data.ReadSaveData<StardewEventHistory>(eventKey);
        if (history != null)
        {
            eventHistory = history;
        }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error loading event history for {Name}");
        }
    }

    public string[] CreateBasicDialogue(DialogueContext context)
    {
        string[] results = Array.Empty<string>();
        var prompts = new Prompts(context,this);

        var pipeline = new ResiliencePipelineBuilder()
                    .AddRetry(
                        new RetryStrategyOptions() 
                        { 
                            MaxRetryAttempts = 4,
                            DelayGenerator = static args =>
                            {
                                var delay = args.AttemptNumber < 2 ? 0 : 5;
                                return new ValueTask<TimeSpan?>(TimeSpan.FromSeconds(delay));
                            }
                        }
                    )
                    .AddTimeout(TimeSpan.FromSeconds(10))
                    .Build();
        int retryCount = 0;
        var commandPrompt = prompts.Command;

        pipeline.Execute(() =>
        {
            retryCount++;
            var resultString = Llm.Instance.RunInference(prompts.System, prompts.GameConstantContext, prompts.NpcConstantContext, $"{prompts.CorePrompt}{commandPrompt}{prompts.Instructions}", prompts.ResponseStart);
            // Apply relaxed validation if this is the second retry
            
            results = ProcessLines(resultString, retryCount > 2).ToArray();
            if (results.Length == 0)
            {
                //Force a retry in the pipeline
                throw new InvalidDataException("No valid lines returned from AI");
            }
            if (ModEntry.Config.Debug)
            {
                // Open 'generation.log' and append values to it
                Log.Debug($"Context:");
                Log.Debug($"Name: {Name}");
                Log.Debug($"Marriage: {context.Married}");
                Log.Debug($"Birthday: {context.Birthday}");
                Log.Debug($"Location: {context.Location}");
                Log.Debug($"Weather: {string.Concat(context.Weather)}");
                Log.Debug($"Time of Day: {context.TimeOfDay}");
                Log.Debug($"Day of Season: {context.DayOfSeason}");
                Log.Debug($"Gift: {context.Accept}");
                Log.Debug($"Spouse Action: {context.SpouseAct}");
                Log.Debug($"Random Action: {context.RandomAct}");
                Log.Debug($"Prompts: {JsonSerializer.Serialize(prompts)}");
                if (context.ScheduleLine != "")
                {
                    Log.Debug($"Original Line: {context.ScheduleLine}");
                }
                Log.Debug($"Results: {results[0]}");
                if (results.Length > 1)
                {
                    foreach (var result in results.Skip(1))
                    {
                        Log.Debug($"Response: {result}");
                    }
                }
                Log.Debug("--------------------------------------------------");
            }
        });
        return results;
    }

    public static IEnumerable<string> ProcessLines(string resultString,bool relaxedValidation = false)
    {
        var resultLines = resultString.Split('\n').AsEnumerable();
        // Remove any line breaks
        resultLines = resultLines.Select(x => x.Replace("\n", "").Replace("\r", ""));
        resultLines = resultLines.Where(x => !string.IsNullOrWhiteSpace(x));
        // Find the first line that starts with '- ' and remove any lines before it
        resultLines = resultLines.SkipWhile(x => !x.StartsWith("- "));
        // Check that the first line starts with '- '.  Omit any subsequent lines that don't start with '% '
        var validLayout = (resultLines.FirstOrDefault()?? string.Empty ).StartsWith("- ");
        if (!validLayout)
        {
            //Log.Debug("Invalid layout detected in AI response.  Returning the full response.");
            return Array.Empty<string>();
        }
        var responseLines = resultLines.Skip(1).Where(x => x.StartsWith("% "));
        resultLines = resultLines.Take(1).Concat(responseLines);
        // Remove any leading punctuation
        resultLines = resultLines.Select(x => x.Trim().TrimStart('-', ' ', '"', '%'));
        resultLines = resultLines.Select(x => x.Trim().TrimEnd('"'));
        // If the string starts or ends with #$b# remove it.
        resultLines = resultLines.Select(x => x.StartsWith("#$b#") ? x[4..] : x);
        resultLines = resultLines.Select(x => x.EndsWith("#$b#") ? x[..^4] : x);
        // If the string contains $e or $b without a # before them, add a #
        resultLines = resultLines.Select(x => x.Replace("$e", "#$e").Replace("$b", "#$b"));
        resultLines = resultLines.Select(x => x.Replace("##$e", "#$e").Replace("##$b", "#$b"));
        resultLines = resultLines.Select(x => x.Replace("#$c .5#",""));
        // If the string contains any emotion indicators ($0, $s, $l, $a or $h) with a # before them, remove the #
        resultLines = resultLines.Select(x => x.Replace("#$0", "$0").Replace("#$s", "$s").Replace("#$l", "$l").Replace("#$a", "$a").Replace("#$h", "$h"));
        // Remove any quotation marks
        resultLines = resultLines.Select(x => x.Replace("\"", ""));
        // Remove any blank lines and trim the rest
        resultLines = resultLines.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Where(x => x.Length > 2);
        var firstElements = resultLines.FirstOrDefault().Split('#');
        if (firstElements.Any(x => x.Length > 200 && !relaxedValidation))
        {
            //Log.Debug("Long line detected in AI response.  Returning nothing.");
            return Array.Empty<string>();
        }
        return resultLines;
    }

    internal void AddDialogue(IEnumerable<StardewValley.DialogueLine> dialogues, int year, StardewValley.Season season, int dayOfMonth, int timeOfDay)
    {
        var time = new StardewTime(year,season, dayOfMonth, timeOfDay);
        
        AddHistory(new DialogueHistory(dialogues),time);
    }

    internal void AddHistory(IHistory theEvent, StardewTime time)
    {
        eventHistory.Add(time,theEvent);
        ModEntry.SHelper.Data.WriteSaveData($"EventHistory_{Name}", eventHistory);
    }

    internal bool MatchLastDialogue(List<StardewValley.DialogueLine> dialogues)
    {
        // Find the last dialogues in the event history
        if (!eventHistory.Any())
        {
            return false;
        }
        var tail = eventHistory.Last().Item2;
        if (tail is DialogueHistory)
        {
            if (((DialogueHistory)tail).Dialogues.Select(x => x.Text).SequenceEqual(dialogues.Select(x => x.Text)))
            {
                return true;
            }
        }
        // Check if the last dialogues match the given dialogues
        return false;
    }

    internal void AddEventDialogue(List<StardewValley.DialogueLine> filteredDialogues, IEnumerable<NPC> actors, string festivalName, int year, StardewValley.Season season, int dayOfMonth, int timeOfDay)
    {
        var time = new StardewTime(year,season, dayOfMonth, timeOfDay);
        var newHistory = new DialogueEventHistory(actors,filteredDialogues,festivalName);
        AddHistory(newHistory,time);
        foreach(var listener in actors)
        {
            var listenerObject = DialogueBuilder.Instance.GetCharacter(listener);
            var thirdPartyHistory = new ThirdPartyHistory(this, filteredDialogues, festivalName);
            listenerObject.AddHistory(thirdPartyHistory, time);
        }
    }

    internal void AddOverheardDialogue(NPC speaker, List<StardewValley.DialogueLine> filteredDialogues, int year, StardewValley.Season season, int dayOfMonth, int timeOfDay)
    {
        var time = new StardewTime(year,season, dayOfMonth, timeOfDay);
        var newHistory = new DialogueEventOverheard(speaker.Name,filteredDialogues);
        AddHistory(newHistory,time);
    }

    internal void AddConversation(string[] chatHistory, int year, StardewValley.Season season, int dayOfMonth, int timeOfDay)
    {
        var time = new StardewTime(year,season, dayOfMonth, timeOfDay);
        var newHistory = new ConversationHistory(chatHistory);
        AddHistory(newHistory,time);
    }

    public string Name { get; }
    public string DialogueFilePath { get; }
    public string BioFilePath { get; }
    public DialogueFile? DialogueData { get; private set; }
    public ConcurrentBag<Tuple<DialogueContext,DialogueValue>> CreatedDialogue { get; private set; } = new ();
    internal BioData Bio
    {
        get
        {
            if (_bioData == null)
            {
                LoadBio();
            }
            return _bioData;
        }
    }

    public NPC StardewNpc { get; internal set; }
}
