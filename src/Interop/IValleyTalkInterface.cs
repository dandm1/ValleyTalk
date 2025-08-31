namespace ValleyTalk;

public interface IValleyTalkInterface
{
    void SetModName(string modName);
    bool IsEnabledForCharacter(StardewValley.NPC character);
    void RegisterPromptOverride(string characterName, string promptElement, string overrideText);
    void ClearPromptOverride(string characterName, string promptElement);
    void ClearPromptOverrides(string characterName = "");
}