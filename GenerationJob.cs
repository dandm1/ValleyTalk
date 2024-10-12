using System;
using System.Collections.Generic;
using StardewValley;
using StardewModdingAPI;
using System.Linq;
using System.Threading.Tasks;

namespace LlamaDialogue;

public class GenerationJob
{
    public GenerationJob(string basePath, Dialogue dialogue, IMonitor monitor)
    {   
        BasePath = basePath;
        BaseDialogue = dialogue;
        SMonitor = monitor;
    }

    public string BasePath { get; }
    public Dialogue BaseDialogue { get; }
    public IMonitor SMonitor { get; }

    internal async Task Generate(ILlmInference engine)
    {
        // Concatenate all of the dialogue lines into a single string from the BaseDialogue
        var originalText = string.Join(" ", BaseDialogue.dialogues.Select(d => d.Text));
        try
        {
        var generation = engine.Generate($"You are an expert computer game copywriter, experienced in the game the StarDew Valley and expert in retaining the voice of characters while adding contextual details.  To add variety to the game you are writing alternatives so some of the standard dialogue.", $"Propose an alternative dialogue for {BaseDialogue.speaker.Name}, retaining the length and style of the original. Just return the alternative text. The original line was '{originalText}'");
        
        // Chunk the generated text into dialogue lines, and then create a new list from those
        var dialogueFlag = new List<DialogueLine>
        {
            new("***PROCESSING***")
        };
        BaseDialogue.dialogues = dialogueFlag;

        var dialogues = new List<DialogueLine>();
        var nextLine = string.Empty;
        var nextSentence = string.Empty;
        var summary = string.Empty;
        await foreach (var word in generation)
        {
            summary += word;
            nextSentence += word;
            if (word.Contains('.'))
            {
                if (nextLine.Length + nextSentence.Length > 150)
                {
                    dialogues.Add(new DialogueLine(nextLine));
                    nextLine = string.Empty;
                }
                nextLine += nextSentence;
                nextSentence = string.Empty;
            }
        }
        if (!string.IsNullOrWhiteSpace(nextSentence) || !string.IsNullOrWhiteSpace(nextLine))
        {
            dialogues.Add(new DialogueLine(nextLine + nextSentence));
        }

        BaseDialogue.dialogues = dialogues;
        SMonitor.Log($"Generated dialogue for {BasePath} of {summary}", LogLevel.Error);
        }
        catch (Exception e)
        {
            SMonitor.Log($"Error generating dialogue for {BasePath} of {originalText}: {e.Message}", LogLevel.Error);
        }
    }
}