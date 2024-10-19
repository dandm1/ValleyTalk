using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Text;
using StardewDialogue;
using StardewValley;
using StardewValley.Characters;

namespace LlamaDialogue
{
    internal class DialogueBuilder
    {
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

        private static DialogueBuilder _instance;
        private Dictionary<string, StardewDialogue.Character> _characters;

        private DialogueBuilder()
        {
            _characters = new Dictionary<string, StardewDialogue.Character>();
        }

        private StardewDialogue.Character GetCharacter(NPC instance)
        {
            if (!_characters.ContainsKey(instance.Name))
            {
                var newCharacter = new StardewDialogue.Character(
                    instance.Name, 
                    $"/home/david/Downloads/Canon Friendly Dialogue Expansion-2544-2-3-1-1717127684/[CP] Canon Friendly Dialogue Expansion/Data/NPCs/{instance.Name}.json",
                    $"bio/{instance.Name}.txt");
                newCharacter.StardewNpc = instance;
                _characters.Add(instance.Name, newCharacter);
            }
            return _characters[instance.Name];
        }

        internal Dialogue GenerateGift(NPC instance, StardewValley.Object gift, int taste)
        {
            var character = GetCharacter(instance);
            DialogueContext context = GetBasicContext(instance);
            context.Accept = gift;
            context.GiftTaste = taste;
            var theLine = character.CreateBasicDialogue(context);

            return new Dialogue(instance, $"Accept_{gift.ItemId}", theLine);
        }

        internal Dialogue Generate(NPC instance, string dialogueKey)
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

            var theLine = character.CreateBasicDialogue(context);

            return new Dialogue(instance, dialogueKey, theLine);
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
                    timeOfDay = $"early morning and both the farmer and {instance.Name} have just woken up";
                    break;
                case <= 1130:
                    timeOfDay = "morning";
                    break;
                case <= 1400:
                    timeOfDay = "midday";
                    break;
                case <= 1700:
                    timeOfDay = "afternoon";
                    break;
                case <= 2200:
                    timeOfDay = "evening";
                    break;
                default:
                    timeOfDay = "late at night";
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

        internal void AddDialogueLine(NPC instance, List<StardewValley.DialogueLine> dialogues)
        {
            var character = GetCharacter(instance);
            character.AddDialogue(dialogues,Game1.year,Game1.season,Game1.dayOfMonth,Game1.timeOfDay);
        }
    }
}