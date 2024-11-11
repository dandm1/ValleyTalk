using System;

namespace StardewDialogue;

public class BioData
{
    private bool? isMale;

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
    public string? Unique { get; set; }
    public bool IsChild { get; set; } = false;
    public bool IsSingle { get; set; } = false;
    public string[] Locations { get; set; } = Array.Empty<string>();
    public string[] ResortTags { get; set; } = Array.Empty<string>();
    public int BirthDay {get; set;} = 0;
    public Season BirthSeason {get; set;} = 0;
    public string Home {get; set;} = string.Empty;
    public string GenderP2 => (isMale ?? false) ? "he" : "she";
    
    public string GenderPronoun => (isMale ?? false) ? "him" : "her";
    public string GenderPossessive => (isMale ?? false) ? "his" : "her";

    public bool HomeLocationBed { get; internal set; } = false;
}