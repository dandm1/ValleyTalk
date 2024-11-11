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
    private readonly Dictionary<string,string> HistoryEvents = new()
    {
        { "cc_Bus", "The bus to Calico Desert was repaired" },
        { "cc_Boulder", "The glittering boulder was removed from the mountain lake" },
        { "cc_Bridge", "The bridge to the quarry was repaired" },
        { "cc_Complete", "The community center was restored" },
        { "cc_Greenhouse", "The farmer's greenhouse was repaired" },
        { "cc_Minecart", "The minecart system was repaired" },
        { "wonIceFishing", "The farmer won the ice fishing competition" },
        { "wonGrange", "The farmer won the Stardew Valley Fair Grange Display" },
        { "wonEggHunt", "The farmer won the egg hunt" }
    };

    public Character(string name, NPC stardewNpc, string bioFilePath)
    {
        Name = name;
        BioFilePath = bioFilePath;
        StardewNpc = stardewNpc;

        // Load and process the dialogue file
        LoadDialogue();
        LoadStardewSummary();
        LoadEventHistory();
        ////Log.Information($"Loaded dialogue for {Name}");
    }


    private void LoadStardewSummary()
    {
        var gameSummaryDict = ModEntry.SHelper.Data.ReadJsonFile<Dictionary<string,string>>("bio/Stardew.txt");
        StardewSummary = gameSummaryDict["Text"];
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
        var prompts = CreatePrompts(context);

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

    private Prompts CreatePrompts(DialogueContext context)
    {
        var npcData = StardewNpc.GetData();
        var result = new Prompts();
        var npcIsMale = npcData.Gender == Gender.Male;
        var dialogueSample = SelectDialogueSample(context);
        var exactLine = SelectExactDialogue(context);
        var allPreviousActivities = Game1.getPlayerOrEventFarmer().previousActiveDialogueEvents.First();
        var previousActivites = allPreviousActivities.Where(x => HistoryEvents.ContainsKey(x.Key) && x.Value < 112).ToList();

        var systemPrompt = new StringBuilder();
        systemPrompt.AppendLine("You are an expert computer game writer that takes great pride in being able to create dialogue for any character in any game that exactly matches that character's situation and personality.");
        result.System = systemPrompt.ToString();
        var gameConstantPrompt = new StringBuilder();
        gameConstantPrompt.AppendLine($"You are creating dialogue to enhance the experience of players in the game Stardew Valley.");
        gameConstantPrompt.AppendLine("While staying true to the characters you are writing for a mature audience looking for more adult and sexual content when appropriate.");
        gameConstantPrompt.AppendLine("##Game Summary");
        gameConstantPrompt.AppendLine(StardewSummary);
        result.GameConstantContext = gameConstantPrompt.ToString();
        var npcConstantPrompt = new StringBuilder();
        npcConstantPrompt.AppendLine($"You are working on dialogue for {Name}, who is talking to the player (referred to as 'the farmer')");
        if ((Bio?.Biography ?? string.Empty).Length > 100)
        {
            npcConstantPrompt.AppendLine($"##{Name} Biography:");
            var bio = Bio.Biography;
            while (bio.Contains("\n\n"))
            {
                bio = bio.Replace("\n\n", "\n");
            }
            npcConstantPrompt.AppendLine(bio);
        }
        result.NpcConstantContext = npcConstantPrompt.ToString();
        var prompt = new StringBuilder();
        prompt.AppendLine("## Game State:");
        if (allPreviousActivities.ContainsKey("cc_Complete"))
        {
            prompt.AppendLine("The community center has been restored.");
        }
        else
        {
            prompt.AppendLine("The community center remains rundown and inaccessible.");
        }
        if (allPreviousActivities.ContainsKey("cc_Bus"))
        {
            prompt.AppendLine("The bus to Calico Desert has been repaired and is driven by Pam.");
        }
        else
        {
            prompt.AppendLine("The bus to Calico Desert remains broken and Pam remains unemployed.");
        }
        if (allPreviousActivities.ContainsKey("cc_Bridge"))
        {
            prompt.AppendLine("The bridge to the quarry has been repaired.");
        }
        else
        {
            prompt.AppendLine("The bridge to the quarry has collapsed.");
        }
        if (allPreviousActivities.ContainsKey("cc_Minecart"))
        {
            prompt.AppendLine("The minecart system has been repaired and is operating across Stardew Valley.");
        }
        else
        {
            prompt.AppendLine("The minecart system remains broken.");
        }
        if (allPreviousActivities.ContainsKey("cc_Boulder"))
        {
            prompt.AppendLine("The glittering boulder has been removed from the mountain lake and ore released into the rivers.");
        }
        else
        {
            prompt.AppendLine("The glittering boulder remains in the mountain lake.");
        }
        if (Game1.year == 1)
        {
            prompt.AppendLine("Kent is away serving in the army, leaving Jodi, Vincent and Sam.  The farmer has not yet met Kent.");
        }
        else
        {
            prompt.AppendLine("Kent has returned from the army and is living with Jodi, Vincent and Sam.  He is adjusting to life back in Pelican Town.");
        }

        if (dialogueSample.Any())
        {
            prompt.AppendLine($"##{Name} Sample Dialogue:");
            prompt.AppendLine($"You have a sample of {Name}'s dialogue as follows:");
            foreach (var dialogue in dialogueSample)
            {
                prompt.AppendLine($"- {dialogue.Value}");
            }
        }
        var timeNow = new StardewTime(Game1.year, Game1.season, Game1.dayOfMonth, Game1.timeOfDay);
        List<Tuple<double,string>> fullHistory = eventHistory.Select(x => new Tuple<double,string>(x.Item1.DaysSince(timeNow), $"- {x.Item1.SinceDescription(timeNow)}: {Name} speaking to farmer : {x.Item2.Text}")).ToList();
        fullHistory.AddRange(previousActivites.Select(x => new Tuple<double,string>(x.Value, $"- {x.Key}: Event occurred : {HistoryEvents[x.Key]}")));

        if (fullHistory.Any())
        {
            prompt.AppendLine($"##Previous interactions with {Name}:");
            prompt.AppendLine($"The farmer and {Name} have spoken previously, and the most recent 20 lines are shown below.");
            prompt.AppendLine("These represent conversations at a different time and in contexts different to the current conversation.");
            prompt.AppendLine("The new line is likely to reference previous conversations or events, as well as the current context.  It may also reference patterns in the previous conversation such as similar gifts or long gaps in the conversation.");
            prompt.AppendLine($"You should avoid {Name} repeating lines or concepts close together, and call out repetition from the farmer. The more recent a previous event or interaction the more likely it will be referenced and the less likely it will be repeated.");
            prompt.AppendLine("History:");
            foreach (var eventHistory in fullHistory.OrderBy(x => x.Item1).Take(20))
            {
                prompt.AppendLine(eventHistory.Item2);
            }
        }

        prompt.AppendLine("## Instructions:");
        prompt.AppendLine("### Context:");
        if (context.MaleFarmer)
        {
            prompt.AppendLine("The farmer is male. As well as running the farm, he is an adventurer and also has interests and habits that are typically male.");
        }
        else
        {
            prompt.AppendLine("The farmer is female. As well as running the farm, the farmer is an adventurer and also has interests and habits that are typically female.");
        }
        if (context.DayOfSeason != null && context.Season != null)
        {
            prompt.AppendLine($"It is day {context.DayOfSeason} of {context.Season}.");
        }
        if (context.TimeOfDay != null)
        {
            prompt.AppendLine($"It is {context.TimeOfDay}.");
            if (context.TimeOfDay == "early morning")
            {
                prompt.AppendLine("This is a normal time for the farmer to be up and about.");
            }
        }
        if (context.Weather != null && context.Weather.Any())
        {
            if (context.Weather.Contains("lightning"))
            {
                prompt.AppendLine("There is a storm with rain and lightning.");
            }
            else if (context.Weather.Contains("green rain"))
            {
                prompt.AppendLine("There is a strange green rain causing the plants to grow wildly.");
            }
            else if (context.Weather.Contains("snow"))
            {
                prompt.AppendLine("It is snowing.");
            }
            else if (context.Weather.Contains("rain"))
            {
                prompt.AppendLine("It is raining heavily.");
            }

        }
        if (context.Year  == 1 )
        {
            prompt.AppendLine("The farmer is new to Pelican Town this year.");
        }
        Game1.getPlayerOrEventFarmer().friendshipData.TryGetValue(Name, out Friendship friendship);
        if (friendship.IsMarried() || friendship.IsRoommate())
        {
            if (friendship.IsRoommate())
            {
                prompt.AppendLine($"The farmer and {Name} are roommates and close, non-romantic friends. They live together at the farm inherited from the farmer's grandfather. {Name} lived in the sewers before meeting the farmer and moved from there to the farm when they became roommates.");
            }
            else
            {
                prompt.AppendLine($"The farmer is married to {Name}. They live together at the farm inherited from the farmer's grandfather.  {Name} lived in Stardew Valley before meeting the farmer and moved from {(npcIsMale? "his" : "her")} original house to the farm when they got married.");

                if (context.Children.Count == 0)
                {
                    prompt.AppendLine($"The farmer and {Name} have no children.");
                }
                else
                {
                    prompt.AppendLine($"The farmer and {Name} have {context.Children.Count()} child{(context.Children.Count > 1 ? "ren":"")}:");
                    foreach (var child in context.Children)
                    {
                        prompt.AppendLine($"- A {(child.IsMale ? "boy" : "girl")} named {child.Name} who is {child.Age} days old.");
                    }
                }
                if (friendship.DaysUntilBirthing > 0)
                {
                    prompt.AppendLine($"The farmer and {Name} are expecting a child in {friendship.DaysUntilBirthing} days.");
                }
            }

            var allBuildings = GetBuildings();
            var allAnimals = GetAnimals();
            var allCrops = GetCrops();
            
            if (allBuildings.Any())
            {
                prompt.AppendLine($"The farm has the following buildings:");
                var completedBuildings = allBuildings.Where(x => x.daysOfConstructionLeft.Value == 0 && x.buildingType.Value != "Greenhouse");
                foreach (var building in completedBuildings.GroupBy(x => x.buildingType))
                {
                    prompt.AppendLine($"- {building.Count()} {building.Key} building{(building.Count() > 1 ? "s" : "")}");
                }
                var greenhouse = allBuildings.FirstOrDefault(x => x.buildingType.Value == "Greenhouse");
                if (greenhouse != null)
                {
                    if (greenhouse.indoors.Value == null)
                    {
                        prompt.AppendLine("- A ruined greenhouse.");
                    }
                    else
                    {
                        prompt.AppendLine("- A repaired greenhouse.");
                    }
                }
                if (allBuildings.Any(x => x.daysOfConstructionLeft.Value > 0))
                {
                    var underConstruction = allBuildings.First(x => x.daysOfConstructionLeft.Value > 0);
                    prompt.AppendLine($"- A {underConstruction.buildingType.Value} which will complete construction in {underConstruction.daysOfConstructionLeft} days.");
                }
            }
            else
            {
                prompt.AppendLine("The farm has no buildings apart from the farmhouse and shipping bin.");
            }
            if (allAnimals.Any())
            {
                prompt.AppendLine($"The farm has the following animals:");
                foreach (var animal in allAnimals.GroupBy(x => x.type))
                {
                    prompt.AppendLine($"- {animal.Count()} {animal.Key} animal{(animal.Count() > 1 ? "s" : "")}");
                }
            }
            else
            {
                prompt.AppendLine("The farm has no animals.");
            }
            var cropData = Game1.objectData;
            if (allCrops.Any())
            {
                prompt.AppendLine($"The following crops are growing on the farm:");
                foreach (var crop in allCrops.GroupBy(x => x.indexOfHarvest.Value))
                {
                    var thisDetails = cropData[crop.Key];
                    prompt.Append($"- {crop.Count()} {thisDetails.Name}");
                    var ripe = crop.Count(x => x.fullyGrown.Value);
                    if (ripe > 0)
                    {
                        prompt.Append($" (of which {ripe} are ready for harvest)");
                    }
                    else
                    {
                        prompt.Append(" (not ready for harvest)");
                    }
                    prompt.AppendLine(".");
                }
            }
            else
            {
                prompt.AppendLine("The farm has no crops.");
            }
            var pet = Game1.getPlayerOrEventFarmer().getPet();
            if (pet != null)
            {
                prompt.AppendLine($"The farm has a pet {pet.petType.Value} named {pet.Name}.");
            }
            else
            {
                prompt.AppendLine("The farm has no pets.");
            }
            switch (context.Hearts)
            {
                case >12:
                    prompt.AppendLine($"{Name} is feeling very positive about {(Name == "Krobus" ? "being roommates" : "the marriage")}.");
                    break;
                case <10:
                    prompt.AppendLine($"{Name} is feeling very negative about {(Name == "Krobus" ? "being roommates" : "the marriage")}. While {Name} should will still talk about the context of the conversation, they will be more likely to be negative or critical.");
                    break;
                default:
                    prompt.AppendLine($"{Name} is generally content, but a little uncertain and conflicted about {(Name == "Krobus" ? "being roommates" : "the marriage")}.");
                    break;
            }
        }
        if (context.Location != null)
        {
            var bedTile = npcData.Home[0].Tile;
            if (context.Location == npcData.Home[0].Location && context.Inlaw != Name)
            {
                if (StardewNpc.TilePoint == bedTile && Bio.HomeLocationBed && !Llm.Instance.IsHighlySensoredModel)
                {
                    prompt.AppendLine($"{Name} is in bed. The farmer has climbed into {Name}'s bed, and is talking to {Bio.GenderPronoun} there.");
                }
                else
                {
                    var mayBeInShop = context.Location.Contains("Shop", StringComparison.OrdinalIgnoreCase)
                        || context.Location.Contains("Science", StringComparison.OrdinalIgnoreCase);
                    prompt.AppendLine($"The farmer and {Name} are talking in {Name}'s home{(mayBeInShop ? " or the shop" : "")}.");
                }
            }
            else
            {
            prompt.Append(context.Location switch
            {
                "Town" => $"The farmer and {Name} are talking outdoors in the center of Pelican Town.",
                "Beach" => $"The farmer and {Name} are on the beach.",
                "Desert" => $"The farmer and {Name} are away from Stardew Valley visiting the Calico desert.",
                "BusStop" => $"The farmer and {Name} are at the bus stop.",
                "Railroad" => $"The farmer and {Name} are at the railroad station, near the spa in the mountains.",
                "Saloon" => $"The farmer and {Name} are at the Stardrop Saloon, relaxing at the end of a busy day. {(StardewNpc.GetData().Age == NpcAge.Child ? "" : "They are a little drunk.")}",
                "SeedShop" => $"The farmer and {Name} are in Pierre's General Store.",
                "JojaMart" => $"The farmer and {Name} are shopping at the JojaMart.",
                "Resort_Chair" => $"The farmer and {Name} are at the Ginger Island tropical resort. {Name} is standing by a chair on the beach.",
                "Resort_Towel" or "Resort_Towel_2" or "Resort_Towel_3" => $"The farmer and {Name} are at the Ginger Island tropical resort. {Name} is relaxing on a beach towel on the beach.",
                "Resort_Umbrella" or "Resort_Umbrella_2" or "Resort_Umbrella_3" => $"The farmer and {Name} are at the Ginger Island tropical resort. {Name} is relaxing on a beach towel on the beach.",
                "Resort_Bar" => $"The farmer and {Name} are at the Ginger Island tropical resort. They are at the bar run by Gus and it is day time. They are focussed on the bar{(StardewNpc.GetData().Age == NpcAge.Child ? "" : " and what they have been drinking. They are a little drunk")}.",
                "Resort_Entering" => $"The farmer and {Name} are at the Ginger Island tropical resort. {Name} is just arriving at the resort from Pelican Town.",
                "Resort_Leaving" => $"The farmer and {Name} are at the Ginger Island tropical resort. {Name} is leaving the resort to return to Pelican Town.",
                "Resort_Shore" => $"The farmer and {Name} are at the Ginger Island tropical resort. They are talking at the shore of the beach, with their feet in the water looking out to sea.",
                "Resort_Shore_2" => $"The farmer and {Name} are at the Ginger Island tropical resort. They are talking at the shore of the beach, with their feet in the water contemplating the waves.",
                "Resort_Wander" => $"The farmer and {Name} are at the Ginger Island tropical resort. {Name} is walking around behind the beach huts, close to the jungle and considering exploring the island away from the resort.",
                "Resort" or "Resort_2" => $"The farmer and {Name} are at the Ginger Island tropical resort. The dialogue should be appropriate wherever in the resort they are.",
                _ => $"The farmer and {Name} are at {context.Location}."
            });
            }
            prompt.AppendLine("The location where the conversation is taking place is significant context for the lines.");
        }
        var eventSection = new StringBuilder();
        foreach(var activity in allPreviousActivities.Where(x => x.Value < 7))
        {
            var theLine = activity.Key switch
            {
                "cc_Boulder" => $"The glittering bounder has recently been removed from the mountain lake, filling the river with precious metal ores.",
                "cc_Bridge" => $"The bridge to the quarry has recently been repaired, allowing the townsfolk to cross the river.",
                "cc_Bus" => $"The bus to Calico Desert has recently been repaired, allowing the townsfolk to visit the desert. This also means that Pam can return to her job as a bus driver, which is a big change in circumstances for both her and Penny.",
                "cc_Greenhouse" => $"The farmer's greenhouse has recently been repaired, allowing the farmer to grow crops all year round.",
                "cc_Minecart" => $"The old minecart system has recently been repaired, allowing the townsfolk to travel between the mines, the town and the bus stop instantly.",
                "cc_Complete" => $"The community center has recently been restored, bringing the town together and revitalising the valley.",
                "movieTheater" => $"The movie theater has recently opened in the former JojaMart building, showing classic films and new releases.",
                "pamHouseUpgrade" => $"Pam's house has recently been upgraded by the Farmer, allowing her and Penny to live in a more comfortable environment. The people of Pelican town including Pam and Penny are aware that the Farmer paid for the upgrade.",
                "pamHouseUpgradeAnonymous" => $"Pam's house has recently been upgraded by the Farmer. The people of Pelican Town are not aware who paid for it and it is a great mystery, particularly for Pam and also for Penny whether or not she is married to the Farmer.",
                "jojaMartStruckByLightning" => $"JojaMart has recently been struck by lightning, causing a fire that destroyed the building.",
                "babyBoy" => $"The farmer and {context.Inlaw} have recently had a baby boy.",
                "babyGirl" => $"The farmer and {context.Inlaw} have recently had a baby girl.",
                "wedding" => $"The farmer has recently gotten married to {context.Inlaw}.",
                "luauBest" => $"The pot luck soup at the Luau was recently declared the best ever.",
                "luauShorts" => $"Lewis's shorts were recently found in the pot luck soup at the Luau, causing a scandle.",
                "luauPoisoned" => $"The pot luck soup at the Luau was recently poisoned, causing a mass illness.",
                "Characters_MovieInvite_Invited" => $"The farmer has recently been invited to the movies by {Name}.",
                "DumpsterDiveComment" => $"The farmer has recently been caught by {Name} going through someone else's trash can.",
                "GreenRainFinished" => $"The green rain has stopped and the townsfolk are returning to their normal routines.",
                _ => $""
            };
            if (!string.IsNullOrWhiteSpace(theLine))
            {
                eventSection.AppendLine(theLine);
            }
        }
        if (eventSection.Length > 0)
        {
            prompt.AppendLine("## Recent Events:");
            prompt.AppendLine("The following events have recently occured in town, which are important in the dialogue lines.");
            prompt.AppendLine(eventSection.ToString());
        }
            
        if (context.DayOfSeason != null)
        {
            prompt.AppendLine((context.Season,context.DayOfSeason) switch
            {
                (Season.Spring,1) => $"It is the first day of the year, which is also the first day of spring.",
                (Season.Spring,12) => $"It is the day before the egg festival.",
                (Season.Spring,23) => $"It is the day before the flower dance.",
                (Season.Summer,1) => $"It is the first day of summer.",
                (Season.Summer,10) => $"It is the day before the luau.",
                (Season.Summer,27) => $"It is the day before the dance of the moonlight jellies, and almost the end of summer.",
                (Season.Fall,1) => $"It is the first day of fall.",
                (Season.Fall,15) => $"It is the day before the Stardew Valley fair.",
                (Season.Fall,26) => $"It is the day before Spirit's Eve.",
                (Season.Winter,1) => $"It is the first day of winter.",
                (Season.Winter,7) => $"It is the day before the ice festival and ice fishing competition.",
                (Season.Winter,24) => $"It is the day before the feast of the winter star.",
                (Season.Winter,28) => $"It is the last day of the year, and the last day of winter.",
                _ => $""
            });
            var stardewBioData = StardewNpc.GetData();
            if (
                string.Equals(
                    context.Season.Value.ToString(),
                    stardewBioData.BirthSeason.ToString(),
                    StringComparison.InvariantCultureIgnoreCase
                ) && context.DayOfSeason == stardewBioData.BirthDay)
            {
                prompt.AppendLine($"It is {Name}'s birthday.");
            }
        }
        if (context.Accept != null)
        {
            prompt.AppendLine($"The farmer has given {Name} a {context.Accept.Name} as an unexpected gift.");
            switch (context.GiftTaste)
            {
                //TODO: Correct the cases
                case 0:
                    prompt.AppendLine($"This gift is one of {Name}'s favourite things in the world.");
                    break;
                case 2:
                    prompt.AppendLine($"This gift is something that {Name} likes.");
                    break;
                case 4:
                    prompt.AppendLine($"This gift is something that {Name} dislikes and {Name} feels that it shows poor taste as a gift.");
                    break;
                case 6:
                    prompt.AppendLine($"This gift is something that {Name} truly hates and wants nothing to do with. {Name} may even be offended by the gift.");
                    break;
                default:
                    prompt.AppendLine($"This gift is something that {Name} really doesn't care about, and doesn't really want as a gift.");
                    break;
            }
            prompt.AppendLine($"The dialogue should include {Name}'s reaction to the gift.");
            if (context.Birthday)
            {
                prompt.AppendLine($"As it is {Name}'s birthday, {Name} takes the gift to be a birthday present and evidence that the farmer remembered. The birthday should be mentioned in the reactions and lead to stronger feelings about gifts that {Name} loves or hates.");
            }
            prompt.AppendLine("The dialogue should be brief, and not ask questions that expect a response from the farmer.");
        }
        if (context.SpouseAct != null)
        {
            prompt.AppendLine(context.SpouseAct switch
            {
                SpouseAction.funLeave => $"{Name} is leaving for the day to have fun without the farmer.",
                SpouseAction.jobLeave => $"{Name} is leaving for the day to go to work.",
                SpouseAction.patio => $"{Name} is standing on the patio to the rear of the farmhouse.  {Name} is engaging in their favourite hobby, and focussing intently.",
                SpouseAction.funReturn => $"{Name} is returning to the farmhouse after a fun day out.",
                SpouseAction.jobReturn => $"{Name} is returning to the farmhouse after a day at work.",
                SpouseAction.spouseRoom => $"{Name} is engaging in their personal hobbies and interests in a special room in the farmhouse dedicates to that.",
                _ => $"{Name} is talking to the farmer."
            });
        }
        if (context.RandomAct != null)
        {
            prompt.AppendLine(context.RandomAct switch
            {
                RandomAction.Indoor => $"{Name} is inside the farmhouse with the farmer.",
                RandomAction.Rainy => $"{Name} is inside the farmhouse with the farmer, on a day when they are kept inside by rain.",
                RandomAction.Outdoor => $"{Name} is standing outside the farmhouse on the front porch.",
                RandomAction.OneKid => $"{Name} is inside the farmhouse with the farmer.  They are talking about their one child, whose name can be inserted using the string %kid1. The lines created should focus on the child, who is a toddler. The child may be a boy or a girl, so the lines should be appropriate for either.",
                RandomAction.TwoKids => $"{Name} is inside the farmhouse with the farmer.  They are talking about their two children, whose names can be inserted using the strings %kid1 and %kid2. The lines created should focus on the children, who are a toddler and a baby. The children may be boys or girls or one of each, so the lines should be appropriate for any combination.",
                RandomAction.Good => $"{Name} is feeling very positive about {(Name == "Krobus" ? "being roommates" : "the marriage")}.",
                RandomAction.Bad => $"{Name} is feeling very negative about {(Name == "Krobus" ? "being roommates" : "the marriage")}.",
                RandomAction.Neutral => $"{Name} is generally content, but a little uncertain and conflicted about {(Name == "Krobus" ? "being roommates" : "the marriage")}.",
                _ => $"{Name} is talking to the farmer."
            });
        }
        
        if (context.Inlaw != Name)
        {
            var isASingle = StardewNpc.GetData().CanBeRomanced;
            var isChild = StardewNpc.GetData().Age == NpcAge.Child;
            
            if (isASingle || context.Hearts <= 6 || context.Hearts == null)
            {
                prompt.AppendLine((context.Hearts ?? 0) switch
                {
                    -1 => $"This is the first time {Name} has spoken to the farmer, though {Name} had heard rumours of the farmer's arrival in town.",
                    <2 => $"{Name} and the farmer are complete strangers. {Name} is not yet sure if the farmer is someone they want to get to know.",
                    <4 => $"{Name} and the farmer know each other by sight, but treat each other as strangers. The dialogue should reflect two people just getting to know each other, no sharing of personal details or gossip or suggesting activities together.",
                    <6 => $"{Name} and the farmer are becoming friends. They know something about eachother and a little about each other's lives. The dialogue should reflect a growing friendship, with some sharing of personal details and gossip and no particular desire to spend more time together.",
                    <8 => $"{Name} and the farmer are close friends. They know a lot about each other and share personal details, gossip and theories about the world. The dialogue should reflect a close friendship, with a desire to spend time together but no romantic interest.",
                    <10 => $"{Name} wants to date the farmer, but hasn't been asked yet. In context, the dialogue should reflect a close, intimate friendship and include occasional suggestive comments.",
                    <=14 => $"{Name} and the farmer are very close and intimate.",
                    _ => throw new InvalidDataException("Invalid heart level.")
                });
            }
            else
            {
                if (context.Hearts == 8 && !isChild)
                {
                    prompt.AppendLine($"{Name} and the farmer are close friends. They know a lot about each other and share personal details and gossip as well as their hopes and dreams. The dialogue should reflect a close friendship, with a desire to spend time together but no romantic interest.");
                }
                else if (isChild)
                {
                    prompt.AppendLine($"{Name} looks to the farmer like a parent, idolising the farmers actions and beliving that the farmer is mostly infallible.");
                }
                else
                {
                    prompt.AppendLine($"{Name} and the farmer are close friends. {Name} wishes {(Llm.Instance.IsHighlySensoredModel ? "they could know the farmer better" : "the world had been different so they could pursue an intimate relationship with the farmer")}.  They know a lot about each other and confide initimate hopes dreams and fears to each other. {Name} sees the farmer as a confidant and openly share their frustrations and annoyances with others who are important in their life.");
                }
            }
        
            if (context.Inlaw != null)
            {
                if (context.Inlaw == "Krobus")
                {
                    prompt.AppendLine($"The farmer is roommates with {context.Inlaw}. {context.Inlaw} lives together at the farm inherited from the farmer's grandfather in a platonic freindship.  {context.Inlaw} lived in the Stardew Valley sewers before becoming the farmer's roommate and is unknown to the townsfolk.");
                }
                else
                {
                    prompt.AppendLine($"The farmer is married to {context.Inlaw}. {context.Inlaw} lives with the Farmer on the farm inherited from the Farmer's grandfather.  {Name} and {context.Inlaw} both lived in Stardew Valley and knew each other before the farmer met either of them.");
                }
            }

            if (friendship.IsDating())
            {
                prompt.AppendLine($"{Name} and the farmer are dating seriously {(context.Inlaw == null ? "and publicly" : "but discretely")} as a {RelationshipWord(context.MaleFarmer,npcIsMale)} couple. {(Llm.Instance.IsHighlySensoredModel ? "" : "They are keeping their sexual relationship a secret from the town but obsessing about it regularly when talking to each other.")} {Name} does not live on the farm.");
            }
            if (friendship.IsEngaged())
            {
                prompt.AppendLine($"{Name} and the farmer are engaged to be married. Their wedding is in {friendship.CountdownToWedding} days and they arelooking forward to their future together. {Name} does not live on the farm.");
            }
            if (friendship.IsDivorced())
            {
                prompt.AppendLine($"{Name} and the farmer are divorced. {Name} remains extremely angry and bitter about the divorce. {Name} does not live on the farm.");
            }
            if (friendship.ProposalRejected)
            {
                prompt.AppendLine($"{Name} has previously rejected the farmer's proposal of marriage. {Name} does not live on the farm.");
            }
        }
        if (context.MaleFarmer)
        {
            prompt.AppendLine("The farmer is male and the dialogue may reflect this, for example by referring to him as a 'man', 'boy' or 'husband' as appropriate in the context, referring to typical male clothing choices or activities.");
        }
        else
        {
            prompt.AppendLine("The farmer is female and the dialogue may reflect this, for example by referring to her as a 'woman', 'girl' or 'wife' as appropriate in the context, referring to typical female clothing choices or activities.");
        }
        if (context.ChatHistory.Length > 0)
        {
            prompt.AppendLine($"###Current Conversation:");
            prompt.AppendLine($"The farmer and {Name} are in the middle of a conversation. The dialogue should be a continuation of the conversation, and should reference the previous lines in the conversation, which were:");
            // Append each line from the chat history, labelling each one alternatively with the NPC's name or 'Farmer'
            for (int i = 0; i < context.ChatHistory.Length; i++)
            {
                prompt.AppendLine(i % 2 == 0 ? $"- {Name}: {context.ChatHistory[i]}" : $"- Farmer: {context.ChatHistory[i]}");
            }
        }
        
        result.Context = prompt.ToString();
        result.Command = $"##Command:\nWrite a single line of dialogue for {Name} to fit the situation and {Name}'s personality.";
        var instructions = new StringBuilder();
        instructions.AppendLine("##Output Format:");
        instructions.AppendLine($"The line should be written in the style of the game and reflect the level of familiarity {Name} has with the farmer.");
        if (dialogueSample.Any())
        {
            instructions.AppendLine("Use the supplied sample dialogue to help you match the tone and style of {Name}'s interactions with the farmer at the current friendship level.");
        }
        instructions.AppendLine("To include the farmer's name use the @ symbol.");
        /*if (context.Location != null && context.Accept != null)
        {
            instructions.AppendLine("Half to a third of the lines should include questions that the farmer would be expected to respond to.");
        }
        else
        {
            instructions.AppendLine("The lines should be appropriate to be said repeatedly in conversation.  They should not contain any questions.");
        }*/
        instructions.AppendLine("If the line should be presented with breaks, use #$b# as a screen divider or use #$e# as a divider for a more significant break. There should not be more than 25 words between each break. Do not put break signifiers on the start or end of the line. Do not signify breaks by starting new lines.");
        instructions.AppendLine($"To express emotions, finish the section with one of these emotion tokens: $h for extremely happy, $0 for neutral, $s for sad, $l for in love, {(string.IsNullOrWhiteSpace(Bio.Unique) ? "" : "$u for " + Bio.Unique + ", ")}or $a for angry. Include the emotion token in the section to which it applies, do not put a # before it. Do not include emojis, actions surrounded by asterisks or other special characters to indicate emotion or actions.");
        instructions.AppendLine("Write the line as a single line of output preceded with a '-' but no other punctuation or numbering.");
        instructions.AppendLine("If the line doesn't call for the farmer to respond, just output the one line.");
        instructions.AppendLine("If the line does invite a response from the farmer, please propose two, three or four possible responses that the farmer could make, covering the full range of possible reactions. Each response should be on a new line, preceded by a '%' and a space. Response lines should be in the voice of, and from the perspective of, the farmer.  They should not contain any special symbols, @s or emotion tokens.");
        instructions.AppendLine("### Example 1:");
        instructions.AppendLine("- \"It is such a lovely spring day today, amazing to meet you at the Jojamart.  How are you?\" $0");
        instructions.AppendLine("% I'm doing well, thank you.");
        instructions.AppendLine("% I'm not doing so well, actually.");
        instructions.AppendLine("% I'm doing great, thanks for asking.");
        instructions.AppendLine("### Example 2:");
        instructions.AppendLine("- \"I'm so glad you came to visit me today.  I've been feeling a little lonely lately, but this rose really brightens my day.\" $s");
        instructions.AppendLine("### Example 3:");
        instructions.AppendLine("- \"Oh hi. I don't think I know you, and I'm rather busy. See you around.\"");
        if (!string.IsNullOrWhiteSpace(Llm.Instance.ExtraInstructions))
        {
            instructions.AppendLine(Llm.Instance.ExtraInstructions);
        }
        result.Instructions = instructions.ToString();
        result.ResponseStart = $"Here is the requested line for {Name} which fit the situation and {Name}'s personality.";
        result.Name = Name;
        result.Gender = context.MaleFarmer ? "Male" : "Female";
        return result;
    }

private string RelationshipWord(bool maleFarmer, bool npcIsMale)
    {
        return maleFarmer ? (npcIsMale ? "gay" : "heterosexual") : (npcIsMale ? "heterosexual" : "lesbian");
    }

    private IEnumerable<Crop> GetCrops()
    {
        return Game1.getFarm().terrainFeatures.Values
            .Where(x => x is StardewValley.TerrainFeatures.HoeDirt)
            .Select(x => (x as StardewValley.TerrainFeatures.HoeDirt).crop)
            .Where(x => x != null);
    }

    private IEnumerable<StardewValley.Buildings.Building> GetBuildings()
    {
        var excludeTypes = new string[] { "Shipping Bin", "Pet Bowl", "Farmhouse" };
        return Game1.getFarm().buildings.Where(x => !excludeTypes.Contains(x.buildingType));
    }

    private IEnumerable<FarmAnimal> GetAnimals()
    {
        var animalsOnFarm = Game1.getFarm().getAllFarmAnimals();
        return animalsOnFarm;
    }

    private IEnumerable<DialogueValue> SelectDialogueSample(DialogueContext context)
    {
        // Pick 20 most relevant dialogue entries
        var orderedDialogue = DialogueData
                    ?.AllEntries
                    .OrderBy(x => context.CompareTo(x.Key));
        return orderedDialogue
                    ?.Take(20)
                    ?.Where(x => x.Value != null)
                    .SelectMany(x => x.Value.AllValues) 
                    ?? Array.Empty<DialogueValue>();
    }

    private IDialogueValue? SelectExactDialogue(DialogueContext context)
    {
        return (DialogueData
                    ?.AllEntries
                    .FirstOrDefault(x => x.Key == context))?.Value;
                    
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

    public string StardewSummary { get; private set; }
    public NPC StardewNpc { get; internal set; }
}
