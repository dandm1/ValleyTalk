# ValleyTalk Development Guidelines

## Build Commands
- Build project: `dotnet build`
- Build for release: `dotnet build -c Release` 
- Run game with mod: `dotnet build && open "$GamePath/StardewValley.exe"` (replace with your game path)

## Code Style Guidelines
- Namespace: `ValleyTalk` for all classes
- Class naming: PascalCase for all types
- Method naming: PascalCase for methods, camelCase for parameters and local variables
- Use explicit access modifiers (public, private, internal)
- Error handling: Use try/catch blocks with specific exception types where possible
- String operations: Use extension methods from StringExtensions
- Type checking: Prefer pattern matching over direct type checking
- Use `StringComparer.InvariantCultureIgnoreCase` for case-insensitive string comparisons
- Organize imports: System imports first, then external libraries, then project namespaces
- For localization, use `SHelper.Translation` and i18n system
- Cache expensive operations when appropriate (see locale handling in ModEntry)
- Always handle null cases with null checks or null-conditional operators