using System.Text;
                   
public static class StringExtensions
{
    public static string ToTitleCase(this string aText)
    {
        if(string.IsNullOrWhiteSpace(aText))
        {
            return aText;
        }
        return char.ToUpper(aText[0]) + aText.Substring(1).ToLower();
    }
}