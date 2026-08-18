---
title: Wire TUI services into existing ServiceProvider
classification: Independent
blocked_by: []
parent: docs/decisions/DECISIONS-CoAttribution-tui-mode.md
---

## Goal

Make all TUI services resolvable from the same `ServiceProvider` that backs the CLI, so the TUI consumes Lib services via DI without spawning subprocesses.

## What to build

Extend `src/CoAttribution.Cli/Program.cs` so that the existing `Cli.Ext.ConfigureServices()` lambda also registers the TUI composition root, the TUI view models, and `DraftStore`. No TTY detection at this layer — the `RootCommand` handler is the gate, per T003. Keep all existing CLI registrations intact.

## Size

- **Files** - 1 file edited

## Recommended Workflow

### Step 1 — Register TUI composition root and view models

Where: `src/CoAttribution.Cli/Program.cs`

- Inside the `Cli.Ext.ConfigureServices(...)` lambda, after the existing `services.AddSingleton<...>(...)` calls, add `services.AddSingleton<TuiCompositionRoot>()` and the TUI view models (`AuthorSelectionViewModel`, `CommitFormViewModel`, `DraftStore`).
- Resolve no TUI services during Program.cs evaluation; defer all resolution to runtime via the composition root.

Verify: `dotnet build src/CoAttribution.Cli` succeeds with the new singletons referenced only through their `using` declarations.

### Step 2 — Verify CLI commands still resolve their services

Where: `src/CoAttribution.Cli/Program.cs` (no changes required beyond step 1)

- Confirm existing DI registrations (`IHostResolver`, `IAuthorRegistry`, `ICommitOrchestrator`, etc.) are still present and untouched.

Verify: `dotnet test` (or the existing test suite) still passes; no CLI subcommand regresses because of the TUI registrations.

## Context pointers

**Files** - `src/CoAttribution.Cli/Program.cs` is the only edit; existing CLI registrations there must remain intact.
**ADRs** - `docs/adr/0001-native-aot-constraint.md` — DI registrations must avoid reflection so the AOT analyzer remains clean.
**Domain terms** - *Host Resolution* — the TUI's view model will consume `IHostResolver` (resolved later); this ticket only registers DI graph entries.
**Ledger records** - `DECISIONS-CoAttribution-tui-mode.md#T004` — register TUI services in the shared container. `DECISIONS-CoAttribution-tui-mode.md#T001` — keep one project / one container. `DECISIONS-CoAttribution-tui-mode.md#T003` — defer TTY check to the `RootCommand` handler. `DECISIONS-CoAttribution-tui-mode.md#D014` — TUI consumes Lib via DI, not subprocesses.

## Acceptance criteria

- [ ] `TuiCompositionRoot`, `AuthorSelectionViewModel`, `CommitFormViewModel`, and `DraftStore` are registered as singletons in the existing `ServiceProvider` configured by `Cli.Ext.ConfigureServices()` (DECISIONS-CoAttribution-tui-mode.md#T004).
- [ ] All existing CLI service registrations (`IHostResolver`, `IAuthorRegistry`, `ICommitOrchestrator`, etc.) remain unchanged (DECISIONS-CoAttribution-tui-mode.md#T004).
- [ ] No TUI service is resolved at Program.cs evaluation time — resolution is deferred to the `RootCommand` `Run` method (DECISIONS-CoAttribution-tui-mode.md#T003, DECISIONS-CoAttribution-tui-mode.md#T004).
- [ ] `dotnet build src/CoAttribution.Cli` succeeds with no new AOT analyzer warnings introduced by the new registrations (DECISIONS-CoAttribution-tui-mode.md#D004).
- [ ] The CLI surface remains a single binary / single .csproj (DECISIONS-CoAttribution-tui-mode.md#T001, DECISIONS-CoAttribution-tui-mode.md#D014).

## Dependencies

**Blocked by** - None - can start immediately
