---
title: DI registration and config-path plumbing
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-audit-fixes.md
---

## Goal

Register `ICommitOrchestrator` in the DI container and fix the config-path plumbing so that the default computed path is visible to all IConfiguration consumers.

## What to build

Two independent fixes in `Program.cs`:

1. Add `services.AddSingleton<ICommitOrchestrator, CommitOrchestrator>();` in the existing `ConfigureServices` block so that `CommitCommand` and `MessageCommand` can be instantiated without a null-service crash.

2. Replace the current `builder.Properties.Add("config-path", ...)` pattern with two changes:
   - Add `AddCommandLine` switch mappings that map `--config-path` to the IConfiguration key `"config-file"`.
   - When no `--config-path` argument is present, inject the computed default path via `AddInMemoryCollection` under the `"config-file"` key.
   - Then update `ConfigResolver` and `AppConfigRegistryPathResolver` to query only the `"config-file"` key, removing the `"coauthor_config_file"` fallback chain.

## Size

- **Files**: 3

## Recommended Workflow

### Step 1 — Register ICommitOrchestrator in Program.cs

Where: `src/CoAttribution.Cli/Program.cs`

- Add `services.AddSingleton<ICommitOrchestrator, CommitOrchestrator>();` in the `ConfigureServices` delegate
- Verify `CommitOrchestrator` is resolvable by checking its constructor dependencies are already registered (`ICommitMessageBuilder`, `IAuthorRegistry`, `IGitClient` — all present)

Verify: `dotnet build` passes with 0 errors

### Step 2 — Add switch mappings for --config-path

Where: `src/CoAttribution.Cli/Program.cs`

- Create a `var switchMappings = new Dictionary<string, string> { { "--config-path", "config-file" } };` dictionary
- Change `configurationBuilder.AddCommandLine(args)` to `configurationBuilder.AddCommandLine(args, switchMappings)`

Verify: `dotnet build` passes

### Step 3 — Replace builder.Properties with AddInMemoryCollection

Where: `src/CoAttribution.Cli/Program.cs`

- Replace `configurationBuilder.Properties.Add("config-path", DetermineDefaultConfigFilePath())` with `configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?> { ["config-file"] = DetermineDefaultConfigFilePath() })`, keeping the `!args.Contains("--config-path")` guard

Verify: `dotnet build` passes

### Step 4 — Standardise consumers on config-file key

Where: `src/CoAttribution.Cli/ConfigResolver.cs`, `src/CoAttribution.Cli/AppConfigRegistryPathResolver.cs`

- In `ConfigResolver.ResolveAppConfig`: remove the `"coauthor_config_file"` fallback — query only `configuration["config-file"]`
- In `AppConfigRegistryPathResolver.GetGlobalRegistryPathAsync`: same change — query only `_configuration["config-file"]`
- In `ConfigCommand.GetValueAsync` and `SetValueAsync`: remove the `"coauthor_config_file"` fallback — query only `_configuration["config-file"]`

Verify: `dotnet build` passes

## Context pointers

**Files**: `src/CoAttribution.Cli/Program.cs` — composition root; `src/CoAttribution.Cli/ConfigResolver.cs` — reads config file path; `src/CoAttribution.Cli/AppConfigRegistryPathResolver.cs` — reads config file path for registry; `src/CoAttribution.Cli/Commands/ConfigCommand.cs` — reads config file path for get/set operations.

**Ledger records**:
- `docs/decisions/DECISIONS-coattribution-audit-fixes.md#T001` — inline registration in Program.cs
- `docs/decisions/DECISIONS-coattribution-audit-fixes.md#T002` — switch mapping plus InMemoryCollection default

## Acceptance criteria

- [ ] `CommitCommand` and `MessageCommand` can be instantiated by DI without null-service error
- [ ] Running the CLI with no `--config-path` uses the platform-appropriate default path (computed by `DetermineDefaultConfigFilePath`)
- [ ] Running the CLI with `--config-path /custom/path` uses the supplied path
- [ ] All consumers (`ConfigResolver`, `AppConfigRegistryPathResolver`, `ConfigCommand`) query only the `"config-file"` key
- [ ] `dotnet build` passes with 0 errors and 0 warnings

## Dependencies

**Blocked by** - None - can start immediately
