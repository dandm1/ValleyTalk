namespace StardewDialogue;

public class Prompts
{
    public Prompts()
    {
    }

    public string System { get; internal set; }
    public string GameConstantContext { get; internal set; }
    public string NpcConstantContext { get; internal set; }
    public string Context { get; internal set; }
    public string Command { get; internal set; }
    public string ResponseStart { get; internal set; }
    public string Instructions { get; internal set; }
    public string Name { get; internal set; }
    public string? Gender { get; internal set; }
}