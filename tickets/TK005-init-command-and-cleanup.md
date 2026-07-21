---
title: Init command implementation and code cleanup
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-audit-fixes.md
---

## Goal

Implement the `CreateAuthorsTomlFileAsync` stub in `InitCommand` so that `init` produces a usable authors registry file, and clean up dead commented-out usings and the empty csproj Folder directive.

## What to build

1. **InitCommand**: Inject `IRegistryPathResolver`, embed `DEFAULT_AUTHORS.toml` as a manifest resource, implement `CreateAuthorsTomlFileAsync` to write the embedded content to the resolved global or local path.

2. **CoAttribution.Cli.csproj**: Add `<EmbeddedResource Include="DEFAULT_AUTHORS.toml" />` and remove the empty `<Folder Include="Components\Windows\" />` directive.

3. **Cleanup**: Remove commented-out `using Terminal.Gui.*` blocks from `AddCoAuthorCommand.cs` (lines 10-12), `InitCommand.cs` (lines 12-14), and `RootCommand.cs` (lines 10-13). Update `InitCommand`'s config-path fallback to read only `"config-file"`.

## Size

- **Files**: 4

## Recommended Workflow

### Step 1 — Add EmbeddedResource to csproj and remove Folder directive

Where: `src/CoAttribution.Cli/CoAttribution.Cli.csproj`

- Add `<EmbeddedResource Include="DEFAULT_AUTHORS.toml" />` inside an `<ItemGroup>`
- Remove `<Folder Include="Components\Windows\" />` line

Verify: `dotnet build` passes

### Step 2 — Add IRegistryPathResolver to InitCommand and implement CreateAuthorsTomlFileAsync

Where: `src/CoAttribution.Cli/Commands/InitCommand.cs`

- Add `private readonly IRegistryPathResolver _pathResolver;` field
- Add `IRegistryPathResolver pathResolver` constructor parameter
- Add `using System.Reflection;` for embedded resource access
- Implement `CreateAuthorsTomlFileAsync`:
  - If `CreateGlobalFile` is true: call `_pathResolver.GetGlobalRegistryPathAsync()` to get the target path, create the directory, read the embedded resource via `Assembly.GetExecutingAssembly().GetManifestResourceStream("DEFAULT_AUTHORS.toml")`, and write the content
  - If `CreateGlobalFile` is false: write to `.coauthor/authors.toml` in the current working directory

Verify: `dotnet build` passes

### Step 3 — Clean up commented-out usings

Where: `src/CoAttribution.Cli/Commands/AddCoAuthorCommand.cs`, `src/CoAttribution.Cli/Commands/InitCommand.cs`, `src/CoAttribution.Cli/Commands/RootCommand.cs`

- Remove the `/*using CoAttribution.Cli.Components.Dialogs; using Terminal.Gui.App; using Terminal.Gui.Views;*/` block from `AddCoAuthorCommand.cs` (lines 10-12)
- Remove the `/*using Terminal.Gui.App; using CoAttribution.Cli.Components.Dialogs;*/` block from `InitCommand.cs` (lines 12-14)
- Remove the `/*using CoAttribution.Cli.Components.Windows; using Terminal.Gui.App;*/` block from `RootCommand.cs` (lines 10-13)

Verify: `dotnet build` passes

### Step 4 — Update InitCommand config-path fallback

Where: `src/CoAttribution.Cli/Commands/InitCommand.cs`

- At line 43, change `_configuration["config-file"] ?? _configuration["coauthor_config_file"]` to `_configuration["config-file"]`

Verify: `dotnet build` passes

## Context pointers

**Files**: `src/CoAttribution.Cli/Commands/InitCommand.cs` — stub implementation to fill in; `src/CoAttribution.Cli/DEFAULT_AUTHORS.toml` — content to embed; `src/CoAttribution.Cli/CoAttribution.Cli.csproj` — project file for resource and folder; `src/CoAttribution.Cli/Commands/AddCoAuthorCommand.cs` and `src/CoAttribution.Cli/Commands/RootCommand.cs` — commented-out usings.

**Ledger records**:
- `docs/decisions/DECISIONS-coattribution-audit-fixes.md#T006` — embedded resource with IRegistryPathResolver injection
- `docs/decisions/DECISIONS-coattribution-audit-fixes.md#Issue11` — commented-out usings removal
- `docs/decisions/DECISIONS-coattribution-audit-fixes.md#Issue12` — csproj Folder directive removal

## Acceptance criteria

- [ ] Running `init` with no flags creates a global authors.toml at the path resolved by `IRegistryPathResolver`
- [ ] Running `init --global false` creates a local `.coauthor/authors.toml` in the current directory
- [ ] The created file contains the DEFAULT_AUTHORS.toml content (agents.copilot, agents.kilo, etc.)
- [ ] Commented-out `using Terminal.Gui.*` blocks are removed from 3 command files
- [ ] The empty `<Folder Include="Components\Windows\" />` directive is removed from csproj
- [ ] `dotnet build` passes with 0 errors and 0 warnings

## Dependencies

**Blocked by** - None - can start immediately (TK001's config-path fix is not required since `InitCommand` reads `"config-file"` directly)
