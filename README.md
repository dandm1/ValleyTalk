# ValleyTalk

[![Nexus Mods](https://img.shields.io/badge/Nexus%20Mods-30319-orange)](https://www.nexusmods.com/stardewvalley/mods/30319)
[![Version](https://img.shields.io/badge/version-1.3.0-blue)](https://github.com/dandm1/ValleyTalk/releases)

**Infinite dialogue for Stardew Valley** - A SMAPI mod that uses AI language models to generate dynamic, contextual conversations with NPCs.

## Features

- 🤖 **AI-Powered Dialogue**: Generate infinite, contextual conversations using various AI language models
- 🎭 **Character Consistency**: Each NPC maintains their unique personality and speaking style
- 🌍 **Multi-Language Support**: Translation on the fly or using prompt translation packs.
- 📱 **Cross-Platform**: Works on PC, Mac and Linux.
- 🔌 **Multiple AI Providers**: Support for OpenAI, Anthropic Claude, Google Gemini, Mistral, DeepSeek, and more
- 📚 **Content Pack Support**: Full Content Patcher support and content pack for SVE.
- ⚙️ **Highly Configurable**: Extensive configuration options through Generic Mod Config Menu

## Configuration

ValleyTalk requires configuration of an AI language model provider. The mod supports:

- **OpenAI** (GPT-3.5, GPT-4)
- **Anthropic Claude**
- **Google Gemini**
- **Mistral AI**
- **DeepSeek**
- **SiliconFlow** (international) and **SiliconFlowCN** (China)
- **VolcEngine**
- **LlamaCpp** (for local models)
- **OpenAI-Compatible APIs**

Configure your preferred provider through the mod's config file or using Generic Mod Config Menu.

### SiliconFlow

Choose `SiliconFlow` for the international platform or `SiliconFlowCN` for the China platform. Enter an API key from the matching platform and the full ID of a chat model available to that account (including any organization prefix). A model name is required; no default model is assumed across the two platforms.

The providers use `https://api.siliconflow.com` and `https://api.siliconflow.cn`, respectively. The adapter appends `/v1/chat/completions` and queries `/v1/models?sub_type=chat` for dialogue models. No Server Address setting is required.

Create keys at [SiliconFlow International](https://cloud.siliconflow.com) or [SiliconFlow China](https://cloud.siliconflow.cn). Switching providers clears the API key, following the existing configuration menu behavior; also select a model available on the destination platform. Keep API keys and personal configuration files out of commits and issue reports.

## Architecture

The mod uses Harmony patches to intercept dialogue requests and generates contextually appropriate responses based on:
- Character personalities and relationships
- Current game state and events
- Player history and interactions
- Seasonal and temporal context

### Key Components

- **DialogueBuilder**: Core AI dialogue generation system
- **Character Management**: Maintains NPC personality profiles
- **Event History**: Tracks game events for contextual awareness
- **LLM Integration**: Supports multiple AI provider APIs
- **Content Packs**: Modular character and prompt system

## Development

### Building from Source

```bash
# Clone the repository
git clone https://github.com/dandm1/ValleyTalk.git
cd ValleyTalk

# Build the project
dotnet build src/ValleyTalk.csproj
```

### Project Structure

```
ValleyTalk/
├── src/                    # Main mod source code
│   ├── llms/              # AI provider implementations
│   ├── config/            # Files related to mod configuration
│   ├── Generation/        # Dialogue generation logic
│   ├── Patches/           # Harmony patches
│   ├── Interop/           # API for interaction with other mods
│   └── UI/                # User interface components
├── ContentPack/           # Base content pack
│   └── assets/            # Character bios and prompts
└── Extensions/            # Mod extensions (SVE support)
```

## API & Mod Interoperability

ValleyTalk provides an API for other mods to interact with the dialogue system:

```csharp
// Example: Access the ValleyTalk interface
var vtInterface = Helper.ModRegistry.GetApi<IValleyTalkInterface>("dandm1.ValleyTalk");
```
This allows other mods to temporarily override parts of the prompt for specific game characters, to update the results based on that mod's context.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## License

This project is available under LGPL v3.

## Support

- **Issues**: Report bugs and request features on [GitHub Issues](https://github.com/dandm1/ValleyTalk/issues)
- **Nexus Mods**: Community discussion on the [mod page](https://www.nexusmods.com/stardewvalley/mods/30319)

## Acknowledgments

- Built with [SMAPI](https://smapi.io/) by Pathoschild
- Uses Harmony for runtime patching
- Stardew Valley by ConcernedApe
- Community translations and feedback

---

*Enhance your Stardew Valley experience with endless, personalized conversations!*
