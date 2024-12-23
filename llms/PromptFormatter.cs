namespace ValleyTalk;

public class PromptFormatter
{
    private string _format;
    private string _eot;

    public PromptFormatter(string format, string EoT)
    {
        _format = format;
        _eot = EoT;
    }

    public string Format(string system, string prompt)
    {
        return string.Format(_format, system, prompt);
    }

    // Strip quotation marks and EoT tokens from response
    public string Strip(string response)
    {
        return response.Replace("\"", "").Replace(_eot, "");
    }
}
