---
title: Remove v1 TUI scaffolding files
classification: Independent
blocked_by: [003-tui-dispatch-tty-check-and-setupdialog-trigger-on-rootcommand]
parent: docs/decisions/DECISIONS-CoAttribution-tui-mode.md
---

## Goal

Delete the existing v1 Terminal.Gui scaffolding so the v2 components under `src/CoAttribution.Cli/Tui/` are the only TUI surface in the project.

## What to build

Delete all six files under `src/CoAttribution.Cli/Components/`:

- `Windows/MainWindow.cs`
- `Windows/MessageWindow.cs`
- `Dialogs/SetupDialog.cs`
- `Dialogs/AddAuthorDialog.cs`
- `Dialogs/MissingHostBlockDialog.cs`
- `Dialogs/MissingHostBlockChoice.cs`

Once these are gone, remove the empty `Components/Windows/` and `Components/Dialogs/` directories. After this ticket, no file in the project references Terminal.Gui v1 idioms or the v1 scaffolding shapes.

## Size

- **Files** - 6 files deleted

## Recommended Workflow

### Step 1 — Delete the v1 scaffolding files

Where: `src/CoAttribution.Cli/Components/Windows/` and `src/CoAttribution.Cli/Components/Dialogs/`

- Delete the six files listed above.
- Delete the now-empty `Windows/` and `Dialogs/` directories under `Components/`.

Verify: `rg -n 'CoAttribution.Cli.Components' src/CoAttribution.Cli` returns no matches; `ls src/CoAttribution.Cli/Components` reports the directory no longer exists.

### Step 2 — Confirm the v1 file list is fully cleared

Where: repo root

- Run `rg -n 'Application.Create\|Terminal.Gui.Views' src/CoAttribution.Cli` to confirm no surviving code references v1 Terminal.Gui v1 idioms.

Verify: the grep returns only files that are about to be added by TK005–TK013, not the deleted v1 files.

### Step 3 — Confirm the build still compiles after the cleanup

Where: repo root

- Run `dotnet build src/CoAttribution.Cli -c Release`.

Verify: the build succeeds without unresolved references to `CoAttribution.Cli.Components.Windows` or `CoAttribution.Cli.Components.Dialogs`.

## Context pointers

**Files** - the six files listed in "What to build" are the only deletions. No other code edits are made by this ticket.
**ADRs** - `docs/adr/0001-native-aot-constraint.md` — Terminal.Gui v2 is the AOT-compatible replacement; v1 had to leave the codebase.
**Domain terms** - *Attribution* — the v1 dialogs touched attribution identity editing in a way v2 will replicate via new components.
**Ledger records** - `DECISIONS-CoAttribution-tui-mode.md#D012` — v1 scaffolding is removed and v2 components are written from scratch. `DECISIONS-CoAttribution-tui-mode.md#T006` — no `#if TUI` MSBuild property may reintroduce the v1 surface.

## Acceptance criteria

- [ ] All six files under `src/CoAttribution.Cli/Components/Windows/` and `src/CoAttribution.Cli/Components/Dialogs/` listed in "What to build" are deleted (DECISIONS-CoAttribution-tui-mode.md#D012).
- [ ] The empty `Components/Windows/` and `Components/Dialogs/` directories are removed (DECISIONS-CoAttribution-tui-mode.md#D012).
- [ ] `rg -n 'CoAttribution.Cli.Components' src/CoAttribution.Cli` returns no matches — no surviving reference to the v1 namespace (DECISIONS-CoAttribution-tui-mode.md#D012).
- [ ] `dotnet build src/CoAttribution.Cli -c Release` succeeds after the deletions (DECISIONS-CoAttribution-tui-mode.md#T006, DECISIONS-CoAttribution-tui-mode.md#D004).
- [ ] No `#if TUI` MSBuild property remains to reintroduce the v1 files (DECISIONS-CoAttribution-tui-mode.md#T006).

## Dependencies

**Blocked by** - 003-tui-dispatch-tty-check-and-setupdialog-trigger-on-rootcommand
