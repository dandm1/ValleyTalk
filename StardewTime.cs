using System;

namespace StardewDialogue;

internal class StardewTime
{
    public StardewValley.Season season { get; set; }
    public int dayOfMonth { get; set; }
    public int timeOfDay { get; set; }
    public int year { get; set; }

    public StardewTime()
    {}

    public StardewTime(int year, StardewValley.Season season, int dayOfMonth, int timeOfDay)
    {
        this.year = year;
        this.season = season;
        this.dayOfMonth = dayOfMonth;
        this.timeOfDay = timeOfDay;
    }
    
    public double DaysSince(int year, StardewValley.Season season, int dayOfMonth, int timeOfDay)
    {
        return DaysSince(new StardewTime(year, season, dayOfMonth, timeOfDay));
    }

    public double DaysSince(StardewTime other)
    {
        double days = 0;
        days += (other.year - year) * 112;
        days += (SeasonToInt(other.season) - SeasonToInt(season)) * 28;
        days += other.dayOfMonth - dayOfMonth;
        days += (other.timeOfDay - timeOfDay) / 2400.0;
        return days;
    }

    public string SinceDescription(StardewTime other)
    {
        double days = DaysSince(other);
        return days switch
        {
            < 0 => "In the future",
            < (double)1/120 => "just now",
            < (double)1/24 => "in the last hour",
            < 1 => other.dayOfMonth == dayOfMonth ? "earlier today" : "yesterday",
            < 14 => $"{(int)days} days ago",
            < 56 => $"{(int)days} days ago on {other.dayOfMonth} {other.season.ToString().ToLower()}",
            < 112 => other.year == year ? "earlier this year on {other.dayOfMonth} {other.season.ToString().ToLower()}" : "last year on {other.dayOfMonth} {other.season.ToString().ToLower()}",
            _ => "a long time ago on {other.dayOfMonth} {other.season.ToString().ToLower()}"
        };
    }

    private int SeasonToInt(StardewValley.Season season)
    {
        return season switch
        {
            StardewValley.Season.Spring => 0,
            StardewValley.Season.Summer => 1,
            StardewValley.Season.Fall => 2,
            StardewValley.Season.Winter => 3,
            _ => throw new Exception("Invalid season")
        };
    }
}
