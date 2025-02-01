using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ValleyTalk;
using StardewValley;
using Polly;
using Polly.Retry;
using System.Threading.Tasks;
using Serilog;
using Microsoft.Xna.Framework.Content;
using StardewModdingAPI.Events;
using System.Text;
using Microsoft.VisualBasic;

namespace StardewDialogue;

public class Character
{
    private BioData _bioData;

    private static readonly Dictionary<string,TimeSpan> filterTimes = new() { { "House", TimeSpan.Zero }, { "Action", TimeSpan.Zero }, { "Received Gift", TimeSpan.Zero }, { "Given Gift", TimeSpan.Zero }, { "Editorial", TimeSpan.Zero }, { "Gender", TimeSpan.Zero }, { "Question", TimeSpan.Zero } };
    private StardewEventHistory eventHistory = new();
    private DialogueFile dialogueData;

    internal IEnumerable<Tuple<StardewTime,IHistory>> EventHistory => eventHistory.AllTypes;

    public NPC StardewNpc { get; internal set; }
    public List<string> ValidPortraits { get; }

    public Character(string name, NPC stardewNpc)
    {
        Name = name;
        BioFilePath = $"assets/bio/{Name}";
        StardewNpc = stardewNpc;

        // Load and process the dialogue file
        LoadBio();
        //LoadDialogue();
        LoadEventHistory();
        ValidPortraits = new List<string>() { "h", "s", "l", "a" };
        ValidPortraits.AddRange(_bioData.ExtraPortraits.Keys);
        PossiblePreoccupations = new List<string>(_bioData.Preoccupations);
        PossiblePreoccupations.AddRange(GetLovedAndHatedGiftNames());
    }

    private IEnumerable<string> GetLovedAndHatedGiftNames()
    {
        if (!Game1.NPCGiftTastes.TryGetValue(Name, out var npcGiftTastes))
        {
            return Array.Empty<string>();
        }

        string[] tasteLevels = npcGiftTastes.Split('/');
        var lovedGifts = ArgUtility.SplitBySpace(tasteLevels[1]);
        var hatedGifts = ArgUtility.SplitBySpace(tasteLevels[7]);

        List<string> returnList = new();
        foreach (var gift in lovedGifts)
        {
            Game1.objectData.TryGetValue(gift, out var data);
            if (data != null)
            {
                returnList.Add(data.DisplayName);
            }
            
        }
        foreach (var gift in hatedGifts)
        {
            Game1.objectData.TryGetValue(gift, out var data);
            if (data != null)
            {
                returnList.Add(data.DisplayName);
            }
        }
        return returnList;
    }

    private void LoadDialogue()
    {
        Dictionary<string, string> canonDialogue = new();
        if (ModEntry.BlockModdedContent)
        {
            var manager = new ContentManager(Game1.content.ServiceProvider, Game1.content.RootDirectory);
            try
            {
                string assetName = $"Characters\\Dialogue\\{Name}";
                foreach(var langSuffix in ModEntry.LanguageFileSuffixes)
                {
                    var path = $"{assetName}{langSuffix}";
                    var unmarriedDialogue = manager.Load<Dictionary<string, string>>(path);
                    if (unmarriedDialogue != null)
                    {
                        canonDialogue = unmarriedDialogue;
                        break;
                    }
                }
            }
            catch (Exception _)
            {
                // If it fails, just continue
            }
            try
            {
                string assetName = $"Characters\\Dialogue\\MarriageDialogue{Name}";
                foreach(var langSuffix in ModEntry.LanguageFileSuffixes)
                {
                    var path = $"{assetName}{langSuffix}";
                    var marriedDialogue = manager.Load<Dictionary<string, string>>(path);
                    if (marriedDialogue != null)
                    {
                        canonDialogue = canonDialogue.Concat(marriedDialogue).ToDictionary(x => x.Key, x => x.Value);
                        break;
                    }
                }
            }
            catch (Exception _)
            {
                // If it fails, just continue
            }
        }
        else
        {
            canonDialogue = StardewNpc.Dialogue;
        }
        if (Bio.Dialogue != null)
        {
            foreach (var dialogue in Bio.Dialogue)
            {
                canonDialogue[dialogue.Key] = dialogue.Value;
            }
            
        }
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
        
        bioData = Util.ReadLocalisedJson<BioData>(BioFilePath,"txt");
        if (bioData == null)
        {
            bioData = new BioData();
            ModEntry.SMonitor.Log($"No bio file found for {Name}.", StardewModdingAPI.LogLevel.Warn);
        }
        bioData.Name = Name;
        _bioData = bioData;
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

    public IEnumerable<string> ProcessLines(string resultString,bool relaxedValidation = false)
    {
        var resultLines = resultString.Split('\n').AsEnumerable();
        // Remove any line breaks
        resultLines = resultLines.Select(x => x.Replace("\n", "").Replace("\r", ""));
        resultLines = resultLines.Where(x => !string.IsNullOrWhiteSpace(x));
        // Find the first line that starts with '- ' and remove any lines before it
        resultLines = resultLines.SkipWhile(x => !x.StartsWith("- "));
        var dialogueLine = resultLines.FirstOrDefault();
        if (dialogueLine == null || !dialogueLine.StartsWith("- "))
        {
            //Log.Debug("Invalid layout detected in AI response.  Returning the full response.");
            return Array.Empty<string>();
        }
        dialogueLine = CommonCleanup(dialogueLine);
        dialogueLine = DialogueLineCleanup(dialogueLine, relaxedValidation);
        if (string.IsNullOrWhiteSpace(dialogueLine))
        {
            //Log.Debug("Empty dialogue line detected in AI response.  Returning nothing.");
            return Array.Empty<string>();
        }
        var responseLines = resultLines.Skip(1).Where(x => x.StartsWith("% "));
        if (responseLines.Any())
        {
            responseLines = responseLines.Select(x => CommonCleanup(x));
            responseLines = responseLines.Select(x => ResponseLineCleanup(x));
            responseLines = responseLines.Where(x => !string.IsNullOrWhiteSpace(x));
            if (responseLines.Count() < 2)
            {
                responseLines = Array.Empty<string>();
            }
        }
        resultLines = new List<string>(){dialogueLine}.Concat(responseLines);
        return resultLines;
    }

    private string CommonCleanup(string line)
    {
        // Remove any leading punctuation and trailing quotation marks
        line = line.Trim().TrimStart('-', ' ', '"', '%');
        line = line.TrimEnd('"');
        // If the string starts or ends with #$b# ot #$e# remove it.
        line = line.StartsWith("#$b#") ? line[4..] : line;
        line = line.EndsWith("#$b#") ? line[..^4] : line;
        line = line.StartsWith("#$e#") ? line[4..] : line;
        line = line.EndsWith("#$e#") ? line[..^4] : line;
        // Remove any quotation marks
        line = line.Replace("\"", "");
        // If the line doesn't end with a sentence end punctuation, add a period
        if (!line.EndsWith(".") && !line.EndsWith("!") && !line.EndsWith("?"))
        {
            line += ".";
        }
        return line;
    }

    private string DialogueLineCleanup(string line,bool relaxedValidation = false)
    {
        // If the string contains $e or $b without a # before them, add a #
        line = line.Replace("$e", "#$e").Replace("$b", "#$b");
        line = line.Replace("##$e", "#$e").Replace("##$b", "#$b");
        line = line.Replace("#$c .5#","");
        line = line.Replace("@@","@");
        // If the string contains any emotion indicators ($0, $s, $l, $a or $h) with a # before them, remove the #
        foreach (var indicator in ValidPortraits)
        {
            line = line.Replace($"#${indicator}", $"${indicator}");
        }

        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '$')
            {
                if (i + 1 < line.Length)
                {
                    var nextChar = line[i + 1];
                    if (nextChar == 'e' || nextChar == 'c' || nextChar == 'b')
                    {
                        i++; // Skip the next character
                    }
                    else
                    {
                        // Collect the string up to the next # or the end of the line
                        var end = line.IndexOf('#', i);
                        if (end == -1)
                        {
                            end = line.Length;
                        }
                        var remainder = line.Substring(i+1, end - i - 1);
                        if (!ValidPortraits.Contains(remainder))
                        {
                            line = line.Remove(i, 1 + remainder.Length);
                            i--; // Adjust index after removal
                        }
                    }
                }
                else
                {
                    line = line.Remove(i, 1);
                    i--; // Adjust index after removal
                }
            }
        }
        
        line = line.Trim();
        var elements = line.Split('#');
        if (elements.Any(x => x.Length > 200 && !relaxedValidation))
        {
            //Log.Debug("Long line detected in AI response.  Returning nothing.");
            return string.Empty;
        }
        return line;
    }

    private string ResponseLineCleanup(string line)
    {
        // Remove any hashes
        line = line.Replace("#", "");
        // If the string contains any commands preceded by a $, remove them
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '$')
            {
                if (i + 1 < line.Length)
                {
                    line = line.Remove(i, 2);
                }
                else
                {
                    line = line.Remove(i, 1);
                }
            }
        }
        if (line.Contains('@'))
        {
            var farmerName = Game1.player.Name;
            line = line.Replace("@", farmerName);
        }
        line = line.Trim();
        if (line.Length > 90)
        {
            //Log.Debug("Long line detected in AI response.  Returning nothing.");
            return string.Empty;
        }
        return line;
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
    public DialogueFile? DialogueData 
    { 
        get 
        {
            if (dialogueData == null)
            {
                LoadDialogue();
            }
            return dialogueData;  
        }
        private set => dialogueData = value; 
    }
    public ConcurrentBag<Tuple<DialogueContext,DialogueValue>> CreatedDialogue { get; private set; } = new ();
    internal BioData Bio
    {
        get => _bioData;
    }
    public List<string> PossiblePreoccupations { get; }
    public string Preoccupation { get; internal set; }
    public WorldDate PreoccupationDate { get; internal set; }
}
