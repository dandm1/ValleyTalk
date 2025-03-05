using System;
using StardewValley;
using ValleyTalk;

namespace StardewDialogue
{

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

    public StardewTime(WorldDate date,int time)
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
        days += (other.timeOfDay - timeOfDay) / 2400.0;
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
        
        if (days < 0)
        {
            return Util.GetString("timeInTheFuture");
        }
        else if (days < (double)1/120)
        {
            return Util.GetString("timeJustNow");
        }
        else if (days < (double)1/24)
        {
            return Util.GetString("timeInTheLastHour");
        }
        else if (days < 1)
        {
            return other.dayOfMonth == dayOfMonth ? Util.GetString("timeEarlierToday") : Util.GetString("timeYesterday");
        }
        else if (days < 14)
        {
            return Util.GetString("timeDaysAgo", new {days = (int)days});
        }
        else if (days < 56)
        {
            return Util.GetString("timeDaysAgoSeasonDay", new {days = (int)days, day = this.dayOfMonth, season = seasonDisplay});
        }
        else if (days < 112)
        {
            return other.year == year ? 
                   Util.GetString("timeEarlierThisYear", new {day = this.dayOfMonth, season = seasonDisplay})
                 : Util.GetString("timeLastYear", new {day = this.dayOfMonth, season = seasonDisplay});
        }
        else
        {
            return Util.GetString("timeALongTimeAgo", new {day = this.dayOfMonth, season = seasonDisplay, year = this.year});
        }
    }

    private static int SeasonToInt(StardewValley.Season season)
    {
        if (season == StardewValley.Season.Spring)
            return 0;
        else if (season == StardewValley.Season.Summer)
            return 1;
        else if (season == StardewValley.Season.Fall)
            return 2;
        else if (season == StardewValley.Season.Winter)
            return 3;
        else
            throw new Exception("Invalid season");
    }

    private static StardewValley.Season IntToSeason(int season)
    {
        if (season == 0)
            return StardewValley.Season.Spring;
        else if (season == 1)
            return StardewValley.Season.Summer;
        else if (season == 2)
            return StardewValley.Season.Fall;
        else if (season == 3)
            return StardewValley.Season.Winter;
        else
            throw new Exception("Invalid season");
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
}
