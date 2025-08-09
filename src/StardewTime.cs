using System;
using StardewValley;

namespace ValleyTalk;

internal class StardewTime : IComparable<StardewTime>
{
    public StardewValley.Season season { get; set; }
    public int dayOfMonth { get; set; }
    public int timeOfDay { get; set; }
    public int year { get; set; }

    public StardewTime()
    {
        year = 0;
        season = StardewValley.Season.Spring;
        dayOfMonth = 0;
        timeOfDay = 600;
    }

    public StardewTime(WorldDate date, int time)
    {
        year = date.Year;
        season = date.Season;
        dayOfMonth = date.DayOfMonth;
        timeOfDay = time;
    }

    public StardewTime(int year, StardewValley.Season season, int dayOfMonth, int timeOfDay)
    {
        this.year = year;
        this.season = season;
        this.dayOfMonth = dayOfMonth;
        this.timeOfDay = timeOfDay;
    }

    public StardewTime(int addDays)
    {
        var worldDate = Game1.Date;
        year = worldDate.Year;
        season = worldDate.Season;
        dayOfMonth = worldDate.DayOfMonth;
        timeOfDay = 600;
        AddDays(addDays);
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
        return days;
    }

    public string SinceDescription(StardewTime other = null)
    {
        if (other == null)
        {
            other = new StardewTime(Game1.Date, Game1.timeOfDay);
        }
        double days = DaysSince(other);
        var thisSeasonKey = Utility.getSeasonKey(this.season);
        var seasonDisplay = Game1.content.LoadString("Strings\\StringsFromCSFiles:" + thisSeasonKey);
        return days switch
        {
            < 0 => Util.GetString("timeInTheFuture"),
            < (double)1 / 120 => Util.GetString("timeJustNow"),
            < (double)1 / 24 => Util.GetString("timeInTheLastHour"),
            < 1 => other.dayOfMonth == dayOfMonth ? Util.GetString("timeEarlierToday") : Util.GetString("timeYesterday"),
            < 14 => Util.GetString("timeDaysAgo", new { days = (int)days }),
            < 56 => Util.GetString("timeDaysAgoSeasonDay", new { days = (int)days, day = this.dayOfMonth, season = seasonDisplay }),
            < 112 => other.year == year ?
                        Util.GetString("timeEarlierThisYear", new { day = this.dayOfMonth, season = seasonDisplay })
                      : Util.GetString("timeLastYear", new { day = this.dayOfMonth, season = seasonDisplay }),
            _ => Util.GetString("timeALongTimeAgo", new { day = this.dayOfMonth, season = seasonDisplay, year = this.year })
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
                    return new StardewTime(0, 0, 0, 600);
                }
                targetYear--;
                targetSeason = 3;
            }
        }
        return new StardewTime(targetYear, IntToSeason(targetSeason), targetDay, 600);
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

    internal bool After(StardewTime compareTo)
    {
        if (year != compareTo.year)
        {
            return year > compareTo.year;
        }
        if (SeasonToInt(season) != SeasonToInt(compareTo.season))
        {
            return SeasonToInt(season) > SeasonToInt(compareTo.season);
        }
        if (dayOfMonth != compareTo.dayOfMonth)
        {
            return dayOfMonth > compareTo.dayOfMonth;
        }
        return true; // Return true on the same day
    }

    internal bool IsJustNow(StardewTime other = null)
    {
        if (other == null)
        {
            other = new StardewTime(Game1.Date, Game1.timeOfDay);
        }
        var elapsed = DaysSince(other);
        return elapsed < (double)1 / 100 && elapsed >= 0;
    }
}