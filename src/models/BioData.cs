using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using StardewValley;
using ValleyTalk;

namespace StardewDialogue;

public class BioData
{
    private static string male;
    private static string female;
    private static string he;
    private static string she;
    private static string him;
    private static string her;
    private static string his;
    private static string hers;

    static BioData()
    {
        var translation = ModEntry.SHelper.Translation;
        male = translation.Get("generalMale");
        female = translation.Get("generalFemale");
        he = translation.Get("generalHe");
        she = translation.Get("generalShe");
        him = translation.Get("generalHim");
        her = translation.Get("generalHer");
        his = translation.Get("generalHis");
        hers = translation.Get("generalHers");
    }
    private bool? isMale;
    public bool? IsMale => isMale ?? null;
    private string unique;
    private string name = string.Empty;

    public string Name 
    { 
        get => name; 
        set
        {
            name = value;
            isMale = Game1.getCharacterFromName(name)?.Gender == StardewValley.Gender.Male;
        } 
    }

    public string Biography { get; set; } = string.Empty;
    public ListEntry[] Relationships { get; set; } = new ListEntry[0];
    public ListEntry[] Traits { get; set; } = new ListEntry[0];
    public string BiographyEnd { get; set; } = string.Empty;
    
    //Only update gender if the value passed is male or female
    public string? Gender {
        get{ return isMale == null ? null : (isMale.Value ? male : female); }
        set
        {
            if (value == null)
            {
                isMale = null; 
                return;
            }
            if (value.Equals(male, StringComparison.OrdinalIgnoreCase) || value.Equals(female, StringComparison.OrdinalIgnoreCase))
            {
                isMale = value.Equals(male, StringComparison.OrdinalIgnoreCase);
                return;
            }
            isMale = null;
        }
    }
    public string Unique 
    { 
        get => unique; 
        set 
        {
            unique = value; 
            if (!ExtraPortraits.ContainsKey("u") && !string.IsNullOrWhiteSpace(value))
            {
                ExtraPortraits.Add("u", value);
            }
        }
    }
    public Dictionary<string,string> ExtraPortraits { get; set; } = new Dictionary<string, string>();
    public List<string> Preoccupations { get; set; } = new List<string>();
    public Dictionary<string,string> Dialogue { get; set; } = new Dictionary<string, string>();
    public bool HomeLocationBed { get; set; } = false;

    public string GenderP2 => (isMale ?? false) ? he : she;
    
    public string GenderPronoun => (isMale ?? false) ? him : her;
    public string GenderPossessive => (isMale ?? false) ? his : hers;

    public Dictionary<string,string> PromptOverrides { get; set; } = new Dictionary<string, string>();
    public bool UsePatchedDialogue { get; set; } = false;
    public bool Missing { get; internal set; }

    public class ListEntry
    {
        public string id { get; set; }
        public string Heading { get; set; }
        public string Description { get; set; }
    }

}