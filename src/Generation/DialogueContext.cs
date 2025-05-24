using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlTypes;
using System.Linq;
using StardewValley.Characters;

namespace StardewDialogue;

public class DialogueContext
{
    private static readonly string[] singles =new string[] {"Emily", "Haley", "Maru", "Penny", "Sam", "Sebastian", "Shane", "Abigail", "Elliott", "Harvey", "Leah", "Alex", "Krobus"};
    private static readonly int[] heartsOptions = new int[] {0, 2, 4, 6, 8, 10};
    private static readonly int[] friendHeartOptions = new int[] {0, 6, 8, 10};
    private static readonly Season?[] seasonOptions = new StardewDialogue.Season?[] {StardewDialogue.Season.Spring, StardewDialogue.Season.Summer, StardewDialogue.Season.Fall, StardewDialogue.Season.Winter, null};
    public static readonly string[] locations = new string[] {"Beach", "Desert", "Railroad", "Saloon", "SeedShop", "JojaMart"};
    public static readonly string[] resortTags = new string[] {"Resort", "Resort_Entering", "Resort_Leaving"};
    private static readonly int[] yearOptions = new int[] {1, 2};
    public static string[] specialContexts = new string[] {"cc_Boulder", "cc_Bridge", "cc_Bus", "cc_Greenhouse", "cc_Minecart", "cc_Complete", "movieTheater", "pamHouseUpgrade", "pamHouseUpgradeAnonymous", "jojaMartStruckByLightning", "babyBoy", "babyGirl", "wedding", "event_postweddingreception", "luauBest", "luauShorts", "luauPoisoned", "Characters_MovieInvite_Invited", "DumpsterDiveComment", "SpouseStardrop", "FlowerDance_Accept_Spouse", "FlowerDance_Accept", "FlowerDance_Decline", "GreenRain", "GreenRainFinished", "GreenRain_2", "Rainy"};
   
    public int? Hearts { get; init; }
    public Season? Season { get; init; }
    public int? Year { get; init; }
    public Weekday? Day {get; set; }
    public int? DayOfSeason { get; init; }
    public string? Inlaw { get; init; }
    public StardewValley.Object Accept { get; set; }
    public string? TimeOfDay { get; init; }
    public RandomAction? RandomAct { get; set; }
    public int? RandomValue { get; init; }
    public SpouseAction? SpouseAct { get; set; }
    public string? Spouse { get; init; }
    public string ChatID { get; init; }
    public string[] ChatHistory { get; set; } = Array.Empty<string>();
    public bool LastLineIsPlayerInput { get; set; } = false; // Tracks if the last line came from the player

    private string[] elements = Array.Empty<string>();
    public bool Married { get; set; } = false;
    public int TargetSamples { get; set; } = 15;

    public DialogueContext(int hearts, Season? season, int? year) : this()
    {
        Hearts = hearts;
        Season = season;
        Year = year;
    }

    public DialogueContext()
    {
    }

    private void BuildElements()
    {
        var newElements = new List<string>();
        if (ChatID != null)
        {
            elements = new string[] {ChatID};
            return;
        }

        if (Location != null)
        {
            if (Hearts != null && Hearts > 0)
            {
                newElements.Add(Location + Hearts.ToString());
            }
            else
            {
                newElements.Add(Location);
            }
        }

        if (Season != null)
        {
            newElements.Add(Season.ToString().ToLower());
        }
        if (Day != null)
        {
            var dayString = Day.ToString();
            if (Hearts != null && Hearts > 0)
            {
                dayString += Hearts.ToString();
            }
            newElements.Add(dayString);
        }
        else if (DayOfSeason != null)
        {
            newElements.Add(DayOfSeason.ToString());
        }
        else if (Spouse != null && SpouseAct == null && RandomAct == null)       
        {
            newElements.Add(Spouse);
        }
        if (Accept != null)
        {
            newElements.Add("AcceptGift");
            newElements.Add($"(O){Accept}");
        }
        if (RandomAct != null)
        {
            newElements.Add(RandomAct.ToString());
            if (TimeOfDay != null)
            {
                newElements.Add(TimeOfDay);
            }
            if (RandomValue != null)
            {
                newElements.Add(RandomValue.ToString());
            }
            else if (Spouse != null)
            {
                newElements.Add(Spouse);
            }
            else
            {
                newElements.Add("");
            }
        }
        if (SpouseAct != null)
        {
            newElements.Add(SpouseAct.ToString());
            newElements.Add(Spouse);
        }
        if (Year != null && Year > 1)
        {
            newElements.Add(Year.ToString());
        }
        if (Inlaw != null)
        {
            newElements.Add("inlaw");
            newElements.Add(Inlaw);
        }
        elements = newElements.ToArray();
    }

    public DialogueContext(string value)
    {
        Value = value;
        elements = value.Split('_');

        // Delete any empty elements from the start
        while (elements.Length > 0 && elements[0] == "")
        {
            elements = elements.Skip(1).ToArray();
        }
        // If the first element in an M then set the context to married, and remove it from the list
        if (elements[0] == "M")
        {
            Married = true;
            elements = elements.Skip(1).ToArray();
        }
        if (elements[0] == "B")
        {
            Birthday = true;
            elements = elements.Skip(1).ToArray();
        }
        // Check if the first element is a season.  If so, set the season and remove it from the list
        if (!int.TryParse(elements[0], out _) && Enum.TryParse<Season>(elements[0], true, out Season season))
        {
            Season = season;
            elements = elements.Skip(1).ToArray();
        }
        if (elements.Length == 0) return;
        // Check if the first element is a valid GUID. If so, set the chat ID and remove it from the list
        if (Guid.TryParse(elements[0], out _))
        {
            ChatID = $"{elements[0]}_{elements[1]}";
            elements = elements.Skip(2).ToArray();
        }
        else if (locations.Any(x => value.StartsWith(x, StringComparison.OrdinalIgnoreCase)))
        {
            Location = locations.First(x => value.StartsWith(x, StringComparison.OrdinalIgnoreCase));
            var rest = value.Substring(Location.Length);
            if (int.TryParse(rest, out var hearts))
            {
                Hearts = hearts;
            }
            else
            {
                Hearts = 0;
            }
            elements = elements.Skip(1).ToArray();
        }
        else if (specialContexts.Any(x => value.StartsWith(x, StringComparison.OrdinalIgnoreCase)) && !value.Contains("_Day") && !value.Contains("_Night"))
        {
            ChatID = specialContexts.First(x => value.StartsWith(x, StringComparison.OrdinalIgnoreCase));
            var rest = value.Substring(ChatID.Length);
            if (int.TryParse(rest, out var hearts))
            {
                Hearts = hearts;
            }
            else
            {
                Hearts = 0;
            }
            elements = elements.Skip(1).ToArray();
        }
        // Check if the first element is a day of the week followed by a number.  If so, set the day and use the number as a heart level.
        else if (elements[0].Length >= 3 && Enum.TryParse<Weekday>(elements[0].Substring(0,3), true, out var day))
        {
            Day = day;
            if (elements[0].Length > 3)
            {
                Hearts = int.Parse(elements[0].Substring(3));
            }
            else
            {
                Hearts = 0;
            }
            elements = elements.Skip(1).ToArray();
        }
        else if (int.TryParse(elements[0], out var dayOfSeason))
        {
            DayOfSeason = dayOfSeason;
            elements = elements.Skip(1).ToArray();
        }
        else if (elements[0].StartsWith("Accept", StringComparison.OrdinalIgnoreCase))
        {
            // If element 1 starts with (O) then remove it
            var gift = elements[1];
            while (gift.StartsWith("(O)"))
            {
                gift = gift.Substring(3);
            }

            //Accept = gift;
            elements = elements.Skip(2).ToArray();
        }
        else if (elements.Length >= 1 && Enum.TryParse<RandomAction>(elements[0], true, out var randomAction))
        {
            RandomAct = randomAction;
            if (( randomAction == RandomAction.Rainy || randomAction == RandomAction.Indoor) && elements.Length >= 2)
            {
                TimeOfDay = elements[1];
                elements = elements.Skip(2).ToArray();
            }
            else
            {
                elements = elements.Skip(1).ToArray();
            }
            if (elements.Length > 0 && int.TryParse(elements[0], out var randomValue))
            {
                RandomValue = randomValue;
                elements = elements.Skip(1).ToArray();
            }
        }
        else if (elements.Length >= 2 && Enum.TryParse<SpouseAction>(elements[0], true, out var spouseAction))
        {
            SpouseAct = spouseAction;
            Spouse = elements[1];
            elements = elements.Skip(2).ToArray();
        }
        else
        {
            ChatID = value;
            elements = Array.Empty<string>();
        }
        if (elements.Length == 0) return;
        // If the first element is a number, set the year and remove it from the list
        if (int.TryParse(elements[0], out var year))
        {
            Year = year;
            elements = elements.Skip(1).ToArray();
        }

        // If there are two or more remaining elements, check if the next element says "inlaw" and if so put the following element in the inlaw parameter.
        if (elements.Length >= 2 && elements[0] == "inlaw")
        {
            Inlaw = elements[1];
            elements = elements.Skip(2).ToArray();
        }
    }

    public DialogueContext(DialogueContext context)
    {
        Hearts = context.Hearts;
        Season = context.Season;
        Year = context.Year;
        Day = context.Day;
        DayOfSeason = context.DayOfSeason;
        Inlaw = context.Inlaw;
        Accept = context.Accept;
        TimeOfDay = context.TimeOfDay;
        RandomAct = context.RandomAct;
        RandomValue = context.RandomValue;
        SpouseAct = context.SpouseAct;
        Spouse = context.Spouse;
        ChatID = context.ChatID;
        ChatHistory = context.ChatHistory;
        Married = context.Married;
        TargetSamples = context.TargetSamples;
        Location = context.Location;
        LastLineIsPlayerInput = context.LastLineIsPlayerInput;
    }

    private string _value;
    public string Value 
    { 
        get
        {
            BuildElements();
            _value = string.Join('_', elements);
            return _value;
        }
        private set
        {
            _value = value;
        }
    }
    public string[] Elements 
    { 
        get
        {
            if (elements.Length == 0)
            {
                BuildElements();
                _value = string.Join("_", elements);
            }
            return elements;
        }
    }

    public string? Gender { get; internal set; }
    public string Location { get; internal set; }
    public bool Birthday { get; internal set; } = false;
    public bool MaleFarmer { get; internal set; }
    public List<ChildDescription> Children { get; internal set; }
    public int GiftTaste { get; internal set; }
    public List<string> Weather { get; internal set; }
    public string ScheduleLine { get; internal set; }
    public bool CanGiveGift { get; internal set; } = false;

    // Add a method to return a value representing how different two contexts are - this will be used to find the most similar context to the current context
    public int CompareTo(DialogueContext other)
    {
        var difference = 0;
        // If all elements of other are null, they are very different
        if (other == null || (other.Hearts == null && other.Season == null && other.Year == null && other.Day == null && other.DayOfSeason == null && other.Inlaw == null && other.Accept == null && other.TimeOfDay == null && other.RandomAct == null && other.SpouseAct == null && other.ChatID == null && other.Married == false && other.Location == null && other.Birthday == false))
        {
            return 10000;
        }
        // If they are both hearts based, favour hearts that are similar.  If only one is hearts based, they are very different.
        difference += Math.Abs(Hearts ?? 0 - other.Hearts ?? 0) * 100;

        if (Season != other.Season)
        {
            difference += 50;
        }
        if (Day != other.Day)
        {
            difference++;
        }
        if (DayOfSeason != other.DayOfSeason)
        {
            difference += 200;
        }
        // If only one is a gift acceptance, they are very different
        if ((Accept == null) ^ (other.Accept == null))
        {
            difference += 2000;
        }
        if (TimeOfDay != other.TimeOfDay)
        {
            difference+= 20;
        }
        difference += CompareValues(RandomAct, other.RandomAct,0,200,2000);
        difference += CompareValues(SpouseAct, other.SpouseAct,0,200,2000);
        difference += CompareValuesNull(Spouse, other.Spouse,0,10000,2000);
        difference += CompareValues(Year, other.Year,0,200,200);
        difference += CompareValuesNull(Inlaw, other.Inlaw,0,500,1000);
        // Add a random factor
        difference += new Random().Next(0, 10);

        return difference;
    }

    private int CompareValues<T>(Nullable<T> item1, Nullable<T> item2, int ifEqual, int ifDifferent, int ifOneUndefined) where T : struct
    {
        if (item1 == null ^ item2 == null)
        {
            return ifOneUndefined;
        }
        if (item1 == null && item2 == null)
        {
            return ifEqual;
        }

        var i1v = (T)item1;
        var i2v = (T)item2;

        if (EqualityComparer<T>.Default.Equals(i1v, i2v))
        {
            return ifEqual;
        }
        
        return ifDifferent;
    }

    private int CompareValuesNull<T>(T item1, T item2, int ifEqual, int ifDifferent, int ifOneUndefined) where T : class
    {
        if (item1 == null ^ item2 == null)
        {
            return ifOneUndefined;
        }
        if (item1 == null && item2 == null)
        {
            return ifEqual;
        }

        var i1v = (T)item1;
        var i2v = (T)item2;

        if (EqualityComparer<T>.Default.Equals(i1v, i2v))
        {
            return ifEqual;
        }
        
        return ifDifferent;
    }

    internal static bool IsSpecialContext(string chatID)
    {
        return specialContexts.Contains(chatID);
    }

    // Override the equals method to compare the value of the context
    public override bool Equals(object obj)
    {
        if (obj is DialogueContext other)
        {
            // Check all the properties from the clone constructor

            return Hearts == other.Hearts &&
                Season == other.Season &&
                Year == other.Year &&
                Day == other.Day &&
                DayOfSeason == other.DayOfSeason &&
                Inlaw == other.Inlaw &&
                Accept == other.Accept &&
                TimeOfDay == other.TimeOfDay &&
                RandomAct == other.RandomAct &&
                SpouseAct == other.SpouseAct &&
                ChatID == other.ChatID &&
                Married == other.Married &&
                Location == other.Location &&
                Birthday == other.Birthday ;
            
        }
        return false;
    }
}
