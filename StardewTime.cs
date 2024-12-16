using System;

namespace StardewDialogue;

internal class StardewTime : IComparable<StardewTime>
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
            < 112 => other.year == year ? $"earlier this year on {other.dayOfMonth} {other.season.ToString().ToLower()}" : $"last year on {other.dayOfMonth} {other.season.ToString().ToLower()}",
            _ => $"a long time ago on {other.dayOfMonth} {other.season.ToString().ToLower()}"
        };
    }

    private static int SeasonToInt(StardewValley.Season season)
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

    private static StardewValley.Season IntToSeason(int season)
    {
        return season switch
        {
            0 => StardewValley.Season.Spring,
            1 => StardewValley.Season.Summer,
            2 => StardewValley.Season.Fall,
            3 => StardewValley.Season.Winter,
            _ => throw new Exception("Invalid season")
        };
    }

    internal StardewTime AddDays(int offset)
    {
        int targetYear = year;
        int targetSeason = SeasonToInt(season);
        int targetDay = dayOfMonth + offset;
        while (targetDay > 28)
        {
            targetSeason++;
            targetDay -= 28;
            if (targetSeason == 4)
            {
                targetYear++;
                targetSeason = 0;
            }
        }
        while (targetDay < 1)
        {
            targetSeason--;
            targetDay += 28;
            if (targetSeason == -1)
            {
                if (targetYear == 0)
                {
                    return new StardewTime(0,0,0,600);
                }
                targetYear--;
                targetSeason = 3;
            }
        }
        return new StardewTime(targetYear,IntToSeason(targetSeason),targetDay,600);
    }

    public int CompareTo(StardewTime other)
    {
        if (year != other.year)
        {
            return year - other.year;
        }
        if (SeasonToInt(season) != SeasonToInt(other.season))
        {
            return SeasonToInt(season) - SeasonToInt(other.season);
        }
        if (dayOfMonth != other.dayOfMonth)
        {
            return dayOfMonth - other.dayOfMonth;
        }
        return timeOfDay - other.timeOfDay;
    }
}
