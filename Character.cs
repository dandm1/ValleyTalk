using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LlamaDialogue;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.GameData.Characters;
using Polly;
using Polly.Retry;
using System.Threading.Tasks;

namespace StardewDialogue;

public class Character
{
    private BioData _bioData;

    public string WorkPath { get; init; } = "working";
    public bool Marriage { get; set; } =true;
    public bool Gift { get; set; } = true;
    public bool AllUnmarried { get; set; } = false;
    public bool ExistingUnmarried { get; set; } = true;

    private static readonly Dictionary<string,TimeSpan> filterTimes = new() { { "House", TimeSpan.Zero }, { "Action", TimeSpan.Zero }, { "Received Gift", TimeSpan.Zero }, { "Given Gift", TimeSpan.Zero }, { "Editorial", TimeSpan.Zero }, { "Gender", TimeSpan.Zero }, { "Question", TimeSpan.Zero } };
    private readonly List<Tuple<StardewTime,StardewValley.DialogueLine>> eventHistory = new();
    internal IEnumerable<Tuple<StardewTime,StardewValley.DialogueLine>> EventHistory => eventHistory;

    public Character(string name, NPC stardewNpc, string bioFilePath)
    {
        Name = name;
        BioFilePath = bioFilePath;
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
            DialogueData.Add("Base",context, value);
        }
    }

    private void LoadBio()
    {
        BioData bioData = null;
        if (File.Exists(BioFilePath))
        {
        // Process the JSON data
            bioData = ModEntry.SHelper.Data.ReadJsonFile<BioData>(BioFilePath);
        }

        _bioData = bioData ?? new BioData();
    }

    private void LoadEventHistory()
    {
        var eventKey = $"EventHistory_{Name}";
        var history = ModEntry.SHelper.Data.ReadSaveData<List<Tuple<StardewTime, StardewValley.DialogueLine>>>(eventKey);
        if (history != null)
        {
            eventHistory.AddRange(history);
        }
    }
    /*internal void CreateDialogue()
    {
        List<string> giftIds = Gift ? new[] {GiftTastes.Love, GiftTastes.Like, GiftTastes.Neutral_Personal, GiftTastes.Dislike_Personal, GiftTastes.Hate_Personal}.SelectMany(x => x).ToList() : new List<string>();
        List<DialogueContext> contexts = DialogueContext.CreateContexts
        (
            Name, 
            Bio, 
            Marriage,
            AllUnmarried,
            ExistingUnmarried,
            giftIds
        );

        if (ExistingUnmarried)
        {
        var missingContextsAll = DialogueData.AllEntries.Where(x => !contexts.Any(y => y.Value == x.Key.Value)).ToList();
        var missingContexts = missingContextsAll.Where(x => x.Key.RandomAct == null && x.Key.SpouseAct == null && x.Key.DayOfSeason == null && x.Key.Day != null).Select(x => x.Key).ToList();
        if (missingContexts.Any())
        {
            foreach(var context in missingContexts)
            {
                context.TargetSamples = 5;
            }
            //Log.Warning($"Added {missingContexts.Count} missing contexts for {Name}");
            contexts.AddRange(missingContexts);
        }
        }
        // Check WorkPath exists
        if (!Directory.Exists(WorkPath))
        {
            Directory.CreateDirectory(WorkPath);
        }
        var basicDialogue = new Dictionary<DialogueContext, string[]>();
        var options = new ParallelOptions { MaxDegreeOfParallelism = 1 };
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new DialogueContextJsonConverter(),
                new DialogueValueJsonConverter(),
                new RandomisedDialogueJsonConverter(),
                new IDialogueValueJsonConverter()
            }
        };
        int i =0;
        int n = contexts.Count();
        Parallel.ForEach(contexts, options, context =>
        {
            i++;
            var iLocal = i;
            if (File.Exists($"{WorkPath}/Processed_{Name}{(context.Married ? "_M" : "")}{(context.Birthday ? "_B" : "")}_{context.Value}.json"))
            {
                //Log.Information($"({iLocal}/{n}) Processed file exists for {Name} with context {context.Value}{(context.Married ? " (married)":"")}");
                return;
            }
            // Check if intermediate file exists
            if (File.Exists($"{WorkPath}/Basic_{Name}{(context.Married ? "_M" : "")}{(context.Birthday ? "_B" : "")}_{context.Value}.json"))
            {
                //Log.Information($"({iLocal}/{n}) Basic file exists for {Name} with context {context.Value}{(context.Married ? " (married)":"")}");
                var jsonIn = File.ReadAllText($"{WorkPath}/Basic_{Name}{(context.Married ? "_M" : "")}{(context.Birthday ? "_B" : "")}_{context.Value}.json");
                var jsonObj = JsonSerializer.Deserialize<string[]>(jsonIn, jsonOptions);
                if (jsonObj != null)
                {
                    basicDialogue.Add(context, jsonObj);
                    return;
                }
            }
            //Log.Information($"({iLocal}/{n}) Creating basic dialogue for {Name} with context {context.Value}{(context.Married ? " (married)":"")}{(context.Birthday ? " (birthday)":"")}");
            basicDialogue.Add(context, CreateBasicDialogue(context));
            foreach (var dialogue in basicDialogue[context])
            {
                //Log.Verbose(dialogue);
            }
            // Save / update the intermediate file.

            var json = JsonSerializer.Serialize(basicDialogue.Where(x => x.Key == context).SelectMany(x => x.Value), jsonOptions);
            File.WriteAllText($"{WorkPath}/Basic_{Name}{(context.Married ? "_M" : "")}{(context.Birthday ? "_B" : "")}_{context.Value}.json", json);
        });
        i=0;
        var lastTime = DateTime.Now;
        Parallel.ForEach(contexts, options, context =>
        {
            i++;
            var iLocal = i;
            List<Tuple<DialogueContext, string>> resultPairs = new();
            if (File.Exists($"{WorkPath}/Processed_{Name}{(context.Married ? "_M" : "")}{(context.Birthday ? "_B" : "")}_{context.Value}.json"))
            {
                var jsonIn = File.ReadAllText($"{WorkPath}/Processed_{Name}{(context.Married ? "_M" : "")}{(context.Birthday ? "_B" : "")}_{context.Value}.json");
                var jsonObj = JsonSerializer.Deserialize<Tuple<DialogueContext, string>[]>(jsonIn, jsonOptions);
                if (jsonObj != null)
                {
                    resultPairs = new List<Tuple<DialogueContext, string>>(jsonObj);
                    if (context.Value.StartsWith("Rainy_"))
                    {
                        var extraValues = new List<Tuple<DialogueContext, string>>();
                        foreach(var x in resultPairs.Where(x => x.Item1.ChatID == "Rainy"))
                        {
                            extraValues.Add(new Tuple<DialogueContext, string>(new DialogueContext(context), x.Item2));
                        }
                        resultPairs.AddRange(extraValues);
                        resultPairs = resultPairs.Where(x => x.Item1.ChatID != "Rainy").ToList();
                    }
                    // Update the married and birthday status unless the ChatID starts with a GUID (has dashes at postions 9, 14 & 19)
                    foreach(var x in resultPairs.Where(x => (x?.Item1?.ChatID?.Length ?? 1) < 20 || x.Item1.ChatID[8] != '-' || x.Item1.ChatID[13] != '-' || x.Item1.ChatID[18] != '-'))
                    {
                        x.Item1.Married = context.Married;
                        x.Item1.Birthday = context.Birthday;
                    }   
                }
            }
            if (!resultPairs.Any() && basicDialogue.ContainsKey(context))
            {
                //Log.Information($"({iLocal}/{n}) Processing dialogue lines for {Name} with context {context.Value}{(context.Married ? " (married)":"")}");

                resultPairs = ApplyAiFilters(basicDialogue[context], context);
            }

            // Save intermediate file
            var json = JsonSerializer.Serialize(resultPairs, jsonOptions);
            File.WriteAllText($"{WorkPath}/Processed_{Name}{(context.Married ? "_M" : "")}{(context.Birthday ? "_B" : "")}_{context.Value}.json", json);
            foreach (var x in resultPairs)
            {
                CreatedDialogue.Add(new Tuple<DialogueContext, DialogueValue>(x.Item1, new DialogueValue(x.Item2)));
                //Log.Debug($"{x.Item1.Value} - {x.Item2}");
            }

            // If more than 30 minutes since last time, log the filter times
            if (DateTime.Now - lastTime > TimeSpan.FromMinutes(30))
            {
                var timeStats = new StringBuilder();
                foreach (var filterTime in filterTimes)
                {
                    timeStats.Append($"{filterTime.Key} : {filterTime.Value.TotalMinutes}   ");
                }
                //Log.Information(timeStats.ToString());
                //Log.Information(Llm.Instance.TokenStats);
                lastTime = DateTime.Now;
            }
        });
    }*/


    public string[] CreateBasicDialogue(DialogueContext context )
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
        pipeline.Execute(() =>
        {
            retryCount++;
            var resultString = Llm.Instance.RunInference(prompts.System, prompts.GameConstantContext, prompts.NpcConstantContext, $"{prompts.Context}{prompts.Command}{prompts.Instructions}", prompts.ResponseStart);
            // Apply relaxed validation if this is the second retry
            
            results = ProcessLines(resultString, retryCount > 2).ToArray();
            if (results.Length == 0)
            {
                //Force a retry in the pipeline
                throw new Exception("No valid lines returned from AI");
            }
            if (ModEntry.Config.Debug)
            {
                // Open 'generation.log' and append values to it
                using (var log = new StreamWriter($"Generation.log", true))
                {
                    log.WriteLine($"Context:");
                    log.WriteLine($"Name: {Name}");
                    log.WriteLine($"Marriage: {context.Married}");
                    log.WriteLine($"Birthday: {context.Birthday}");
                    log.WriteLine($"Location: {context.Location}");
                    log.WriteLine($"Weather: {string.Concat(context.Weather)}");
                    log.WriteLine($"Time of Day: {context.TimeOfDay}");
                    log.WriteLine($"Day of Season: {context.DayOfSeason}");
                    log.WriteLine($"Gift: {context.Accept}");
                    log.WriteLine($"Spouse Action: {context.SpouseAct}");
                    log.WriteLine($"Random Action: {context.RandomAct}");
                    log.WriteLine($"Prompts: {JsonSerializer.Serialize(prompts)}");
                    log.WriteLine($"Results: {results[0]}");
                    if (results.Length > 1)
                    {
                        foreach (var result in results.Skip(1))
                        {
                            log.WriteLine($"Response: {result}");
                        }
                    }
                    log.WriteLine("--------------------------------------------------");
                }
            }
        });
        return results;
    }

    /*private List<Tuple<DialogueContext, string>> ApplyAiFilters(IEnumerable<string> resultLines, DialogueContext context)
    {
        var filters = new List<IDialogueFilter>();
        var basePrompts = CreatePrompts(context);
        var questionPrompts = CreatePrompts(context, genderNeutral: true);
        basePrompts.System = FilterBase.SystemPrompt;
        
        filters.Add(new TrueFalseFilter("House", context,basePrompts,FilterPrompts.House));
        if (context.SpouseAct != SpouseAction.funLeave && context.SpouseAct != SpouseAction.jobLeave)
        {
            filters.Add(new TrueFalseFilter("Action", context,basePrompts,string.Format(FilterPrompts.ActionTemplate,basePrompts.Name)));
        }
        if (string.IsNullOrWhiteSpace(context.Accept))
        {
            filters.Add(new TrueFalseFilter("Received Gift", context,basePrompts,string.Format(FilterPrompts.ReceivedGiftTemplate,basePrompts.Name)));
        }
        filters.Add(new TrueFalseFilter("Given Gift", context,basePrompts,string.Format(FilterPrompts.GivenGiftTemplate,basePrompts.Name)));
        //filters.Add(new TrueFalseFilter("Editorial", context,basePrompts,editorial));

        filters.Add(new GenderFilter(context,basePrompts));
        filters.Add(new QuestionFilter(context,questionPrompts) { Probability = 0.25 });
        List<Tuple<DialogueContext, string>> filteredLines = new();
        foreach (var line in resultLines)
        {
            var outLines = ApplyFilters(line, filters, context);
            filteredLines.AddRange(outLines);
            //Log.Verbose($"{outLines.Count()} lines produced processing line {line}.");
        }
        return filteredLines;
    }*/

/*    private List<Tuple<DialogueContext, string>> ApplyFilters(string inLine, List<IDialogueFilter> filters, DialogueContext context)
    {
        var lines = new List<Tuple<DialogueContext, string>>() { new Tuple<DialogueContext, string>(context, inLine) };
        using(var filterLog = new StreamWriter($"{WorkPath}/Filter//Log.txt", true))
        {
        foreach (var filter in filters)
        {
            var startTime = DateTime.Now;
            var newLines = new List<Tuple<DialogueContext, string>>();
            foreach( var line in lines)
            {
                if (filter.Applies(line.Item2))
                {
                    // Append the details to the filter log file
                    if (filter.Omit)
                    {
                        //Log.Verbose($"Filter {filter.Name} applied.  Discarding {line.Item2}");
                    }
                    else
                    {
                        var outLines = filter.Process(line);
                        if ( outLines.Count() > 1 && filter.RecursiveStart != null)
                        {
                            newLines.AddRange(outLines.Take(filter.RecursiveStart.Value));
                            foreach(var newLine in outLines.Skip(filter.RecursiveStart.Value))
                            {
                                var recursiveLines = ApplyFilters(newLine.Item2, filters, newLine.Item1);
                                newLines.AddRange(recursiveLines);
                            }
                        }
                        else
                        {
                            newLines.AddRange(outLines);
                        }
                    }
                }
                else
                {
                    newLines.Add(line);
                }
            }
            lines = newLines;
            if (!filterTimes.ContainsKey(filter.Name))
            {
                filterTimes.Add(filter.Name, TimeSpan.Zero);
            }
            filterTimes[filter.Name] += DateTime.Now - startTime;
        }
        } 
        return lines;
    }*/

    public static IEnumerable<string> ProcessLines(string resultString,bool relaxedValidation = false)
    {
        var resultLines = resultString.Split('\n').AsEnumerable();
        // Remove any line breaks
        resultLines = resultLines.Select(x => x.Replace("\n", "").Replace("\r", ""));
        resultLines = resultLines.Where(x => !string.IsNullOrWhiteSpace(x));
        // Check that the first line starts with '- '.  Omit any subsequent lines that don't start with '% '
        var validLayout = (resultLines.FirstOrDefault()?? string.Empty ).StartsWith("- ");
        if (!validLayout)
        {
            //log.WriteLine("Invalid layout detected in AI response.  Returning the full response.");
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
            //log.WriteLine("Long line detected in AI response.  Returning nothing.");
            return Array.Empty<string>();
        }
        return resultLines;
    }


    internal void AddDialogue(IEnumerable<StardewValley.DialogueLine> dialogues, int year, StardewValley.Season season, int dayOfMonth, int timeOfDay)
    {
        var time = new StardewTime(year,season, dayOfMonth, timeOfDay);
        foreach(var dialogue in dialogues)
        {
            eventHistory.Add(new Tuple<StardewTime, StardewValley.DialogueLine>(time, dialogue));
        }
        ModEntry.SHelper.Data.WriteSaveData($"EventHistory_{Name}", eventHistory);
    }

    internal bool MatchLastDialogue(List<StardewValley.DialogueLine> dialogues)
    {
        // Find the last dialogues in the event history
        var tail = eventHistory.TakeLast(dialogues.Count);
        // Check if the last dialogues match the given dialogues
        return tail.Select(x => x.Item2.Text).SequenceEqual(dialogues.Select(x => x.Text));
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
