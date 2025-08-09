namespace ValleyTalk;

internal class ActivityHistory : IHistory
{
    private string activity;

    public ActivityHistory(string activity)
    {
        this.activity = activity;
    }

    public string Format(string npcName)
    {
        return activity;
    }
}