# AGENTS.md - CoAuthorCli

## Architecture
- **CLI**: `src/CoAuthor.Cli/` - Entry point, TUI (`Terminal.Gui`), and command handling.
- **Library**: `src/CoAuthorLib/` - Core logic, abstractions, and implementations. Reusable logic must reside here.
- **Solution**: Managed via `src/CoAuthorCli.slnx`.

## Constraints & Conventions
- **NativeAOT**: The CLI must maintain NativeAOT compatibility. 
  - `IsTrimmable` and `IsAoTCompatible` are enabled in `.csproj` files.
  - `EnableAoTAnalyzer` is active in the library.
- **TUI**: Triggered when the CLI is run with no subcommands or no arguments.
- **Config**: AI agent co-author defaults are stored in `DEFAULT_AUTHORS.toml`.

## Developer Commands
- **Build/Test**: Use standard `dotnet` commands targeting the `.slnx` or individual projects.
- **Verification**: Ensure NativeAOT analyzers are checked during build to prevent compatibility regressions.
