namespace LlamaDialogue;
using LLama.Common;
using LLama;
using System.Collections.Generic;
using LLama.Native;
using System;

public class LocalLlmInference : ILlmInference
{
    public LLamaContext Context { get; private set; }
    public InstructExecutor Executor { get; }
    public PromptFormatter Formatter { get; }
    public InferenceParams Params { get; }

    public LocalLlmInference()
    {
        string modelPath = @"/home/david/Downloads/mistral-7b-instruct-v0.2.Q8_0.gguf";

         var parameters = new ModelParams(modelPath)
        {
            ContextSize = 1024 // The longest length of chat as memory.
        };
        var model = LLamaWeights.LoadFromFile(parameters);
        Context = model.CreateContext(parameters);
        Executor = new InstructExecutor(Context); 
        
        Formatter = new PromptFormatter("<s>[INST] {0} {1} [/INST]","</s>");

        Params = new InferenceParams() { Temperature = 0.8f, MaxTokens = 600,  };
    }

    async private IAsyncEnumerable<string> MakeStaticResult(string result)
    {
        // Yield a fixed string for testing purposes in an async format
        var syncResponse = new List<string>( result.Split(' '));
        foreach (var word in syncResponse) { yield return word; } 
    }

    async public IAsyncEnumerable<string> Generate(string system, string prompt)
    {
        var interpolatedPrompt = Formatter.Format(system, prompt);
        NativeApi.llama_kv_cache_clear(Context.NativeHandle);
        var response = Executor.InferAsync(interpolatedPrompt, Params);
        // Yield a fixed string for testing purposes in an async format
        
        //var response = MakeStaticResult("This is a test response");
        await foreach (var word in response)
        {
            yield return word;
        }
    }
}
