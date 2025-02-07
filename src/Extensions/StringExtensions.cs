using System.Text;
                   
public static class StringExtensions
{
    public static string ToTitleCase(this string aText)
    {
        if(string.IsNullOrWhiteSpace(aText))
        {
            return aText;
        }
        return char.ToUpper(aText[0]) + aText[1..].ToLower();
    }
}