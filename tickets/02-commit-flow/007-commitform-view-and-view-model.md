---
title: CommitForm view and view model (subject, body, N/72 counter)
classification: Independent
blocked_by: [005-tui-composition-root-and-status-bar-infrastructure]
parent: docs/decisions/DECISIONS-CoAttribution-tui-mode.md
---

## Goal

Expose the commit-message editor as two clearly-labeled fields — a single-line subject and a multi-line body — with a live `N/72` subject counter that turns warning at 50 and red at 72.

## What to build

Add two new files under `src/CoAttribution.Cli/Tui/`:

- `Views/CommitFormView.cs` — a Terminal.Gui v2 view with a single-line `TextField` for the subject, a multi-line `TextView` for the body, and a right-aligned `N/72` counter label next to the subject field. Implements `IStatusBarProvider` returning `Enter next`, `Tab next field`, `Esc quit`.
- `ViewModels/CommitFormViewModel.cs` — backs the view with `[Subject]`, `[Body]`, `[SubjectLength]`, and `[SubjectColor]` observable properties; `SubjectLength` updates on each keystroke; `SubjectColor` flips normal → warning at 50+ → red at 72+.

## Size

- **Files** - 2 new files

## Recommended Workflow

### Step 1 — Build `CommitFormViewModel` with the counter logic

Where: `src/CoAttribution.Cli/Tui/ViewModels/CommitFormViewModel.cs`

- Declare observable properties for `Subject`, `Body`, `SubjectLength`, `SubjectColor`.
- On `Subject` setter, recompute `SubjectLength = Subject.Length` and pick `SubjectColor` per the T012 thresholds (normal below 50, warning 50–71, red 72+).

Verify: a unit test (or a small inline assertion harness) confirms color flips at the three thresholds.

### Step 2 — Build `CommitFormView` against Terminal.Gui v2

Where: `src/CoAttribution.Cli/Tui/Views/CommitFormView.cs`

- Lay out two fields: subject (single-line) above body (multi-line).
- Bind the right-aligned `N/72` label to `CommitFormViewModel.SubjectLength` and `SubjectColor`.
- Implement `IStatusBarProvider` with the documented bindings.

Verify: manual smoke on a real TTY shows the counter updating live as the subject changes; the color flips at 50 and 72.

### Step 3 — Confirm the view is reachable from DI

Where: `src/CoAttribution.Cli/Program.cs`

- Confirm `CommitFormView` and `CommitFormViewModel` are registered in the shared `ServiceProvider` (added in TK002; complete here only if missing).

Verify: `dotnet build src/CoAttribution.Cli` resolves the registrations.

## Context pointers

**Files** - the two new files listed in "What to build" plus `src/CoAttribution.Cli/Program.cs` (DI verification only).
**ADRs** - `docs/adr/0001-native-aot-constraint.md` — MVVM via DI is AOT-friendly; no reflection in the view ↔ view model binding.
**Domain terms** - *Commit Trailer* — the editor produces the subject / body that the orchestrator will combine with trailers to form the final commit message. *Default Attribution Type* — relevant downstream, not in this ticket.
**Ledger records** - `DECISIONS-CoAttribution-tui-mode.md#D008` — two separate fields, single-line subject and multi-line body. `DECISIONS-CoAttribution-tui-mode.md#D015` — live `N/72` counter. `DECISIONS-CoAttribution-tui-mode.md#T012` — counter label right-aligned; colors normal → warning at 50+ → red at 72+. `DECISIONS-CoAttribution-tui-mode.md#T013` — implement `IStatusBarProvider`. `DECISIONS-CoAttribution-tui-mode.md#D002` — TUI covers the commit flow.

## Acceptance criteria

- [ ] `src/CoAttribution.Cli/Tui/Views/CommitFormView.cs` exists with a single-line `Subject` field and a multi-line `Body` field (DECISIONS-CoAttribution-tui-mode.md#D008).
- [ ] A live `N/72` counter label is rendered right-aligned next to the subject; its color flips normal → warning at 50+ → red at 72+ (DECISIONS-CoAttribution-tui-mode.md#D015, DECISIONS-CoAttribution-tui-mode.md#T012).
- [ ] `src/CoAttribution.Cli/Tui/ViewModels/CommitFormViewModel.cs` exposes observable `[Subject]`, `[Body]`, `[SubjectLength]`, `[SubjectColor]` properties (DECISIONS-CoAttribution-tui-mode.md#T012).
- [ ] `CommitFormView` implements `IStatusBarProvider` and returns `Enter next`, `Tab next field`, `Esc quit` (DECISIONS-CoAttribution-tui-mode.md#T013).
- [ ] `CommitFormView` and `CommitFormViewModel` are resolvable from the shared `ServiceProvider` (DECISIONS-CoAttribution-tui-mode.md#D014).
- [ ] `dotnet build src/CoAttribution.Cli` introduces no warnings on the new files (DECISIONS-CoAttribution-tui-mode.md#D004).

## Dependencies

**Blocked by** - 005-tui-composition-root-and-status-bar-infrastructure
