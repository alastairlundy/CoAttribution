---
id: TK002
title: ConfigCommand AllowedValues and model fix
status: ready
Depends on: none
---

## Goal

Fix the `"authors.global.path"` entry in ConfigCommand's AllowedValues so it resolves through the prefix-matching logic, and move the corresponding field to its semantically correct location in AppConfig.

## What to build

1. In `AppConfig.cs`: move the `"authors.global.path"` key from the `PathsSettings` dictionary to the `AuthorsRegistry` dictionary as `"paths.global"`. The TOML section changes from `[paths]` to `[authors_registry]`.

2. In `ConfigCommand.cs`: change the `AllowedValues` entry from `"authors.global.path"` to `"authors_registry.paths.global"` so it matches the existing `authors_registry.` prefix branch.

## Size

- **Files**: 2

## Recommended Workflow

### Step 1 — Update the AppConfig model

Where: `src/CoAttribution.Lib/Models/AppConfig.cs`

- From `PathsSettings` dictionary, remove the `"authors.global.path"` key (or note that it should default-initialise to empty since it was never populated)
- In `AuthorsRegistry` dictionary, add or ensure `"paths.global"` is a recognised key (the dictionary is already public, so no structural change — just documentation that the key lives here)

Verify: `dotnet build` passes

### Step 2 — Update ConfigCommand AllowedValues

Where: `src/CoAttribution.Cli/Commands/ConfigCommand.cs`

- Change the `AllowedValues` array at line 27: replace `"authors.global.path"` with `"authors_registry.paths.global"`
- Verify that the `if (key.StartsWith("authors_registry.", ...))` branch in both `GetValue` and `SetValueAsync` will resolve this key

Verify: `dotnet build` passes

## Context pointers

**Files**: `src/CoAttribution.Lib/Models/AppConfig.cs` — TOML model with five dictionary properties; `src/CoAttribution.Cli/Commands/ConfigCommand.cs` — CLI get/set with AllowedValues and prefix matching.

**Ledger records**:
- `docs/decisions/DECISIONS-coattribution-audit-fixes.md#T003` — move field to AuthorsRegistry, update AllowedValues

## Acceptance criteria

- [ ] `config get authors_registry.paths.global` resolves through the prefix-matching logic without throwing `KeyNotFoundException`
- [ ] `config set authors_registry.paths.global /path/to/registry` writes the value and persists it
- [ ] The `PathsSettings` dictionary no longer contains the `"authors.global.path"` key
- [ ] `dotnet build` passes with 0 errors and 0 warnings

## Dependencies

**Blocked by** - None - can start immediately
