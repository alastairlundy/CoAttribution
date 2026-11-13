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

### CLI Framework

The CLI uses **System.CommandLine 3.0.0** (pinned in `Directory.Packages.props`) with explicit command construction — no attribute model, no source generators.

**Entry Point**
- `Program.cs` runs the bootstrap scans (`ExtractConfigPath`, `ExtractRegistryPath`) on the raw arguments FIRST, builds `IConfiguration`, then builds a `ServiceProvider` manually from a `ServiceCollection` populated by `AppServiceCollection.AddCoAttributionServices` (`src/CoAttribution.Cli/AppServiceCollection.cs`). Registrations live there — not in `Program.cs` — so tests can build the real container and assert the commit/message/TUI dependency graphs resolve.
- The command tree is built with `RootCommand.Create(serviceProvider)` and executed via `CliRunner.RunAsync(rootCommand, args)`.
- The bootstrap scans keep their all-arguments behavior (no `--` awareness); they must stay ahead of container construction so config overrides apply to the whole run.

**Command Hierarchy**
- Command classes live under `src/CoAttribution.Cli/Commands/` as standalone (non-nested) classes.
- Each command class exposes `public static Command Create(IServiceProvider services)` (the root returns `System.CommandLine.RootCommand`) which constructs its own `Command`/`Option`/`Argument` instances and registers the handler action with `SetAction`.
- Parents compose children inside `Create`: `RootCommand.Create` adds `author`, `commit`, `config`, `init`, `message`; `AuthorRootCommand.Create` adds `add`, `list`, `remove`.
- Handlers are instantiated inside the action with `ActivatorUtilities.CreateInstance<T>(services)`, bind their properties from the `ParseResult`, and return their exit code through `CliContext`.

**⚠ RootCommand naming**
- The handler class `CoAttribution.Cli.Commands.RootCommand` shares its name with `System.CommandLine.RootCommand`. Keep exactly one unqualified name in scope per file: files inside `namespace CoAttribution.Cli.Commands` resolve the local class first, so they must reference `System.CommandLine.RootCommand` by its full name; `Program.cs` must NOT add `using System.CommandLine;`.

**Options and aliases**
- Root options `--config-path` and `--registry` are declared with `Recursive = true` so every subcommand accepts and lists them.
- Short aliases are declared explicitly per option (`-m`, `-b`, `-v`, `-g`); System.CommandLine does not auto-generate aliases, so alias conflicts are impossible by construction. `config` keeps its explicit `cfg` alias via `command.Aliases.Add("cfg")`.
- POSIX bundling (`-vm` == `-v -m`) is enabled by default (`ParserConfiguration.EnablePosixBundling`).
- `CliRunner` switches off System.CommandLine's help-on-parse-error output to keep the historical behavior of printing only error details (plus typo suggestions) when a command line fails to parse.
- Help rendering goes through `BannerHelpAction` (`src/CoAttribution.Cli/Commands/BannerHelpAction.cs`), which wraps System.CommandLine's built-in `HelpAction` on the root help option: it restores the `co-attr v<version>` / copyright banner and the subcommand title line, and suppresses the framework's empty `Description:` heading. `CliContext.ShowHelp` builds the same wrapper so handler-driven help matches `--help`.

## Constraints & Conventions
- **NativeAOT**: The CLI must maintain NativeAOT compatibility. 
  - `IsTrimmable` and `IsAoTCompatible` are enabled in `.csproj` files.
  - `EnableAoTAnalyzer` is active in the library.
- **TUI**: Always compiled in — there is no `#if TUI` gate. Launched when the CLI is run with no subcommands on a TTY (a non-TTY session prints help instead).
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

Single-context layout (`GLOSSARY.md` and `docs/adr/` at root). See `docs/agents/domain.md`.

### Repo-specific skills

None — CLI-framework conventions live in the `CLI Framework` section above.