using Newtonsoft.Json.Serialization;

internal class LlmResponse
{
    public string Text { get; set; }
    public string ErrorMessage { get; set; }
    public int ResponseCode { get; set; }
    public bool IsSuccess { get; set; } = false; // Default to true for non-streaming responses
    public LlmResponse(string text, bool IsSuccess = true)
    {
        Text = text;
        this.IsSuccess = IsSuccess;
    }

    public LlmResponse(string errorMessage, int responseCode, bool IsSuccess = false)
    {
        ErrorMessage = errorMessage;
        ResponseCode = responseCode;
        this.IsSuccess = IsSuccess;
    }
}