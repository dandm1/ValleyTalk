using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using StardewDialogue;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;

namespace ValleyTalk
{
    internal class DialogueBuilder
    {
        private static int responseIndex = 20000;
        public static DialogueBuilder Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DialogueBuilder();
                }
                return _instance;
            }
        }

        public ModConfig Config { get; internal set; }
        public DialogueContext LastContext { get; private set; }
        public bool LlmDisabled { get; set; } = false;

        private static DialogueBuilder _instance;
        private Dictionary<string, StardewDialogue.Character> _characters;
        private Random _random;
        private int _patchDate;
        private Dictionary<string, bool> _patchCharacters;

        private DialogueBuilder()
        {
            _characters = new Dictionary<string, StardewDialogue.Character>();
            _random = new Random();
        }

        private void PopulateCharacters()
        {
            foreach (var npc in Game1.characterData.Keys)
            {
                if (!_characters.ContainsKey(npc))
                {
                    var npcObject = Game1.getCharacterFromName(npc);
                    GetCharacter(npcObject);
                }
            }
        }

        public StardewDialogue.Character GetCharacter(NPC instance)
        {
            if (instance == null)
            {
                return null;
            }
            if (!_characters.ContainsKey(instance.Name))
            {
                var newCharacter = new StardewDialogue.Character(
                    instance.Name, 
                    instance);
                _characters.Add(instance.Name, newCharacter);
            }
            return _characters[instance.Name];
        }

        internal async Task<string> GenerateResponse(NPC instance, string[] conversation)
        {
            var character = GetCharacter(instance);
            DialogueContext context = LastContext;
            context.CanGiveGift = false;
            var fullHistory = context.ChatHistory.ToList();
            fullHistory.AddRange(conversation);
            context.ChatHistory = fullHistory.ToArray();
            LastContext = context;
            var theLine = await character.CreateBasicDialogue(context);
            string formattedLine = FormatLine(theLine);
            //return formattedLine;
            return "skip#"+formattedLine;
        }

        internal async Task<Dialogue> GenerateGift(NPC instance, StardewValley.Object gift, int taste)
        {
            var character = GetCharacter(instance);
            DialogueContext context = GetBasicContext(instance);
            context.Accept = gift;
            context.GiftTaste = taste;
            LastContext = context;
            var theLine = await character.CreateBasicDialogue(context);
            string formattedLine = FormatLine(theLine);
            var newDialogue = new Dialogue(instance, $"Accept_{gift.Name}", formattedLine);
            return newDialogue;
        }

        internal async Task<Dialogue> Generate(NPC instance, string dialogueKey, string originalLine = "")
        {
            var character = GetCharacter(instance);
            DialogueContext context = GetBasicContext(instance);
            var firstElement = dialogueKey.Split('_')[0];
            if (Enum.TryParse<RandomAction>(firstElement, true, out var randomAction))
            {
                context.RandomAct = randomAction;
            }
            if (Enum.TryParse<SpouseAction>(firstElement, true, out var spouseAction))
            {
                context.SpouseAct = spouseAction;
            }
            context.CanGiveGift = string.IsNullOrWhiteSpace(originalLine);
            LastContext = context;
            context.ScheduleLine = originalLine;
            var theLine = await character.CreateBasicDialogue(context);
            string formattedLine = FormatLine(theLine);
            return new Dialogue(instance, dialogueKey, formattedLine);
        }

        private string FormatLine(string[] theLine)
        {
            if (theLine.Length == 1)
            {
                return theLine[0];
            }
            var sb = new StringBuilder();
            sb.Append(theLine[0]);
            //sb.Append("#$b#Respond:");
            sb.Append($"#$q {responseIndex++} {SldConstants.DialogueKeyPrefix}Default#{ModEntry.SHelper.Translation.Get("outputRespond")}");
            sb.Append($"#$r -999999 0 {SldConstants.DialogueKeyPrefix}Silent#{ModEntry.SHelper.Translation.Get("outputStaySilent")}");
            for (int i = 1; i < theLine.Length; i++)
            {
                sb.Append($"#$r -999998 0 {SldConstants.DialogueKeyPrefix}Next#");
                sb.Append(theLine[i]);
            }
            return sb.ToString();
        }

        private DialogueContext GetBasicContext(NPC instance)
        {
            var farmer = Game1.getPlayerOrEventFarmer();
            StardewDialogue.Season season;
            switch (Game1.currentSeason)
            {
                case "spring":
                    season = StardewDialogue.Season.Spring;
                    break;
                case "summer":
                    season = StardewDialogue.Season.Summer;
                    break;
                case "fall":
                    season = StardewDialogue.Season.Fall;
                    break;
                case "winter":
                    season = StardewDialogue.Season.Winter;
                    break;
                default:
                    throw new Exception("Invalid season");
            }
            string timeOfDay;
            switch (Game1.timeOfDay)
            {
                case <= 800:
                    timeOfDay = ModEntry.SHelper.Translation.Get("generalEarlyMorning");
                    break;
                case <= 1130:
                    timeOfDay = ModEntry.SHelper.Translation.Get("generalLateMorning");
                    break;
                case <= 1400:
                    timeOfDay = ModEntry.SHelper.Translation.Get("generalMidday");
                    break;
                case <= 1700:
                    timeOfDay = ModEntry.SHelper.Translation.Get("generalAfternoon");
                    break;
                case <= 2200:
                    timeOfDay = ModEntry.SHelper.Translation.Get("generalEvening");
                    break;
                default:
                    timeOfDay = ModEntry.SHelper.Translation.Get("generalLateNight");
                    break;
            }
            timeOfDay += $" ({(Game1.timeOfDay / 100) % 24}:{Game1.timeOfDay % 100:00})";
            StardewDialogue.Weekday day;
            switch (Game1.dayOfMonth % 7)
            {
                case 0:
                    day = StardewDialogue.Weekday.Sun;
                    break;
                case 1:
                    day = StardewDialogue.Weekday.Mon;
                    break;
                case 2:
                    day = StardewDialogue.Weekday.Tue;
                    break;
                case 3:
                    day = StardewDialogue.Weekday.Wed;
                    break;
                case 4:
                    day = StardewDialogue.Weekday.Thu;
                    break;
                case 5:
                    day = StardewDialogue.Weekday.Fri;
                    break;
                case 6:
                    day = StardewDialogue.Weekday.Sat;
                    break;
                default:
                    throw new Exception("Invalid day");
            }
            var children = ConvertChildren(farmer.getChildren());
            var weather = new List<string>();
            if (Game1.IsRainingHere()) weather.Add("rain");
            if (Game1.IsSnowingHere()) weather.Add("snow");
            if (Game1.IsLightningHere()) weather.Add("lightning");
            if (Game1.IsGreenRainingHere()) weather.Add("green rain");
            
            var hearts = farmer.friendshipData.ContainsKey(instance.Name) ? 
                    (
                        farmer.friendshipData[instance.Name].Points == 0 ? 
                                -1 : 
                                farmer.friendshipData[instance.Name].Points / 250
                    ) 
                    : -1;
            var context = new StardewDialogue.DialogueContext()
            {
                Season = season,
                DayOfSeason = Game1.dayOfMonth,
                TimeOfDay = timeOfDay,
                Hearts = hearts,
                Location = instance.currentLocation.Name,
                Year = Game1.year,
                Day = day,
                MaleFarmer = farmer.IsMale,
                Inlaw = farmer.getSpouse()?.Name,
                Children = children,
                Married = farmer.getSpouse() != null,
                Spouse = farmer.getSpouse()?.Name,
                Weather = weather
            };
            return context;
        }

        private List<ChildDescription> ConvertChildren(List<Child> children)
        {
            var result = new List<ChildDescription>();
            foreach (var child in children)
            {
                result.Add(new ChildDescription(
                    child.Name,
                    child.Gender == Gender.Male,
                    child.Age
                ));
            }
            return result;
        }

        internal bool AddDialogueLine(NPC instance, List<StardewValley.DialogueLine> dialogues)
        {
            var character = GetCharacter(instance);
            var filteredDialogues = FilterForHistory(dialogues, character);
            if (!filteredDialogues.Any())
            {
                return false;
            }
            character.AddDialogue(filteredDialogues, Game1.year, Game1.season, Game1.dayOfMonth, Game1.timeOfDay);
            return true;
        }

        private static List<StardewValley.DialogueLine> FilterForHistory(List<StardewValley.DialogueLine> dialogues, StardewDialogue.Character character)
        {
            if (character.MatchLastDialogue(dialogues))
            {
                return new();
            }
            // Remove any lines just just contain Respond:
            return dialogues.Where(d => !d.Text.StartsWith("Respond:")).ToList();
        }

        internal void AddEventLine(NPC instance, IEnumerable<NPC> actors, string festivalName, List<StardewValley.DialogueLine> dialogues)
        {
            var character = GetCharacter(instance);
            var filteredDialogues = FilterForHistory(dialogues, character);
            if (!filteredDialogues.Any()) return;
            character.AddEventDialogue(filteredDialogues,actors,festivalName,Game1.year,Game1.season,Game1.dayOfMonth,Game1.timeOfDay);
        }

        internal void AddOverheardLine(NPC otherNpc, NPC instance, List<StardewValley.DialogueLine> theLine)
        {
            var character = GetCharacter(otherNpc);
            var filteredDialogues = FilterForHistory(theLine, character);
            character.AddOverheardDialogue(instance, filteredDialogues, Game1.year, Game1.season, Game1.dayOfMonth, Game1.timeOfDay);
        }

        internal void AddConversation(NPC otherNpc, string newDialogue)
        {
            var character = GetCharacter(otherNpc);
            DialogueContext context = LastContext;
            var fullHistory = context.ChatHistory.ToList();
            if (!string.IsNullOrEmpty(newDialogue))
            {
                fullHistory.Add(newDialogue);
            }
            character.AddConversation(fullHistory.ToArray(), Game1.year, Game1.season, Game1.dayOfMonth, Game1.timeOfDay);
        }

        internal bool PatchNpc(NPC n,int probability=4,bool retainResult=false)
        {
            if (LlmDisabled || !ModEntry.Config.EnableMod || probability == 0)
            {
                return false;
            }
            if (ModEntry.Config.DisabledCharactersList.Contains(n.Name))
            {
                return false;
            }
            if (ModEntry.BlockModdedContent)
            {
                if (_characters.Count == 0)
                {
                    PopulateCharacters();
                }
                var character = GetCharacter(n);
                if (string.IsNullOrWhiteSpace(character?.Bio?.Biography ?? ""))
                {
                    return false;
                }
            }
            if (probability < 4)
            {
                if (retainResult)
                {
                    if (_patchDate != Game1.Date.TotalDays || _patchCharacters == null)
                    {
                        _patchDate = Game1.Date.TotalDays;
                        _patchCharacters = new Dictionary<string, bool>();
                    }
                    if (_patchCharacters.ContainsKey(n.Name))
                    {
                        return _patchCharacters[n.Name];
                    }
                }
                if (probability == -1)
                {
                    // To do - ask for interaction type
                }
                else if (_random.Next(4) >= probability)
                {
                    if (retainResult)
                    {
                        _patchCharacters.Add(n.Name, false);
                    }
                    return false;
                }
                else if (retainResult)
                {
                    _patchCharacters.Add(n.Name, true);
                }
            }

            return true;
        }
    }
}