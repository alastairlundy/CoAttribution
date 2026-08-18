---
title: MissingHostBlockDialog (host-block recovery in the TUI)
classification: Collaborative
blocked_by: [005-tui-composition-root-and-status-bar-infrastructure]
parent: docs/decisions/DECISIONS-CoAttribution-tui-mode.md
---

## Goal

When the resolved host has no entry in the registry, recover the commit flow in-place by capturing the user's `(name, email)` for the host, writing it through `HostBlockWriter`, and retrying — all without ejecting the user back to the CLI.

## What to build

Add `src/CoAttribution.Cli/Tui/Dialogs/MissingHostBlockDialog.cs`. It must be a Terminal.Gui v2 modal dialog with two fields (`Name`, `Email`) and `Save` / `Cancel` buttons. On `Save`, the dialog resolves `HostBlockWriter` from the shared container, writes the host block into the registry, and signals `AuthorSelectionView` (TK008) to retry the commit flow. The registry file written must round-trip through `AuthorRegistry` without diff. Implements `IStatusBarProvider`.

Collaborative because the retry contract — what state survives, what rebuilds, what the user sees on retry success or failure — is non-trivial and benefits from a design check before code lands.

## Size

- **Files** - 1 new file

## Recommended Workflow

### Step 1 — Implement the dialog fields and buttons

Where: `src/CoAttribution.Cli/Tui/Dialogs/MissingHostBlockDialog.cs`

- Two `TextField`s: `Name`, `Email`; two buttons: `Save`, `Cancel`.
- Inline email-shape validation; the `Save` button is disabled until validation passes.

Verify: a manually-driven run walks through the dialog on a fresh host config.

### Step 2 — Wire the host-block writer and the round-trip guarantee

Where: `src/CoAttribution.Cli/Tui/Dialogs/MissingHostBlockDialog.cs`

- Resolve the current `HostResolutionResult` and the registry `GitCoAuthorConfig` from the shared `ServiceProvider`.
- Resolve `HostBlockWriter` and call `Write(config, contributorId, hostKey, new HostOverride(name, email))`.
- Confirm the resulting registry file round-trips through `AuthorRegistry` without diff.

Verify: after `Save`, the registry file on disk matches what the CLI's `host add` would have produced (same keys, same order, no cosmetic diffs).

### Step 3 — Hook the retry

Where: `src/CoAttribution.Cli/Tui/Dialogs/MissingHostBlockDialog.cs`

- On successful write, notify `AuthorSelectionView` (TK008) to retry: re-resolve the host, refresh the host row, and the user returns to the selection screen with their picks intact.

Verify: with the dialog dismissed via `Save`, the user lands back on `AuthorSelectionView`; the host row is now populated and pre-toggled; the commit can proceed without leaving the TUI.

### Step 4 — Implement `IStatusBarProvider`

Where: `src/CoAttribution.Cli/Tui/Dialogs/MissingHostBlockDialog.cs`

- Return `Tab next field`, `Enter save`, `Esc cancel`.

Verify: the bar pinned by `StatusBarComposer` (TK005) carries these bindings on this dialog.

## Context pointers

**Files** - `src/CoAttribution.Cli/Tui/Dialogs/MissingHostBlockDialog.cs` (new). The v1 file at `src/CoAttribution.Cli/Components/Dialogs/MissingHostBlockDialog.cs` (and its `MissingHostBlockChoice.cs`) is removed in TK004.
**ADRs** - `docs/adr/0001-native-aot-constraint.md` — `HostBlockWriter` is already AOT-safe.
**Domain terms** - *Host Resolution* — the dialog exists exactly because the resolver returned `NoHostDetected` / `MissingHostBlock`. *Commit Trailer* — the trailer that would have been malformed is now valid because of the new block.
**Ledger records** - `DECISIONS-CoAttribution-tui-mode.md#D011` — guided dialog lets the user type `(name, email)`; writes the host block; re-runs the commit flow without leaving the TUI; registry file round-trips through `AuthorRegistry` without diff. `DECISIONS-CoAttribution-tui-mode.md#T014` — two-field dialog (`Name`, `Email`); `Save` / `Cancel`; calls `HostBlockWriter` and re-runs the commit. `DECISIONS-CoAttribution-tui-mode.md#T013` — implement `IStatusBarProvider`. `DECISIONS-CoAttribution-tui-mode.md#D012` — v1 scaffolding is replaced. `DECISIONS-CoAttribution-tui-mode.md#D014` — services resolved via DI.

## Acceptance criteria

- [ ] `src/CoAttribution.Cli/Tui/Dialogs/MissingHostBlockDialog.cs` exists with `Name` and `Email` fields and `Save` / `Cancel` buttons (DECISIONS-CoAttribution-tui-mode.md#T014).
- [ ] On `Save`, the dialog calls `HostBlockWriter.Write(...)` so the registry file on disk round-trips through `AuthorRegistry` without diff (DECISIONS-CoAttribution-tui-mode.md#D011, DECISIONS-CoAttribution-tui-mode.md#T014).
- [ ] On successful write, `AuthorSelectionView` is notified to retry: the host re-resolves, the host row is populated, and the user is back on the selection screen with picks intact — the TUI never ejects to the CLI (DECISIONS-CoAttribution-tui-mode.md#D011).
- [ ] `MissingHostBlockDialog` implements `IStatusBarProvider` with dialog-relevant keys (DECISIONS-CoAttribution-tui-mode.md#T013).
- [ ] The v1 files `MissingHostBlockDialog.cs` and `MissingHostBlockChoice.cs` stay deleted (TK004) — this ticket does not reintroduce them (DECISIONS-CoAttribution-tui-mode.md#D012).
- [ ] `dotnet build src/CoAttribution.Cli` compiles the new dialog without warnings (DECISIONS-CoAttribution-tui-mode.md#D004).

## Dependencies

**Blocked by** - 005-tui-composition-root-and-status-bar-infrastructure
