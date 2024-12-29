using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using ValleyTalk;
using StardewValley;
using StardewValley.GameData.Characters;

namespace StardewDialogue;

public class Prompts
{
    [JsonIgnore]
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

    public Prompts()
    {
    }

    private static Prompts CreatePrompts(DialogueContext Context, Character character)
    {
        return new Prompts(Context, character);
    }

    static Prompts()
    {
        LoadStardewSummary();
    }

    private static void LoadStardewSummary()
    {
        var gameSummaryDict = ModEntry.SHelper.Data.ReadJsonFile<Dictionary<string,string>>("assets/bio/Stardew.txt");
        _stardewSummary = gameSummaryDict["Text"];
    }


    [JsonIgnore]
    static string _stardewSummary;
    private string _system;
    [JsonIgnore]
    public string System { get => _system ??= GetSystemPrompt(); internal set => _system = value; }
    private string _gameConstantContext;
    [JsonIgnore]
    public string GameConstantContext { get => _gameConstantContext ??= GetGameConstantContext(); internal set => _gameConstantContext = value; }
    private string _npcConstantContext;
    public string NpcConstantContext { get => _npcConstantContext ??= GetNpcConstantContext(); internal set => _npcConstantContext = value; }
    private string _corePrompt;
    public string CorePrompt { get => _corePrompt ??= GetCorePrompt(); internal set => _corePrompt = value; }
    private string _command;
    public string Command { get => _command ??= GetCommand(); internal set => _command = value; }
    private string _responseStart;
    public string ResponseStart { get => _responseStart ??= GetResponseStart(); internal set => _responseStart = value; }
    private string _instructions;
    public string Instructions { get => _instructions ??= GetInstructions(); internal set => _instructions = value; }

    [JsonIgnore]
    public string Name { get; internal set; }
    
    [JsonIgnore]
    public string Gender { get; internal set; }
    
    
    [JsonIgnore]
    public Character Character { get; internal set;}
    
    [JsonIgnore]
    internal DialogueContext Context { private get; set; }

    [JsonIgnore]
    CharacterData npcData;
    
    [JsonIgnore]
    bool npcIsMale;
    
    [JsonIgnore]
    IEnumerable<DialogueValue> dialogueSample;
    
    [JsonIgnore]
    IDialogueValue exactLine;
    
    [JsonIgnore]
    SerializableDictionary<string,int> allPreviousActivities;
    
    [JsonIgnore]
    List<KeyValuePair<string,int>> previousActivites;

    public Prompts(DialogueContext context, Character character)
    {

        npcData = character.StardewNpc.GetData();
        npcIsMale = npcData.Gender == StardewValley.Gender.Male;
        Context = context;
        Character = character;

        dialogueSample = SelectDialogueSample();
        exactLine = SelectExactDialogue();
        allPreviousActivities = Game1.getPlayerOrEventFarmer().previousActiveDialogueEvents.First();
        previousActivites = allPreviousActivities.Where(x => HistoryEvents.ContainsKey(x.Key) && x.Value < 112).ToList();

        Name = character.Name;
        Gender = Context.MaleFarmer ? "Male" : "Female";
    }

    private string GetSystemPrompt()
    {
        var systemPrompt = new StringBuilder();
        systemPrompt.AppendLine("You are an expert computer game writer that takes great pride in being able to create dialogue for any character in any game that exactly matches that character's situation and personality.");
        if (ModEntry.Config.ApplyTranslation)
        {
            systemPrompt.AppendLine($"You are an expert at writing dialogue in {ModEntry.Language} based on instructions given mostly in English, while keeping the responses in character.  All responses you write must be in {ModEntry.Language}.");
        }
        return systemPrompt.ToString();
    }

    private string GetGameConstantContext()
    {
        var gameConstantPrompt = new StringBuilder();
        gameConstantPrompt.AppendLine($"You are creating dialogue to enhance the experience of players in the game Stardew Valley.");
        gameConstantPrompt.AppendLine("While staying true to the characters you are writing for a mature audience and looking to add variety and depth when appropriate.");
        gameConstantPrompt.AppendLine("##Game Summary");
        gameConstantPrompt.AppendLine(_stardewSummary);
        return gameConstantPrompt.ToString();
    }

    private string GetNpcConstantContext()
    {
        var npcConstantPrompt = new StringBuilder();
        npcConstantPrompt.AppendLine($"You are working on dialogue for {Name}, who is talking to the player (referred to as 'the farmer')");
        if ((Character.Bio?.Biography ?? string.Empty).Length > 100)
        {
            npcConstantPrompt.AppendLine($"##{Name} Biography:");
            var bio = Character.Bio.Biography;
            while (bio.Contains("\n\n"))
            {
                bio = bio.Replace("\n\n", "\n");
            }
            npcConstantPrompt.AppendLine(bio);
        }
        return npcConstantPrompt.ToString();
    }

    private string GetCorePrompt()
    {
        var prompt = new StringBuilder();
        GetGameState(prompt);
        GetSampleDialogue(prompt);
        GetEventHistory(prompt);

        prompt.AppendLine("## Instructions:");
        prompt.AppendLine("### Context:");
        if (Context.MaleFarmer)
        {
            prompt.AppendLine("The farmer is male. As well as running the farm, he is an adventurer and also has interests and habits that are typically male.");
        }
        else
        {
            prompt.AppendLine("The farmer is female. As well as running the farm, the farmer is an adventurer and also has interests and habits that are typically female.");
        }
        GetDateAndTime(prompt);
        GetWeather(prompt);
        GetOtherNpcs(prompt);
        Game1.getPlayerOrEventFarmer().friendshipData.TryGetValue(Name, out Friendship friendship);
        if (friendship.IsMarried() || friendship.IsRoommate())
        {
            if (friendship.IsRoommate())
            {
                prompt.AppendLine($"The farmer and {Name} are roommates and close, non-romantic friends. They live together at the farm inherited from the farmer's grandfather. {Name} lived in the sewers before meeting the farmer and moved from there to the farm when they became roommates.");
            }
            else
            {
                prompt.AppendLine($"The farmer is married to {Name}. They live together at the farm inherited from the farmer's grandfather.  {Name} lived in Stardew Valley before meeting the farmer and moved from {(npcIsMale ? "his" : "her")} original house to the farm when they got married.");

                GetChildren(prompt, friendship);
            }

            GetFarmContents(prompt);
            GetMarriageFeelings(prompt);
        }
        GetLocation(prompt);
        GetRecentEvents(prompt);

        GetSpecialDatesAndBirthday(prompt);
        GetGift(prompt);
        GetSpouseAction(prompt);
        if (!friendship.IsMarried())
        {
            GetNonSpouseFriendshipLevel(prompt);
            GetSpouse(prompt);
            GetSpecialRelationshipStatus(prompt, friendship);
        }
        if (Context.MaleFarmer)
        {
            prompt.AppendLine("The farmer is male and the dialogue may reflect this, for example by referring to him as a 'man', 'boy' or 'husband' as appropriate in the Context, referring to typical male clothing choices or activities.");
        }
        else
        {
            prompt.AppendLine("The farmer is female and the dialogue may reflect this, for example by referring to her as a 'woman', 'girl' or 'wife' as appropriate in the Context, referring to typical female clothing choices or activities.");
        }
        GetPreoccupation(prompt);
        GetCurrentConversation(prompt);

        return prompt.ToString();
    }

    private void GetPreoccupation(StringBuilder prompt)
    {
        if (Game1.random.NextDouble() < 0.5) return;

        var nPreoccupations = Character.PossiblePreoccupations.Count;
        var preoccupation = Character.PossiblePreoccupations[Game1.random.Next(nPreoccupations)];
        prompt.AppendLine($"Before the farmer arrived, {Name} was thinking about {preoccupation}.");
    }

    private void GetOtherNpcs(StringBuilder prompt)
    {
        var otherNpcs = Util.GetNearbyNpcs(Character.StardewNpc);
        if (otherNpcs.Any())
        {
            prompt.AppendLine("### Other NPCs:");
            prompt.AppendLine($"As well as the farmer and {Character.Name}, the following villagers are present:");
            foreach (var npc in otherNpcs)
            {
                prompt.AppendLine($"- {npc.Name}");
            }
            prompt.AppendLine("The dialogue should account for the presence of these other villagers, and may reference them or be addressed to them as well as to the farmer.");
        }
    }

    private void GetCurrentConversation(StringBuilder prompt)
    {
        if (Context.ChatHistory.Length == 0) return;

        prompt.AppendLine($"###Current Conversation:");
        prompt.AppendLine($"The farmer and {Name} are in the middle of a conversation. The dialogue should be a continuation of the conversation, and should build on the previous lines without repetition. The previous lines were:");
        // Append each line from the chat history, labelling each one alternatively with the NPC's name or 'Farmer'
        for (int i = 0; i < Context.ChatHistory.Length; i++)
        {
            prompt.AppendLine(i % 2 == 0 ? $"- {Name}: {Context.ChatHistory[i]}" : $"- Farmer: {Context.ChatHistory[i]}");
        }
    }

    private void GetSpecialRelationshipStatus(StringBuilder prompt, Friendship friendship)
    {
        if (friendship == null) return;
        
        if (friendship.IsDating())
        {
            prompt.AppendLine($"{Name} and the farmer are dating seriously {(Context.Inlaw == null ? "and publicly" : "but discretely")} as a {RelationshipWord(Context.MaleFarmer, npcIsMale)} couple. {Name} does not live on the farm.");
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

    private void GetSpouse(StringBuilder prompt)
    {
        var spouses = Game1
                    .getPlayerOrEventFarmer()
                    .friendshipData
                    .FieldDict
                    .Where(x => x.Value.Value.IsMarried() || !x.Value.Value.IsRoommate())
                    .Select(x => x.Key);
        bool talkingToSpouse = spouses.Any(x => x == Name);
        spouses = spouses.Where(x => x != Name);
                    
        if (spouses.Any())
        {
            bool multipleOthers = spouses.Count() > 1;
            if (talkingToSpouse)
            {
                prompt.AppendLine($"As well as {Name} the Farmer is also married to {(multipleOthers ? $"{spouses.Count()} other people: {string.Join(", ", spouses)}":spouses.First())}. {Name}, the Farmer and {(multipleOthers ? "all the other spouses" : spouses.First())} live together at the farm inherited from the farmer's grandfather. The spouses lived in Stardew Valley before the Farmer arrived and knew each other before the farmer met any of them.");
            }
            else
            {
                if (multipleOthers)
                {
                    prompt.AppendLine($"The farmer is married to {spouses.Count()} people: {string.Join(", ", spouses)}. The Farmer's spouses all live with the farmer on the farm inherited from the farmer's grandfather.  {string.Join(", ", spouses)} and {Name} all lived in Stardew Valley and knew each other before the farmer met any of them.");
                }
                else
                {
                    prompt.AppendLine($"The farmer is married to {spouses.First()}. {spouses.First()} lives with the farmer on the farm inherited from the farmer's grandfather.  {Name} and {spouses.First()} both lived in Stardew Valley and knew each other before the farmer met either of them.");
                }
            }
        }
        var roommates = Game1
                    .getPlayerOrEventFarmer()
                    .friendshipData
                    .FieldDict
                    .Where(x => x.Value.Value.IsMarried() && x.Value.Value.IsRoommate())
                    .Select(x => x.Key);
        bool talkingToRoommate = roommates.Any(x => x == Name);
        roommates = roommates.Where(x => x != Name);
        
        if (roommates.Any())
        {
            bool multipleOthers = roommates.Count() > 1;
            if (talkingToRoommate)
            {
                prompt.AppendLine($"As well as {Name} the Farmer is a roommate with {(multipleOthers ? $"{roommates.Count()} other people: {string.Join(", ", roommates)}":roommates.First())}. {Name}, the Farmer and {(multipleOthers ? "all the other roommates" : roommates.First())} live together at the farm inherited from the farmer's grandfather in a platonic friendship.");
            }
            else
            {
                if (multipleOthers)
                {
                    prompt.AppendLine($"The farmer has {roommates.Count()} roommates: {string.Join(", ", roommates)}. The Farmer's roommates all live with the farmer on the farm inherited from the farmer's grandfather.");
                }
                else
                {
                    prompt.AppendLine($"The farmer has {roommates.First()} as a roommate. {roommates.First()} lives with the farmer on the farm inherited from the farmer's grandfather.");
                }
            }
        }

        var engaged = Game1
                    .getPlayerOrEventFarmer()
                    .friendshipData
                    .FieldDict
                    .Where(x => x.Value.Value.IsEngaged())
                    .Select(x => x.Key);
        if (engaged.Any(x => x != Name))
        {
            var firstEngaged = engaged.First();
            prompt.AppendLine($"The farmer is engaged to {firstEngaged}. The wedding is in {Game1.getPlayerOrEventFarmer().friendshipData[firstEngaged].CountdownToWedding} days and the farmer is looking forward to their future together. {firstEngaged} does not live on the farm.");
        }
        var total = spouses.Count() + engaged.Count();
        if (total > 1 && !talkingToSpouse && !talkingToRoommate)
        {
            prompt.AppendLine($"{Name} is aware that the farmer has a polyamorous lifestyle and accepts this.");
        }
    }

    private void GetNonSpouseFriendshipLevel(StringBuilder prompt)
    {
        var isASingle = npcData.CanBeRomanced;
        var isChild = npcData.Age == NpcAge.Child;

        if (isASingle || Context.Hearts <= 6 || Context.Hearts == null)
        {
            prompt.AppendLine((Context.Hearts ?? 0) switch
            {
                -1 => $"This is the first time {Name} has spoken to the farmer, though {Name} had heard rumours of the farmer's arrival in town.",
                < 2 => $"{Name} and the farmer are strangers, though they have spoken before. {Name} is not yet sure if the farmer is someone they want to get to know.",
                < 4 => $"{Name} and the farmer know each other by sight, but treat each other as strangers. The dialogue should reflect two people just getting to know each other, no sharing of personal details or gossip or suggesting activities together.",
                < 6 => $"{Name} and the farmer are becoming friends. They know something about eachother and a little about each other's lives. The dialogue should reflect a growing friendship, with some sharing of personal details and gossip and no particular desire to spend more time together.",
                < 8 => $"{Name} and the farmer are close friends. They know a lot about each other and share personal details, gossip and theories about the world. The dialogue should reflect a close friendship, with a desire to spend time together but no romantic interest.",
                <= 10 => $"{Name} wants to date the farmer. In context, the dialogue should reflect a close, intimate friendship and share personal details, gossip and theories about the world. The dialogue should reflect a desire to spend time together and growing romantic interest.",
                <= 14 => $"{Name} and the farmer are very close and intimate.", // Backup = should never be called
                _ => throw new InvalidDataException("Invalid heart level.")
            });
        }
        else
        {
            if (Context.Hearts <= 8 && !isChild)
            {
                prompt.AppendLine($"{Name} and the farmer are close friends. They know a lot about each other and share personal details and gossip as well as their hopes and dreams. The dialogue should reflect a close friendship, with a desire to spend time together but no romantic interest.");
            }
            else if (isChild)
            {
                prompt.AppendLine($"{Name} looks to the farmer like a parent, idolising the farmer's actions and believing that the farmer is mostly infallible.");
            }
            else
            {
                prompt.AppendLine($"{Name} and the farmer are close friends. They know a lot about each other and confide initimate hopes dreams and fears to each other. {Name} sees the farmer as a confidant and openly share their frustrations and annoyances with others who are important in their life.");
            }
        }
    }

    private void GetSpouseAction(StringBuilder prompt)
    {
        if (Context.SpouseAct == null) return;

        prompt.Append(Context.SpouseAct switch
        {
            SpouseAction.funLeave => $"{Name} is leaving for the day to have fun without the farmer.\n",
            SpouseAction.jobLeave => $"{Name} is leaving for the day to go to work.\n",
            SpouseAction.patio => $"{Name} is standing on the patio to the rear of the farmhouse.  {Name} is engaging in their favourite hobby, and focussing intently.\n",
            SpouseAction.funReturn => $"{Name} is returning to the farmhouse after a fun day out.\n",
            SpouseAction.jobReturn => $"{Name} is returning to the farmhouse after a day at work.\n",
            SpouseAction.spouseRoom => $"{Name} is engaging in their personal hobbies and interests in a special room in the farmhouse dedicates to that.\n",
            _ => $""
        });
    }

    private void GetGift(StringBuilder prompt)
    {
        if (Context.Accept == null) return;

        prompt.AppendLine($"The farmer has given {Name} a {Context.Accept.Name} as an unexpected gift.");
        switch (Context.GiftTaste)
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
        if (Context.Birthday)
        {
            prompt.AppendLine($"As it is {Name}'s birthday, {Name} takes the gift to be a birthday present and evidence that the farmer remembered. The birthday should be mentioned in the reactions and lead to stronger feelings about gifts that {Name} loves or hates.");
        }
        prompt.AppendLine("The dialogue should be brief, and not ask questions that expect a response from the farmer.");
    }

    private void GetSpecialDatesAndBirthday(StringBuilder prompt)
    {
        if (Context.DayOfSeason == null) return;

        prompt.AppendLine((Context.Season, Context.DayOfSeason) switch
        {
            (Season.Spring, 1) => $"It is the first day of the year, which is also the first day of spring.",
            (Season.Spring, 12) => $"It is the day before the egg festival.",
            (Season.Spring, 23) => $"It is the day before the flower dance.",
            (Season.Summer, 1) => $"It is the first day of summer.",
            (Season.Summer, 10) => $"It is the day before the luau.",
            (Season.Summer, 27) => $"It is the day before the dance of the moonlight jellies, and almost the end of summer.",
            (Season.Summer, 28) => $"It is the day of the dance of the moonlight jellies, and the last day of summer.",
            (Season.Fall, 1) => $"It is the first day of fall.",
            (Season.Fall, 15) => $"It is the day before the Stardew Valley fair.",
            (Season.Fall, 26) => $"It is the day before Spirit's Eve.",
            (Season.Winter, 1) => $"It is the first day of winter.",
            (Season.Winter, 7) => $"It is the day before the ice festival and ice fishing competition.",
            (Season.Winter, 24) => $"It is the day before the feast of the winter star.",
            (Season.Winter, 28) => $"It is the last day of the year, and the last day of winter.",
            _ => $""
        });
        var stardewBioData = Character.StardewNpc.GetData();
        if (
            string.Equals(
                Context.Season.Value.ToString(),
                stardewBioData.BirthSeason.ToString(),
                StringComparison.InvariantCultureIgnoreCase
            ) && Context.DayOfSeason == stardewBioData.BirthDay)
        {
            prompt.AppendLine($"It is {Name}'s birthday.");
        }
    }

    private void GetRecentEvents(StringBuilder prompt)
    {
        var eventSection = new StringBuilder();
        foreach (var activity in allPreviousActivities.Where(x => x.Value < 7))
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
                "babyBoy" => $"The farmer and the farmer's spouse have recently had a baby boy.",
                "babyGirl" => $"The farmer and the farmer's spouse have recently had a baby girl.",
                "wedding" => $"The farmer has recently gotten married.",
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
    }

    private void GetLocation(StringBuilder prompt)
    {
        if (Context.Location == null && Character.StardewNpc.DirectionsToNewLocation == null) return;
        
        var bedTile = npcData.Home[0].Tile;
        if (Context.Location == npcData.Home[0].Location && Context.Inlaw != Name)
        {
            if (Character.StardewNpc.TilePoint == bedTile && Character.Bio.HomeLocationBed && !Llm.Instance.IsHighlySensoredModel)
            {
                prompt.AppendLine($"{Name} is in bed. The farmer has climbed into {Name}'s bed, and is talking to {Character.Bio.GenderPronoun} there.");
            }
            else
            {
                var mayBeInShop = Context.Location.Contains("Shop", StringComparison.OrdinalIgnoreCase)
                    || Context.Location.Contains("Science", StringComparison.OrdinalIgnoreCase);
                prompt.AppendLine($"The farmer and {Name} are talking in {Name}'s home{(mayBeInShop ? " or the shop" : "")}.");
            }
        }
        else if (Context.Location != null)
        {
            prompt.Append(Context.Location switch
            {
                "Town" => $"The farmer and {Name} are talking outdoors in the center of Pelican Town.",
                "Beach" => $"The farmer and {Name} are on the beach.",
                "Desert" => $"The farmer and {Name} are away from Stardew Valley visiting the Calico desert.",
                "BusStop" => $"The farmer and {Name} are at the bus stop.",
                "Railroad" => $"The farmer and {Name} are at the railroad station, near the spa in the mountains.",
                "Saloon" => $"The farmer and {Name} are at the Stardrop Saloon, relaxing at the end of a busy day. {(npcData.Age == NpcAge.Child ? "" : "They are a little drunk.")}",
                "SeedShop" => $"The farmer and {Name} are in Pierre's General Store.",
                "JojaMart" => $"The farmer and {Name} are shopping at the JojaMart.",
                "Resort_Chair" => $"The farmer and {Name} are at the Ginger Island tropical resort. {Name} is standing by a chair on the beach.",
                "Resort_Towel" or "Resort_Towel_2" or "Resort_Towel_3" => $"The farmer and {Name} are at the Ginger Island tropical resort. {Name} is relaxing on a beach towel on the beach.",
                "Resort_Umbrella" or "Resort_Umbrella_2" or "Resort_Umbrella_3" => $"The farmer and {Name} are at the Ginger Island tropical resort. {Name} is relaxing on a beach towel on the beach.",
                "Resort_Bar" => $"The farmer and {Name} are at the Ginger Island tropical resort. They are at the bar run by Gus and it is day time. They are focussed on the bar{(npcData.Age == NpcAge.Child ? "" : " and what they have been drinking. They are a little drunk")}.",
                "Resort_Entering" => $"The farmer and {Name} are at the Ginger Island tropical resort. {Name} is just arriving at the resort from Pelican Town.",
                "Resort_Leaving" => $"The farmer and {Name} are at the Ginger Island tropical resort. {Name} is leaving the resort to return to Pelican Town.",
                "Resort_Shore" => $"The farmer and {Name} are at the Ginger Island tropical resort. They are talking at the shore of the beach, with their feet in the water looking out to sea.",
                "Resort_Shore_2" => $"The farmer and {Name} are at the Ginger Island tropical resort. They are talking at the shore of the beach, with their feet in the water contemplating the waves.",
                "Resort_Wander" => $"The farmer and {Name} are at the Ginger Island tropical resort. {Name} is walking around behind the beach huts, close to the jungle and considering exploring the island away from the resort.",
                "Resort" or "Resort_2" => $"The farmer and {Name} are at the Ginger Island tropical resort. The dialogue should be appropriate wherever in the resort they are.",
                _ => $"The farmer and {Name} are at {Context.Location}."
            });
            prompt.AppendLine("The location where the conversation is taking place may be significant for the lines.");
        }

        if (Character.StardewNpc.DirectionsToNewLocation != null && Context.Location != Character.StardewNpc.DirectionsToNewLocation.targetLocationName)
        {
            prompt.AppendLine($"{Name} is going to {Character.StardewNpc.DirectionsToNewLocation.targetLocationName}.");
        }
    }

    private void GetMarriageFeelings(StringBuilder prompt)
    {
        var IsRoommate = Game1.getPlayerOrEventFarmer().friendshipData[Name].IsRoommate();
        switch (Context.Hearts)
        {
            case > 12:
                prompt.AppendLine($"{Name} is feeling very positive about {(IsRoommate ? "being roommates" : "the marriage")}.");
                break;
            case < 10:
                prompt.AppendLine($"{Name} is feeling very negative about {(IsRoommate ? "being roommates" : "the marriage")}. While {Name} should will still talk about the Context of the conversation, they will be more likely to be negative or critical.");
                break;
            default:
                prompt.AppendLine($"{Name} is generally content, but a little uncertain and conflicted about {(IsRoommate ? "being roommates" : "the marriage")}.");
                break;
        }
    }

    private void GetFarmContents(StringBuilder prompt)
    {
        GetFarmBuildings(prompt);
        GetFarmAnimals(prompt);
        GetFarmCrops(prompt);
        var pet = Game1.getPlayerOrEventFarmer().getPet();
        if (pet != null)
        {
            prompt.AppendLine($"The farm has a pet {pet.petType.Value} named {pet.Name}.");
        }
        else
        {
            prompt.AppendLine("The farm has no pets.");
        }
    }

    private void GetFarmCrops(StringBuilder prompt)
    {
        var cropData = Game1.objectData;
        var allCrops = GetCrops();
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
    }

    private void GetFarmAnimals(StringBuilder prompt)
    {
        var allAnimals = GetAnimals();

        if (allAnimals.Any())
        {
            prompt.AppendLine($"The farm has the following animals:");
            foreach (var animal in allAnimals.GroupBy(x => x.type))
            {
                prompt.AppendLine($"- {animal.Count()} {animal.Key}{(animal.Count() > 1 ? "s" : "")}");
            }
        }
        else
        {
            prompt.AppendLine("The farm has no animals.");
        }
    }

    private void GetFarmBuildings(StringBuilder prompt)
    {
        var allBuildings = GetBuildings();

        if (allBuildings.Any())
        {
            prompt.AppendLine($"The farm has the following buildings:");
            var completedBuildings = allBuildings.Where(x => x.daysOfConstructionLeft.Value == 0 && x.buildingType.Value != "Greenhouse");
            foreach (var building in completedBuildings.GroupBy(x => x.buildingType))
            {
                prompt.AppendLine($"- {building.Count()} {building.Key}{(building.Count() > 1 ? "s" : "")}");
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
    }

    private void GetChildren(StringBuilder prompt, Friendship friendship)
    {
        if (Context.Children.Count == 0)
        {
            prompt.AppendLine($"The farmer and {Name} have no children.");
        }
        else
        {
            prompt.AppendLine($"The farmer and {Name} have {Context.Children.Count()} child{(Context.Children.Count > 1 ? "ren" : "")}:");
            foreach (var child in Context.Children)
            {
                prompt.AppendLine($"- A {(child.IsMale ? "boy" : "girl")} named {child.Name} who is {child.Age} years old.");
            }
        }
        if (friendship.DaysUntilBirthing > 0)
        {
            prompt.AppendLine($"The farmer and {Name} are expecting a child in {friendship.DaysUntilBirthing} days.");
        }
    }

    private void GetWeather(StringBuilder prompt)
    {
        if (Context.Weather == null || !Context.Weather.Any()) return;

        if (Context.Weather.Contains("lightning"))
        {
            prompt.AppendLine("There is a storm with rain and lightning.");
        }
        else if (Context.Weather.Contains("green rain"))
        {
            prompt.AppendLine("There is a strange green rain causing the plants to grow wildly.");
        }
        else if (Context.Weather.Contains("snow"))
        {
            prompt.AppendLine("It is snowing.");
        }
        else if (Context.Weather.Contains("rain"))
        {
            prompt.AppendLine("It is raining heavily.");
        }
    }

    private void GetDateAndTime(StringBuilder prompt)
    {
        if (Context.DayOfSeason != null && Context.Season != null)
        {
            prompt.AppendLine($"It is day {Context.DayOfSeason} of {Context.Season}.");
        }
        if (Context.TimeOfDay != null)
        {
            prompt.AppendLine($"It is {Context.TimeOfDay}.");
            if (Context.TimeOfDay == "early morning")
            {
                prompt.AppendLine("This is a normal time for the farmer to be up and about.");
            }
        }
        if (Context.Year == 1)
        {
            prompt.AppendLine("The farmer is new to Pelican Town this year.");
        }

    }

    private void GetEventHistory(StringBuilder prompt)
    {
        var timeNow = new StardewTime(Game1.year, Game1.season, Game1.dayOfMonth, Game1.timeOfDay);
        var fullHistory = Character.EventHistory.Concat(previousActivites.Select(x => MakeActivityHistory(x)));

        if (fullHistory.Any())
        {
            prompt.AppendLine($"##Event history:");
            prompt.AppendLine($"{Name} is aware of the following recent events and conversations with the farmer.");
            prompt.AppendLine("Conversations happened at the times indicated, possibly in contexts different to the current conversation.");
            prompt.AppendLine("The new line may be based on previous conversations and events as well as the current context.  The line should reference any events that happened just now. It may reference patterns in the previous conversation such as similar gifts or long gaps in the conversation.");
            prompt.AppendLine($"You should avoid {Name} repeating lines or concepts from previous lines. The more recent a previous event or interaction the more likely it will be referenced and the less likely it will be repeated.");
            prompt.AppendLine("History:");
            foreach (var eventHistory in fullHistory.OrderBy(x => x.Item1).Take(30))
            {
                prompt.AppendLine($"- {eventHistory.Item1.SinceDescription(timeNow)}: {eventHistory.Item2.Format(Name)}");
            }
        }
    }

    private Tuple<StardewTime, IHistory> MakeActivityHistory(KeyValuePair<string, int> x)
    {
        var timeNow = new StardewTime(Game1.year, Game1.season, Game1.dayOfMonth, Game1.timeOfDay);
        var targetDate = timeNow.AddDays(-x.Value);
        return new(targetDate, new ActivityHistory(x.Key));
    }

    private void GetSampleDialogue(StringBuilder prompt)
    {
        if (!dialogueSample.Any()) return;

        prompt.AppendLine($"##{Name} Sample Dialogue:");
        prompt.AppendLine($"You have a sample of {Name}'s dialogue as follows:");
        foreach (var dialogue in dialogueSample)
        {
            prompt.AppendLine($"- {dialogue.Value}");
        }
    }

    private void GetGameState(StringBuilder prompt)
    {
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
    }

    private string GetCommand()
    {
        var commandPrompt = new StringBuilder();
        commandPrompt.AppendLine($"##Command:\nWrite a single line of dialogue for {Name} to fit the situation and {Name}'s personality.");
        if (!string.IsNullOrWhiteSpace(Context.ScheduleLine) && Context.ChatHistory.Length == 0)
        {
            commandPrompt.AppendLine($"\nThe line will be used to replace a piece of situational dialogue and should communicate similar themes while being different.  The original line was:{Context.ScheduleLine}");
        }
        return commandPrompt.ToString();
    }

    private string GetInstructions()
    {
        var instructions = new StringBuilder();
        instructions.AppendLine("##Output Format:");
        instructions.AppendLine($"The line should be written in the style of the game and reflect the level of familiarity {Name} has with the farmer.");
        if (dialogueSample.Any())
        {
            instructions.AppendLine($"Use the supplied sample dialogue to help you match the tone and style of {Name}'s interactions with the farmer at the current friendship level.");
        }
        instructions.AppendLine("To include the farmer's name use the @ symbol.");
        instructions.AppendLine("If the line should be presented with breaks, use #$b# as a screen divider or use #$e# as a divider for a more significant break. There should not be more than 24 words between each break. Do not put break signifiers on the start or end of the line. Do not signify breaks by starting new lines.");
        var extraPortraits = new StringBuilder();
        foreach (var portrait in Character.Bio.ExtraPortraits)
        {
            extraPortraits.Append($"${portrait.Key} for {portrait.Value}, ");
        }
        instructions.AppendLine($"To express emotions, finish the section with one of these emotion tokens: $h for extremely happy, $0 for neutral, $s for sad, $l for in love, {extraPortraits.ToString()}or $a for angry. Include the emotion token in the section to which it applies, do not put a # before it. Do not include emojis, actions surrounded by asterisks or other special characters to indicate emotion or actions.");
        instructions.AppendLine("Write the line as a single line of output preceded with a '-' only. The line should be properly punctuated and capitalised.");
        instructions.AppendLine("If the line doesn't call for the farmer to respond, just output the one line.");
        instructions.AppendLine($"If the line does invite a response from the farmer, please propose two, three or four possible responses that the farmer could make, covering the full range of possible reactions. As the farmer gets more friendly with {Name} responses should be available more often. Each response should be on a new line, no more than 12 words, preceded by a '%' and a space. Response lines should be in the voice of, and from the perspective of, the farmer.  They should not contain any special symbols, @s or emotion tokens.");
        instructions.AppendLine("### Example 1:");
        instructions.AppendLine("- \"It is such a lovely spring day today, amazing to meet you at the Jojamart.  How are you?\" $0");
        instructions.AppendLine("% I'm doing well, thank you.");
        instructions.AppendLine("% I'm not doing so well, actually.");
        instructions.AppendLine("% I'm doing great, thanks for asking.");
        instructions.AppendLine("### Example 2:");
        instructions.AppendLine("- \"I'm so glad you came to visit me today.  I've been feeling a little lonely lately, but this rose really brightens my day.\" $s");
        instructions.AppendLine("### Example 3:");
        instructions.AppendLine("- \"Oh hi. I don't think I know you, and I'm rather busy. See you around.\"");
        if (ModEntry.Config.ApplyTranslation)
        {
            instructions.AppendLine($"Please express the line and any responses in {ModEntry.Language}.  Keep the responses natural and in character, but always use {ModEntry.Language} despite the fact this prompt is in English.");
        }
        if (!string.IsNullOrWhiteSpace(Llm.Instance.ExtraInstructions))
        {
            instructions.AppendLine(Llm.Instance.ExtraInstructions);
        }
        return instructions.ToString();
    }

    private string GetResponseStart()
    {
        return $"Here is the requested line for {Name} which fit the situation and {Name}'s personality and voice.";
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
        return Game1.getFarm().buildings.Where(x => !excludeTypes.Contains(x.buildingType.Value));
    }

    private IEnumerable<FarmAnimal> GetAnimals()
    {
        var animalsOnFarm = Game1.getFarm().getAllFarmAnimals();
        return animalsOnFarm;
    }

    private IEnumerable<DialogueValue> SelectDialogueSample()
    {
        // Pick 20 most relevant dialogue entries
        var orderedDialogue = Character.DialogueData
                    ?.AllEntries
                   .OrderBy(x => Context.CompareTo(x.Key));
        return orderedDialogue
                    ?.Where(x => x.Value != null)
                    .SelectMany(x => x.Value.AllValues)
                    .Take(20)
                    ?? Array.Empty<DialogueValue>();
    }

    private IDialogueValue? SelectExactDialogue()
    {
        return (Character.DialogueData
                    ?.AllEntries
                    .FirstOrDefault(x => x.Key == Context))?.Value;
                    
    }


}
