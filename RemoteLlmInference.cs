using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace LlamaDialogue;

internal class RemoteLlmInference : ILlmInference
{
    private string serverAddress;
    private static PromptFormatter _formatter;
    private HttpClient _client = new();
    public RemoteLlmInference(string serverAddress)
    {
        this.serverAddress = serverAddress;

    }

    public async IAsyncEnumerable<string> Generate(string system, string prompt)
    {
        var interpolatedPrompt = _formatter.Format(system, prompt);
        // Use httpclient to retreive a response from http://[serverAddress]/completion where a POST call is made with a json document 
        // consisting of the interpolatedPrompt as prompt and n_predict as 2048.  Return the 'content' field of the response.

        var requestBody = new { prompt = interpolatedPrompt, n_predict = 2048, stream = false };
        var response = await _client.PostAsJsonAsync($"{serverAddress}/completion", requestBody);

        // do the same as below, but one streaming message at a time
        // var responseStream = await response.Content.ReadAsStreamAsync();
        // var responseJson = await JsonDocument.ParseAsync(responseStream);
        // var responseText = responseJson.RootElement.GetProperty("content").GetString();
        // responseText = _formatter.Strip(responseText);
        // foreach (var word in responseText.Split(' '))
        // {
        //     yield return word+' ';
        // }

        var responseStream = await response.Content.ReadAsStringAsync();
        var responseJson = JsonDocument.Parse(responseStream);
        var responseText = responseJson.RootElement.GetProperty("content").GetString();
        responseText = _formatter.Strip(responseText);
        foreach (var word in responseText.Split(' '))
        {
            yield return word+' ';
        }
    }
}