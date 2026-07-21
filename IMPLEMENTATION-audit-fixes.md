# Consolidated Implementation Plan: CoAttribution Audit Fixes

**Date:** 2026-07-21
**Linked Spec:** `CoAttribution-handoff-2026-07-21-full-audit.md`
**Decision Ledger:** `docs/decisions/DECISIONS-coattribution-audit-fixes.md`

**Scope Binding:** This plan is a context pointer valid ONLY for the linked spec and must not be applied to other specifications without explicit authorization.

---

## `src/CoAttribution.Cli/Program.cs`

| Change | Driven by |
|--------|-----------|
| Add switch mappings to `AddCommandLine(args, switchMappings)` mapping `--config-path` → `"config-file"` | `DECISIONS-coattribution-audit-fixes.md#T002` |
| Replace `builder.Properties.Add("config-path", ...)` with `configurationBuilder.AddInMemoryCollection(...)` containing `["config-file"] = DetermineDefaultConfigFilePath()`, guarded by the same `!args.Contains("--config-path")` check | `DECISIONS-coattribution-audit-fixes.md#T002` |
| Add `services.AddSingleton<ICommitOrchestrator, CommitOrchestrator>();` in the `ConfigureServices` block | `DECISIONS-coattribution-audit-fixes.md#T001` |

## `src/CoAttribution.Cli/Commands/ConfigCommand.cs`

| Change | Driven by |
|--------|-----------|
| Change `AllowedValues` entry `"authors.global.path"` to `"authors_registry.paths.global"` | `DECISIONS-coattribution-audit-fixes.md#T003` |
| In `GetValue` and `SetValueAsync`: remove `"config-path"` fallback — consumer queries only `"config-file"` | `DECISIONS-coattribution-audit-fixes.md#T002` |

## `src/CoAttribution.Cli/ConfigResolver.cs`

| Change | Driven by |
|--------|-----------|
| Remove `"coauthor_config_file"` fallback — consumer queries only `"config-file"` | `DECISIONS-coattribution-audit-fixes.md#T002` |

## `src/CoAttribution.Cli/AppConfigRegistryPathResolver.cs`

| Change | Driven by |
|--------|-----------|
| Remove `"coauthor_config_file"` fallback — consumer queries only `"config-file"` | `DECISIONS-coattribution-audit-fixes.md#T002` |

## `src/CoAttribution.Lib/Models/AppConfig.cs`

| Change | Driven by |
|--------|-----------|
| Move `"authors.global.path"` entry from `PathsSettings` dictionary to `AuthorsRegistry` dictionary as `"paths.global"` | `DECISIONS-coattribution-audit-fixes.md#T003` |

## `src/CoAttribution.Lib/HostResolution/Abstractions/IGitConfigClient.cs`

| Change | Driven by |
|--------|-----------|
| Change `bool TryGet(string key, [NotNullWhen(true)] out string? value)` → `Task<(bool Found, string? Value)> TryGetAsync(string key)` | `DECISIONS-coattribution-audit-fixes.md#T005` |
| Change `void Set(string key, string value)` → `Task SetAsync(string key, string value)` | `DECISIONS-coattribution-audit-fixes.md#T005` |

## `src/CoAttribution.Lib/HostResolution/GitConfigClient.cs`

| Change | Driven by |
|--------|-----------|
| Replace `.GetAwaiter().GetResult()` with `await` in both `TryGetAsync` and `SetAsync` | `DECISIONS-coattribution-audit-fixes.md#T005` |

## `src/CoAttribution.Lib/HostResolution/Abstractions/IHostResolver.cs`

| Change | Driven by |
|--------|-----------|
| Change `HostResolutionResult ResolveHost(string? hostInput)` → `Task<HostResolutionResult> ResolveHostAsync(string? hostInput)` | `DECISIONS-coattribution-audit-fixes.md#T005` |

## `src/CoAttribution.Lib/HostResolution/HostResolver.cs`

| Change | Driven by |
|--------|-----------|
| Change method signature to `async Task<HostResolutionResult> ResolveHostAsync` | `DECISIONS-coattribution-audit-fixes.md#T005` |
| Replace `.GetAwaiter().GetResult()` with `await` on `GetPrimaryRemoteUrlAsync` | `DECISIONS-coattribution-audit-fixes.md#T005` |
| Await `_gitConfigClient.TryGetAsync` instead of `.TryGet(... out ...)` | `DECISIONS-coattribution-audit-fixes.md#T005` |

## `src/CoAttribution.Lib/CommitOrchestrator.cs`

| Change | Driven by |
|--------|-----------|
| Add `IHostResolver` constructor parameter (wiring deferred — signature prepared for T007) | `DECISIONS-coattribution-audit-fixes.md#T007` |

## `src/CoAttribution.Cli/Commands/InitCommand.cs`

| Change | Driven by |
|--------|-----------|
| Add `IRegistryPathResolver` constructor parameter | `DECISIONS-coattribution-audit-fixes.md#T006` |
| Implement `CreateAuthorsTomlFileAsync`: read embedded resource, write to global or local path | `DECISIONS-coattribution-audit-fixes.md#T006` |
| Add `using System.Reflection;` for embedded resource access | `DECISIONS-coattribution-audit-fixes.md#T006` |
| Remove commented-out `using Terminal.Gui.*` blocks (lines 12-14) | `DECISIONS-coattribution-audit-fixes.md#Issue11` |
| Update config-path fallback: read only `"config-file"` instead of `"config-file" ?? "coauthor_config_file"` | `DECISIONS-coattribution-audit-fixes.md#T002` |

## `src/CoAttribution.Cli/CoAttribution.Cli.csproj`

| Change | Driven by |
|--------|-----------|
| Add `<EmbeddedResource Include="DEFAULT_AUTHORS.toml" />` | `DECISIONS-coattribution-audit-fixes.md#T006` |
| Remove empty `<Folder Include="Components\Windows\" />` directive (line 62) | `DECISIONS-coattribution-audit-fixes.md#Issue12` |

## `src/CoAttribution.Cli/Commands/AddCoAuthorCommand.cs`

| Change | Driven by |
|--------|-----------|
| Remove commented-out `using Terminal.Gui.*` blocks (lines 10-12) | `DECISIONS-coattribution-audit-fixes.md#Issue11` |

## `src/CoAttribution.Cli/Commands/RootCommand.cs`

| Change | Driven by |
|--------|-----------|
| Remove commented-out `using Terminal.Gui.*` blocks (lines 10-13) | `DECISIONS-coattribution-audit-fixes.md#Issue11` |

## No changes

| Item | Reason |
|------|--------|
| TUI component files (`Components/Dialogs/*`, `Components/Windows/*`) | Retained for future TUI session — `DECISIONS-coattribution-audit-fixes.md#Issue10` |
| `ListCoAuthorsCommand.cs` | Issue 7 already fixed in prior commit `d5b189f` |
| `Resources.resx` | Issue 8 already fixed — format string accepts 2 args matching the calling code |

## Ledger Reference

- `DECISIONS-coattribution-audit-fixes.md#D001` — session goal
- `DECISIONS-coattribution-audit-fixes.md#T001` — DI Registration scope
- `DECISIONS-coattribution-audit-fixes.md#T002` — Config path key unification
- `DECISIONS-coattribution-audit-fixes.md#T003` — ConfigCommand AllowedValues consistency
- `DECISIONS-coattribution-audit-fixes.md#T004` — host_aliases config coverage (skipped)
- `DECISIONS-coattribution-audit-fixes.md#T005` — Async deadlock fix strategy
- `DECISIONS-coattribution-audit-fixes.md#T006` — Stub implementation content
- `DECISIONS-coattribution-audit-fixes.md#T007` — IHostResolver consumer
- `DECISIONS-coattribution-audit-fixes.md#Issue10` — TUI files retained
- `DECISIONS-coattribution-audit-fixes.md#Issue11` — Commented-out usings removed
- `DECISIONS-coattribution-audit-fixes.md#Issue12` — csproj Folder directive removed
