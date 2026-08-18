---
title: MainWindow orchestrates commit flow
classification: Collaborative
blocked_by: [005-tui-composition-root-and-status-bar-infrastructure]
parent: docs/decisions/DECISIONS-CoAttribution-tui-mode.md
---

## Goal

Make the top-level window the conductor of the TUI commit flow: `CommitFormView` → `AuthorSelectionView` → `PreviewModal`, with a pinned status bar and a quit hook that defers to `QuitDialog`.

## What to build

Add `src/CoAttribution.Cli/Tui/Views/MainWindow.cs`. It must:

- Construct `CommitFormView`, `AuthorSelectionView`, and `PreviewModal` from the shared `ServiceProvider`.
- Sequence navigation: form → selection → modal; on `PreviewModal` confirm, run `ICommitOrchestrator.CommitAsync(...)`.
- Implement `IStatusBarProvider` so `StatusBarComposer` pins screen-relevant keys (`Esc quit`, `Enter next`).
- Handle the quit attempt (Esc / Ctrl+C) by routing to `QuitDialog` (TK013) so the user can save a draft before the window closes.

Collaborative because the wiring contract between `CommitFormView` / `AuthorSelectionView` / `PreviewModal` is set here, and small ambiguities in the navigation state machine (e.g. when validation should block progression) may need a design check before code lands.

## Size

- **Files** - 1 new file

## Recommended Workflow

### Step 1 — Build the CommitFormView → AuthorSelectionView → PreviewModal sequence

Where: `src/CoAttribution.Cli/Tui/Views/MainWindow.cs`

- Construct the three screens via DI.
- After `CommitFormView` confirms, transition to `AuthorSelectionView`; after `AuthorSelectionView` confirms, transition to `PreviewModal`.
- On `PreviewModal` confirm, call `ICommitOrchestrator.CommitAsync(...)` and surface success or failure back to the user.

Verify: a manually-driven smoke run on a real TTY steps through all three screens in order on a happy path.

### Step 2 — Implement `IStatusBarProvider` and let `StatusBarComposer` pin the bar

Where: `src/CoAttribution.Cli/Tui/Views/MainWindow.cs`

- Return a list with `Esc quit` and `Enter next` (and any other screen-level keys).

Verify: when this window is shown, a Terminal.Gui v2 `StatusBar` is pinned to the bottom of the viewport (per TK005's composer).

### Step 3 — Wire the quit hook to `QuitDialog`

Where: `src/CoAttribution.Cli/Tui/Views/MainWindow.cs`

- On Esc / Ctrl+C, defer to `QuitDialog` (added in TK013) with the in-progress `CommitFormViewModel` state.
- Pass the form state to `DraftStore.SaveDraftAsync` when the user picks `Save draft`; when the user picks `Discard`, close the window; when the user picks `Cancel`, return to the form.

Verify: Esc from an in-progress form opens `QuitDialog`; `Save draft` writes a draft file under the TK013 directory; `Discard` closes the window.

### Step 4 — Make the public type visible to DI

Where: `src/CoAttribution.Cli/Program.cs`

- Confirm `MainWindow` is registered as a transient in the shared `ServiceProvider` (added in TK002 if not already; this step only verifies completeness).

Verify: `dotnet build src/CoAttribution.Cli` compiles the navigation wiring without warnings.

## Context pointers

**Files** - `src/CoAttribution.Cli/Tui/Views/MainWindow.cs` (new). Depends on `CommitFormView` (TK007), `AuthorSelectionView` (TK008), `PreviewModal` (TK012), and `QuitDialog` / `DraftStore` (TK013).
**ADRs** - `docs/adr/0001-native-aot-constraint.md` — DI-based resolution only.
**Domain terms** - *Commit Trailer* — the orchestrator emits these from the form's author choices. *Host Resolution* — `AuthorSelectionView` re-resolves host; `MainWindow` re-runs the commit on retry after `MissingHostBlockDialog` (TK011).
**Ledger records** - `DECISIONS-CoAttribution-tui-mode.md#D010` — confirm before `git commit` runs. `DECISIONS-CoAttribution-tui-mode.md#T013` — implement `IStatusBarProvider` and pin to the bottom. `DECISIONS-CoAttribution-tui-mode.md#D018` — Esc / Ctrl+C defers to `QuitDialog`. `DECISIONS-CoAttribution-tui-mode.md#T015` — `Save draft` calls `DraftStore.SaveDraftAsync`. `DECISIONS-CoAttribution-tui-mode.md#D014` — services resolved via DI, no subprocesses.

## Acceptance criteria

- [ ] `MainWindow` is created in `src/CoAttribution.Cli/Tui/Views/MainWindow.cs` and resolves `CommitFormView`, `AuthorSelectionView`, `PreviewModal`, and `ICommitOrchestrator` from the shared `ServiceProvider` (DECISIONS-CoAttribution-tui-mode.md#D014, DECISIONS-CoAttribution-tui-mode.md#D010).
- [ ] The screen sequence is `CommitFormView` → `AuthorSelectionView` → `PreviewModal`; `PreviewModal` confirm invokes `ICommitOrchestrator.CommitAsync(...)` (DECISIONS-CoAttribution-tui-mode.md#D010).
- [ ] `MainWindow` implements `IStatusBarProvider` and returns a list with `Esc quit` and `Enter next` (DECISIONS-CoAttribution-tui-mode.md#T013).
- [ ] Esc / Ctrl+C defers to `QuitDialog` with the in-progress `CommitFormViewModel` state passed through (DECISIONS-CoAttribution-tui-mode.md#D018).
- [ ] `MainWindow` is registered as a transient or singleton in the shared `ServiceProvider` (DECISIONS-CoAttribution-tui-mode.md#T004).
- [ ] `dotnet build src/CoAttribution.Cli` compiles the new wiring without warnings (DECISIONS-CoAttribution-tui-mode.md#D004).

## Dependencies

**Blocked by** - 005-tui-composition-root-and-status-bar-infrastructure
