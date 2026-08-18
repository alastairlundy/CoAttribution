---
title: TUI composition root and status-bar infrastructure
classification: Independent
blocked_by: [002-wire-tui-services-into-existing-service-provider]
parent: docs/decisions/DECISIONS-CoAttribution-tui-mode.md
---

## Goal

Provide the cross-cutting plumbing every TUI screen will share: the composition root that initializes Terminal.Gui v2 and runs `MainWindow`, the contract that screens implement to expose their key bindings, and the helper that pins the status bar to the bottom of the viewport.

## What to build

Add three new files under `src/CoAttribution.Cli/Tui/`:

- `Composition/TuiCompositionRoot.cs` — resolves all TUI view models from the shared `ServiceProvider`, initializes Terminal.Gui v2 `Application`, builds `MainWindow`, applies the `StatusBarComposer`, and runs it.
- `Abstractions/IStatusBarProvider.cs` — defines `IReadOnlyList<StatusBarKeyBinding> GetKeyBindings()` so every screen has a compile-time contract for its key shortcuts.
- `Composition/StatusBarComposer.cs` — wraps the bindings a screen returns via `IStatusBarProvider` in a Terminal.Gui v2 `StatusBar` widget pinned to the bottom of the viewport.

After this ticket, every screen can implement `IStatusBarProvider` and the composition root will wire a consistent bottom status bar across the app.

## Size

- **Files** - 3 new files

## Recommended Workflow

### Step 1 — Define `IStatusBarProvider`

Where: `src/CoAttribution.Cli/Tui/Abstractions/IStatusBarProvider.cs`

- Declare the interface with one method: `IReadOnlyList<StatusBarKeyBinding> GetKeyBindings()`.
- Define a small `StatusBarKeyBinding` value type in the same file or a sibling under `Tui/Abstractions/`.

Verify: `dotnet build src/CoAttribution.Cli` finds the symbol; later tickets can implement the interface.

### Step 2 — Implement `StatusBarComposer`

Where: `src/CoAttribution.Cli/Tui/Composition/StatusBarComposer.cs`

- Provide a `StatusBar Build(IStatusBarProvider provider)` method that returns a Terminal.Gui v2 `StatusBar` widget whose entries come from `provider.GetKeyBindings()`.
- Pin the bar to the bottom of the screen via Terminal.Gui v2's dock / positional API.

Verify: the composer compiles; a manual sanity check that the bar appears at the bottom of a test window can be deferred to TK006.

### Step 3 — Implement `TuiCompositionRoot`

Where: `src/CoAttribution.Cli/Tui/Composition/TuiCompositionRoot.cs`

- Take the shared `IServiceProvider` via constructor injection.
- In a `LaunchAsync()` method: initialize Terminal.Gui v2 `Application`, resolve `MainWindow` from the container, apply `StatusBarComposer` to it, call `app.Run(window)`, and dispose the application on exit.

Verify: `dotnet build src/CoAttribution.Cli` resolves all constructor dependencies; `MainWindow` is registered (or will be by TK006) so the composition root compiles end-to-end.

### Step 4 — Register the composition root in DI

Where: `src/CoAttribution.Cli/Program.cs`

- Confirm `TuiCompositionRoot` is registered as a singleton (added in TK002). If not, add it now so the `RootCommand` handler (TK003) can resolve it.

Verify: `dotnet build src/CoAttribution.Cli` succeeds; no registration drift between the two tickets.

## Context pointers

**Files** - the three new files listed in "What to build" plus `src/CoAttribution.Cli/Program.cs` (DI registration already done in TK002 — this step only verifies / completes it if needed).
**ADRs** - `docs/adr/0001-native-aot-constraint.md` — Terminal.Gui v2 idioms (no reflection-heavy patterns).
**Domain terms** - *Status Bar* — the pinned key-binding widget at the bottom of every screen. *Composition Root* — the screen-init-and-run entry point invoked by `RootCommand`.
**Ledger records** - `DECISIONS-CoAttribution-tui-mode.md#T001` — TUI code lives under `src/CoAttribution.Cli/Tui/`. `DECISIONS-CoAttribution-tui-mode.md#T002` — sub-folder organization by responsibility (`Composition/`, `Abstractions/`, ...). `DECISIONS-CoAttribution-tui-mode.md#T004` — services resolved from the shared container. `DECISIONS-CoAttribution-tui-mode.md#T013` — `IStatusBarProvider` contract and pinned `StatusBar`. `DECISIONS-CoAttribution-tui-mode.md#D017` — status bar pinned (not floating, not pop-up). `DECISIONS-CoAttribution-tui-mode.md#T003` — `TuiCompositionRoot.LaunchAsync()` is what `RootCommand` calls. `DECISIONS-CoAttribution-tui-mode.md#D014` — DI-resolved services, not subprocesses.

## Acceptance criteria

- [ ] `src/CoAttribution.Cli/Tui/Composition/TuiCompositionRoot.cs` exists and exposes `LaunchAsync()` that initializes Terminal.Gui v2, builds `MainWindow`, applies `StatusBarComposer`, and runs the application (DECISIONS-CoAttribution-tui-mode.md#T003).
- [ ] `src/CoAttribution.Cli/Tui/Abstractions/IStatusBarProvider.cs` defines `IReadOnlyList<StatusBarKeyBinding> GetKeyBindings()` as the cross-screen contract (DECISIONS-CoAttribution-tui-mode.md#T013).
- [ ] `src/CoAttribution.Cli/Tui/Composition/StatusBarComposer.cs` wraps a screen's bindings in a Terminal.Gui v2 `StatusBar` pinned to the bottom of the viewport (DECISIONS-CoAttribution-tui-mode.md#T013, DECISIONS-CoAttribution-tui-mode.md#D017).
- [ ] Every screen implementation in later tickets implements `IStatusBarProvider`; a missing implementation is a compile-time error (DECISIONS-CoAttribution-tui-mode.md#T013).
- [ ] No screen in the project implements the old v1 scaffolding shapes — the v1 files are removed in TK004 (DECISIONS-CoAttribution-tui-mode.md#D012, DECISIONS-CoAttribution-tui-mode.md#T001).
- [ ] `dotnet build src/CoAttribution.Cli` introduces no AOT analyzer warnings on the new files (DECISIONS-CoAttribution-tui-mode.md#D004).

## Dependencies

**Blocked by** - 002-wire-tui-services-into-existing-service-provider
