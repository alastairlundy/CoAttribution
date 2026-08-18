---
title: QuitDialog and DraftStore (save draft on quit)
classification: Collaborative
blocked_by: [005-tui-composition-root-and-status-bar-infrastructure]
parent: docs/decisions/DECISIONS-CoAttribution-tui-mode.md
---

## Goal

Make accidental quit forgiving: when the user hits Esc / Ctrl+C with an in-progress commit form, offer Save draft / Discard / Cancel, where Save draft persists the form state so the TUI can resume it on the next launch.

## What to build

Add two new files under `src/CoAttribution.Cli/Tui/`:

- `ViewModels/DraftStore.cs` — persists in-progress form state as JSON to `%LOCALAPPDATA%/CoAttribution/drafts/` on Windows or `~/.local/share/CoAttribution/drafts/` on POSIX. Serialization uses a source-generated `JsonSerializerContext` for AOT safety. Exposes `SaveDraftAsync(formState)` and `TryLoadDraftAsync()`. Auto-creates the draft directory on first save.
- `Dialogs/QuitDialog.cs` — Terminal.Gui v2 modal with three buttons: `Save draft`, `Discard`, `Cancel`. On `Save draft` calls `DraftStore.SaveDraftAsync`; on `Discard` closes the window; on `Cancel` returns to the form. Implements `IStatusBarProvider`.

Collaborative because the draft's schema, where it lives on each OS, and the resume path on next launch are interrelated and benefit from a quick design check before code lands.

## Size

- **Files** - 2 new files

## Recommended Workflow

### Step 1 — Implement `DraftStore`

Where: `src/CoAttribution.Cli/Tui/ViewModels/DraftStore.cs`

- Resolve the draft directory per platform: `%LOCALAPPDATA%/CoAttribution/drafts/` on Windows (via `Environment.GetFolderPath(SpecialFolder.LocalApplicationData)`); `~/.local/share/CoAttribution/drafts/` on POSIX (via `Environment.GetFolderPath(SpecialFolder.UserProfile)` + `.local/share/...`).
- Provide `SaveDraftAsync(CommitFormViewModel formState)` and `TryLoadDraftAsync()` that round-trip the in-progress form via a source-generated `JsonSerializerContext`.
- Create the directory on first save if missing.

Verify: a unit test (or inline assertion) round-trips a non-empty form via `SaveDraftAsync` / `TryLoadDraftAsync` and recovers the subject / body / chosen authors.

### Step 2 — Implement `QuitDialog`

Where: `src/CoAttribution.Cli/Tui/Dialogs/QuitDialog.cs`

- Three buttons: `Save draft`, `Discard`, `Cancel`.
- Resolve `DraftStore` from the shared `ServiceProvider`; on `Save draft` call `SaveDraftAsync(formState)`, then close.
- On `Discard` close without saving; on `Cancel` close and signal `MainWindow` to return to the form.

Verify: with an in-progress form, Esc opens `QuitDialog`; `Save draft` writes a draft file under the platform-correct directory and the next launch can `TryLoadDraftAsync` to resume.

### Step 3 — Implement `IStatusBarProvider` on the dialog

Where: `src/CoAttribution.Cli/Tui/Dialogs/QuitDialog.cs`

- Return `Tab next button`, `Enter select`, `Esc cancel`.

Verify: the bar pinned by `StatusBarComposer` (TK005) appears at the bottom of `QuitDialog`.

### Step 4 — Confirm DI registrations

Where: `src/CoAttribution.Cli/Program.cs`

- Confirm `DraftStore` is registered as a singleton in the shared `ServiceProvider` (added in TK002; verify here only).

Verify: `dotnet build src/CoAttribution.Cli` resolves the registration; `MainWindow` (TK006) and `QuitDialog` (this ticket) both find `DraftStore` via DI.

## Context pointers

**Files** - the two new files listed in "What to build" plus `src/CoAttribution.Cli/Program.cs` (DI verification only).
**ADRs** - `docs/adr/0001-native-aot-constraint.md` — JSON via source-generated `JsonSerializerContext` keeps AOT clean.
**Domain terms** - *Commit Trailer* — draft saves enough of the form that the next launch can resume the partial commit.
**Ledger records** - `DECISIONS-CoAttribution-tui-mode.md#D018` — Quit / Cancel semantics: dialog offers Save draft / Discard / Cancel; draft persists across session restart; cleanup rules (age, count) are out of scope. `DECISIONS-CoAttribution-tui-mode.md#T015` — draft JSON under `%LOCALAPPDATA%/CoAttribution/drafts/` (Windows) or `~/.local/share/CoAttribution/drafts/` (POSIX); source-generated `JsonSerializerContext` for AOT safety; auto-create directory on first save. `DECISIONS-CoAttribution-tui-mode.md#T013` — implement `IStatusBarProvider`. `DECISIONS-CoAttribution-tui-mode.md#D014` — services resolved via DI. `DECISIONS-CoAttribution-tui-mode.md#D004` — AOT compatibility must hold.

## Acceptance criteria

- [ ] `src/CoAttribution.Cli/Tui/ViewModels/DraftStore.cs` exposes `SaveDraftAsync(formState)` and `TryLoadDraftAsync()` that round-trip via a source-generated `JsonSerializerContext` (DECISIONS-CoAttribution-tui-mode.md#T015).
- [ ] The draft directory resolves to `%LOCALAPPDATA%/CoAttribution/drafts/` on Windows and `~/.local/share/CoAttribution/drafts/` on POSIX; auto-created on first save (DECISIONS-CoAttribution-tui-mode.md#T015).
- [ ] Drafts survive a session restart — `SaveDraftAsync` followed by process exit and re-launch allows `TryLoadDraftAsync` to recover the same in-progress form (DECISIONS-CoAttribution-tui-mode.md#D018).
- [ ] `src/CoAttribution.Cli/Tui/Dialogs/QuitDialog.cs` exists with `Save draft` / `Discard` / `Cancel` buttons wired to `DraftStore.SaveDraftAsync` (save), form close (discard), and form return (cancel) (DECISIONS-CoAttribution-tui-mode.md#D018, DECISIONS-CoAttribution-tui-mode.md#T015).
- [ ] `QuitDialog` implements `IStatusBarProvider` with dialog-relevant keys (DECISIONS-CoAttribution-tui-mode.md#T013).
- [ ] Draft age- and count-based cleanup rules are not implemented in this ticket — explicitly out of scope per D018.
- [ ] `dotnet build src/CoAttribution.Cli` introduces no AOT analyzer warnings on the new files (DECISIONS-CoAttribution-tui-mode.md#D004).

## Dependencies

**Blocked by** - 005-tui-composition-root-and-status-bar-infrastructure
