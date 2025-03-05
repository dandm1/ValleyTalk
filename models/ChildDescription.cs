namespace StardewDialogue
{

public class ChildDescription
{
    public ChildDescription(string name, bool isMale, int age)
    {
        Name = name;
        IsMale = isMale;
        Age = age;
    }

    public string Name { get; internal set; }
    public bool IsMale { get; internal set; }
    public int Age { get; internal set; }
}
}