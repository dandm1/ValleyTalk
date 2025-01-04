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
        { "cc_Bus", _translationHelper.Get("cc_Bus_Repaired") },
        { "cc_Boulder", _translationHelper.Get("cc_Boulder_Removed") },
        { "cc_Bridge", _translationHelper.Get("cc_Bridge") },
        { "cc_Complete", _translationHelper.Get("cc_Complete") },
        { "cc_Greenhouse", _translationHelper.Get("cc_Greenhouse") },
        { "cc_Minecart", _translationHelper.Get("cc_Minecart") },
        { "wonIceFishing", _translationHelper.Get("wonIceFishing") },
        { "wonGrange", _translationHelper.Get("wonGrange") },
        { "wonEggHunt", _translationHelper.Get("wonEggHunt") }
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

    private static StardewModdingAPI.ITranslationHelper _translationHelper = ModEntry.SHelper.Translation;
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

        Name = character.StardewNpc.displayName;
        Gender = character.Bio.Gender;
    }

    private string GetSystemPrompt()
    {
        var systemPrompt = new StringBuilder();
        systemPrompt.AppendLine(_translationHelper.Get("systemPrompt"));
        if (ModEntry.Config.ApplyTranslation)
        {
            systemPrompt.AppendLine(_translationHelper.Get("systemPromptTranslation", new { Language = ModEntry.Language }));
        }
        return systemPrompt.ToString();
    }

    private string GetGameConstantContext()
    {
        var gameConstantPrompt = new StringBuilder();
        gameConstantPrompt.AppendLine(_translationHelper.Get("gameContext"));
        gameConstantPrompt.AppendLine($"##{_translationHelper.Get("gameSummaryHeading")}");
        gameConstantPrompt.AppendLine(_stardewSummary);
        return gameConstantPrompt.ToString();
    }

    private string GetNpcConstantContext()
    {
        var npcConstantPrompt = new StringBuilder();
        npcConstantPrompt.AppendLine(_translationHelper.Get("npcContextIntro", new { Name = Name }));
        if ((Character.Bio?.Biography ?? string.Empty).Length > 100)
        {
            npcConstantPrompt.AppendLine($"##{_translationHelper.Get("npcContextBiographyHeading", new { Name = Name })}");
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

        prompt.AppendLine($"## {_translationHelper.Get("coreInstructionHeading")}");
        prompt.AppendLine($"### {_translationHelper.Get("coreContextHeading")}");
        if (Context.MaleFarmer)
        {
            prompt.AppendLine(_translationHelper.Get("coreMaleFarmer"));
        }
        else
        {
            prompt.AppendLine(_translationHelper.Get("coreFemaleFarmer"));
        }
        GetDateAndTime(prompt);
        GetWeather(prompt);
        GetOtherNpcs(prompt);
        Game1.getPlayerOrEventFarmer().friendshipData.TryGetValue(Name, out Friendship friendship);
        if (friendship.IsMarried() || friendship.IsRoommate())
        {
            if (friendship.IsRoommate())
            {
                prompt.AppendLine(_translationHelper.Get("coreRoommates", new { Name= Name }));
            }
            else
            {
                prompt.AppendLine(_translationHelper.Get("coreMarried", new { Name= Name, Pronoun = npcIsMale ? "his" : "her" }));

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
            prompt.AppendLine(_translationHelper.Get("coreMaleReferences"));
        }
        else
        {
            prompt.AppendLine(_translationHelper.Get("coreFemaleReferences"));
        }
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
        
        prompt.AppendLine(_translationHelper.Get("preoccupation", new { Name= Name, preoccupation= preoccupation }));
    }

    private void GetOtherNpcs(StringBuilder prompt)
    {
        var otherNpcs = Util.GetNearbyNpcs(Character.StardewNpc);
        if (otherNpcs.Any())
        {
            prompt.AppendLine($"### {_translationHelper.Get("openNpcsHeading")}");
            prompt.AppendLine(_translationHelper.Get("otherNpcsIntro", new { Name= Name }));
            foreach (var npc in otherNpcs)
            {
                prompt.AppendLine($"- {npc.displayName}");
            }
            prompt.AppendLine(_translationHelper.Get("otherNpcsOutro"));
        }
    }

    private void GetCurrentConversation(StringBuilder prompt)
    {
        if (Context.ChatHistory.Length == 0) return;

        prompt.AppendLine($"###{_translationHelper.Get("currentConversationHeading")}");
        prompt.AppendLine(_translationHelper.Get("currentConversationIntro", new { Name= Name }));
        // Append each line from the chat history, labelling each one alternatively with the NPC's name or 'Farmer'
        for (int i = 0; i < Context.ChatHistory.Length; i++)
        {
            prompt.AppendLine(i % 2 == 0 ? $"- {Name}: {Context.ChatHistory[i]}" : $"- {_translationHelper.Get("generalFarmerLabel")}: {Context.ChatHistory[i]}");
        }
    }

    private void GetSpecialRelationshipStatus(StringBuilder prompt, Friendship friendship)
    {
        if (friendship == null) return;
        
        if (friendship.IsDating())
        {
            var relationshipPublic = Context.Inlaw == null ? _translationHelper.Get("specialRelationshipDatingPublic") : _translationHelper.Get("specialRelationshipDatingDiscrete");
            var relationshipWord = RelationshipWord(Context.MaleFarmer, npcIsMale);
            prompt.AppendLine(_translationHelper.Get("specialRelationshipDating", new { Name= Name, relationshipPublic= relationshipPublic, relationshipWord= relationshipWord }));
        }
        if (friendship.IsEngaged())
        {
            var daysToWedding = friendship.CountdownToWedding;
            prompt.AppendLine(_translationHelper.Get("specialRelationshipEngaged", new { Name= Name, daysToWedding= daysToWedding }));
        }
        if (friendship.IsDivorced())
        {
            prompt.AppendLine(_translationHelper.Get("specialRelationshipDivorced", new { Name= Name }));
        }
        if (friendship.ProposalRejected)
        {
            prompt.AppendLine(_translationHelper.Get("specialRelationshipProposalRejected", new { Name= Name }));
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
                var otherSpousesList = multipleOthers ? $"{_translationHelper.Get("spousesNOtherPeople", new { nSpouses= nSpouses })} {spouseList}":spouses.First();
                var otherSpousesReference = multipleOthers ? _translationHelper.Get("spousesAllTheOthers") : spouses.First();
                prompt.AppendLine(_translationHelper.Get("spousesMarriedToOthers", new { Name= Name, otherSpousesList= otherSpousesList, otherSpousesReference= otherSpousesReference }));
            }
            else
            {
                if (multipleOthers)
                {
                    prompt.AppendLine(_translationHelper.Get("spousesMarriedToMany", new { nSpouses= nSpouses, spouseList= spouseList, Name= Name }));
                }
                else
                {
                    prompt.AppendLine(_translationHelper.Get("spousesMarriedToOne", new { spouseList= spouseList, Name= Name }));
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
            var roommateList = multipleOthers ? $"{_translationHelper.Get("spousesNOtherPeople", new { nSpouses= roommates.Count() })} {string.Join(", ", roommates)}":roommates.First();
            var roommateReference = multipleOthers ? _translationHelper.Get("spouseRoommatesAllTheOthers") : roommates.First();
            if (talkingToRoommate)
            {
                prompt.AppendLine(_translationHelper.Get("spouseRoommatesWithOthers", new { Name= Name, roommateList= roommateList, roommateReference= roommateReference }));
            }
            else
            {
                if (multipleOthers)
                {
                    prompt.AppendLine(_translationHelper.Get("spouseRoommateWithMany", new { roommateList= roommateList }));
                }
                else
                {
                    prompt.AppendLine(_translationHelper.Get("spouseRoommateWithOne", new { roommateList= roommateList }));
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
            prompt.AppendLine(_translationHelper.Get("spouseEngaged", new { engagedTo= engagedTo, weddingDays= weddingDays }));
        }
        var total = spouses.Count() + engaged.Count();
        if (total > 1 && !talkingToSpouse && !talkingToRoommate)
        {
            prompt.AppendLine(_translationHelper.Get("spousePoly", new { Name= Name }));
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
                -1 => _translationHelper.Get("nonSpouseFriendshipFirstConversation", new { Name= Name }),
                < 2 => _translationHelper.Get("nonSpouseFreindshipStrangers", new { Name= Name }),
                < 4 => _translationHelper.Get("nonSpouseFriendshipAcquaintances", new { Name= Name }),
                < 6 => _translationHelper.Get("nonSpouseFriendshipFriends", new { Name= Name }),
                < 8 => _translationHelper.Get("nonSpouseFriendshipCloseFriends", new { Name= Name }),
                <= 10 => _translationHelper.Get("nonSpouseFriendshipWantToDate", new { Name= Name }),
                <= 14 => _translationHelper.Get("nonSpouseFriendshipIntimate", new { Name= Name }), // Backup = should never be called
                _ => throw new InvalidDataException("Invalid heart level.")
            });
        }
        else
        {
            if (Context.Hearts <= 8 && !isChild)
            {
                prompt.AppendLine(_translationHelper.Get("nonSpouseFriendshipNonSingleAdult8", new { Name= Name }));
            }
            else if (isChild)
            {
                prompt.AppendLine(_translationHelper.Get("nonSpouseFriendshipChild8Plus", new { Name= Name }));
            }
            else
            {
                prompt.AppendLine(_translationHelper.Get("nonSpouseFriendshipNonSingleAdult10", new { Name= Name }));
            }
        }
    }

    private void GetSpouseAction(StringBuilder prompt)
    {
        if (Context.SpouseAct == null) return;

        prompt.AppendLine(Context.SpouseAct switch
        {
            SpouseAction.funLeave => _translationHelper.Get("spouseActionFunLeave", new { Name= Name }),
            SpouseAction.jobLeave => _translationHelper.Get("spouseActionJobLeave", new { Name= Name }),
            SpouseAction.patio => _translationHelper.Get("spouseActionPatio", new { Name= Name }),
            SpouseAction.funReturn => _translationHelper.Get("spouseActionFunReturn", new { Name= Name }),
            SpouseAction.jobReturn => _translationHelper.Get("spouseActionJobReturn", new { Name= Name }),
            SpouseAction.spouseRoom => _translationHelper.Get("spouseActionSpouseRoom", new { Name= Name, GenderPossessive= Character.Bio.GenderPossessive }),
            _ => $""
        });
    }

    private void GetGift(StringBuilder prompt)
    {
        if (Context.Accept == null) return;

        var giftName = Context.Accept.DisplayName;
        prompt.AppendLine(_translationHelper.Get("giftIntro", new { Name= Name, giftName= giftName }));
        switch (Context.GiftTaste)
        {
            //TODO: Correct the cases
            case 0:
                prompt.AppendLine(_translationHelper.Get("giftLoved", new { Name= Name }));
                break;
            case 2:
                prompt.AppendLine(_translationHelper.Get("giftLiked", new { Name= Name }));
                break;
            case 4:
                prompt.AppendLine(_translationHelper.Get("giftDislike", new { Name= Name }));
                break;
            case 6:
                prompt.AppendLine(_translationHelper.Get("giftHate", new { Name= Name }));
                break;
            default:
                prompt.AppendLine(_translationHelper.Get("giftNeutral", new { Name= Name }));
                break;
        }
        prompt.AppendLine(_translationHelper.Get("giftMustIncludeReaction", new { Name= Name }));
        if (Context.Birthday)
        {
            prompt.AppendLine(_translationHelper.Get("giftBirthday", new { Name= Name }));
        }
        prompt.AppendLine(_translationHelper.Get("giftOutro"));
    }

    private void GetSpecialDatesAndBirthday(StringBuilder prompt)
    {
        if (Context.DayOfSeason == null) return;

        prompt.AppendLine((Context.Season, Context.DayOfSeason) switch
        {
            (Season.Spring, 1) => _translationHelper.Get("specialDatesSpring1"),
            (Season.Spring, 12) => _translationHelper.Get("specialDatesSpring12"),
            (Season.Spring, 23) => _translationHelper.Get("specialDatesSpring23"),
            (Season.Summer, 1) => _translationHelper.Get("specialDatesSummer1"),
            (Season.Summer, 10) => _translationHelper.Get("specialDatesSummer10"),
            (Season.Summer, 27) => _translationHelper.Get("specialDatesSummer27"),
            (Season.Summer, 28) => _translationHelper.Get("specialDatesSummer28"),
            (Season.Fall, 1) => _translationHelper.Get("specialDatesFall1"),
            (Season.Fall, 15) => _translationHelper.Get("specialDatesFall15"),
            (Season.Fall, 26) => _translationHelper.Get("specialDatesFall26"),
            (Season.Winter, 1) => _translationHelper.Get("specialDatesWInter1"),
            (Season.Winter, 7) => _translationHelper.Get("specialDatesWinter7"),
            (Season.Winter, 24) => _translationHelper.Get("specialDatesWinter24"),
            (Season.Winter, 28) => _translationHelper.Get("specialDatesWinter28"),
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
            prompt.AppendLine(_translationHelper.Get("specialDatesBirthday", new { Name= Name }));
        }
    }

    private void GetRecentEvents(StringBuilder prompt)
    {
        var eventSection = new StringBuilder();
        foreach (var activity in allPreviousActivities.Where(x => x.Value < 7))
        {
            var theLine = activity.Key switch
            {
                "cc_Boulder" => _translationHelper.Get("recentEventsBoulder"),
                "cc_Bridge" => _translationHelper.Get("recentEventsQuarryBridge"),
                "cc_Bus" => _translationHelper.Get("recentEventsBus"),
                "cc_Greenhouse" => _translationHelper.Get("recentEventsGreenhouse"),
                "cc_Minecart" => _translationHelper.Get("recentEventsMinecarts"),
                "cc_Complete" => _translationHelper.Get("recentEventsCommunityCenter"),
                "movieTheater" => _translationHelper.Get("recentEventsMovieTheatre"),
                "pamHouseUpgrade" => _translationHelper.Get("recentEventsPamHouse"),
                "pamHouseUpgradeAnonymous" => _translationHelper.Get("recentEventsPamHouseAnonymous"),
                "jojaMartStruckByLightning" => _translationHelper.Get("recentEventsJojaLightning"),
                "babyBoy" => _translationHelper.Get("recentEventsBabyBoy"),
                "babyGirl" => _translationHelper.Get("recentEventsBabyGirl"),
                "wedding" => _translationHelper.Get("recentEventsMarried"),
                "luauBest" => _translationHelper.Get("recentEventsLuauBest"),
                "luauShorts" => _translationHelper.Get("recentEventsLuauShorts"),
                "luauPoisoned" => _translationHelper.Get("recentEventsLuauPoisoned"),
                "Characters_MovieInvite_Invited" => _translationHelper.Get("recentEventsMovieInvited", new { Name= Name }),
                "DumpsterDiveComment" => _translationHelper.Get("recentEventsDumpsterDive", new { Name= Name }),
                "GreenRainFinished" => _translationHelper.Get("recentEventsGreenRain"),
                _ => $""
            };
            if (!string.IsNullOrWhiteSpace(theLine))
            {
                eventSection.AppendLine(theLine);
            }
        }
        if (eventSection.Length > 0)
        {
            prompt.AppendLine($"## {_translationHelper.Get("recentEventsHeading")}");
            prompt.AppendLine(_translationHelper.Get("recentEventsIntro"));
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
                prompt.AppendLine(_translationHelper.Get("locationBed", new { Name= Name, GenderPronoun= Character.Bio.GenderPronoun }));
            }
            else
            {
                var mayBeInShop = Context.Location.Contains("Shop", StringComparison.OrdinalIgnoreCase)
                    || Context.Location.Contains("Science", StringComparison.OrdinalIgnoreCase);
                var inShopString = mayBeInShop ? _translationHelper.Get("locationAtHomeOrShop") : "";
                prompt.AppendLine(_translationHelper.Get("locationAtHome", new { Name= Name, inShopString= inShopString }));
            }
        }
        else if (Context.Location != null)
        {
            prompt.Append(Context.Location switch
            {
                "Town" => _translationHelper.Get("locationTown", new { Name= Name }),
                "Beach" => _translationHelper.Get("locationBeach", new { Name= Name }),
                "Desert" => _translationHelper.Get("locationDesert", new { Name= Name }),
                "BusStop" => _translationHelper.Get("locationBusStop", new { Name= Name }),
                "Railroad" => _translationHelper.Get("locationRailroad", new { Name= Name }),
                "Saloon" => $"{_translationHelper.Get("locationSaloon", new { Name= Name })}{((npcData.Age == NpcAge.Child || Character.Name == "Emily") ? "" : _translationHelper.Get("locationSaloonDrunk"))}",
                "SeedShop" => _translationHelper.Get("locationPierres", new { Name= Name }),
                "JojaMart" => _translationHelper.Get("locationJojaMart", new { Name= Name }),
                "Resort_Chair" => _translationHelper.Get("locationResortChair", new { Name= Name }),
                "Resort_Towel" or "Resort_Towel_2" or "Resort_Towel_3" => _translationHelper.Get("locationResortTowel", new { Name= Name }),
                "Resort_Umbrella" or "Resort_Umbrella_2" or "Resort_Umbrella_3" => _translationHelper.Get("locationResortUmbrella", new { Name= Name }),
                "Resort_Bar" => $"{_translationHelper.Get("locationResortBar", new { Name= Name })}{((npcData.Age == NpcAge.Child) ? "" : _translationHelper.Get("locationSaloonDrunk"))}.",
                "Resort_Entering" => _translationHelper.Get("locationResortEntering", new { Name= Name }),
                "Resort_Leaving" => _translationHelper.Get("locationResortLeaving", new { Name= Name }),
                "Resort_Shore" or "Resort_Shore_2" => _translationHelper.Get("locationResortShore", new { Name= Name }),
                "Resort_Wander" => _translationHelper.Get("locationResortWander", new { Name= Name }),
                "Resort" or "Resort_2" => _translationHelper.Get("locationResort", new { Name= Name }),
                _ => $"The farmer and {Name} are at {Context.Location}."
            });
            prompt.AppendLine(_translationHelper.Get("locationOutro"));
        }

        if (Character.StardewNpc.DirectionsToNewLocation != null && Context.Location != Character.StardewNpc.DirectionsToNewLocation.targetLocationName)
        {
            var destination = Character.StardewNpc.DirectionsToNewLocation.targetLocationName;
            prompt.AppendLine(_translationHelper.Get("locationTravelling", new { Name= Name, destination= destination }));
        }
    }

    private void GetMarriageFeelings(StringBuilder prompt)
    {
        var IsRoommate = Game1.getPlayerOrEventFarmer().friendshipData[Name].IsRoommate();
        var marriageOrRoommate = IsRoommate ? _translationHelper.Get("generalBeingRoommates") : _translationHelper.Get("generalTheMarriage");
        switch (Context.Hearts)
        {
            case > 12:
                prompt.AppendLine(_translationHelper.Get("marriageSentimentGood", new { Name= Name, marriageOrRoommate= marriageOrRoommate }));
                break;
            case < 10:
                prompt.AppendLine(_translationHelper.Get("marriageSentimentBad", new { Name= Name, marriageOrRoommate= marriageOrRoommate }));
                break;
            default:
                prompt.AppendLine(_translationHelper.Get("marriageSentimentNeutral", new { Name= Name, marriageOrRoommate= marriageOrRoommate }));
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
            var petType = pet.petType.Value;
            prompt.AppendLine(_translationHelper.Get("farmContentsPet", new { petType= petType, Name= pet.Name }));
        }
        else
        {
            prompt.AppendLine(_translationHelper.Get("farmContentsNoPets"));
        }
    }

    private void GetFarmCrops(StringBuilder prompt)
    {
        var cropData = Game1.objectData;
        var allCrops = GetCrops();
        if (allCrops.Any())
        {
            prompt.AppendLine(_translationHelper.Get("farmCropsIntro"));
            foreach (var crop in allCrops.GroupBy(x => x.indexOfHarvest.Value))
            {
                var thisDetails = cropData[crop.Key];
                prompt.Append($"- {crop.Count()} {thisDetails.Name}");
                var ripe = crop.Count(x => x.fullyGrown.Value);
                if (ripe > 0)
                {
                    prompt.AppendLine(_translationHelper.Get("farmCropsReadyForHarvest", new { ripe= ripe }));
                }
                else
                {
                    prompt.AppendLine(_translationHelper.Get("farmCropsNotReady"));
                }
            }
        }
        else
        {
            prompt.AppendLine(_translationHelper.Get("farmCropsNone"));
        }
    }

    private void GetFarmAnimals(StringBuilder prompt)
    {
        var allAnimals = GetAnimals();

        if (allAnimals.Any())
        {
            prompt.AppendLine(_translationHelper.Get("farmAnimalsIntro"));
            foreach (var animal in allAnimals.GroupBy(x => x.type))
            {
                prompt.AppendLine($"- {animal.Count()} {animal.Key}{(animal.Count() > 1 ? "s" : "")}");
            }
        }
        else
        {
            prompt.AppendLine(_translationHelper.Get("farmAnimalsNone"));
        }
    }

    private void GetFarmBuildings(StringBuilder prompt)
    {
        var allBuildings = GetBuildings();

        if (allBuildings.Any())
        {
            prompt.AppendLine(_translationHelper.Get("farmBuildingsIntro"));
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
                    prompt.AppendLine($"- {_translationHelper.Get("farmBuildingsRuinedGreenhouse")}");
                }
                else
                {
                    prompt.AppendLine($"- {_translationHelper.Get("farmBuildingsRepairedGreenhouse")}");
                }
            }
            if (allBuildings.Any(x => x.daysOfConstructionLeft.Value > 0))
            {
                var underConstruction = allBuildings.First(x => x.daysOfConstructionLeft.Value > 0);
                prompt.AppendLine($"- {_translationHelper.Get("farmBuildingsConstruction", new { buildingType= underConstruction.buildingType.Value, daysOfConstructionLeft= underConstruction.daysOfConstructionLeft })}");
            }
        }
        else
        {
            prompt.AppendLine(_translationHelper.Get("farmBuildingsNone"));
        }
    }

    private void GetChildren(StringBuilder prompt, Friendship friendship)
    {
        if (Context.Children.Count == 0)
        {
            prompt.AppendLine(_translationHelper.Get("childrenNone", new { Name= Name }));
        }
        else
        {
            if (Context.Children.Count > 1)
            {
                var count = Context.Children.Count;
                prompt.AppendLine(_translationHelper.Get("childrenMultiple", new { Name= Name, count= count }));
            }
            else
            {
                prompt.AppendLine(_translationHelper.Get("childrenSingle", new { Name= Name }));
            }
            prompt.AppendLine();
            foreach (var child in Context.Children)
            {
                var childGender = child.IsMale ? _translationHelper.Get("generalBoy") : _translationHelper.Get("generalGirl");
                prompt.AppendLine($"- {_translationHelper.Get("childrenDescription", new { Gender = childGender, Name= child.Name, Age= child.Age })}");
            }
        }
        if (friendship.DaysUntilBirthing > 0)
        {
            var daysUntilBirth = friendship.DaysUntilBirthing;
            prompt.AppendLine(_translationHelper.Get("childrenPregnant", new { Name= Name, daysUntilBirth= daysUntilBirth }));
        }
    }

    private void GetWeather(StringBuilder prompt)
    {
        if (Context.Weather == null || !Context.Weather.Any()) return;

        if (Context.Weather.Contains("lightning"))
        {
            prompt.AppendLine(_translationHelper.Get("weatherLightning"));
        }
        else if (Context.Weather.Contains("green rain"))
        {
            prompt.AppendLine(_translationHelper.Get("weatherGreenRain"));
        }
        else if (Context.Weather.Contains("snow"))
        {
            prompt.AppendLine(_translationHelper.Get("weatherSnow"));
        }
        else if (Context.Weather.Contains("rain"))
        {
            prompt.AppendLine(_translationHelper.Get("weatherRain"));
        }
    }

    private void GetDateAndTime(StringBuilder prompt)
    {
        if (Context.DayOfSeason != null && Context.Season != null)
        {
            prompt.AppendLine(_translationHelper.Get("dateTimeDayOfSeason", new { DayOfSeason= Context.DayOfSeason, Season= Context.Season }));
        }
        if (Context.TimeOfDay != null)
        {
            prompt.AppendLine(_translationHelper.Get("dateTimeTimeOfDay", new { TimeOfDay= Context.TimeOfDay }));
            if (Context.TimeOfDay == "early morning")
            {
                prompt.AppendLine(_translationHelper.Get("dateTimeEarlyMorningNormal"));
            }
        }
        if (Context.Year == 1)
        {
            prompt.AppendLine(_translationHelper.Get("dateTimeNewThisYear"));
        }

    }

    private void GetEventHistory(StringBuilder prompt)
    {
        var timeNow = new StardewTime(Game1.year, Game1.season, Game1.dayOfMonth, Game1.timeOfDay);
        var fullHistory = Character.EventHistory.Concat(previousActivites.Select(x => MakeActivityHistory(x)));

        if (fullHistory.Any())
        {
            prompt.AppendLine($"##{_translationHelper.Get("eventHistoryHeading")}");
            prompt.AppendLine(_translationHelper.Get("eventHistoryIntro", new { Name= Name }));
            prompt.AppendLine(_translationHelper.Get("eventHistorySubheading"));
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

        prompt.AppendLine($"##{_translationHelper.Get("sampleDialogueHeading", new { Name= Name })}");
        prompt.AppendLine(_translationHelper.Get("sampleDialogueIntro", new { Name= Name }));
        foreach (var dialogue in dialogueSample)
        {
            prompt.AppendLine($"- {dialogue.Value}");
        }
    }

    private void GetGameState(StringBuilder prompt)
    {
        prompt.AppendLine($"## {_translationHelper.Get("gameStateHeading")}");
        if (allPreviousActivities.ContainsKey("cc_Complete"))
        {
            prompt.AppendLine(_translationHelper.Get("gameStateCommunityCenterYes"));
        }
        else
        {
            prompt.AppendLine(_translationHelper.Get("gameStateCommunityCenterNo"));
        }
        if (allPreviousActivities.ContainsKey("cc_Bus"))
        {
            prompt.AppendLine(_translationHelper.Get("gameStateBusYes"));
        }
        else
        {
            prompt.AppendLine(_translationHelper.Get("gameStateBusNo"));
        }
        if (allPreviousActivities.ContainsKey("cc_Bridge"))
        {
            prompt.AppendLine(_translationHelper.Get("gameStateQuarryBridgeYes"));
        }
        else
        {
            prompt.AppendLine(_translationHelper.Get("gameStateQuarryBridgeNo"));
        }
        if (allPreviousActivities.ContainsKey("cc_Minecart"))
        {
            prompt.AppendLine(_translationHelper.Get("gameStateMinecartYes"));
        }
        else
        {
            prompt.AppendLine(_translationHelper.Get("gameStateMinecartNo"));
        }
        if (allPreviousActivities.ContainsKey("cc_Boulder"))
        {
            prompt.AppendLine(_translationHelper.Get("gameStateBoulderYes"));
        }
        else
        {
            prompt.AppendLine(_translationHelper.Get("gameStateBoulderNo"));
        }
        if (Game1.year == 1)
        {
            prompt.AppendLine(_translationHelper.Get("gameStateKentNo"));
        }
        else
        {
            prompt.AppendLine(_translationHelper.Get("gameStateKentYes"));
        }
    }

    private string GetCommand()
    {
        var commandPrompt = new StringBuilder();
        commandPrompt.AppendLine($"##{_translationHelper.Get("commandHeading")}");
        commandPrompt.AppendLine(_translationHelper.Get("commandIntro", new { Name= Name }));
        if (!string.IsNullOrWhiteSpace(Context.ScheduleLine) && Context.ChatHistory.Length == 0)
        {
            commandPrompt.AppendLine();
            commandPrompt.AppendLine(_translationHelper.Get("commandReplaceSchedule", new { ScheduleLine= Context.ScheduleLine }));
        }
        return commandPrompt.ToString();
    }

    private string GetInstructions()
    {
        var instructions = new StringBuilder();
        instructions.AppendLine($"##{_translationHelper.Get("instructionsHeading")}");
        instructions.AppendLine(_translationHelper.Get("instructionsIntro", new { Name= Name }));
        if (dialogueSample.Any())
        {
            instructions.AppendLine(_translationHelper.Get("instructionsSampleDialogue", new { Name= Name }));
        }
        instructions.AppendLine(_translationHelper.Get("instructionsFarmersName"));
        instructions.AppendLine(_translationHelper.Get("instructionsBreaks"));
        var extraPortraits = new StringBuilder();
        foreach (var portrait in Character.Bio.ExtraPortraits)
        {
            extraPortraits.Append(_translationHelper.Get("instructionsExtraPortraitLine", new { Key= portrait.Key, Value= portrait.Value }));
        }
        instructions.AppendLine(_translationHelper.Get("instructionsEmotion", new { extraPortraits= extraPortraits }));
        instructions.AppendLine(_translationHelper.Get("instructionsSingleLine"));
        instructions.AppendLine(_translationHelper.Get("instructionsResponses", new { Name= Name }));
        if (ModEntry.Config.ApplyTranslation)
        {
            instructions.AppendLine(_translationHelper.Get("instructionsTranslate", new { Language= ModEntry.Language }));
        }
        if (!string.IsNullOrWhiteSpace(Llm.Instance.ExtraInstructions))
        {
            instructions.AppendLine(Llm.Instance.ExtraInstructions);
        }
        return instructions.ToString();
    }

    private string GetResponseStart()
    {
        return _translationHelper.Get("responseStart", new { Name= Name });
    }

    private string RelationshipWord(bool maleFarmer, bool npcIsMale)
    {
        return maleFarmer ? (npcIsMale ? _translationHelper.Get("generalGayMale") : _translationHelper.Get("generalHeterosexual")) : (npcIsMale ? _translationHelper.Get("generalHeterosexual") : _translationHelper.Get("generalLesbian"));
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
