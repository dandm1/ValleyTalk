namespace LlamaDialogue;
using System.Collections.Generic;

public interface ILlmInference
{
    IAsyncEnumerable<string> Generate(string system, string prompt);
}