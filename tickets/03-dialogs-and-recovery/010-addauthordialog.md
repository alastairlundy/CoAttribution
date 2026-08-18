---
title: AddAuthorDialog (in-place add from selection screen)
classification: Collaborative
blocked_by: [005-tui-composition-root-and-status-bar-infrastructure, 008-authorselection-view-and-view-model]
parent: docs/decisions/DECISIONS-CoAttribution-tui-mode.md
---

## Goal

Let a user mid-commit add the author they just realized they need, without leaving the author-selection screen or losing their in-progress picks.

## What to build

Add `src/CoAttribution.Cli/Tui/Dialogs/AddAuthorDialog.cs`. It must be a Terminal.Gui v2 modal dialog with two fields (`Name`, `Email`) plus `Add author` and `Cancel` buttons. On `Add author`, it resolves `IAuthorRegistry` from the shared container and calls `AddAsync` so the new author is persisted with the same on-disk shape the CLI uses. When invoked from `AuthorSelectionView`, closing the dialog returns control to the selection screen with picks, filter text, and the basic/advanced toggle state intact. Implements `IStatusBarProvider`.

Collaborative because the state-preservation contract between this dialog and `AuthorSelectionView` (TK008) is non-trivial: which observable properties must be snapshotted, when, and what to do if registry mutation happens mid-confirm.

## Size

- **Files** - 1 new file

## Recommended Workflow

### Step 1 — Implement the dialog fields and buttons

Where: `src/CoAttribution.Cli/Tui/Dialogs/AddAuthorDialog.cs`

- Two `TextField`s: `Name`, `Email`; two buttons: `Add author`, `Cancel`.
- Inline email-shape validation; the `Add author` button is disabled until validation passes.

Verify: a manually-driven run on a real TTY enters a name + email, hits `Add author`, and sees the new row appear.

### Step 2 — Wire the registry call

Where: `src/CoAttribution.Cli/Tui/Dialogs/AddAuthorDialog.cs`

- Resolve `IAuthorRegistry` from the shared `ServiceProvider` and call `AddAsync(...)` with the entered `(name, email)` and the chosen `ContributorType` / `DefaultAttributionType` defaults.
- On success, close the dialog and notify `AuthorSelectionView` to refresh its row list.

Verify: the new author appears in `AuthorSelectionView` after the dialog closes; the registry file round-trips through `AuthorRegistry` without diff (same as the CLI path).

### Step 3 — Implement `IStatusBarProvider` and the state-preservation contract

Where: `src/CoAttribution.Cli/Tui/Dialogs/AddAuthorDialog.cs`

- Return `Tab next field`, `Enter confirm`, `Esc cancel`.
- Accept the `AuthorSelectionViewModel` via constructor; on close, signal the view to restore its snapshot (picks, filter text, toggle state).

Verify: open dialog → add an author → close → original picks, filter text, and toggle state are preserved.

## Context pointers

**Files** - `src/CoAttribution.Cli/Tui/Dialogs/AddAuthorDialog.cs` (new). The v1 file at `src/CoAttribution.Cli/Components/Dialogs/AddAuthorDialog.cs` is removed in TK004. Invoked from `AuthorSelectionView` (TK008).
**ADRs** - `docs/adr/0001-native-aot-constraint.md` — `IAuthorRegistry` is already AOT-safe.
**Domain terms** - *Contributor Classification*, *Default Attribution Type*, *Attribution* — the dialog's defaults propagate through the same code path as `author add`.
**Ledger records** - `DECISIONS-CoAttribution-tui-mode.md#D013` — `+ Add author` button opens `AddAuthorDialog` in-place; preserve picks / filter / toggle; top-level menu does not duplicate the action. `DECISIONS-CoAttribution-tui-mode.md#D014` — call `IAuthorRegistry.AddAsync` via DI. `DECISIONS-CoAttribution-tui-mode.md#D012` — v1 scaffolding is replaced. `DECISIONS-CoAttribution-tui-mode.md#D002` — TUI registry edits must produce the same on-disk state as the equivalent CLI command. `DECISIONS-CoAttribution-tui-mode.md#T013` — implement `IStatusBarProvider`.

## Acceptance criteria

- [ ] `src/CoAttribution.Cli/Tui/Dialogs/AddAuthorDialog.cs` exists with `Name` and `Email` fields plus `Add author` / `Cancel` buttons (DECISIONS-CoAttribution-tui-mode.md#D013).
- [ ] On `Add author`, the dialog calls `IAuthorRegistry.AddAsync(...)` so the on-disk registry round-trips through `AuthorRegistry` without diff, matching the CLI `author add` path (DECISIONS-CoAttribution-tui-mode.md#D002, DECISIONS-CoAttribution-tui-mode.md#D014).
- [ ] The dialog can be opened from the `+ Add author` button on `AuthorSelectionView` and on close leaves the selection screen's current picks, filter text, and basic/advanced toggle state intact (DECISIONS-CoAttribution-tui-mode.md#D013).
- [ ] The top-level menu does not duplicate the `+ Add author` action (DECISIONS-CoAttribution-tui-mode.md#D013).
- [ ] `AddAuthorDialog` implements `IStatusBarProvider` and returns dialog-relevant keys (DECISIONS-CoAttribution-tui-mode.md#T013).
- [ ] The v1 file at `src/CoAttribution.Cli/Components/Dialogs/AddAuthorDialog.cs` stays deleted (TK004) — this ticket does not reintroduce it (DECISIONS-CoAttribution-tui-mode.md#D012).
- [ ] `dotnet build src/CoAttribution.Cli` compiles the new dialog without warnings (DECISIONS-CoAttribution-tui-mode.md#D004).

## Dependencies

**Blocked by** - 005-tui-composition-root-and-status-bar-infrastructure, 008-authorselection-view-and-view-model
