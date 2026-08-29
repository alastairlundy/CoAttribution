---
title: Add AuthorListRow DTO + view-model row source
classification: Independent
blocked_by: []
parent: docs/decisions/DECISIONS-CoAttribution-tui-modernization.md
---

## Goal

Provide a clean, testable row model for the author `ListView` so selection, filter, and attribution semantics are preserved when the manual checkbox stack is replaced.

## What to build

1. New `src/CoAttribution.Cli/Tui/ViewModels/AuthorListRow.cs` - `public sealed class AuthorListRow` with `Id`, `DisplayLabel`, `IsSelected`, `SelectedAttributionType`, `IsHostRow` mapped from the existing `AuthorRow` (T018, T013).
2. Edit `AuthorSelectionViewModel` to expose an `IReadOnlyList<AuthorListRow>` row source for the `ListView`; preserve filter, multi-select, and advanced attribution cycling (T013, T018, D006).

## Size

- **Files** - 2 (new `AuthorListRow.cs`, edit `AuthorSelectionViewModel.cs`)

## Recommended Workflow

### Step 1 - Create AuthorListRow

Where: `src/CoAttribution.Cli/Tui/ViewModels/AuthorListRow.cs`

- Define `public sealed class AuthorListRow` with the 5 fields, mapped from `AuthorRow` (carry the underlying author id and `IsHostRow`).

Verify: Fields present and map cleanly from `AuthorRow`.

### Step 2 - Expose row source

Where: `src/CoAttribution.Cli/Tui/ViewModels/AuthorSelectionViewModel.cs`

- Add `IReadOnlyList<AuthorListRow>` derived from `Rows`, preserving filter, multi-select, and advanced attribution cycling.

Verify: `GetSelectedIds` still groups correctly; behavior unchanged.

## Context pointers

##### Files

- `src/CoAttribution.Cli/Tui/ViewModels/AuthorListRow.cs` - new DTO.
- `src/CoAttribution.Cli/Tui/ViewModels/AuthorSelectionViewModel.cs` - existing `AuthorRow` source and `GetSelectedIds`.

##### ADRs

- `docs/adr/0001-native-aot-constraint.md` - AoT-safe view models.

##### Ledger records

- `DECISIONS-CoAttribution-tui-modernization.md#T018` - `AuthorListRow` DTO mapped from `AuthorRow`.
- `DECISIONS-CoAttribution-tui-modernization.md#T013` - `ListView` + glyph selection migration.
- `DECISIONS-CoAttribution-tui-modernization.md#T015` - presentation types in `Cli.Tui`.
- `DECISIONS-CoAttribution-tui-modernization.md#T004` - TUnit verification.

## Acceptance criteria

- [ ] `AuthorListRow` is a sealed class with `Id`/`DisplayLabel`/`IsSelected`/`SelectedAttributionType`/`IsHostRow` mapped from `AuthorRow` (T018)
- [ ] `AuthorSelectionViewModel` exposes `IReadOnlyList<AuthorListRow>` (T013)
- [ ] Filter, multi-select, and advanced attribution cycling semantics are preserved (D006, T013)
- [ ] New type lives in `Cli.Tui`; Lib stays Terminal.Gui-free (T015)

## Dependencies

**Blocked by** - None - can start immediately
