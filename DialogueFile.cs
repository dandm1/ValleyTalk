using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StardewDialogue;

public class DialogueFile
{
    public List<Change> Changes { get; set; }
    public Dictionary<DialogueContext,IDialogueValue> AllEntries => 
        Changes.SelectMany(c => c.Entries).ToDictionary(e => e.Key, e => e.Value);
}

public class Change
{
    public string LogName { get; set; }
    public string Action { get; set; }
    public string Target { get; set; }
    public Dictionary<DialogueContext,IDialogueValue> Entries { get; set; }
}

public class DialogueValue : IDialogueValue
{
    public DialogueValue(string value)
    {
        Value = value;
        var elements = value.Split('#');
        Elements = new List<IDialogueElement>();
        // Get the index of all elements containing ^
        var genderIndex = elements.Select((e, i) => (e, i)).Where(e => e.e.Contains('^')).Select(e => e.i).ToList();

        if(genderIndex.Count == 1 && genderIndex[0] == (elements.Length - 1) / 2 )
        {
            // recreate elements with the gendered item split
            elements = elements.Take(genderIndex[0]).Concat(elements[genderIndex[0]].Split('^')).Concat(elements.Skip(genderIndex[0] + 1)).ToArray();
            
            var halfLength = elements.Length / 2;
            for (int i = 0; i < halfLength; i++)
            {
                if (elements[i] == elements[i + halfLength])
                {
                    if (elements[i].StartsWith('$') && elements[i].Length == 2)
                    {
                        Elements.Add(new DialogueCommand(elements[i].Substring(1)));
                    }
                    else
                    {
                        Elements.Add(new DialogueLine(elements[i]));
                    }
                    
                }
                else
                {
                    Elements.Add(new DialogueLineGender(elements[i], elements[i + halfLength]));
                }
            }

            Value = string.Join("#", Elements.Select(e => e switch
            {
                DialogueLine line => line.Value,
                DialogueLineGender line => $"{line.Male.Value}^{line.Female.Value}",
                DialogueCommand command => $"${command.Value}",
                _ => string.Empty
            }));
        }
        else 
        {
            foreach (var element in elements)
            {
                if (element.Contains('^'))
                {
                    var genderElements = element.Split('^');
                    if (genderElements.Length == 2)
                    {
                        Elements.Add(new DialogueLineGender(genderElements[0], genderElements[1]));
                    }
                }
                else if (element.Contains("$c"))
                {
                    // No op - skip choice element.
                }
                else
                {
                    if (element.StartsWith('$') && element.Length == 2)
                    {
                        Elements.Add(new DialogueCommand(element.Substring(1)));
                    }
                    else
                    {
                        Elements.Add(new DialogueLine(element));
                    }
                }
            }
        }
    }
    public string Value { get; set; }
    public List<IDialogueElement> Elements { get; }
    public IEnumerable<DialogueValue> AllValues => new List<DialogueValue> { this };
}

public interface IDialogueValue
{
    IEnumerable<DialogueValue> AllValues { get; }
}

public interface IDialogueElement{}

public class RandomisedDialogue : IDialogueValue
{
    public RandomisedDialogue(IEnumerable<DialogueValue> dialogue)
    {
        Dialogue = dialogue;
    }

    public IEnumerable<DialogueValue> Dialogue { get; }

    public IEnumerable<DialogueValue> AllValues => Dialogue;

    public string Value
    {
        get 
        {
            if (Dialogue.Count() <= 1)
            {
                return Dialogue.FirstOrDefault()?.Value ?? string.Empty;
            }
            var builder = new StringBuilder();
            builder.Append("{{Random: ");
            builder.Append(Dialogue.First().Value);
            foreach (var dialogue in Dialogue.Skip(1))
            {
                builder.Append(" ++ ");
                builder.Append(dialogue.Value);
            }
            builder.Append(" |inputSeparator=++}}");

            return builder.ToString();
        }
    }
}

public class DialogueLine : IDialogueElement
{
    public DialogueLine(string element)
    {
        if (element.Contains('$'))
        {
            var index = element.IndexOf('$') + 1;
            if (index == element.Length)
            {
                Value = element;
                Command = string.Empty;
                return;
            }
            var command = element.Substring(index, 1);
            var remainderOfElement =  element.Substring(0, index - 1) + element.Substring(index+1);
            Value = remainderOfElement;
            Command = command;
        }
        else
        {
            Value = element;
            Command = string.Empty;
        }
    }

    public DialogueLine(string command, string value)
    {
        Command = command;
        Value = value;
    }

    public string Command { get; set; }
    public string Value { get; set; }
}

public class DialogueLineGender : IDialogueElement
{
    public DialogueLineGender(string male, string female)
    {
        Male = new DialogueLine(male);
        Female = new DialogueLine(female);
    }

    public DialogueLine Male { get; }
    public DialogueLine Female { get; }
}

public class DialogueCommand : IDialogueElement
{
    public DialogueCommand(string value)
    {
        Value = value;
    }

    public string Value { get; set; }
}

public sealed class DialogueValueJsonConverter : JsonConverter<DialogueValue>
{
    public override DialogueValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new DialogueValue(reader.GetString() ?? string.Empty);
    }

    public override void Write(Utf8JsonWriter writer, DialogueValue value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] DialogueValue value, JsonSerializerOptions options)
    {
        writer.WritePropertyName(value.Value);
    }

    public override DialogueValue ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? value = reader.GetString();
        return new DialogueValue(value ?? string.Empty);
    }
}

public sealed class RandomisedDialogueJsonConverter : JsonConverter<RandomisedDialogue>
{
    public override RandomisedDialogue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dialogue = JsonSerializer.Deserialize<List<DialogueValue>>(ref reader, options);
        return new RandomisedDialogue(dialogue);
    }

    public override void Write(Utf8JsonWriter writer, RandomisedDialogue value, JsonSerializerOptions options)
    {
        if (value.Dialogue.Count() <= 1)
        {
            JsonSerializer.Serialize(writer, value.Dialogue.FirstOrDefault()?.Value, options);
            return;
        }
        var builder = new StringBuilder();
        builder.Append("Random{{");
        builder.Append(JsonSerializer.Serialize(value.Dialogue.First(), options));
        foreach (var dialogue in value.Dialogue.Skip(1))
        {
            builder.Append("++");
            builder.Append(JsonSerializer.Serialize(dialogue, options));
        }
        builder.Append("|inputSeparator=++}}");

        JsonSerializer.Serialize(writer, builder.ToString(), options);
    }
}

public sealed class IDialogueValueJsonConverter : JsonConverter<IDialogueValue>
{
    public override IDialogueValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<DialogueValue>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, IDialogueValue value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}

public sealed class DialogueContextJsonConverter : JsonConverter<DialogueContext>
{
    public override DialogueContext Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new DialogueContext(reader.GetString() ?? string.Empty);
    }

    public override void Write(Utf8JsonWriter writer, DialogueContext value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] DialogueContext value, JsonSerializerOptions options)
    {
        writer.WritePropertyName(value.Value);
    }

    public override DialogueContext ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? key = reader.GetString();
        return new DialogueContext(key ?? string.Empty);
    }
}