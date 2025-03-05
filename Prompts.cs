using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using ValleyTalk;
using StardewValley;
using StardewValley.GameData.Characters;

namespace StardewDialogue
{

public class Prompts
{
    [JsonIgnore]
    private readonly Dictionary<string,string> HistoryEvents = new Dictionary<string,string>()
    {
        { "cc_Bus", Util.GetString("cc_Bus_Repaired") },
        { "cc_Boulder", Util.GetString("cc_Boulder_Removed") },
        { "cc_Bridge", Util.GetString("cc_Bridge") },
        { "cc_Complete", Util.GetString("cc_Complete") },
        { "cc_Greenhouse", Util.GetString("cc_Greenhouse") },
        { "cc_Minecart", Util.GetString("cc_Minecart") },
        { "wonIceFishing", Util.GetString("wonIceFishing") },
        { "wonGrange", Util.GetString("wonGrange") },
        { "wonEggHunt", Util.GetString("wonEggHunt") }
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
        var gameSummaryDict = Util.ReadLocalisedJson<Dictionary<string,string>>("assets/bio/Stardew","txt");
        _stardewSummary = gameSummaryDict["Text"];
        var gameSummaryTranslations = Util.GetString("gameSummaryTranslations");
        if (!string.IsNullOrWhiteSpace(gameSummaryTranslations))
        {
            _stardewSummary += $"\n{gameSummaryTranslations}";
        }
    }


    [JsonIgnore]
    static string _stardewSummary;
    private string _system;
    [JsonIgnore]
    public string System
    { 
        get 
        { 
            if (_system == null)
            {
                _system = GetSystemPrompt();
            }
            return _system;
        }
        internal set => _system = value;
    }
    private string _gameConstantContext;
    [JsonIgnore]
    public string GameConstantContext 
    { 
        get 
        { 
            if (_gameConstantContext == null)
            {
                _gameConstantContext = GetGameConstantContext();
            }
            return _gameConstantContext;
        }
        internal set => _gameConstantContext = value; 
    }
    private string _npcConstantContext;
    public string NpcConstantContext 
    { 
        get 
        { 
            if (_npcConstantContext == null)
            {
                _npcConstantContext = GetNpcConstantContext();
            }
            return _npcConstantContext;
        }
        internal set => _npcConstantContext = value; 
    }
    private string _corePrompt;
    public string CorePrompt 
    { 
        get 
        { 
            if (_corePrompt == null)
            {
                _corePrompt = GetCorePrompt();
            }
            return _corePrompt;
        }
        internal set => _corePrompt = value; 
    }
    private string _command;
    public string Command 
    { 
        get 
        { 
            if (_command == null)
            {
                _command = GetCommand();
            }
            return _command;
        }
        internal set => _command = value;
    }
    private string _responseStart;
    public string ResponseStart 
    { 
        get 
        { 
            if (_responseStart == null)
            {
                _responseStart = GetResponseStart();
            }
            return _responseStart;
        }
        internal set => _responseStart = value; 
    }
    private string _instructions;
    public string Instructions 
    { 
        get 
        { 
            if (_instructions == null)
            {
                _instructions = GetInstructions();
            }
            return _instructions;
        }
        internal set => _instructions = value; 
    }

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
    [JsonIgnore]
    string giveGift;
    [JsonIgnore]
    public string GiveGift => giveGift;

    public Prompts(DialogueContext context, Character character)
    {

        npcData = character.StardewNpc.GetData();
        npcIsMale = npcData.Gender == StardewValley.Gender.Male;
        Context = context;
        Character = character;

        dialogueSample = SelectDialogueSample();
        exactLine = SelectExactDialogue();
        giveGift = context.CanGiveGift ? SelectGiftGiven() : string.Empty;
        allPreviousActivities = Game1.getPlayerOrEventFarmer().previousActiveDialogueEvents.First();
        previousActivites = allPreviousActivities.Where(x => HistoryEvents.ContainsKey(x.Key) && (x.Value < 112 || x.Value % 112 == 0)).ToList();

        Name = character.StardewNpc.displayName;
        Gender = character.Bio.Gender;
    }

    private string SelectGiftGiven()
    {
        // Check if Character is the players spouse.  If not, return null.  If so, still 80% chance return null.
        if (!Game1.getPlayerOrEventFarmer().friendshipData.TryGetValue(Character.Name, out Friendship friendship) || !friendship.IsMarried())
        {
            return null;
        }
        if (Game1.random.NextDouble() < 0.8)
        {
            return null;
        }
        // Get the gift the spouse is giving the Farmer
        var options = Character.DialogueData.AllEntries.SelectMany(x => x.Value.AllValues).SelectMany(x => x.Elements).SelectMany(x => x.GiftOptions).ToList();
        if (options.Count == 0)
        {
            return null;
        }
        return options[Game1.random.Next(options.Count)];
    }

    private string GetSystemPrompt()
    {
        var systemPrompt = new StringBuilder();
        systemPrompt.AppendLine(Util.GetString(Character,"systemPrompt"));
        if (ModEntry.Config.ApplyTranslation)
        {
            systemPrompt.AppendLine(Util.GetString(Character,"systemPromptTranslation", new { Language = ModEntry.Language }));
        }
        return systemPrompt.ToString();
    }

    private string GetGameConstantContext()
    {
        var gameConstantPrompt = new StringBuilder();
        gameConstantPrompt.AppendLine(Util.GetString(Character,"gameContext"));
        gameConstantPrompt.AppendLine($"##{Util.GetString(Character,"gameSummaryHeading")}");
        gameConstantPrompt.AppendLine(_stardewSummary);
        return gameConstantPrompt.ToString();
    }

    private string GetNpcConstantContext()
    {
        var npcConstantPrompt = new StringBuilder();
        var intro = Util.GetString(Character,"npcContextIntro", new { Name = Name });
        npcConstantPrompt.AppendLine(intro);
        if ((Character.Bio?.Biography ?? string.Empty).Length > 100)
        {
            npcConstantPrompt.AppendLine($"##{Util.GetString(Character,"npcContextBiographyHeading", new { Name = Name })}");
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

        prompt.AppendLine($"## {Util.GetString(Character,"coreInstructionHeading")}");
        prompt.AppendLine($"### {Util.GetString(Character,"coreContextHeading")}");
        prompt.AppendLine(Util.GetString(Character,"coreFarmerGender"));
        GetDateAndTime(prompt);
        GetWeather(prompt);
        GetOtherNpcs(prompt);
        Game1.getPlayerOrEventFarmer().friendshipData.TryGetValue(Character.Name, out Friendship friendship);
        if (friendship.IsMarried() || friendship.IsRoommate())
        {
            if (friendship.IsRoommate())
            {
                prompt.AppendLine(Util.GetString(Character,"coreRoommates", new { Name= Name }));
            }
            else
            {
                prompt.AppendLine(Util.GetString(Character,"coreMarried", new { Name= Name, Pronoun = npcIsMale ? "his" : "her" }));
                var dateNow = new StardewTime(Game1.Date,600);
                var whenMarried = dateNow.AddDays(-friendship.DaysMarried);
                prompt.AppendLine(Util.GetString(Character,"coreMarriedSince", new { Name= Name, RelativeDate = whenMarried.SinceDescription(dateNow) }));
                GetChildren(prompt, friendship);
            }

            GetSpouse(prompt);

            GetFarmContents(prompt);
            GetWealth(prompt);
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
        prompt.AppendLine(Util.GetString(Character,"coreGenderReferences"));
        GetPreoccupation(prompt);
        GetCurrentConversation(prompt);

        return prompt.ToString();
    }

    private void GetPreoccupation(StringBuilder prompt)
    {
        if (Game1.random.NextDouble() < 0.5 || Context.ChatHistory.Length > 0 ) return;

        var nPreoccupations = Character.PossiblePreoccupations.Count;
        string preoccupation;
        if (Game1.Date == Character.PreoccupationDate)
        {
            preoccupation = Character.Preoccupation;
        }
        else
        {
            preoccupation = Character.PossiblePreoccupations[Game1.random.Next(nPreoccupations)];
            Character.Preoccupation = preoccupation;
            Character.PreoccupationDate = Game1.Date;
        }
        
        prompt.AppendLine(Util.GetString(Character,"preoccupation", new { Name= Name, preoccupation= preoccupation }));
    }

    private void GetOtherNpcs(StringBuilder prompt)
    {
        var otherNpcs = Util.GetNearbyNpcs(Character.StardewNpc);
        if (otherNpcs.Any())
        {
            prompt.AppendLine($"### {Util.GetString(Character,"openNpcsHeading")}");
            prompt.AppendLine(Util.GetString(Character,"otherNpcsIntro", new { Name= Name }));
            foreach (var npc in otherNpcs)
            {
                prompt.AppendLine($"- {npc.displayName}");
            }
            prompt.AppendLine(Util.GetString(Character,"otherNpcsOutro"));
        }
    }

    private void GetCurrentConversation(StringBuilder prompt)
    {
        if (Context.ChatHistory.Length == 0) return;

        prompt.AppendLine($"###{Util.GetString(Character,"currentConversationHeading")}");
        prompt.AppendLine(Util.GetString(Character,"currentConversationIntro", new { Name= Name }));
        // Append each line from the chat history, labelling each one alternatively with the NPC's name or 'Farmer'
        for (int i = 0; i < Context.ChatHistory.Length; i++)
        {
            prompt.AppendLine(i % 2 == 0 ? $"- {Name}: {Context.ChatHistory[i]}" : $"- {Util.GetString(Character,"generalFarmerLabel")}: {Context.ChatHistory[i]}");
        }
    }

    private void GetSpecialRelationshipStatus(StringBuilder prompt, Friendship friendship)
    {
        if (friendship == null) return;
        
        if (friendship.IsDating())
        {
            var relationshipPublic = Context.Inlaw == null ? Util.GetString(Character,"specialRelationshipDatingPublic") : Util.GetString(Character,"specialRelationshipDatingDiscrete");
            var relationshipWord = RelationshipWord(Context.MaleFarmer, npcIsMale);
            prompt.AppendLine(Util.GetString(Character,"specialRelationshipDating", new { Name= Name, relationshipPublic= relationshipPublic, relationshipWord= relationshipWord }));
        }
        if (friendship.IsEngaged())
        {
            var daysToWedding = friendship.CountdownToWedding;
            prompt.AppendLine(Util.GetString(Character,"specialRelationshipEngaged", new { Name= Name, daysToWedding= daysToWedding }));
        }
        if (friendship.IsDivorced())
        {
            prompt.AppendLine(Util.GetString(Character,"specialRelationshipDivorced", new { Name= Name }));
        }
        if (friendship.ProposalRejected)
        {
            prompt.AppendLine(Util.GetString(Character,"specialRelationshipProposalRejected", new { Name= Name }));
        }
    }

    private void GetSpouse(StringBuilder prompt)
    {
        var spouses = Game1
                    .getPlayerOrEventFarmer()
                    .friendshipData
                    .FieldDict
                    .Where(x => x.Value.Value.IsMarried() && !x.Value.Value.IsRoommate())
                    .Select(x => x.Key);
        bool talkingToSpouse = spouses.Any(x => x == Name);
        spouses = spouses.Where(x => x != Name);
                    
        if (spouses.Any())
        {
            bool multipleOthers = spouses.Count() > 1;
            var spouseList = string.Join(", ", spouses);
            var nSpouses = spouses.Count();
            if (talkingToSpouse)
            {
                var otherSpousesList = multipleOthers ? $"{Util.GetString(Character,"spousesNOtherPeople", new { nSpouses= nSpouses })} {spouseList}":spouses.First();
                var otherSpousesReference = multipleOthers ? Util.GetString(Character,"spousesAllTheOthers") : spouses.First();
                prompt.AppendLine(Util.GetString(Character,"spousesMarriedToOthers", new { Name= Name, otherSpousesList= otherSpousesList, otherSpousesReference= otherSpousesReference }));
            }
            else
            {
                if (multipleOthers)
                {
                    prompt.AppendLine(Util.GetString(Character,"spousesMarriedToMany", new { nSpouses= nSpouses, spouseList= spouseList, Name= Name }));
                }
                else
                {
                    prompt.AppendLine(Util.GetString(Character,"spousesMarriedToOne", new { spouseList= spouseList, Name= Name }));
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
            var roommateList = multipleOthers ? $"{Util.GetString(Character,"spousesNOtherPeople", new { nSpouses= roommates.Count() })} {string.Join(", ", roommates)}":roommates.First();
            var roommateReference = multipleOthers ? Util.GetString(Character,"spouseRoommatesAllTheOthers") : roommates.First();
            if (talkingToRoommate)
            {
                prompt.AppendLine(Util.GetString(Character,"spouseRoommatesWithOthers", new { Name= Name, roommateList= roommateList, roommateReference= roommateReference }));
            }
            else
            {
                if (multipleOthers)
                {
                    prompt.AppendLine(Util.GetString(Character,"spouseRoommateWithMany", new { roommateList= roommateList }));
                }
                else
                {
                    prompt.AppendLine(Util.GetString(Character,"spouseRoommateWithOne", new { roommateList= roommateList }));
                }
            }
        }

        var engaged = Game1
                    .getPlayerOrEventFarmer()
                    .friendshipData
                    .FieldDict
                    .Where(x => x.Value.Value.IsEngaged());
        if (engaged.Any(x => x.Key != Name))
        {
            var engagedFirst = engaged.First();
            var engagedTo = Game1.characterData[engagedFirst.Key].DisplayName;
            var weddingDays = engagedFirst.Value.Value.CountdownToWedding;
            prompt.AppendLine(Util.GetString(Character,"spouseEngaged", new { engagedTo= engagedTo, weddingDays= weddingDays }));
        }
        var total = spouses.Count() + engaged.Count();
        if (total > 1 && !talkingToSpouse && !talkingToRoommate)
        {
            prompt.AppendLine(Util.GetString(Character,"spousePoly", new { Name= Name }));
        } else if (total >= 1 && (talkingToSpouse || talkingToRoommate))
        {
            prompt.AppendLine(Util.GetString(Character,"spousePolyView", new { Name= Name }));
        }
    }

    private void GetNonSpouseFriendshipLevel(StringBuilder prompt)
    {
        var isASingle = npcData.CanBeRomanced;
        var isChild = npcData.Age == NpcAge.Child;

        if (isASingle || Context.Hearts <= 6 || Context.Hearts == null)
        {
            int hearts = Context.Hearts ?? 0;
            if (hearts == -1)
                prompt.AppendLine(Util.GetString(Character,"nonSpouseFriendshipFirstConversation", new { Name= Name }));
            else if (hearts < 2) 
                prompt.AppendLine(Util.GetString(Character,"nonSpouseFreindshipStrangers", new { Name= Name }));
            else if (hearts < 4)
                prompt.AppendLine(Util.GetString(Character,"nonSpouseFriendshipAcquaintances", new { Name= Name }));
            else if (hearts < 6)
                prompt.AppendLine(Util.GetString(Character,"nonSpouseFriendshipFriends", new { Name= Name }));
            else if (hearts < 8)
                prompt.AppendLine(Util.GetString(Character,"nonSpouseFriendshipCloseFriends", new { Name= Name }));
            else if (hearts <= 10)
                prompt.AppendLine(Util.GetString(Character,"nonSpouseFriendshipWantToDate", new { Name= Name }));
            else if (hearts <= 14)
                prompt.AppendLine(Util.GetString(Character,"nonSpouseFriendshipIntimate", new { Name= Name })); // Backup = should never be called
            else
                throw new InvalidDataException("Invalid heart level.");
        }
        else
        {
            if (Context.Hearts <= 8 && !isChild)
            {
                prompt.AppendLine(Util.GetString(Character,"nonSpouseFriendshipNonSingleAdult8", new { Name= Name }));
            }
            else if (isChild)
            {
                prompt.AppendLine(Util.GetString(Character,"nonSpouseFriendshipChild8Plus", new { Name= Name }));
            }
            else
            {
                prompt.AppendLine(Util.GetString(Character,"nonSpouseFriendshipNonSingleAdult10", new { Name= Name }));
            }
        }
    }

    private void GetSpouseAction(StringBuilder prompt)
    {
        if (Context.SpouseAct == null) return;

        if (Context.SpouseAct == SpouseAction.funLeave)
            prompt.AppendLine(Util.GetString(Character,"spouseActionFunLeave", new { Name= Name }));
        else if (Context.SpouseAct == SpouseAction.jobLeave)
            prompt.AppendLine(Util.GetString(Character,"spouseActionJobLeave", new { Name= Name }));
        else if (Context.SpouseAct == SpouseAction.patio)
            prompt.AppendLine(Util.GetString(Character,"spouseActionPatio", new { Name= Name }));
        else if (Context.SpouseAct == SpouseAction.funReturn)
            prompt.AppendLine(Util.GetString(Character,"spouseActionFunReturn", new { Name= Name }));
        else if (Context.SpouseAct == SpouseAction.jobReturn)
            prompt.AppendLine(Util.GetString(Character,"spouseActionJobReturn", new { Name= Name }));
        else if (Context.SpouseAct == SpouseAction.spouseRoom)
            prompt.AppendLine(Util.GetString(Character,"spouseActionSpouseRoom", new { Name= Name }));
        else
            prompt.AppendLine("");
    }

    private void GetGift(StringBuilder prompt)
    {
        if (Context.Accept != null)
        {
        string giftName = Context.Accept.DisplayName;
        prompt.AppendLine(Util.GetString(Character,"giftIntro", new { Name= Name, giftName= giftName }));
        switch (Context.GiftTaste)
        {
            //TODO: Correct the cases
            case 0:
                prompt.AppendLine(Util.GetString(Character,"giftLoved", new { Name= Name }));
                break;
            case 2:
                prompt.AppendLine(Util.GetString(Character,"giftLiked", new { Name= Name }));
                break;
            case 4:
                prompt.AppendLine(Util.GetString(Character,"giftDislike", new { Name= Name }));
                break;
            case 6:
                prompt.AppendLine(Util.GetString(Character,"giftHate", new { Name= Name }));
                break;
            default:
                prompt.AppendLine(Util.GetString(Character,"giftNeutral", new { Name= Name }));
                break;
        }
        prompt.AppendLine(Util.GetString(Character,"giftMustIncludeReaction", new { Name= Name }));
        if (Context.Birthday)
        {
            prompt.AppendLine(Util.GetString(Character,"giftBirthday", new { Name= Name }));
        }
        prompt.AppendLine(Util.GetString(Character,"giftOutro"));
        }
        else if (!string.IsNullOrEmpty(giveGift))
        {
            string giftName = giveGift;
            if (Game1.objectData.ContainsKey(giveGift))
            {
                giftName = Game1.objectData[giveGift].DisplayName;
                giftName = LoadLocalised(giftName);
            }
            prompt.AppendLine(Util.GetString(Character,"giftGiving", new { Name= Name, GiftName= giftName }));
        }
    }

    private void GetSpecialDatesAndBirthday(StringBuilder prompt)
    {
        if (Context.DayOfSeason == null) return;

        if (Context.Season == Season.Spring && Context.DayOfSeason == 1)
            prompt.AppendLine(Util.GetString(Character,"specialDatesSpring1"));
        else if (Context.Season == Season.Spring && Context.DayOfSeason == 12)
            prompt.AppendLine(Util.GetString(Character,"specialDatesSpring12"));
        else if (Context.Season == Season.Spring && Context.DayOfSeason == 23)
            prompt.AppendLine(Util.GetString(Character,"specialDatesSpring23"));
        else if (Context.Season == Season.Summer && Context.DayOfSeason == 1)
            prompt.AppendLine(Util.GetString(Character,"specialDatesSummer1"));
        else if (Context.Season == Season.Summer && Context.DayOfSeason == 10)
            prompt.AppendLine(Util.GetString(Character,"specialDatesSummer10"));
        else if (Context.Season == Season.Summer && Context.DayOfSeason == 27)
            prompt.AppendLine(Util.GetString(Character,"specialDatesSummer27"));
        else if (Context.Season == Season.Summer && Context.DayOfSeason == 28)
            prompt.AppendLine(Util.GetString(Character,"specialDatesSummer28"));
        else if (Context.Season == Season.Fall && Context.DayOfSeason == 1)
            prompt.AppendLine(Util.GetString(Character,"specialDatesFall1"));
        else if (Context.Season == Season.Fall && Context.DayOfSeason == 15)
            prompt.AppendLine(Util.GetString(Character,"specialDatesFall15"));
        else if (Context.Season == Season.Fall && Context.DayOfSeason == 26)
            prompt.AppendLine(Util.GetString(Character,"specialDatesFall26"));
        else if (Context.Season == Season.Winter && Context.DayOfSeason == 1)
            prompt.AppendLine(Util.GetString(Character,"specialDatesWInter1"));
        else if (Context.Season == Season.Winter && Context.DayOfSeason == 7)
            prompt.AppendLine(Util.GetString(Character,"specialDatesWinter7"));
        else if (Context.Season == Season.Winter && Context.DayOfSeason == 24)
            prompt.AppendLine(Util.GetString(Character,"specialDatesWinter24"));
        else if (Context.Season == Season.Winter && Context.DayOfSeason == 28)
            prompt.AppendLine(Util.GetString(Character,"specialDatesWinter28"));
        var stardewBioData = Character.StardewNpc.GetData();
        if (
            string.Equals(
                Context.Season.Value.ToString(),
                stardewBioData.BirthSeason.ToString(),
                StringComparison.InvariantCultureIgnoreCase
            ) && Context.DayOfSeason == stardewBioData.BirthDay)
        {
            prompt.AppendLine(Util.GetString(Character,"specialDatesBirthday", new { Name= Name }));
        }
    }

    private void GetRecentEvents(StringBuilder prompt)
    {
        var eventSection = new StringBuilder();
        foreach (var activity in allPreviousActivities.Where(x => x.Value < 7))
        {
            string theLine = "";
            if (activity.Key == "cc_Boulder")
                theLine = Util.GetString(Character,"recentEventsBoulder");
            else if (activity.Key == "cc_Bridge")
                theLine = Util.GetString(Character,"recentEventsQuarryBridge");
            else if (activity.Key == "cc_Bus")
                theLine = Util.GetString(Character,"recentEventsBus");
            else if (activity.Key == "cc_Greenhouse")
                theLine = Util.GetString(Character,"recentEventsGreenhouse");
            else if (activity.Key == "cc_Minecart")
                theLine = Util.GetString(Character,"recentEventsMinecarts");
            else if (activity.Key == "cc_Complete")
                theLine = Util.GetString(Character,"recentEventsCommunityCenter");
            else if (activity.Key == "movieTheater")
                theLine = Util.GetString(Character,"recentEventsMovieTheatre");
            else if (activity.Key == "pamHouseUpgrade")
                theLine = Util.GetString(Character,"recentEventsPamHouse");
            else if (activity.Key == "pamHouseUpgradeAnonymous")
                theLine = Util.GetString(Character,"recentEventsPamHouseAnonymous");
            else if (activity.Key == "jojaMartStruckByLightning")
                theLine = Util.GetString(Character,"recentEventsJojaLightning");
            else if (activity.Key == "babyBoy")
                theLine = Util.GetString(Character,"recentEventsBabyBoy");
            else if (activity.Key == "babyGirl")
                theLine = Util.GetString(Character,"recentEventsBabyGirl");
            else if (activity.Key == "wedding")
                theLine = Util.GetString(Character,"recentEventsMarried");
            else if (activity.Key == "luauBest")
                theLine = Util.GetString(Character,"recentEventsLuauBest");
            else if (activity.Key == "luauShorts")
                theLine = Util.GetString(Character,"recentEventsLuauShorts");
            else if (activity.Key == "luauPoisoned")
                theLine = Util.GetString(Character,"recentEventsLuauPoisoned");
            else if (activity.Key == "Characters_MovieInvite_Invited")
                theLine = Util.GetString(Character,"recentEventsMovieInvited", new { Name= Name });
            else if (activity.Key == "DumpsterDiveComment")
                theLine = Util.GetString(Character,"recentEventsDumpsterDive", new { Name= Name });
            else if (activity.Key == "GreenRainFinished")
                theLine = Util.GetString(Character,"recentEventsGreenRain");
            else
                theLine = "";
            if (!string.IsNullOrWhiteSpace(theLine))
            {
                eventSection.AppendLine(theLine);
            }
        }
        if (eventSection.Length > 0)
        {
            prompt.AppendLine($"## {Util.GetString(Character,"recentEventsHeading")}");
            prompt.AppendLine(Util.GetString(Character,"recentEventsIntro"));
            prompt.AppendLine(eventSection.ToString());
        }
    }

    private void GetLocation(StringBuilder prompt)
    {
        if (Context.Location == null && Character.StardewNpc.DirectionsToNewLocation == null) return;
        
        var bedTile = npcData.Home[0].Tile;
        if (Context.Location == npcData.Home[0].Location && Context.Inlaw != Name)
        {
            if (Character.StardewNpc.TilePoint == bedTile && Character.Bio.HomeLocationBed && !Llm.Instance.IsHighlySensoredModel && StardewModdingAPI.Context.IsMainPlayer)
            {
                prompt.AppendLine(Util.GetString(Character,"locationBed", new { Name= Name }));
            }
            else
            {
                var mayBeInShop = Context.Location.IndexOf("Shop", StringComparison.OrdinalIgnoreCase) >= 0
                    || Context.Location.IndexOf("Science", StringComparison.OrdinalIgnoreCase) >= 0;
                var inShopString = mayBeInShop ? Util.GetString(Character,"locationAtHomeOrShop") : "";
                prompt.AppendLine(Util.GetString(Character,"locationAtHome", new { Name= Name, inShopString= inShopString }));
            }
        }
        else if (Context.Location != null)
        {
            var locationName = GetLocationDisplayNameIfAvailable(Context.Location);
            if (Context.Location == "Town")
                prompt.Append(Util.GetString(Character,"locationTown", new { Name= Name }));
            else if (Context.Location == "Beach")
                prompt.Append(Util.GetString(Character,"locationBeach", new { Name= Name }));
            else if (Context.Location == "Desert")
                prompt.Append(Util.GetString(Character,"locationDesert", new { Name= Name }));
            else if (Context.Location == "BusStop")
                prompt.Append(Util.GetString(Character,"locationBusStop", new { Name= Name }));
            else if (Context.Location == "Railroad")
                prompt.Append(Util.GetString(Character,"locationRailroad", new { Name= Name }));
            else if (Context.Location == "Saloon")
                prompt.Append($"{Util.GetString(Character,"locationSaloon", new { Name= Name })}{((npcData.Age == NpcAge.Child || Character.Name == "Emily") ? "" : Util.GetString(Character,"locationSaloonDrunk"))}");
            else if (Context.Location == "SeedShop")
                prompt.Append(Util.GetString(Character,"locationPierres", new { Name= Name }));
            else if (Context.Location == "JojaMart")
                prompt.Append(Util.GetString(Character,"locationJojaMart", new { Name= Name }));
            else if (Context.Location == "Resort_Chair")
                prompt.Append(Util.GetString(Character,"locationResortChair", new { Name= Name }));
            else if (Context.Location == "Resort_Towel" || Context.Location == "Resort_Towel_2" || Context.Location == "Resort_Towel_3")
                prompt.Append(Util.GetString(Character,"locationResortTowel", new { Name= Name }));
            else if (Context.Location == "Resort_Umbrella" || Context.Location == "Resort_Umbrella_2" || Context.Location == "Resort_Umbrella_3")
                prompt.Append(Util.GetString(Character,"locationResortUmbrella", new { Name= Name }));
            else if (Context.Location == "Resort_Bar")
                prompt.Append($"{Util.GetString(Character,"locationResortBar", new { Name= Name })}{((npcData.Age == NpcAge.Child) ? "" : Util.GetString(Character,"locationSaloonDrunk"))}.");
            else if (Context.Location == "Resort_Entering")
                prompt.Append(Util.GetString(Character,"locationResortEntering", new { Name= Name }));
            else if (Context.Location == "Resort_Leaving")
                prompt.Append(Util.GetString(Character,"locationResortLeaving", new { Name= Name }));
            else if (Context.Location == "Resort_Shore" || Context.Location == "Resort_Shore_2")
                prompt.Append(Util.GetString(Character,"locationResortShore", new { Name= Name }));
            else if (Context.Location == "Resort_Wander")
                prompt.Append(Util.GetString(Character,"locationResortWander", new { Name= Name }));
            else if (Context.Location == "Resort" || Context.Location == "Resort_2")
                prompt.Append(Util.GetString(Character,"locationResort", new { Name= Name }));
            else if (Context.Location == "FarmHouse")
                prompt.Append(Util.GetString(Character,"locationFarmHouse", new { Name= Name }));
            else if (Context.Location == "Farm")
                prompt.Append(Util.GetString(Character,"locationFarm", new { Name= Name }));
            else if (locationName.Length > 2)
                prompt.Append(Util.GetString("locationGeneric", new {Name = Name, Location = locationName}));
            else
                prompt.Append(string.Empty);
            prompt.AppendLine(Util.GetString(Character,"locationOutro"));
        }

        string destination = string.Empty;
        if (Character.StardewNpc.DirectionsToNewLocation != null && Context.Location != Character.StardewNpc.DirectionsToNewLocation.targetLocationName)
        {
            destination = Character.StardewNpc.DirectionsToNewLocation.targetLocationName;
            var destinationName = GetLocationDisplayNameIfAvailable(destination);
            prompt.AppendLine(Util.GetString(Character,"locationTravelling", new { Name= Name, destination= destinationName }));
        }

        var schedule = Character.StardewNpc.Schedule;
        if (schedule != null)
        {
            var remainderOfSchedule = schedule.Where(x => x.Key > Game1.timeOfDay);
            var remainingLocations = remainderOfSchedule
                    .Select(x => x.Value.targetLocationName)
                    .Distinct()
                    .Where(x => x != Context.Location && x!= "Town" && x != Character.StardewNpc.DefaultMap && x != destination && !string.IsNullOrWhiteSpace(x));
            if (remainingLocations.Any())
            {
                var displayNames = remainingLocations.Select(x => GetLocationDisplayNameIfAvailable(x));
                prompt.AppendLine(Util.GetString(Character,"locationFuturePlans", new { Name= Name, Locations= string.Join(", ", displayNames) }));
            }
        }
    }

    private string GetLocationDisplayNameIfAvailable(string location)
    {
        if (Game1.locationData.TryGetValue(location, out StardewValley.GameData.Locations.LocationData locationData))
        {
            return LoadLocalised(locationData.DisplayName);
        }
        return location;
    }

    private void GetMarriageFeelings(StringBuilder prompt)
    {
        var IsRoommate = Game1.getPlayerOrEventFarmer().friendshipData[Character.Name].IsRoommate();
        var marriageOrRoommate = IsRoommate ? Util.GetString(Character,"generalBeingRoommates") : Util.GetString(Character,"generalTheMarriage");
        if (Context.Hearts > 12)
        {
            prompt.AppendLine(Util.GetString(Character,"marriageSentimentGood", new { Name= Name, marriageOrRoommate= marriageOrRoommate }));
        }
        else if (Context.Hearts < 10)
        {
            prompt.AppendLine(Util.GetString(Character,"marriageSentimentBad", new { Name= Name, marriageOrRoommate= marriageOrRoommate }));
        }
        else
        {
            prompt.AppendLine(Util.GetString(Character,"marriageSentimentNeutral", new { Name= Name, marriageOrRoommate= marriageOrRoommate }));
        }
    }

    private void GetWealth(StringBuilder prompt)
    {
        var wealth = Game1.getPlayerOrEventFarmer()._money;
        if (wealth < 1000)
        {
            prompt.AppendLine(Util.GetString(Character,"wealthPoor", new { wealth= wealth, Name = Name }));
        }
        else if (wealth < 10000)
        {
            prompt.AppendLine(Util.GetString(Character,"wealthMiddle", new { wealth= wealth, Name = Name }));
        }
        else if (wealth < 100000)
        {
            prompt.AppendLine(Util.GetString(Character,"wealthRich", new { wealth= wealth, Name = Name }));
        }
        else
        {
            prompt.AppendLine(Util.GetString(Character,"wealthVeryRich", new { wealth= wealth, Name = Name }));
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
            if (Game1.petData.TryGetValue(pet?.petType.Value, out StardewValley.GameData.Pets.PetData petData))
            {
                var petType = petData.DisplayName;
                petType = LoadLocalised(petType);
                prompt.AppendLine(Util.GetString(Character,"farmContentsPet", new { petType= petType, Name= pet.Name }));
            }
            else
            {
                var petType = pet.petType.Value;
                prompt.AppendLine(Util.GetString(Character,"farmContentsPet", new { petType= petType, Name= pet.Name }));
            }
        }
        else
        {
            prompt.AppendLine(Util.GetString(Character,"farmContentsNoPets"));
        }
    }

    private void GetFarmCrops(StringBuilder prompt)
    {
        var cropData = Game1.objectData;
        var allCrops = GetCrops();
        if (allCrops.Any())
        {
            prompt.AppendLine(Util.GetString(Character,"farmCropsIntro"));
            foreach (var crop in allCrops.GroupBy(x => x.indexOfHarvest.Value))
            {
                string thisName = crop.Key;
                if (cropData.TryGetValue(crop.Key, out var thisDetails))
                {
                    thisName = thisDetails.DisplayName;
                    thisName = LoadLocalised(thisName);
                }
                prompt.AppendLine($"- {crop.Count()} {thisName}");
                
                var ripe = crop.Count(x => x.fullyGrown.Value);
                if (ripe > 0)
                {
                    prompt.AppendLine(Util.GetString(Character,"farmCropsReadyForHarvest", new { ripe = ripe }));
                }
                else
                {
                    prompt.AppendLine(Util.GetString(Character,"farmCropsNotReady"));
                }
            }
        }
        else
        {
            prompt.AppendLine(Util.GetString(Character,"farmCropsNone"));
        }
    }



    private void GetFarmAnimals(StringBuilder prompt)
    {
        var allAnimals = GetAnimals();

        if (allAnimals.Any())
        {
            prompt.AppendLine(Util.GetString(Character,"farmAnimalsIntro"));
            foreach (var animal in allAnimals.GroupBy(x => x.type))
            {
                prompt.AppendLine($"- {animal.Count()} {animal.First().displayType}{(animal.Count() > 1 ? "s" : "")}");
            }
        }
        else
        {
            prompt.AppendLine(Util.GetString(Character,"farmAnimalsNone"));
        }
    }

    private void GetFarmBuildings(StringBuilder prompt)
    {
        var allBuildings = GetBuildings();

        if (allBuildings.Any())
        {
            prompt.AppendLine(Util.GetString(Character,"farmBuildingsIntro"));
            var completedBuildings = allBuildings.Where(x => x.daysOfConstructionLeft.Value == 0 && x.buildingType.Value != "Greenhouse");
            foreach (var building in completedBuildings.GroupBy(x => x.buildingType))
            {
                var thisName = building.First().buildingType.Value;
                var translation = Game1.content.LoadString($"Strings//Buildings:{thisName}_Name");
                prompt.AppendLine($"- {building.Count()} {translation}");
            }
            var greenhouse = allBuildings.FirstOrDefault(x => x.buildingType.Value == "Greenhouse");
            if (greenhouse != null)
            {
                if (greenhouse.indoors.Value == null)
                {
                    prompt.AppendLine($"- {Util.GetString(Character,"farmBuildingsRuinedGreenhouse")}");
                }
                else
                {
                    prompt.AppendLine($"- {Util.GetString(Character,"farmBuildingsRepairedGreenhouse")}");
                }
            }
            if (allBuildings.Any(x => x.daysOfConstructionLeft.Value > 0))
            {
                var underConstruction = allBuildings.First(x => x.daysOfConstructionLeft.Value > 0);
                prompt.AppendLine($"- {Util.GetString(Character,"farmBuildingsConstruction", new { buildingType= underConstruction.buildingType.Value, daysOfConstructionLeft= underConstruction.daysOfConstructionLeft })}");
            }
        }
        else
        {
            prompt.AppendLine(Util.GetString(Character,"farmBuildingsNone"));
        }
    }

    private void GetChildren(StringBuilder prompt, Friendship friendship)
    {
        if (Context.Children.Count == 0)
        {
            prompt.AppendLine(Util.GetString(Character,"childrenNone", new { Name= Name }));
        }
        else
        {
            if (Context.Children.Count > 1)
            {
                var count = Context.Children.Count;
                prompt.AppendLine(Util.GetString(Character,"childrenMultiple", new { Name= Name, count= count }));
            }
            else
            {
                prompt.AppendLine(Util.GetString(Character,"childrenSingle", new { Name= Name }));
            }
            prompt.AppendLine();
            foreach (var child in Context.Children)
            {
                prompt.AppendLine($"- {Util.GetString(Character,child.IsMale ? "childrenDescriptionBoy" : "childDescriptionGirl", new { Name= child.Name, Age= child.Age })}");
            }
        }
        if (friendship.DaysUntilBirthing > 0)
        {
            var daysUntilBirth = friendship.DaysUntilBirthing;
            prompt.AppendLine(Util.GetString(Character,"childrenPregnant", new { Name= Name, daysUntilBirth= daysUntilBirth }));
        }
    }

    private void GetWeather(StringBuilder prompt)
    {
        if (Context.Weather == null || !Context.Weather.Any()) return;

        if (Context.Weather.Contains("lightning"))
        {
            prompt.AppendLine(Util.GetString(Character,"weatherLightning"));
        }
        else if (Context.Weather.Contains("green rain"))
        {
            prompt.AppendLine(Util.GetString(Character,"weatherGreenRain"));
        }
        else if (Context.Weather.Contains("snow"))
        {
            prompt.AppendLine(Util.GetString(Character,"weatherSnow"));
        }
        else if (Context.Weather.Contains("rain"))
        {
            prompt.AppendLine(Util.GetString(Character,"weatherRain"));
        }
    }

    private void GetDateAndTime(StringBuilder prompt)
    {
        if (Context.DayOfSeason != null && Context.Season != null)
        {
            prompt.AppendLine(Util.GetString(Character,"dateTimeDayOfSeason", new { DayOfSeason= Context.DayOfSeason, Season= Game1.CurrentSeasonDisplayName }));
        }
        if (Context.TimeOfDay != null)
        {
            prompt.AppendLine(Util.GetString(Character,"dateTimeTimeOfDay", new { TimeOfDay= Context.TimeOfDay }));
            if (Context.TimeOfDay == "early morning")
            {
                prompt.AppendLine(Util.GetString(Character,"dateTimeEarlyMorningNormal"));
            }
        }
        if (Context.Year == 1)
        {
            prompt.AppendLine(Util.GetString(Character,"dateTimeNewThisYear"));
        }

    }

    private void GetEventHistory(StringBuilder prompt)
    {
        var timeNow = new StardewTime(Game1.Date, Game1.timeOfDay);
        var fullHistory = Character.EventHistory.Concat(previousActivites.Select(x => MakeActivityHistory(x)));

        if (fullHistory.Any())
        {
            prompt.AppendLine($"##{Util.GetString(Character,"eventHistoryHeading")}");
            prompt.AppendLine(Util.GetString(Character,"eventHistoryIntro", new { Name= Name }));
            prompt.AppendLine(Util.GetString(Character,"eventHistorySubheading"));
            var orderedFullHistory = fullHistory.OrderBy(x => x.Item1).ToList();
            var historySample = orderedFullHistory.Skip(Math.Max(0, orderedFullHistory.Count - 30));
            
            foreach (var eventHistory in historySample)
            {
                prompt.AppendLine($"- {eventHistory.Item1.SinceDescription(timeNow)}: {eventHistory.Item2.Format(Name)}");
            }
        }
    }

    private Tuple<StardewTime, IHistory> MakeActivityHistory(KeyValuePair<string, int> x)
    {
        var timeNow = new StardewTime(Game1.year, Game1.season, Game1.dayOfMonth, Game1.timeOfDay);
        var targetDate = timeNow.AddDays(-x.Value);
        return new Tuple<StardewTime, IHistory>(targetDate, new ActivityHistory(x.Key));
    }

    private void GetSampleDialogue(StringBuilder prompt)
    {
        if (!dialogueSample.Any()) return;

        prompt.AppendLine($"##{Util.GetString(Character,"sampleDialogueHeading", new { Name= Name })}");
        prompt.AppendLine(Util.GetString(Character,"sampleDialogueIntro", new { Name= Name }));
        foreach (var dialogue in dialogueSample)
        {
            prompt.AppendLine($"- {dialogue.Value}");
        }
    }

    private void GetGameState(StringBuilder prompt)
    {
        prompt.AppendLine($"## {Util.GetString(Character,"gameStateHeading")}");
        if (allPreviousActivities.ContainsKey("cc_Complete"))
        {
            prompt.AppendLine(Util.GetString(Character,"gameStateCommunityCenterYes"));
        }
        else
        {
            prompt.AppendLine(Util.GetString(Character,"gameStateCommunityCenterNo"));
        }
        if (allPreviousActivities.ContainsKey("cc_Bus"))
        {
            prompt.AppendLine(Util.GetString(Character,"gameStateBusYes"));
        }
        else
        {
            prompt.AppendLine(Util.GetString(Character,"gameStateBusNo"));
        }
        if (allPreviousActivities.ContainsKey("cc_Bridge"))
        {
            prompt.AppendLine(Util.GetString(Character,"gameStateQuarryBridgeYes"));
        }
        else
        {
            prompt.AppendLine(Util.GetString(Character,"gameStateQuarryBridgeNo"));
        }
        if (allPreviousActivities.ContainsKey("cc_Minecart"))
        {
            prompt.AppendLine(Util.GetString(Character,"gameStateMinecartYes"));
        }
        else
        {
            prompt.AppendLine(Util.GetString(Character,"gameStateMinecartNo"));
        }
        if (allPreviousActivities.ContainsKey("cc_Boulder"))
        {
            prompt.AppendLine(Util.GetString(Character,"gameStateBoulderYes"));
        }
        else
        {
            prompt.AppendLine(Util.GetString(Character,"gameStateBoulderNo"));
        }
        if (Game1.year == 1)
        {
            prompt.AppendLine(Util.GetString(Character,"gameStateKentNo"));
        }
        else
        {
            prompt.AppendLine(Util.GetString(Character,"gameStateKentYes"));
        }
    }

    private string GetCommand()
    {
        var commandPrompt = new StringBuilder();
        commandPrompt.AppendLine($"##{Util.GetString(Character,"commandHeading")}");
        commandPrompt.AppendLine(Util.GetString(Character,"commandIntro", new { Name= Name }));
        if (!string.IsNullOrWhiteSpace(Context.ScheduleLine) && Context.ChatHistory.Length == 0)
        {
            commandPrompt.AppendLine();
            commandPrompt.AppendLine(Util.GetString(Character,"commandReplaceSchedule", new { ScheduleLine= Context.ScheduleLine }));
        }
        return commandPrompt.ToString();
    }

    private string GetInstructions()
    {
        var instructions = new StringBuilder();
        instructions.AppendLine($"##{Util.GetString(Character,"instructionsHeading")}");
        instructions.AppendLine(Util.GetString(Character,"instructionsIntro", new { Name= Name }));
        if (dialogueSample.Any())
        {
            instructions.AppendLine(Util.GetString(Character,"instructionsSampleDialogue", new { Name= Name }));
        }
        instructions.AppendLine(Util.GetString(Character,"instructionsFarmersName"));
        instructions.AppendLine(Util.GetString(Character,"instructionsBreaks"));
        if (!Character.Bio.ExtraPortraits.ContainsKey("!"))
        {
            var extraPortraits = new StringBuilder();
            foreach (var portrait in Character.Bio.ExtraPortraits)
            {
                extraPortraits.Append(Util.GetString(Character,"instructionsExtraPortraitLine", new { Key= portrait.Key, Value= portrait.Value }));
            }
            instructions.AppendLine(Util.GetString(Character,"instructionsEmotion", new { extraPortraits= extraPortraits }));
        }
        instructions.AppendLine(Util.GetString(Character,"instructionsSingleLine"));
        instructions.AppendLine(Util.GetString(Character,"instructionsResponses", new { Name= Name }));
        if (ModEntry.Config.ApplyTranslation)
        {
            instructions.AppendLine(Util.GetString(Character,"instructionsTranslate", new { Language= ModEntry.Language }));
        }
        if (!string.IsNullOrWhiteSpace(Llm.Instance.ExtraInstructions))
        {
            instructions.AppendLine(Llm.Instance.ExtraInstructions);
        }
        return instructions.ToString();
    }

    private string GetResponseStart()
    {
        return Util.GetString(Character,"responseStart", new { Name= Name });
    }

    private string RelationshipWord(bool maleFarmer, bool npcIsMale)
    {
        return maleFarmer ? (npcIsMale ? Util.GetString(Character,"generalGayMale") : Util.GetString(Character,"generalHeterosexual")) : (npcIsMale ? Util.GetString(Character,"generalHeterosexual") : Util.GetString(Character,"generalLesbian"));
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

    private static string LoadLocalised(string thisName)
    {
        if (string.IsNullOrWhiteSpace(thisName)) return string.Empty;
        if (thisName.StartsWith("[LocalizedText ", StringComparison.InvariantCultureIgnoreCase))
        {
            thisName = thisName.Substring(15, thisName.Length - 16);
            var translation = Game1.content.LoadString(thisName);

            if (translation != null)
            {
                thisName = translation;
            }
        }

        return thisName;
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

    private IDialogueValue SelectExactDialogue()
    {
        return (Character.DialogueData
                    ?.AllEntries
                    .FirstOrDefault(x => x.Key == Context))?.Value;
                    
    }


}
}
