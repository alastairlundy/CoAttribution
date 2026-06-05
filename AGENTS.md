# AGENTS.md - CoAttribution

## Project Purpose & Scope
CoAttribution is strictly an **Attribution Metadata Orchestrator**. Its sole purpose is to manage the 'Who' and 'How' of Git commit trailers.

### In-Scope
- Managing a local TOML registry of Authors (Name, Email).
- Providing a TUI/CLI to select Authors and a commit message.
- Appending `Co-authored-by` and `Assisted-by` trailers to the commit message.
- Wrapping the `git commit` command.

### Out-of-Scope (Strict Boundaries)
- **No Index Manipulation**: The tool does NOT perform `git add` or manage the staging area. It assumes changes are already staged.
- **No Intelligence**: The tool does NOT integrate with LLMs. It does not generate commit messages; it only appends trailers to messages provided by the caller.
- **No Remote Integration**: The tool does NOT contact GitHub/GitLab/Bitbucket APIs. It relies entirely on local config files and local Git binaries.
- **No Prompt Management**: It is not a prompt engineering or AI agent management tool.

## Architecture
- **CLI**: `src/CoAttribution.Cli/` - Entry point, TUI (`Terminal.Gui`), and command handling.
- **Library**: `src/CoAttribution.Lib/` - Core logic, abstractions, and implementations. Reusable logic must reside here.
- **Solution**: Managed via `src/CoAttribution.slnx`.

## Constraints & Conventions
- **NativeAOT**: The CLI must maintain NativeAOT compatibility. 
  - `IsTrimmable` and `IsAoTCompatible` are enabled in `.csproj` files.
  - `EnableAoTAnalyzer` is active in the library.
- **TUI**: Triggered when the CLI is run with no subcommands or no arguments.
- **Config**: AI agent co-author defaults are stored in `DEFAULT_AUTHORS.toml`.

## Developer Commands
- **Build/Test**: Use standard `dotnet` commands targeting the `.slnx` or individual projects.
- **Verification**: Ensure NativeAOT analyzers are checked during build to prevent compatibility regressions.

## Agent skills

### Issue tracker

Issues live in the repo's GitHub Issues (uses the `gh` CLI). See `docs/agents/issue-tracker.md`.

### Triage labels

Standard canonical labels are used (`needs-triage`, `needs-info`, etc.). See `docs/agents/triage-labels.md`.

### Domain docs

Single-context layout (`CONTEXT.md` and `docs/adr/` at root). See `docs/agents/domain.md`.