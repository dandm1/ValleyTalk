using System.Collections.Concurrent;
using System.Threading;

namespace LlamaDialogue;

public class DialogueGenerator
{
    private readonly Thread _worker;
    
    private DialogueGenerator()
    {
        _worker = new Thread(Worker);
        _worker.Start();
    }

    private async void Worker(object obj)
    {
        while (Config == null)
        {
            Thread.Sleep(100);
        }
        if (string.IsNullOrWhiteSpace(Config.ServerAddress))
        {
            Engine = new LocalLlmInference();
        }
        else
        {
            Engine = new RemoteLlmInference(Config.ServerAddress);
        }
        while (true)
        {
            if (PriorityJob != null)
            {
                await PriorityJob.Generate(Engine);
                PriorityJob = null;
            }
            else
            {
                if (Jobs.TryDequeue(out var job))
                {
                    await job.Generate(Engine);
                }
                else
                {
                    Thread.Sleep(20);
                }
            }
        }
    }

    public ILlmInference Engine { get; private set;}

    public static DialogueGenerator Instance { get; } = new();

    public ConcurrentQueue<GenerationJob> Jobs { get; } = new();

    public GenerationJob PriorityJob { get; set; }

    public ModConfig Config { get; set; } = null;
}
