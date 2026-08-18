---
title: PreviewModal (commit preview and confirm)
classification: Collaborative
blocked_by: [005-tui-composition-root-and-status-bar-infrastructure]
parent: docs/decisions/DECISIONS-CoAttribution-tui-mode.md
---

## Goal

Show the user exactly what `git commit` will receive — composed subject, body, and trailers — before the commit lands, and gate the actual `git commit` call behind an explicit confirm.

## What to build

Add `src/CoAttribution.Cli/Tui/Dialogs/PreviewModal.cs`. It must be a Terminal.Gui v2 modal dialog that:

- Shows the composed subject, multi-line body, and the rendered trailer list using the same host-overridden `(name, email)` identities computed by `AuthorSelectionViewModel` (so what the user sees is what lands in the commit).
- Has `Confirm` and `Cancel` buttons. `Confirm` invokes `ICommitOrchestrator.CommitAsync(...)`; `Cancel` aborts without calling `git commit`.
- Scrolls long trailer lists within the dialog.
- Implements `IStatusBarProvider` returning `Enter confirm`, `Esc cancel`.

Collaborative because the preview's identity text must be derived from the same display-time override path as TK008 (`CommitOrchestrator.ApplyHostOverride`); any divergence between the two is a silent parity bug that this ticket must explicitly close.

## Size

- **Files** - 1 new file

## Recommended Workflow

### Step 1 — Render the preview content with identity parity

Where: `src/CoAttribution.Cli/Tui/Dialogs/PreviewModal.cs`

- Take `AuthorSelectionViewModel` (or a snapshot of its committed row state) and the `CommitFormViewModel` via constructor.
- Compose the preview text: subject line, blank line, body, blank line, trailer lines (one per chosen author / host row).
- Every trailer's `(name, email)` is computed via `CommitOrchestrator.ApplyHostOverride`, mirroring what TK008 renders in the checklist.

Verify: a manually-driven run shows the preview text byte-identical to what `git commit` will receive (subject, body, trailer lines with the host-overridden identities).

### Step 2 — Implement the Confirm / Cancel gate

Where: `src/CoAttribution.Cli/Tui/Dialogs/PreviewModal.cs`

- `Confirm`: resolve `ICommitOrchestrator` from the shared `ServiceProvider` and call `CommitAsync(...)`; close the dialog and propagate success / failure to `MainWindow`.
- `Cancel`: close the dialog without calling the orchestrator.

Verify: confirm triggers `git commit`; cancel does not (no staged state changes, no orchestrator invocation).

### Step 3 — Ensure long trailer lists scroll within the modal

Where: `src/CoAttribution.Cli/Tui/Dialogs/PreviewModal.cs`

- Wrap the preview text in a Terminal.Gui v2 scrollable `TextView`; the dialog must remain modal.

Verify: a deliberately long author set (e.g. 20+ rows) scrolls within the dialog without growing it taller than the viewport.

### Step 4 — Implement `IStatusBarProvider`

Where: `src/CoAttribution.Cli/Tui/Dialogs/PreviewModal.cs`

- Return `Enter confirm`, `Esc cancel`.

Verify: the status bar pinned by `StatusBarComposer` (TK005) appears at the bottom of the modal.

## Context pointers

**Files** - `src/CoAttribution.Cli/Tui/Dialogs/PreviewModal.cs` (new). Depends on `CommitFormViewModel` (TK007), `AuthorSelectionViewModel` (TK008), and `ICommitOrchestrator` from the Lib.
**ADRs** - `docs/adr/0001-native-aot-constraint.md` — `ICommitOrchestrator.CommitAsync` is the existing AOT-safe entry point.
**Domain terms** - *Commit Trailer*, *Attribution Resolution Priority*, *Default Attribution Type* — these are exactly what the preview's trailer lines render and what the orchestrator then emits.
**Ledger records** - `DECISIONS-CoAttribution-tui-mode.md#D010` — confirm before `git commit` runs. `DECISIONS-CoAttribution-tui-mode.md#T011` — modal dialog blocks the form; `Confirm` / `Cancel`; long trailer lists scroll within the dialog. `DECISIONS-CoAttribution-tui-mode.md#T008` — preview's identity text uses `CommitOrchestrator.ApplyHostOverride` so it matches the selection-screen display. `DECISIONS-CoAttribution-tui-mode.md#T013` — implement `IStatusBarProvider`. `DECISIONS-CoAttribution-tui-mode.md#D014` — orchestrator resolved via DI, not subprocess.

## Acceptance criteria

- [ ] `src/CoAttribution.Cli/Tui/Dialogs/PreviewModal.cs` exists and shows the composed subject, multi-line body, and the rendered trailer list with the host-overridden `(name, email)` for every chosen row (DECISIONS-CoAttribution-tui-mode.md#D010, DECISIONS-CoAttribution-tui-mode.md#T008).
- [ ] Each trailer's text is byte-identical to what `ICommitOrchestrator.CommitAsync(...)` will produce — single source of truth is `CommitOrchestrator.ApplyHostOverride` (DECISIONS-CoAttribution-tui-mode.md#T008, DECISIONS-CoAttribution-tui-mode.md#D019).
- [ ] `Confirm` invokes `ICommitOrchestrator.CommitAsync(...)`; `Cancel` closes without calling the orchestrator (DECISIONS-CoAttribution-tui-mode.md#D010, DECISIONS-CoAttribution-tui-mode.md#T011).
- [ ] Long trailer lists (≥ 20 rows) scroll within the dialog without overflowing the viewport; the dialog remains modal (DECISIONS-CoAttribution-tui-mode.md#T011).
- [ ] `PreviewModal` implements `IStatusBarProvider` and returns `Enter confirm`, `Esc cancel` (DECISIONS-CoAttribution-tui-mode.md#T013).
- [ ] `dotnet build src/CoAttribution.Cli` compiles the new dialog without warnings (DECISIONS-CoAttribution-tui-mode.md#D004).

## Dependencies

**Blocked by** - 005-tui-composition-root-and-status-bar-infrastructure
