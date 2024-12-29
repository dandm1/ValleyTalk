using System;
using System.Collections.Generic;

namespace StardewDialogue;

public class BioData
{
    private bool? isMale;
    private string unique;

    public string Biography { get; set; } = string.Empty;
    //Only update gender if the value passed is male or female
    public string? Gender {
        get{ return isMale == null ? null : (isMale.Value ? "Male" : "Female"); }
        set
        {
            if (value == null)
            {
                isMale = null; 
                return;
            }
            if (value.Equals("male", StringComparison.OrdinalIgnoreCase) || value.Equals("female", StringComparison.OrdinalIgnoreCase))
            {
                isMale = value.Equals("male", StringComparison.OrdinalIgnoreCase);
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
            if (!ExtraPortraits.ContainsKey('u') && !string.IsNullOrWhiteSpace(value))
            {
                ExtraPortraits.Add('u', value);
            }
        }
    }
    public Dictionary<char,string> ExtraPortraits { get; set; } = new Dictionary<char, string>();
    public List<string> Preoccupations { get; set; } = new List<string>();
    public Dictionary<string,string> Dialogue { get; set; } = new Dictionary<string, string>();
    public bool HomeLocationBed { get; set; } = false;

    public string GenderP2 => (isMale ?? false) ? "he" : "she";
    
    public string GenderPronoun => (isMale ?? false) ? "him" : "her";
    public string GenderPossessive => (isMale ?? false) ? "his" : "her";

}