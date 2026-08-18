---
title: AuthorSelection view and view model
classification: Collaborative
blocked_by: [005-tui-composition-root-and-status-bar-infrastructure]
parent: docs/decisions/DECISIONS-CoAttribution-tui-mode.md
---

## Goal

Build the multi-select author checklist that is the user-facing hub of the commit flow: every registered author plus the resolved host appears as a toggleable row, with a type-ahead filter, AI/bot visual distinction, a basic/advanced view toggle, and an in-place `Add author` action.

## What to build

Add two new files under `src/CoAttribution.Cli/Tui/`:

- `Views/AuthorSelectionView.cs` — renders the multi-select `CheckBox` list returned by the view model; optional type-ahead filter at the top; "Advanced view" toggle widget near the top; `+ Add author` button that opens `AddAuthorDialog` in-place (TK010). Implements `IStatusBarProvider` returning `Space toggle`, `Enter confirm`, `Esc quit`.
- `ViewModels/AuthorSelectionViewModel.cs` — resolves authors via `IAuthorRegistry`, host via `IHostResolver`, and `GitCoAuthorConfig` per author. Injects a synthetic pre-toggled host row at the top of the list. Calls `CommitOrchestrator.ApplyHostOverride` to compute the displayed `(name, email)` for each row (so what is shown matches what will be committed). Renders AI/bot authors with the T009 icon prefix and falls back to a `[AI]` / `[Bot]` text badge when UTF-8 rendering is unavailable. Subscribes to host-resolution changes to rebuild rows. Switches between the basic view (auto-determine attribution by `ContributorType`) and the advanced view (per-author `Co-author` / `Assisted-by` / `Default` selector) per the `AdvancedViewEnabled` toggle.

Collaborative because this single ticket wires together D006, D009, D011, D013, D016, D019 and T007, T008, T009, T010; small ambiguities (e.g. fallback behavior when host resolution races with `AddAuthorDialog`) benefit from a quick design check before code lands.

## Size

- **Files** - 2 new files

## Recommended Workflow

### Step 1 — Build `AuthorSelectionViewModel` row construction

Where: `src/CoAttribution.Cli/Tui/ViewModels/AuthorSelectionViewModel.cs`

- Resolve authors via `IAuthorRegistry`; resolve host via `IHostResolver`; on `MissingHostBlockException`, surface that to the view (TK011's dialog will catch).
- Build a list of rows: each registered author plus a synthetic host row at the top.
- For every author row, call `CommitOrchestrator.ApplyHostOverride(config, hostKey).Resolved...(name, email)` to compute the displayed identity.
- For host row: the row text reflects the resolved `(name, email)` and is pre-toggled.
- For AI/bot authors: prefix the row text with the T009 icon (default) or `[AI]` / `[Bot]` text badge when UTF-8 is unavailable.
- Re-build rows when the resolved host changes (subscribe to whatever event the host resolver exposes).

Verify: a unit test on the row builder confirms host row order, override text parity with the orchestrator, and AI/bot prefix selection per UTF-8 capability.

### Step 2 — Implement the basic/advanced view toggle in the view model

Where: `src/CoAttribution.Cli/Tui/ViewModels/AuthorSelectionViewModel.cs`

- Expose `AdvancedViewEnabled` (default false).
- In basic view, each row's chosen attribution is auto-determined by `ContributorType` (human → co-author, agent → assisted-by baseline, with stored default rules applied).
- In advanced view, each row exposes a tri-state selector (`Co-author`, `Assisted-by`, `Default`); the stored default is pre-selected.

Verify: with `AdvancedViewEnabled = false`, the basic view picks the default for each row; flipping to `true` reveals the per-row selectors and the stored default is the initial selection.

### Step 3 — Build `AuthorSelectionView` against Terminal.Gui v2

Where: `src/CoAttribution.Cli/Tui/Views/AuthorSelectionView.cs`

- Lay out: type-ahead filter at top, "Advanced view" toggle widget below the filter, `CheckBox` list, `+ Add author` button at the bottom.
- Implement `IStatusBarProvider` with `Space toggle`, `Enter confirm`, `Esc quit`.

Verify: a smoke run shows the type-ahead filter narrows the list live; the toggle switches between basic and advanced; the `+ Add author` button opens `AddAuthorDialog` (TK010) and preserves current picks, filter text, and toggle state across the round-trip.

### Step 4 — Confirm DI registrations

Where: `src/CoAttribution.Cli/Program.cs`

- Confirm `AuthorSelectionView` and `AuthorSelectionViewModel` are registered in the shared `ServiceProvider` (added in TK002; verify here only).

Verify: `dotnet build src/CoAttribution.Cli` resolves both registrations.

## Context pointers

**Files** - the two new files listed in "What to build". Depends on `IAuthorRegistry`, `IHostResolver`, and `CommitOrchestrator.ApplyHostOverride` in `src/CoAttribution.Lib/`. The view references `AddAuthorDialog` (TK010) for the in-place add.
**ADRs** - `docs/adr/0001-native-aot-constraint.md` — re-using the orchestrator's existing ApplyHostOverride method satisfies AOT (no new reflection).
**Domain terms** - *Contributor Classification*, *Host Resolution*, *Default Attribution Type* — the row builder is precisely where these compose into a `Co-authored-by` / `Assisted-by` decision. *Attribution Resolution Priority* — advanced view exposes the explicit priority choice.
**Ledger records** - `DECISIONS-CoAttribution-tui-mode.md#D006` — multi-select checklist with optional filter; AI/bot visual distinction. `DECISIONS-CoAttribution-tui-mode.md#D009` — basic/advanced view toggle; basic is the default landing state. `DECISIONS-CoAttribution-tui-mode.md#D011` — `MissingHostBlockException` surfaces to the screen so the dialog can catch it. `DECISIONS-CoAttribution-tui-mode.md#D013` — `+ Add author` button opens `AddAuthorDialog` in-place; preserve picks, filter, toggle. `DECISIONS-CoAttribution-tui-mode.md#D016` — host row pre-toggled with resolved `(name, email)`; updates when host changes. `DECISIONS-CoAttribution-tui-mode.md#D019` — per-author row text uses host-overridden `(name, email)` when an override exists; matches what `CommitOrchestrator.ApplyHostOverride` commits. `DECISIONS-CoAttribution-tui-mode.md#T007` — view model injects the synthetic host row. `DECISIONS-CoAttribution-tui-mode.md#T008` — display-time override via `CommitOrchestrator.ApplyHostOverride`. `DECISIONS-CoAttribution-tui-mode.md#T009` — icon prefix by default, text badge fallback when UTF-8 unavailable. `DECISIONS-CoAttribution-tui-mode.md#T010` — single "Advanced view" toggle widget near the top. `DECISIONS-CoAttribution-tui-mode.md#T013` — implement `IStatusBarProvider`. `DECISIONS-CoAttribution-tui-mode.md#D014` — services resolved via DI.

## Acceptance criteria

- [ ] `AuthorSelectionView` renders a multi-select `CheckBox` list of every registered author plus a synthetic host row at the top (DECISIONS-CoAttribution-tui-mode.md#D006, DECISIONS-CoAttribution-tui-mode.md#T007, DECISIONS-CoAttribution-tui-mode.md#D016).
- [ ] The host row is pre-toggled; its text shows the resolved `(name, email)`; updates when the resolved host changes mid-session (DECISIONS-CoAttribution-tui-mode.md#D016).
- [ ] Each author row's text uses the host-overridden `(name, email)` computed via `CommitOrchestrator.ApplyHostOverride`; the same identity lands in the eventual commit per TK012 (DECISIONS-CoAttribution-tui-mode.md#D019, DECISIONS-CoAttribution-tui-mode.md#T008).
- [ ] AI/bot authors render with the icon prefix by default and fall back to `[AI]` / `[Bot]` text badge when UTF-8 is unavailable; no replacement characters or `?` ever appear (DECISIONS-CoAttribution-tui-mode.md#D006, DECISIONS-CoAttribution-tui-mode.md#T009).
- [ ] A type-ahead filter at the top narrows the list live as the user types (DECISIONS-CoAttribution-tui-mode.md#D006).
- [ ] An "Advanced view" toggle widget near the top switches between the basic view (auto-determine by `ContributorType`) and the advanced view (per-row tri-state); basic is the default landing state on every entry (DECISIONS-CoAttribution-tui-mode.md#D009, DECISIONS-CoAttribution-tui-mode.md#T010).
- [ ] A `+ Add author` button opens `AddAuthorDialog` in-place and preserves current picks, filter text, and toggle state across the round-trip (DECISIONS-CoAttribution-tui-mode.md#D013).
- [ ] `MissingHostBlockException` from `IHostResolver` surfaces to the screen without crashing; TK011's `MissingHostBlockDialog` catches it (DECISIONS-CoAttribution-tui-mode.md#D011).
- [ ] `AuthorSelectionView` implements `IStatusBarProvider` returning `Space toggle`, `Enter confirm`, `Esc quit` (DECISIONS-CoAttribution-tui-mode.md#T013).
- [ ] `dotnet build src/CoAttribution.Cli` introduces no warnings on the new files (DECISIONS-CoAttribution-tui-mode.md#D004).

## Dependencies

**Blocked by** - 005-tui-composition-root-and-status-bar-infrastructure
