---
title: Add AuthorSelectionPanelView two-pane + replace CheckBox stack with ListView
classification: Independent
blocked_by: [006-authorlistrow-dto-row-source, 005-statusbar-glyph-text-cells]
parent: docs/decisions/DECISIONS-CoAttribution-tui-modernization.md
---

## Goal

Modernize author selection into a split-panel (left filterable `ListView`, right selected/attribution panel) using a bound `ListView` with glyph checks, while preserving the commit flow, multi-select, filter, and attribution cycling.

## What to build

1. New `src/CoAttribution.Cli/Tui/Views/AuthorSelectionPanelView.cs` - `public sealed class AuthorSelectionPanelView : View` with a left filterable `ListView` `FrameView` (bound to `AuthorListRow`) and a right selected/attribution `FrameView` (T020, T014, D007, T011).
2. Edit `AuthorSelectionView` to replace `RebuildCheckboxes` (the manual `CheckBox` stack) with a `ListView` bound to `AuthorListRow`, selection shown via `GlyphSet.Check`; host the panes inside `AuthorSelectionPanelView`; preserve multi-select, filter, advanced attribution cycling, `Enter`/`Esc` behavior, and the screen flow (T013, T14, T18, T20, D06, D001, I001).

## Size

- **Files** - 2 (new `AuthorSelectionPanelView.cs`, edit `AuthorSelectionView.cs`)
- **Large Edits required** - `AuthorSelectionView.cs` (~317 lines) is heavily reworked when the checkbox stack is replaced.

## Recommended Workflow

### Step 1 - Create AuthorSelectionPanelView

Where: `src/CoAttribution.Cli/Tui/Views/AuthorSelectionPanelView.cs`

- Build a two-pane composite: left `FrameView` with a filterable `ListView` bound to `AuthorListRow`; right `FrameView` showing the selected/attribution summary.
- Manage focus across panes.

Verify: Panes construct; focus moves between panes correctly.

### Step 2 - Migrate AuthorSelectionView

Where: `src/CoAttribution.Cli/Tui/Views/AuthorSelectionView.cs`

- Replace `RebuildCheckboxes` with a `ListView` bound to `AuthorListRow`; render selection via `GlyphSet.Check`.
- Integrate `AuthorSelectionPanelView`; keep `FilterText`, advanced toggle, `AddAuthor`, `Back`, `Progress`, `Enter`/`Esc`, and `HostBlockMissing` behavior.

Verify: Multi-select, filter, and advanced attribution cycling still work; flow unchanged.

## Context pointers

##### Files

- `src/CoAttribution.Cli/Tui/Views/AuthorSelectionPanelView.cs` - new two-pane sub-view.
- `src/CoAttribution.Cli/Tui/Views/AuthorSelectionView.cs` - replace `RebuildCheckboxes`.
- `src/CoAttribution.Cli/Tui/ViewModels/AuthorListRow.cs` - row source (TK006).
- `src/CoAttribution.Cli/Tui/ViewModels/AuthorSelectionViewModel.cs` - existing row/selection logic.

##### ADRs

- `docs/adr/0001-native-aot-constraint.md` - AoT-safe view construction.

##### Ledger records

- `DECISIONS-CoAttribution-tui-modernization.md#T020` - `AuthorSelectionPanelView` two-pane sub-view.
- `DECISIONS-CoAttribution-tui-modernization.md#T014` - two-pane `FrameView` split-panel.
- `DECISIONS-CoAttribution-tui-modernization.md#T013` - `ListView` + glyph selection migration.
- `DECISIONS-CoAttribution-tui-modernization.md#T018` - `AuthorListRow` row source.
- `DECISIONS-CoAttribution-tui-modernization.md#T011` - `FrameView` sectioning.
- `DECISIONS-CoAttribution-tui-modernization.md#D006` - `ListView` + glyph checks replace checkbox stack.
- `DECISIONS-CoAttribution-tui-modernization.md#D007` - split-panel author selection.
- `DECISIONS-CoAttribution-tui-modernization.md#I001` - `D001` permits UX changes while functionality is retained.
- `DECISIONS-CoAttribution-tui-modernization.md#D001` - functionality (flow, keys) retained.

## Acceptance criteria

- [ ] `AuthorSelectionPanelView : View` has a left filterable `ListView` `FrameView` (bound to `AuthorListRow`) and a right selected/attribution `FrameView` (T020, T014)
- [ ] The manual `CheckBox` stack is replaced by a `ListView`; selection shown via `GlyphSet.Check` (T013, D06)
- [ ] Multi-select, filter, advanced attribution cycling, and `Enter`/`Esc` behavior are preserved (D06, D001, I001)
- [ ] The `CommitForm -> AuthorSelection -> Preview` flow is unchanged (D001, I001, D007)
- [ ] Focus management across panes is correct (T14)

## Dependencies

**Blocked by** - 006-authorlistrow-dto-row-source (needs `AuthorListRow`), 005-statusbar-glyph-text-cells (shared `AuthorSelectionView.cs` edit)
