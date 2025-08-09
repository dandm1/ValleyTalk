using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using ValleyTalk;
using StardewValley;
using StardewValley.GameData.Characters;

namespace ValleyTalk;

public class Prompts
{
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
        var builder = new GameSummaryBuilder();
        _stardewSummary = builder.Build();
    }

    static string _stardewSummary;
    private string _system;
    public string System { get => _system ??= GetSystemPrompt(); internal set => _system = value; }
    private string _gameConstantContext;
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

    public string Name { get; internal set; }
    
    public string Gender { get; internal set; }


    public Character Character { get; internal set; }

    internal DialogueContext Context { private get; set; }

    CharacterData npcData;
    bool npcIsMale;

    IEnumerable<DialogueValue> dialogueSample;

    IDialogueValue exactLine;

    string giveGift;
    private SerializableDictionary<string, int> allPreviousActivities;

    public string GiveGift => giveGift;

    public Prompts(DialogueContext context, Character character)
    {

        npcData = character.StardewNpc.GetData();
        npcIsMale = npcData.Gender == StardewValley.Gender.Male;
        Context = context;
        Character = character;

        dialogueSample = character.SelectDialogueSample(context);
        exactLine = SelectExactDialogue();
        giveGift = context.CanGiveGift ? SelectGiftGiven() : string.Empty;
        allPreviousActivities = Game1.getPlayerOrEventFarmer().previousActiveDialogueEvents.First();

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
        if ((Character.Bio?.Biography ?? string.Empty).Length > 10)
        {
            npcConstantPrompt.AppendLine($"##{Util.GetString(Character,"npcContextBiographyHeading", new { Name = Name })}");
            var bio = Character.Bio.Biography;
            while (bio.Contains("\n\n"))
            {
                bio = bio.Replace("\n\n", "\n");
            }
            npcConstantPrompt.AppendLine(bio);
            if (Character.Bio.Relationships.Any())
            {
                npcConstantPrompt.AppendLine($"## {Util.GetString("biographyRelationships")}:");
                foreach (var relationship in Character.Bio.Relationships.Values)
                {
                    npcConstantPrompt.AppendLine($"* **{relationship.Heading}**: {relationship.Description}");
                }
            }
            if (Character.Bio.Traits.Any())
            {
                npcConstantPrompt.AppendLine($"## {Util.GetString("biographyPersonality")}:");
                foreach (var trait in Character.Bio.Traits.Values)
                {
                    npcConstantPrompt.AppendLine($"* **{trait.Heading}**: {trait.Description}");
                }
            }
            npcConstantPrompt.AppendLine(Character.Bio.BiographyEnd);
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
        if (Game1.random.NextDouble() < 0.5 || Context.ChatHistory.Any() ) return;

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
        if (Context.ChatHistory.Any())
        {
            prompt.AppendLine($"###{Util.GetString(Character, "currentConversationHeading")}");
            prompt.AppendLine(Util.GetString(Character, "currentConversationIntro", new { Name = Name }));
            // Append each line from the chat history, labelling each one alternatively with the NPC's name or 'Farmer'
            for (int i = 0; i < Context.ChatHistory.Count; i++)
            {
                prompt.AppendLine(Context.ChatHistory[i].IsPlayerLine ? $"- {Util.GetString(Character, "generalFarmerLabel")}: {Context.ChatHistory[i].Text}" : $"- {Name}: {Context.ChatHistory[i].Text}");
            }
        }
        else if (Character.SpokeJustNow())
        {
            prompt.AppendLine($"###{Util.GetString(Character, "currentConversationHeading")}");
            prompt.AppendLine(Util.GetString(Character, "currentConversationJustSpoke", new { Name = Name }));
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
            prompt.AppendLine((Context.Hearts ?? 0) switch
            {
                -1 => Util.GetString(Character,"nonSpouseFriendshipFirstConversation", new { Name= Name }),
                < 2 => Util.GetString(Character,"nonSpouseFreindshipStrangers", new { Name= Name }),
                < 4 => Util.GetString(Character,"nonSpouseFriendshipAcquaintances", new { Name= Name }),
                < 6 => Util.GetString(Character,"nonSpouseFriendshipFriends", new { Name= Name }),
                < 8 => Util.GetString(Character,"nonSpouseFriendshipCloseFriends", new { Name= Name }),
                <= 10 => Util.GetString(Character,"nonSpouseFriendshipWantToDate", new { Name= Name }),
                <= 14 => Util.GetString(Character,"nonSpouseFriendshipIntimate", new { Name= Name }), // Backup = should never be called
                _ => throw new InvalidDataException("Invalid heart level.")
            });
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

        prompt.AppendLine(Context.SpouseAct switch
        {
            SpouseAction.funLeave => Util.GetString(Character,"spouseActionFunLeave", new { Name= Name }),
            SpouseAction.jobLeave => Util.GetString(Character,"spouseActionJobLeave", new { Name= Name }),
            SpouseAction.patio => Util.GetString(Character,"spouseActionPatio", new { Name= Name }),
            SpouseAction.funReturn => Util.GetString(Character,"spouseActionFunReturn", new { Name= Name }),
            SpouseAction.jobReturn => Util.GetString(Character,"spouseActionJobReturn", new { Name= Name }),
            SpouseAction.spouseRoom => Util.GetString(Character,"spouseActionSpouseRoom", new { Name= Name }),
            _ => $""
        });
    }

    private void GetGift(StringBuilder prompt)
    {
        if (Context.Accept != null)
        {
        var giftName = Context.Accept.DisplayName;
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

        prompt.AppendLine((Context.Season, Context.DayOfSeason) switch
        {
            (Season.Spring, 1) => Util.GetString(Character,"specialDatesSpring1"),
            (Season.Spring, 12) => Util.GetString(Character,"specialDatesSpring12"),
            (Season.Spring, 23) => Util.GetString(Character,"specialDatesSpring23"),
            (Season.Summer, 1) => Util.GetString(Character,"specialDatesSummer1"),
            (Season.Summer, 10) => Util.GetString(Character,"specialDatesSummer10"),
            (Season.Summer, 27) => Util.GetString(Character,"specialDatesSummer27"),
            (Season.Summer, 28) => Util.GetString(Character,"specialDatesSummer28"),
            (Season.Fall, 1) => Util.GetString(Character,"specialDatesFall1"),
            (Season.Fall, 15) => Util.GetString(Character,"specialDatesFall15"),
            (Season.Fall, 26) => Util.GetString(Character,"specialDatesFall26"),
            (Season.Winter, 1) => Util.GetString(Character,"specialDatesWInter1"),
            (Season.Winter, 7) => Util.GetString(Character,"specialDatesWinter7"),
            (Season.Winter, 24) => Util.GetString(Character,"specialDatesWinter24"),
            (Season.Winter, 28) => Util.GetString(Character,"specialDatesWinter28"),
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
            prompt.AppendLine(Util.GetString(Character,"specialDatesBirthday", new { Name= Name }));
        }
    }

    private void GetRecentEvents(StringBuilder prompt)
    {
        var eventSection = new StringBuilder();
        foreach (var activity in allPreviousActivities.Where(x => x.Value < 7))
        {
            var theLine = activity.Key switch
            {
                "cc_Boulder" => Util.GetString(Character,"recentEventsBoulder"),
                "cc_Bridge" => Util.GetString(Character,"recentEventsQuarryBridge"),
                "cc_Bus" => Util.GetString(Character,"recentEventsBus"),
                "cc_Greenhouse" => Util.GetString(Character,"recentEventsGreenhouse"),
                "cc_Minecart" => Util.GetString(Character,"recentEventsMinecarts"),
                "cc_Complete" => Util.GetString(Character,"recentEventsCommunityCenter"),
                "movieTheater" => Util.GetString(Character,"recentEventsMovieTheatre"),
                "pamHouseUpgrade" => Util.GetString(Character,"recentEventsPamHouse"),
                "pamHouseUpgradeAnonymous" => Util.GetString(Character,"recentEventsPamHouseAnonymous"),
                "jojaMartStruckByLightning" => Util.GetString(Character,"recentEventsJojaLightning"),
                "babyBoy" => Util.GetString(Character,"recentEventsBabyBoy"),
                "babyGirl" => Util.GetString(Character,"recentEventsBabyGirl"),
                "wedding" => Util.GetString(Character,"recentEventsMarried"),
                "luauBest" => Util.GetString(Character,"recentEventsLuauBest"),
                "luauShorts" => Util.GetString(Character,"recentEventsLuauShorts"),
                "luauPoisoned" => Util.GetString(Character,"recentEventsLuauPoisoned"),
                "Characters_MovieInvite_Invited" => Util.GetString(Character,"recentEventsMovieInvited", new { Name= Name }),
                "DumpsterDiveComment" => Util.GetString(Character,"recentEventsDumpsterDive", new { Name= Name }),
                "GreenRainFinished" => Util.GetString(Character,"recentEventsGreenRain"),
                _ => $""
            };
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
        
        //var bedTile = npcData.Home[0].Tile;
        //if (bedTile == null || bedTile.X <= 0 || bedTile.Y <= 0)
        //{
            // If the bed tile is not set, use the first tile of the home
        //    bedTile = new Microsoft.Xna.Framework.Point(-1, -1);
        //}
        if (Context.Location == npcData.Home[0].Location && Context.Inlaw != Name)
        {
            //if (Character.StardewNpc.TilePoint == bedTile && Character.Bio.HomeLocationBed && !Llm.Instance.IsHighlySensoredModel && StardewModdingAPI.Context.IsMainPlayer)
            //{
            //    prompt.AppendLine(Util.GetString(Character, "locationBed", new { Name = Name }));
            //}
            //else
            //{
                var mayBeInShop = Context.Location.Contains("Shop", StringComparison.OrdinalIgnoreCase)
                    || Context.Location.Contains("Science", StringComparison.OrdinalIgnoreCase);
                var inShopString = mayBeInShop ? Util.GetString(Character, "locationAtHomeOrShop") : "";
                prompt.AppendLine(Util.GetString(Character, "locationAtHome", new { Name = Name, inShopString = inShopString }));
            //}
        }
        else if (Context.Location != null)
        {
            var locationName = GetLocationDisplayNameIfAvailable(Context.Location);
            prompt.Append(Context.Location switch
            {
                "Town" => Util.GetString(Character, "locationTown", new { Name = Name }),
                "Beach" => Util.GetString(Character, "locationBeach", new { Name = Name }),
                "Desert" => Util.GetString(Character, "locationDesert", new { Name = Name }),
                "BusStop" => Util.GetString(Character, "locationBusStop", new { Name = Name }),
                "Railroad" => Util.GetString(Character, "locationRailroad", new { Name = Name }),
                "Saloon" => $"{Util.GetString(Character, "locationSaloon", new { Name = Name })}{((npcData.Age == NpcAge.Child || Character.Name == "Emily") ? "" : Util.GetString(Character, "locationSaloonDrunk"))}",
                "SeedShop" => Util.GetString(Character, "locationPierres", new { Name = Name }),
                "JojaMart" => Util.GetString(Character, "locationJojaMart", new { Name = Name }),
                "Resort_Chair" => Util.GetString(Character, "locationResortChair", new { Name = Name }),
                "Resort_Towel" or "Resort_Towel_2" or "Resort_Towel_3" => Util.GetString(Character, "locationResortTowel", new { Name = Name }),
                "Resort_Umbrella" or "Resort_Umbrella_2" or "Resort_Umbrella_3" => Util.GetString(Character, "locationResortUmbrella", new { Name = Name }),
                "Resort_Bar" => $"{Util.GetString(Character, "locationResortBar", new { Name = Name })}{((npcData.Age == NpcAge.Child) ? "" : Util.GetString(Character, "locationSaloonDrunk"))}.",
                "Resort_Entering" => Util.GetString(Character, "locationResortEntering", new { Name = Name }),
                "Resort_Leaving" => Util.GetString(Character, "locationResortLeaving", new { Name = Name }),
                "Resort_Shore" or "Resort_Shore_2" => Util.GetString(Character, "locationResortShore", new { Name = Name }),
                "Resort_Wander" => Util.GetString(Character, "locationResortWander", new { Name = Name }),
                "Resort" or "Resort_2" => Util.GetString(Character, "locationResort", new { Name = Name }),
                "FarmHouse" => Util.GetString(Character, "locationFarmHouse", new { Name = Name }),
                "Farm" => Util.GetString(Character, "locationFarm", new { Name = Name }),
                _ => locationName.Length > 2 ? Util.GetString("locationGeneric", new { Name = Name, Location = locationName }) : string.Empty
            });
            prompt.AppendLine(Util.GetString(Character, "locationOutro"));
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
        switch (Context.Hearts)
        {
            case > 12:
                prompt.AppendLine(Util.GetString(Character,"marriageSentimentGood", new { Name= Name, marriageOrRoommate= marriageOrRoommate }));
                break;
            case < 10:
                prompt.AppendLine(Util.GetString(Character,"marriageSentimentBad", new { Name= Name, marriageOrRoommate= marriageOrRoommate }));
                break;
            default:
                prompt.AppendLine(Util.GetString(Character,"marriageSentimentNeutral", new { Name= Name, marriageOrRoommate= marriageOrRoommate }));
                break;
        }
    }

    private void GetWealth(StringBuilder prompt)
    {
        var wealth = Game1.getPlayerOrEventFarmer()._money;
        switch (wealth)
        {
            case < 1000:
                prompt.AppendLine(Util.GetString(Character,"wealthPoor", new { wealth= wealth, Name = Name }));
                break;
            case < 10000:
                prompt.AppendLine(Util.GetString(Character,"wealthMiddle", new { wealth= wealth, Name = Name }));
                break;
            case < 100000:
                prompt.AppendLine(Util.GetString(Character,"wealthRich", new { wealth= wealth, Name = Name }));
                break;
            default:
                prompt.AppendLine(Util.GetString(Character,"wealthVeryRich", new { wealth= wealth, Name = Name }));
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
        var historySample = Character.EventHistorySample();

        if (historySample.Any())
        {
            prompt.AppendLine($"##{Util.GetString(Character, "eventHistoryHeading")}");
            prompt.AppendLine(Util.GetString(Character, "eventHistoryIntro", new { Name = Name }));
            prompt.AppendLine(Util.GetString(Character, "eventHistorySubheading"));

            foreach (var eventHistory in historySample)
            {
                // Exclude the current conversation
                if (eventHistory.Item2 is ConversationHistory conv && Context.ChatHistory.Any())
                {
                    if (conv.Id == Context.ChatHistory.First().Id)
                    {
                        continue;
                    }
                }
                prompt.AppendLine($"- {eventHistory.Item1.SinceDescription(timeNow)}: {eventHistory.Item2.Format(Name)}");
            }
        }
    }


    private void GetSampleDialogue(StringBuilder prompt)
    {
        if (dialogueSample == null || !dialogueSample.Any()) return;

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
        if (!string.IsNullOrWhiteSpace(Context.ScheduleLine) && !Context.ChatHistory.Any() && !Character.SpokeJustNow())
        {
            commandPrompt.AppendLine();
            commandPrompt.AppendLine(Util.GetString(Character,"commandReplaceSchedule", new { ScheduleLine= Context.ScheduleLine }));
        }
        if (ModEntry.Config.ApplyTranslation)
        {
            commandPrompt.AppendLine(Util.GetString(Character,"instructionsTranslate", new { Language= ModEntry.Language }));
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
        instructions.AppendLine(Util.GetString(Character,"instructionsSingleLine"));
        instructions.AppendLine(Util.GetString(Character,"instructionsResponses", new { Name= Name }));
        if (!Character.Bio.ExtraPortraits.ContainsKey("!"))
        {
            var extraPortraits = new StringBuilder();
            foreach (var portrait in Character.Bio.ExtraPortraits)
            {
                extraPortraits.Append(Util.GetString(Character,"instructionsExtraPortraitLine", new { Key= portrait.Key, Value= portrait.Value }));
            }
            instructions.AppendLine(Util.GetString(Character,"instructionsEmotion", new { extraPortraits= extraPortraits }));
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

    private IDialogueValue SelectExactDialogue()
    {
        return (Character.DialogueData
                    ?.AllEntries
                    .FirstOrDefault(x => x.Key == Context))?.Value;
                    
    }


}
