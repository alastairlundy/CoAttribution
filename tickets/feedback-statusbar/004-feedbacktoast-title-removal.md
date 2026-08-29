---
title: Add FeedbackToast and stop mutating Window.Title
classification: Independent
blocked_by: []
parent: docs/decisions/DECISIONS-CoAttribution-tui-modernization.md
---

## Goal

Communicate commit success/failure/error via a transient overlay instead of mutating the window title, keeping the title a stable identity string.

## What to build

1. New `src/CoAttribution.Cli/Tui/Views/FeedbackToast.cs` - `public sealed class FeedbackToast : View` with `Show(string message, FeedbackKind kind)` and `Dismiss()`; auto-dismiss via `Application.MainLoop.AddTimeout` (single AoT-safe timer). `Window.Title` is never mutated (T017, T009, D003).
2. Edit `MainWindow.cs` `RunCommitAsync` to remove the `Title` mutation (current lines 297/302/307) and call `FeedbackToast.Show(...)`; own and add the `FeedbackToast` above the active screen (T009, T017, D003).

## Size

- **Files** - 2 (new `FeedbackToast.cs`, edit `MainWindow.cs`)

## Recommended Workflow

### Step 1 - Create FeedbackToast View

Where: `src/CoAttribution.Cli/Tui/Views/FeedbackToast.cs`

- Define `public sealed class FeedbackToast : View` with `Show`/`Dismiss` and a single `MainLoop.AddTimeout` for auto-dismiss; render above content; non-blocking.

Verify: Compiles; NativeAOT/AoT analyzer passes.

### Step 2 - Integrate in MainWindow

Where: `src/CoAttribution.Cli/Tui/Views/MainWindow.cs`

- Remove the three `Title = ...` assignments in `RunCommitAsync`.
- Instantiate and `Add` the `FeedbackToast` above the active screen; call `Show` on success/failure/error.

Verify: `Title` remains the stable identity string across outcomes; `FeedbackToast` appears and auto-dismisses.

## Context pointers

##### Files

- `src/CoAttribution.Cli/Tui/Views/FeedbackToast.cs` - new transient overlay.
- `src/CoAttribution.Cli/Tui/Views/MainWindow.cs` - remove `Title` mutation, host the toast.

##### ADRs

- `docs/adr/0001-native-aot-constraint.md` - AoT-safe timer lifecycle.

##### Ledger records

- `DECISIONS-CoAttribution-tui-modernization.md#T017` - `FeedbackToast : View` with `MainLoop` timeout.
- `DECISIONS-CoAttribution-tui-modernization.md#T009` - transient toast overlay for commit feedback.
- `DECISIONS-CoAttribution-tui-modernization.md#D003` - dedicated feedback surface; do not mutate `Window.Title`.
- `DECISIONS-CoAttribution-tui-modernization.md#T003` - logging stays as-is; banner uses UI state, not the file logger.
- `DECISIONS-CoAttribution-tui-modernization.md#T005` - no new TUI log region (file logger unchanged).

## Acceptance criteria

- [ ] `FeedbackToast : View` renders above content and auto-dismisses via `MainLoop.AddTimeout` (T017)
- [ ] `MainWindow.RunCommitAsync` no longer mutates `Title`; uses `FeedbackToast.Show` (D003, T009)
- [ ] `Window.Title` remains a stable identity string across commit outcomes (D003)
- [ ] No new file log region introduced; `FileLogger` unchanged (T003, T005)
- [ ] Timer lifecycle is AoT-safe (T017, ADR 0001)

## Dependencies

**Blocked by** - None - can start immediately
