---
title: Add TUnit headless/behavior tests for new presentation types
classification: Independent
blocked_by: [002-glyphset-config-accessor-di, 004-feedbacktoast-title-removal, 006-authorlistrow-dto-row-source, 007-commitformsectionsview-composite, 008-authorselectionpanelview-listview]
parent: docs/decisions/DECISIONS-CoAttribution-tui-modernization.md
---

## Goal

Verify the new presentation types under NativeAOT with headless construction/behavior tests, preferring behavior over visual-only checks.

## What to build

Add TUnit tests in `tests/CoAttribution.Cli.Tests/` for `GlyphSet` (parse from config, no reflection), `FeedbackToast` (Show/Dismiss timer lifecycle headless), `CommitFormSectionsView` (construction, controls wrapped), `AuthorSelectionPanelView` (pane construction, `ListView` binding), and `AuthorListRow` mapping (T004, T16, T17, T18, T19, T20, T002).

## Size

- **Files** - 5 (new test files under `tests/CoAttribution.Cli.Tests/`)

## Recommended Workflow

### Step 1 - Test GlyphSet

Where: `tests/CoAttribution.Cli.Tests/GlyphSetTests.cs`

- Assert `GlyphSet` parses the 7 keys from config and exposes them without runtime reflection.

Verify: Test passes; no reflection path exercised.

### Step 2 - Test FeedbackToast

Where: `tests/CoAttribution.Cli.Tests/FeedbackToastTests.cs`

- Assert `Show` sets the message/kind and `Dismiss` stops the timer (headless `MainLoop` or mock).

Verify: Non-blocking lifecycle verified.

### Step 3 - Test CommitFormSectionsView

Where: `tests/CoAttribution.Cli.Tests/CommitFormSectionsViewTests.cs`

- Assert construction wraps Subject/Body `FrameView`s and inner controls are reachable.

Verify: Controls present.

### Step 4 - Test AuthorSelectionPanelView

Where: `tests/CoAttribution.Cli.Tests/AuthorSelectionPanelViewTests.cs`

- Assert both panes construct and the `ListView` binds to `AuthorListRow`.

Verify: Panes + binding verified.

### Step 5 - Test AuthorListRow mapping

Where: `tests/CoAttribution.Cli.Tests/AuthorListRowTests.cs`

- Assert mapping from `AuthorRow` preserves `Id`/`IsSelected`/`SelectedAttributionType`/`IsHostRow`.

Verify: Mapping correct.

## Context pointers

##### Files

- `tests/CoAttribution.Cli.Tests/*.cs` - new TUnit test files (project already references TUnit).
- The five presentation-type source files produced by TK002/TK004/TK006/TK007/TK008.

##### ADRs

- `docs/adr/0001-native-aot-constraint.md` - prefer trim/AoT-safe tests.

##### Ledger records

- `DECISIONS-CoAttribution-tui-modernization.md#T004` - TUnit test framework.
- `DECISIONS-CoAttribution-tui-modernization.md#T016` - `GlyphSet` parse/no-reflection.
- `DECISIONS-CoAttribution-tui-modernization.md#T017` - `FeedbackToast` timer lifecycle.
- `DECISIONS-CoAttribution-tui-modernization.md#T018` - `AuthorListRow` mapping.
- `DECISIONS-CoAttribution-tui-modernization.md#T019` - `CommitFormSectionsView`.
- `DECISIONS-CoAttribution-tui-modernization.md#T020` - `AuthorSelectionPanelView`.
- `DECISIONS-CoAttribution-tui-modernization.md#T002` - Terminal.Gui v2 view construction.
- `DECISIONS-CoAttribution-tui-modernization.md#T015` - types stay in `Cli.Tui`.

## Acceptance criteria

- [ ] TUnit tests exist for `GlyphSet`, `FeedbackToast`, `CommitFormSectionsView`, `AuthorSelectionPanelView`, and `AuthorListRow` (T004)
- [ ] `GlyphSet` test asserts no runtime reflection and parses from config (T16, T06)
- [ ] `FeedbackToast` test asserts non-blocking `Show`/`Dismiss` timer lifecycle (T17)
- [ ] `AuthorListRow` test asserts mapping from `AuthorRow` preserves selection/attribution/host flags (T18)
- [ ] New presentation types remain in `Cli.Tui`; tests run under trimming/AoT where feasible (T15, T002)

## Dependencies

**Blocked by** - 002-glyphset-config-accessor-di, 004-feedbacktoast-title-removal, 006-authorlistrow-dto-row-source, 007-commitformsectionsview-composite, 008-authorselectionpanelview-listview
