---
title: TUI dispatch, TTY check, SetupDialog trigger on RootCommand
classification: Independent
blocked_by: []
parent: docs/decisions/DECISIONS-CoAttribution-tui-mode.md
---

## Goal

Make `co-attr` invoked with no subcommand and no arguments launch the TUI, print help and exit 0 in non-TTY contexts, and route to `SetupDialog` first when the registry is empty.

## What to build

Rewrite `src/CoAttribution.Cli/Commands/RootCommand.cs`. Remove the two existing `#if TUI` blocks (the `using` block at lines 10–14 and the `RunAsync` body at lines 23–38). Replace the body with a single `RunAsync(CliContext)` that (1) TTY-checks via `Console.IsOutputRedirected || Console.IsInputRedirected` and prints help + returns 0 when either is true, (2) otherwise queries `IAuthorRegistry.Count == 0` and shows `SetupDialog` first when true, (3) otherwise resolves `TuiCompositionRoot` from the shared `ServiceProvider` and calls `LaunchAsync()`.

## Size

- **Files** - 1 file rewritten

## Recommended Workflow

### Step 1 — Remove the `#if TUI` blocks from RootCommand

Where: `src/CoAttribution.Cli/Commands/RootCommand.cs`

- Delete the `#if TUI` `using` block at the top of the file.
- Delete the `#if TUI` / `#else` / `#endif` conditional inside `RunAsync(CliContext)`; keep one unconditional body.

Verify: `rg -n '#if TUI' src/CoAttribution.Cli/Commands/RootCommand.cs` returns no matches.

### Step 2 — Implement the unconditional RunAsync body

Where: `src/CoAttribution.Cli/Commands/RootCommand.cs`

- After TTY check (see step 3), resolve `IAuthorRegistry` from the `CliContext` `IServiceProvider`; if `Count == 0`, resolve `SetupDialog` and run it modal first.
- Otherwise resolve `TuiCompositionRoot` and call `LaunchAsync()`.

Verify: `dotnet build src/CoAttribution.Cli` compiles the rewritten `RootCommand` without warnings about unreachable branches.

### Step 3 — Apply the TTY print-help-and-exit-zero behavior

Where: `src/CoAttribution.Cli/Commands/RootCommand.cs`

- Inside `RunAsync`, before any DI resolution, evaluate `Console.IsOutputRedirected || Console.IsInputRedirected`; if either is true, call `context.ShowHelp()` and return `Task.FromResult(0)`.

Verify: piping `echo "" | dotnet run --project src/CoAttribution.Cli --` (no subcommand) prints help text and exits 0.

### Step 4 — Confirm SetupDialog trigger fires on empty registry

Where: `src/CoAttribution.Cli/Commands/RootCommand.cs`

- After TTY check, before resolving `TuiCompositionRoot`, call `IAuthorRegistry.Count`; if zero, resolve `SetupDialog` from the container and run it (this ticket does not implement `SetupDialog` itself — that lands in TK009).

Verify: With an empty author registry and a real TTY, `co-attr` (no args) enters `SetupDialog` instead of `MainWindow`.

## Context pointers

**Files** - `src/CoAttribution.Cli/Commands/RootCommand.cs` is the only edit. `src/CoAttribution.Cli/Program.cs` already registers the shared `ServiceProvider` (see TK002).
**ADRs** - `docs/adr/0001-native-aot-constraint.md` — `Console.IsOutputRedirected` / `Console.IsInputRedirected` are NativeAOT-friendly.
**Domain terms** - *Attribution*, *Commit Trailer* — `co-attr` orchestrates these via the orchestrator; this ticket only routes to a screen.
**Ledger records** - `DECISIONS-CoAttribution-tui-mode.md#T003` — Run method fires when no subcommand matches. `DECISIONS-CoAttribution-tui-mode.md#T005` — TTY detection via `Console.IsOutputRedirected || Console.IsInputRedirected`. `DECISIONS-CoAttribution-tui-mode.md#T016` — query `IAuthorRegistry.Count == 0` to trigger `SetupDialog`. `DECISIONS-CoAttribution-tui-mode.md#D003` — no-subcommand launch. `DECISIONS-CoAttribution-tui-mode.md#D005` — non-TTY exits 0 with help. `DECISIONS-CoAttribution-tui-mode.md#T006` — remove the `#if TUI` blocks. `DECISIONS-CoAttribution-tui-mode.md#D014` — TUI consumes Lib services via DI. `DECISIONS-CoAttribution-tui-mode.md#D020` — session goal framing.

## Acceptance criteria

- [ ] The two `#if TUI` blocks in `src/CoAttribution.Cli/Commands/RootCommand.cs` are deleted (DECISIONS-CoAttribution-tui-mode.md#T006).
- [ ] `RunAsync` is implemented as an unconditional method that fires when no DotMake subcommand matches (DECISIONS-CoAttribution-tui-mode.md#T003, DECISIONS-CoAttribution-tui-mode.md#D003).
- [ ] When `Console.IsOutputRedirected || Console.IsInputRedirected` is true, `RunAsync` calls `context.ShowHelp()` and returns `0` without launching the TUI (DECISIONS-CoAttribution-tui-mode.md#T005, DECISIONS-CoAttribution-tui-mode.md#D005).
- [ ] When the TTY check passes and `IAuthorRegistry.Count == 0`, `RunAsync` shows `SetupDialog` before `MainWindow` (DECISIONS-CoAttribution-tui-mode.md#T016, DECISIONS-CoAttribution-tui-mode.md#D007).
- [ ] When the TTY check passes and the registry is non-empty, `RunAsync` resolves `TuiCompositionRoot` from the shared `ServiceProvider` and calls `LaunchAsync()` (DECISIONS-CoAttribution-tui-mode.md#T003, DECISIONS-CoAttribution-tui-mode.md#T004).
- [ ] `dotnet build src/CoAttribution.Cli` emits no warnings from the rewritten `RootCommand` (DECISIONS-CoAttribution-tui-mode.md#D004).

## Dependencies

**Blocked by** - None - can start immediately
