---
title: SetupDialog (first-time and empty-registry guidance)
classification: Independent
blocked_by: [005-tui-composition-root-and-status-bar-infrastructure]
parent: docs/decisions/DECISIONS-CoAttribution-tui-mode.md
---

## Goal

Walk a user who has just installed CoAttribution (or whose registry is empty) through adding their first author before the main commit flow is reached.

## What to build

Add `src/CoAttribution.Cli/Tui/Dialogs/SetupDialog.cs`. It must be a Terminal.Gui v2 modal dialog that:

- Is shown only when `IAuthorRegistry.Count == 0` (gated by the `RootCommand` handler in TK003).
- Prompts the user for the minimum identity fields (Name, Email) and at least one attribution default.
- On confirm, calls the existing `IAuthorRegistry.AddAsync(...)` (or equivalent Lib entry) so the registry file on disk has the same shape it would after the equivalent CLI command.
- Implements `IStatusBarProvider` so the pinned status bar carries the dialog's keys.
- On cancel, exits the TUI cleanly (no commit is attempted); on confirm, hands control to `MainWindow`.

## Size

- **Files** - 1 new file

## Recommended Workflow

### Step 1 — Implement the SetupDialog fields and buttons

Where: `src/CoAttribution.Cli/Tui/Dialogs/SetupDialog.cs`

- Lay out at minimum `Name`, `Email`, and an attribution-default selector.
- Add `Add author` and `Cancel` buttons.

Verify: a smoke test on a real TTY walks through the dialog and ends with a non-empty registry.

### Step 2 — Wire the registry call so the TUI path matches the CLI path

Where: `src/CoAttribution.Cli/Tui/Dialogs/SetupDialog.cs`

- On confirm, resolve `IAuthorRegistry` from the shared `ServiceProvider` and call `AddAsync` with the values entered.
- The resulting on-disk registry file round-trips through `AuthorRegistry` without a diff (the same guarantee given to the CLI's `author add`).

Verify: after the dialog, `IAuthorRegistry.Count > 0` and the registry file is readable by both the TUI and the CLI.

### Step 3 — Implement `IStatusBarProvider` for the dialog

Where: `src/CoAttribution.Cli/Tui/Dialogs/SetupDialog.cs`

- Return `Tab next field`, `Enter confirm`, `Esc cancel`.

Verify: the status bar pinned by `StatusBarComposer` (TK005) appears at the bottom of this dialog.

### Step 4 — Confirm the RootCommand gate fires this dialog

Where: `src/CoAttribution.Cli/Commands/RootCommand.cs` (already updated in TK003)

- With an empty author registry and a real TTY, `co-attr` (no args) enters this `SetupDialog` instead of `MainWindow`.

Verify: a manual run confirms.

## Context pointers

**Files** - `src/CoAttribution.Cli/Tui/Dialogs/SetupDialog.cs` (new). The v1 file at `src/CoAttribution.Cli/Components/Dialogs/SetupDialog.cs` is deleted in TK004.
**ADRs** - `docs/adr/0001-native-aot-constraint.md` — registry round-trip uses the existing `AuthorRegistry` Lib entry, no new reflection.
**Domain terms** - *Attribution*, *Default Attribution Type* — the dialog establishes the user's first entry so `IAuthorRegistry.Count > 0` after.
**Ledger records** - `DECISIONS-CoAttribution-tui-mode.md#D007` — show `SetupDialog` whenever the loaded registry has zero entries. `DECISIONS-CoAttribution-tui-mode.md#T016` — query `IAuthorRegistry.Count == 0` and show `SetupDialog` before `MainWindow`. `DECISIONS-CoAttribution-tui-mode.md#D012` — v1 scaffolding is replaced. `DECISIONS-CoAttribution-tui-mode.md#D002` — TUI registry edits must produce the same on-disk state as the equivalent CLI command. `DECISIONS-CoAttribution-tui-mode.md#T013` — implement `IStatusBarProvider`. `DECISIONS-CoAttribution-tui-mode.md#D014` — registry call via DI.

## Acceptance criteria

- [ ] `src/CoAttribution.Cli/Tui/Dialogs/SetupDialog.cs` exists and prompts for the minimum identity fields plus an attribution-default selector (DECISIONS-CoAttribution-tui-mode.md#D007).
- [ ] On confirm, the dialog calls `IAuthorRegistry.AddAsync(...)` (or equivalent) so the on-disk registry file round-trips through `AuthorRegistry` without a diff, matching the equivalent CLI `author add` state (DECISIONS-CoAttribution-tui-mode.md#D002, DECISIONS-CoAttribution-tui-mode.md#D014).
- [ ] On cancel, the TUI exits cleanly with no commit attempted (DECISIONS-CoAttribution-tui-mode.md#D007).
- [ ] `SetupDialog` implements `IStatusBarProvider` returning dialog-relevant keys (DECISIONS-CoAttribution-tui-mode.md#T013).
- [ ] `RootCommand` (TK003) routes empty-registry startups to `SetupDialog` before `MainWindow` (DECISIONS-CoAttribution-tui-mode.md#T016, DECISIONS-CoAttribution-tui-mode.md#D007).
- [ ] The v1 `src/CoAttribution.Cli/Components/Dialogs/SetupDialog.cs` is removed in TK004; this ticket does not reintroduce it (DECISIONS-CoAttribution-tui-mode.md#D012).
- [ ] `dotnet build src/CoAttribution.Cli` compiles the new dialog without warnings (DECISIONS-CoAttribution-tui-mode.md#D004).

## Dependencies

**Blocked by** - 005-tui-composition-root-and-status-bar-infrastructure
