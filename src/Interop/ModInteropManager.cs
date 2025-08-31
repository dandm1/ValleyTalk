using System.Collections.Generic;
using System.Linq;

namespace ValleyTalk;

public class ModInteropManager
{
    private ModInteropManager() { }
    private static ModInteropManager _instance;
    public static ModInteropManager Instance => _instance ??= new ModInteropManager();
    private Dictionary<string, Dictionary<string, Dictionary<string, string>>> _promptOverrides = new();

    public void RegisterPromptOverride(string modName, string characterName, string promptElement, string overrideText)
    {
        if (!_promptOverrides.ContainsKey(characterName))
        {
            _promptOverrides[characterName] = new Dictionary<string, Dictionary<string, string>>();
        }
        if (!_promptOverrides[characterName].ContainsKey(promptElement))
        {
            _promptOverrides[characterName][promptElement] = new Dictionary<string, string>();
        }
        _promptOverrides[characterName][promptElement][modName] = overrideText;
    }

    public void ClearPromptOverride(string modName, string characterName, string promptElement)
    {
        if (_promptOverrides.ContainsKey(characterName) && _promptOverrides[characterName].ContainsKey(promptElement))
        {
            _promptOverrides[characterName][promptElement].Remove(modName);
            if (_promptOverrides[characterName][promptElement].Count == 0)
            {
                _promptOverrides[characterName].Remove(promptElement);
                if (_promptOverrides[characterName].Count == 0)
                {
                    _promptOverrides.Remove(characterName);
                }
            }
        }
    }

    public void ClearPromptOverrides(string modName, string characterName = "")
    {
        if (string.IsNullOrWhiteSpace(characterName))
        {
            foreach (var character in _promptOverrides.Keys.ToList())
            {
                foreach (var promptElement in _promptOverrides[character].Keys.ToList())
                {
                    _promptOverrides[character][promptElement].Remove(modName);
                    if (_promptOverrides[character][promptElement].Count == 0)
                    {
                        _promptOverrides[character].Remove(promptElement);
                    }
                }
                if (_promptOverrides[character].Count == 0)
                {
                    _promptOverrides.Remove(character);
                }
            }
        }
        else
        {
            if (_promptOverrides.ContainsKey(characterName))
            {
                foreach (var promptElement in _promptOverrides[characterName].Keys.ToList())
                {
                    _promptOverrides[characterName][promptElement].Remove(modName);
                    if (_promptOverrides[characterName][promptElement].Count == 0)
                    {
                        _promptOverrides[characterName].Remove(promptElement);
                    }
                }
                if (_promptOverrides[characterName].Count == 0)
                {
                    _promptOverrides.Remove(characterName);
                }
            }
        }
    }

    internal Dictionary<string, IEnumerable<string>> GetPromptOverrides(Character character)
    {
        if (_promptOverrides.TryGetValue(character.Name, out var overrides))
        {
            return overrides.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Values.AsEnumerable());
        }
        return [];
    }
}
