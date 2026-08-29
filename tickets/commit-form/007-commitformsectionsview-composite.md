---
title: Add CommitFormSectionsView composite (Subject/Body FrameViews)
classification: Independent
blocked_by: [005-statusbar-glyph-text-cells]
parent: docs/decisions/DECISIONS-CoAttribution-tui-modernization.md
---

## Goal

Group commit-form controls into labeled `FrameView`s (Subject, Body) for a modern visual hierarchy without changing any control behavior.

## What to build

1. New `src/CoAttribution.Cli/Tui/Views/CommitFormSectionsView.cs` - `public sealed class CommitFormSectionsView : View` hosting a `FrameView(Subject)` and `FrameView(Body)` that wrap the existing commit-form controls (T019, T011, D008).
2. Edit `CommitFormView` to move the Subject/Body controls into `CommitFormSectionsView`; counters, hard caps, and `Tab` navigation stay unchanged (T19, D008, D001).

## Size

- **Files** - 2 (new `CommitFormSectionsView.cs`, edit `CommitFormView.cs`)

## Recommended Workflow

### Step 1 - Create CommitFormSectionsView

Where: `src/CoAttribution.Cli/Tui/Views/CommitFormSectionsView.cs`

- Host a `FrameView` for Subject and a `FrameView` for Body wrapping the existing controls; expose the inner controls for focus/counter wiring.

Verify: Compiles; behavior unchanged.

### Step 2 - Refactor CommitFormView

Where: `src/CoAttribution.Cli/Tui/Views/CommitFormView.cs`

- Move the subject/body controls into `CommitFormSectionsView`; keep counters, hard caps, and `Tab` navigation.

Verify: Subject/body counters and caps behave as before; `Tab` still cycles fields.

## Context pointers

##### Files

- `src/CoAttribution.Cli/Tui/Views/CommitFormSectionsView.cs` - new composite sub-view.
- `src/CoAttribution.Cli/Tui/Views/CommitFormView.cs` - existing controls to wrap.

##### ADRs

- `docs/adr/0001-native-aot-constraint.md` - AoT-safe view construction.

##### Ledger records

- `DECISIONS-CoAttribution-tui-modernization.md#T019` - `CommitFormSectionsView` composite sub-view.
- `DECISIONS-CoAttribution-tui-modernization.md#T011` - `FrameView` sectioning via composite sub-views.
- `DECISIONS-CoAttribution-tui-modernization.md#D008` - `FrameView` sectioning by role.
- `DECISIONS-CoAttribution-tui-modernization.md#T015` - presentation types in `Cli.Tui`.
- `DECISIONS-CoAttribution-tui-modernization.md#D001` - control behavior unchanged.

## Acceptance criteria

- [ ] `CommitFormSectionsView : View` has `FrameView(Subject)` and `FrameView(Body)` wrapping existing controls (T019, T011)
- [ ] Control behavior (counters, hard caps, `Tab` navigation) is unchanged (D001, D008)
- [ ] Uses the TrueColor schemes from T007 where applicable (T19, T007)
- [ ] New type lives in `Cli.Tui` (T015)

## Dependencies

**Blocked by** - 005-statusbar-glyph-text-cells (shared `CommitFormView.cs` edit)
